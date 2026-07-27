import argparse
import logging
from pathlib import Path

import numpy as np
import pydicom
from PIL import Image
from pydicom.pixel_data_handlers.util import apply_modality_lut, apply_voi_lut


DICOM_SUFFIXES = {".dcm", ".dicom"}


def setup_logger(verbose: bool = False) -> None:
    """Configure logging format and level."""
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format="%(asctime)s | %(levelname)s | %(message)s",
        datefmt="%H:%M:%S",
    )


def normalize_array(arr: np.ndarray, bit_depth: int) -> np.ndarray:
    """Normalize array values to uint8 or uint16 range."""
    arr = np.asarray(arr, dtype=np.float32)
    min_val = float(np.min(arr))
    max_val = float(np.max(arr))

    logging.debug("Pixel range before normalize: min=%s, max=%s", min_val, max_val)

    if max_val == min_val:
        logging.warning("Input image has flat intensity values; output will be black.")
        if bit_depth == 8:
            return np.zeros(arr.shape, dtype=np.uint8)
        return np.zeros(arr.shape, dtype=np.uint16)

    scaled = (arr - min_val) / (max_val - min_val)

    if bit_depth == 8:
        return np.round(scaled * 255.0).astype(np.uint8)
    return np.round(scaled * 65535.0).astype(np.uint16)


def pick_frame(pixel_array: np.ndarray, frame_index: int, samples_per_pixel: int) -> np.ndarray:
    """Pick one frame from 2D/3D/4D DICOM pixel arrays."""
    if pixel_array.ndim == 2:
        return pixel_array

    if pixel_array.ndim == 3:
        # If SamplesPerPixel > 1 and last channel is RGB/RGBA, this is likely a color image.
        if samples_per_pixel > 1 and pixel_array.shape[-1] in (3, 4):
            return pixel_array
        if frame_index >= pixel_array.shape[0]:
            raise IndexError(
                f"frame_index {frame_index} is out of range. Total frames: {pixel_array.shape[0]}"
            )
        return pixel_array[frame_index]

    if pixel_array.ndim == 4:
        if frame_index >= pixel_array.shape[0]:
            raise IndexError(
                f"frame_index {frame_index} is out of range. Total frames: {pixel_array.shape[0]}"
            )
        return pixel_array[frame_index]

    raise ValueError(f"Unsupported pixel array dimensions: {pixel_array.ndim}")


def save_png(image_array: np.ndarray, out_file: Path, bit_depth: int) -> None:
    """Save processed numpy array to PNG with proper PIL mode."""
    if image_array.ndim == 2:
        if bit_depth == 16:
            img = Image.fromarray(image_array, mode="I;16")
        else:
            img = Image.fromarray(image_array, mode="L")
    elif image_array.ndim == 3:
        # Pillow RGB/RGBA PNG is typically 8-bit per channel in common workflows.
        # Convert to uint8 for broad compatibility.
        if image_array.dtype != np.uint8:
            logging.warning(
                "Color image will be saved as 8-bit RGB/RGBA for compatibility."
            )
            image_array = normalize_array(image_array, bit_depth=8)

        channels = image_array.shape[-1]
        if channels == 3:
            img = Image.fromarray(image_array, mode="RGB")
        elif channels == 4:
            img = Image.fromarray(image_array, mode="RGBA")
        else:
            raise ValueError(f"Unsupported color channels: {channels}")
    else:
        raise ValueError("Unsupported image shape for saving PNG.")

    img.save(out_file, format="PNG", optimize=True)


def convert_dicom_to_png(
    input_file: Path,
    output_dir: Path,
    bit_depth: int = 16,
    frame_index: int = 0,
    use_voi_lut: bool = True,
    output_name: str | None = None,
) -> Path:
    """Convert one DICOM file to one PNG image."""
    if not input_file.exists():
        raise FileNotFoundError(f"Input file does not exist: {input_file}")

    output_dir.mkdir(parents=True, exist_ok=True)
    out_name = output_name or f"{input_file.stem}.png"
    out_file = output_dir / out_name

    logging.info("Reading DICOM: %s", input_file)
    ds = pydicom.dcmread(input_file)

    samples_per_pixel = int(ds.get("SamplesPerPixel", 1))
    photometric = str(ds.get("PhotometricInterpretation", "")).upper()
    number_of_frames = int(ds.get("NumberOfFrames", 1))

    logging.info(
        "DICOM tags: SamplesPerPixel=%d, PhotometricInterpretation=%s, NumberOfFrames=%d",
        samples_per_pixel,
        photometric or "N/A",
        number_of_frames,
    )

    pixel_array = ds.pixel_array
    logging.debug("Raw pixel array shape=%s, dtype=%s", pixel_array.shape, pixel_array.dtype)

    selected = pick_frame(pixel_array, frame_index, samples_per_pixel)
    logging.info("Selected frame index: %d", frame_index)

    if selected.ndim == 2:
        # Apply DICOM intensity transforms for grayscale images.
        selected = apply_modality_lut(selected, ds)
        if use_voi_lut:
            selected = apply_voi_lut(selected, ds)

        if photometric == "MONOCHROME1":
            # MONOCHROME1 means darker values are brighter on display, so invert for PNG.
            selected = np.max(selected) - selected

        selected = normalize_array(selected, bit_depth=bit_depth)
    elif selected.ndim == 3:
        # For color images, keep native color channels.
        if selected.dtype != np.uint8:
            selected = normalize_array(selected, bit_depth=8)
    else:
        raise ValueError("Unsupported frame shape after selection.")

    save_png(selected, out_file, bit_depth=bit_depth)
    logging.info("Saved PNG: %s", out_file)
    return out_file


