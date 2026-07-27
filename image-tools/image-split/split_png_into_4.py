import argparse
import logging
from pathlib import Path

from PIL import Image


def setup_logger(verbose: bool = False) -> None:
    """Configure logging format and log level."""
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format="%(asctime)s | %(levelname)s | %(message)s",
        datefmt="%H:%M:%S",
    )


def split_png_into_four(
    input_file: Path, output_dir: Path, stretch_to_original_size: bool = False
) -> list[Path]:
    """
    Split one PNG image into 4 quadrants by center horizontal + vertical cut.
    Returns a list of paths for all generated split images.

    If stretch_to_original_size = True:
        Every cropped quadrant will be stretched/scaled to match the full width and height
        of the original input image, no transparent white/empty background padding.
        LANCZOS high-quality resampling is used to preserve image detail during scaling.
    If stretch_to_original_size = False:
        Output images retain their native cropped quadrant size.

    Warning: Odd width/height will create quadrants with slightly different native pixel sizes;
    stretching these unequal slices to identical full dimensions will introduce minor distortion.
    """
    if not input_file.exists():
        raise FileNotFoundError(f"Input file does not exist: {input_file}")

    if input_file.suffix.lower() != ".png":
        raise ValueError("Input file must be a PNG image (.png)")

    logging.info("Output save directory: %s", output_dir)
    logging.info("Stretch each quadrant to full original image size: %s", stretch_to_original_size)
    output_dir.mkdir(parents=True, exist_ok=True)

    logging.info("Loading source image: %s", input_file)
    with Image.open(input_file) as img:
        orig_width, orig_height = img.size
        logging.info("Original source image dimension: width=%d, height=%d", orig_width, orig_height)

        # Calculate center split coordinate for horizontal & vertical cut
        center_x = orig_width // 2
        center_y = orig_height // 2

        if orig_width % 2 != 0 or orig_height % 2 != 0:
            logging.warning(
                "Source image has odd width or height. Four quadrants will have unequal native pixel sizes. "
                "Stretching to full original size will create slight aspect distortion on split outputs."
            )

        # Crop region format: (left, upper, right, lower)
        crop_regions = {
            "top_left": (0, 0, center_x, center_y),
            "top_right": (center_x, 0, orig_width, center_y),
            "bottom_left": (0, center_y, center_x, orig_height),
            "bottom_right": (center_x, center_y, orig_width, orig_height),
        }

        output_file_list: list[Path] = []
        file_stem = input_file.stem
        # High quality scaling filter for down/up resizing to preserve texture detail
        high_quality_resample = Image.Resampling.LANCZOS

        for quadrant_name, crop_box in crop_regions.items():
            logging.debug("Extracting quadrant [%s] with crop boundary %s", quadrant_name, crop_box)
            cropped_slice = img.crop(crop_box)

            if stretch_to_original_size:
                # Stretch small cropped quadrant to fill full original image size
                final_output_img = cropped_slice.resize((orig_width, orig_height), resample=high_quality_resample)
            else:
                # Keep native cropped quadrant pixel dimensions
                final_output_img = cropped_slice

            save_path = output_dir / f"{file_stem}_{quadrant_name}.png"
            # Save with default PNG lossless compression to retain quality
            final_output_img.save(save_path, format="PNG")
            output_file_list.append(save_path)
            logging.info("Successfully exported split image: %s", save_path)

    logging.info("Image split task finished. Total generated files: %d", len(output_file_list))
    return output_file_list


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Split single PNG into four center-cut quadrants; optionally stretch each slice to full original resolution."
    )
    parser.add_argument("input", type=Path, help="File path of source PNG image")
    parser.add_argument(
        "-o",
        "--output-dir",
        type=Path,
        default=None,
        help="Target folder to store split images (defaults to [input folder]/output)",
    )
    parser.add_argument(
        "-v",
        "--verbose",
        action="store_true",
        help="Enable verbose debug logging",
    )
    parser.add_argument(
        "--stretch-full-size",
        action="store_true",
        help="Stretch every split quadrant to match the original image full width and height (high quality scaling)",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    setup_logger(args.verbose)

    # Auto assign output folder if user does not manually specify --output-dir
    target_output_dir = args.output_dir or (args.input.parent / "output")

    try:
        split_png_into_four(args.input, target_output_dir, args.stretch_full_size)
    except Exception as exc:
        logging.error("Image split operation failed with error: %s", exc)
        raise SystemExit(1) from exc


if __name__ == "__main__":
    main()