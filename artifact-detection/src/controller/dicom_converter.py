"""DICOM to bitmap conversion helpers for the artifact pipeline."""

from __future__ import annotations

import logging
from pathlib import Path

import numpy as np
import pydicom
from PIL import Image

LOGGER = logging.getLogger(__name__)

def convert_dicom_to_bitmap(source_path: Path, output_path: Path) -> Path:
    ds = pydicom.dcmread(str(source_path))
    try:
        pixel_array = ds.pixel_array.astype(np.float32)

        pixel_array -= np.min(pixel_array)
        max_value = np.max(pixel_array)
        if max_value > 0:
            pixel_array /= max_value
        pixel_array = (pixel_array * 255).astype(np.uint8)
    except Exception as exc:
        LOGGER.exception("Failed to convert DICOM to bitmap: source=%s: %s", source_path, str(exc))
        raise

    output_path.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(pixel_array).save(str(output_path), format="PNG")
    return output_path
