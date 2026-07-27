"""Batch convert DICOM files to PNG."""

from pathlib import Path
import sys

import pydicom
from pydicom.errors import InvalidDicomError


CURRENT_DIR = Path(__file__).resolve().parent
SRC_DIR = CURRENT_DIR.parent
if str(SRC_DIR) not in sys.path:
    sys.path.insert(0, str(SRC_DIR))

from controller.dicom_converter import convert_dicom_to_bitmap


def is_dicom_file(file_path: Path) -> bool:
    """Return True if file is a readable DICOM file."""
    try:
        pydicom.dcmread(str(file_path), stop_before_pixels=True, force=False)
        return True
    except (InvalidDicomError, FileNotFoundError, PermissionError, IsADirectoryError):
        return False
    except Exception:
        return False


def dicom_to_png(dcm_path: Path, output_path: Path) -> None:
    """Convert one DICOM file to one PNG image."""
    convert_dicom_to_bitmap(dcm_path, output_path)


def convert_folder(src_dir: Path, dst_dir: Path) -> None:
    """Recursively scan src_dir and convert all DICOM files to PNG."""
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
        out_path = (dst_dir / rel_path).with_suffix(".png")

        try:
            dicom_to_png(file_path, out_path)
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
    source_dirs = [
        Path("无伪影的dicom文件"),
        Path("有伪影的dicom文件"),
    ]
    output_root = Path("converted_png")

    for source_dir in source_dirs:
        if not source_dir.exists():
            print(f"[SKIP] Source folder not found: {source_dir}")
            continue

        output_dir = output_root / source_dir.name
        print(f"\n=== Converting: {source_dir} -> {output_dir} ===")
        convert_folder(source_dir, output_dir)