def convert_dicom_directory_to_png(
    input_dir: Path,
    output_root: Path,
    bit_depth: int = 16,
    frame_index: int = 0,
    use_voi_lut: bool = True,
) -> tuple[int, int]:
    """
    Recursively convert DICOM files under input_dir.

    Output keeps the same relative folder structure under output_root.
    Returns (success_count, failed_count).
    """
    if not input_dir.exists() or not input_dir.is_dir():
        raise NotADirectoryError(f"Input directory does not exist: {input_dir}")

    dicom_files = [
        p for p in input_dir.rglob("*") if p.is_file() and p.suffix.lower() in DICOM_SUFFIXES
    ]

    if not dicom_files:
        raise FileNotFoundError(
            f"No DICOM files found in directory: {input_dir}. "
            f"Supported suffixes: {sorted(DICOM_SUFFIXES)}"
        )

    logging.info("Found %d DICOM files in: %s", len(dicom_files), input_dir)
    logging.info("Batch output root: %s", output_root)

    success_count = 0
    failed_count = 0

    for dicom_file in dicom_files:
        relative_parent = dicom_file.parent.relative_to(input_dir)
        target_dir = output_root / relative_parent

        try:
            convert_dicom_to_png(
                input_file=dicom_file,
                output_dir=target_dir,
                bit_depth=bit_depth,
                frame_index=frame_index,
                use_voi_lut=use_voi_lut,
                output_name=None,
            )
            success_count += 1
        except Exception as exc:
            failed_count += 1
            logging.error("Failed file: %s | Error: %s", dicom_file, exc)

    logging.info(
        "Batch conversion finished. Success=%d, Failed=%d, Total=%d",
        success_count,
        failed_count,
        len(dicom_files),
    )
    return success_count, failed_count


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Convert DICOM image(s) to PNG.")
    parser.add_argument(
        "input",
        type=Path,
        help="Path to input DICOM file, or a folder to convert recursively",
    )
    parser.add_argument(
        "-o",
        "--output-dir",
        type=Path,
        default=None,
        help="Directory to save PNG (default: <input_folder>/output)",
    )
    parser.add_argument(
        "--output-name",
        type=str,
        default=None,
        help="Custom PNG file name (default: <input_stem>.png)",
    )
    parser.add_argument(
        "--bit-depth",
        type=int,
        choices=[8, 16],
        default=16,
        help="Bit depth for grayscale output PNG (8 or 16, default: 16)",
    )
    parser.add_argument(
        "--frame-index",
        type=int,
        default=0,
        help="Frame index for multi-frame DICOM (default: 0)",
    )
    parser.add_argument(
        "--no-voi-lut",
        action="store_true",
        help="Disable VOI LUT/windowing",
    )
    parser.add_argument(
        "-v",
        "--verbose",
        action="store_true",
        help="Enable debug logging",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    setup_logger(args.verbose)

    output_dir = args.output_dir or (args.input.parent / "output")

    try:
        if args.input.is_dir():
            if args.output_name is not None:
                logging.warning(
                    "--output-name is ignored in directory mode because each file keeps its own name."
                )

            # For directory mode, default output is <input_dir>/output.
            if args.output_dir is None:
                output_dir = args.input / "output"

            convert_dicom_directory_to_png(
                input_dir=args.input,
                output_root=output_dir,
                bit_depth=args.bit_depth,
                frame_index=args.frame_index,
                use_voi_lut=not args.no_voi_lut,
            )
        else:
            convert_dicom_to_png(
                input_file=args.input,
                output_dir=output_dir,
                bit_depth=args.bit_depth,
                frame_index=args.frame_index,
                use_voi_lut=not args.no_voi_lut,
                output_name=args.output_name,
            )
    except Exception as exc:
        logging.error("Failed to convert DICOM to PNG: %s", exc)
        raise SystemExit(1) from exc


if __name__ == "__main__":
    main()
