"""Batch convert DICOM files to JPG."""

from pathlib import Path

import numpy as np
import pydicom
from PIL import Image
from pydicom.errors import InvalidDicomError


def is_dicom_file(file_path: Path) -> bool:
    """Return True if file is a readable DICOM file."""
    try:
        pydicom.dcmread(str(file_path), stop_before_pixels=True, force=False)
        return True
    except (InvalidDicomError, FileNotFoundError, PermissionError, IsADirectoryError):
        return False
    except Exception:
        return False


def dicom_to_jpg(dcm_path: Path, output_path: Path) -> None:
    """Convert one DICOM file to one JPG image."""
    ds = pydicom.dcmread(str(dcm_path))
    pixel_array = ds.pixel_array.astype(np.float32)

    # 将像素归一化到 0-255，避免图像发黑或发白
    pixel_array -= np.min(pixel_array)
    max_value = np.max(pixel_array)
    if max_value > 0:
        pixel_array /= max_value
    pixel_array = (pixel_array * 255).astype(np.uint8)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(pixel_array).save(str(output_path), format="JPEG")


def convert_folder(src_dir: Path, dst_dir: Path) -> None:
    """Recursively scan src_dir and convert all DICOM files to JPG."""
    total_files = 0
    converted = 0
    skipped = 0

    for file_path in src_dir.rglob("*"):
        if not file_path.is_file():
            continue

        total_files += 1
        if not is_dicom_file(file_path):
            skipped += 1
            continue

        rel_path = file_path.relative_to(src_dir)
        out_path = (dst_dir / rel_path).with_suffix(".jpg")

        try:
            dicom_to_jpg(file_path, out_path)
            converted += 1
            print(f"[OK] {file_path} -> {out_path}")
        except Exception as exc:
            skipped += 1
            print(f"[SKIP] {file_path} ({exc})")

    print("\nDone")
    print(f"Total files: {total_files}")
    print(f"Converted : {converted}")
    print(f"Skipped   : {skipped}")


if __name__ == "__main__":
    # 要处理的两个源目录
    source_dirs = [
        Path("无伪影的dicom文件"),
        Path("有伪影的dicom文件"),
    ]
    # 总输出目录
    output_root = Path("converted_jpg")

    for source_dir in source_dirs:
        if not source_dir.exists():
            print(f"[SKIP] Source folder not found: {source_dir}")
            continue

        # 每个源目录写入独立子目录，避免重名冲突
        output_dir = output_root / source_dir.name
        print(f"\n=== Converting: {source_dir} -> {output_dir} ===")
        convert_folder(source_dir, output_dir)
