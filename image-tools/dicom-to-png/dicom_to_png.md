# DICOM to PNG Converter

This project provides a beginner-friendly Python script to convert DICOM file(s) to PNG.

Script file: `dicom_to_png.py`

## 1. Requirements

- Python 3.9+ (recommended)
- `pydicom`
- `Pillow`
- `numpy`

## 2. Installation

Open terminal in this folder and run:

```bash
pip install pydicom pillow numpy
```

Note: Some compressed DICOM files need extra decoders.
If you get a pixel decoding error, install:

```bash
pip install pylibjpeg pylibjpeg-libjpeg
```

## 3. Basic Usage (Single File)

```bash
python dicom_to_png.py <input_dicom_file>
```

Example:

```bash
python dicom_to_png.py D:\cuifawei-d\workspace\2026\842768-freddie-widescreen\calibration-image-layout\tools\input\TG18(质控测试图)
```

Default output directory:

```text
<input_folder>/output
```

Default output file name:

```text
<input_stem>.png
```

## 4. Folder Mode (Recursive)

You can pass a folder path as input.
The script will scan this folder and all subfolders, convert all DICOM files,
and keep the same relative folder structure in the output.

```bash
python dicom_to_png.py D:\data\dicom_root
```

Default output root in folder mode:

```text
<input_folder>/output
```

Example:

- Input file: `D:\data\dicom_root\studyA\series1\img001.dcm`
- Output file: `D:\data\dicom_root\output\studyA\series1\img001.png`

## 5. Common Options

Set custom output directory:

```bash
python dicom_to_png.py D:\data\sample.dcm -o D:\data\png_output
```

Set custom output file name:

```bash
python dicom_to_png.py D:\data\sample.dcm --output-name sample_converted.png
```

Choose grayscale output bit depth (8 or 16):

```bash
python dicom_to_png.py D:\data\sample.dcm --bit-depth 16
```

Convert a specific frame from a multi-frame DICOM:

```bash
python dicom_to_png.py D:\data\cine.dcm --frame-index 5
```

Disable VOI LUT/windowing:

```bash
python dicom_to_png.py D:\data\sample.dcm --no-voi-lut
```

Enable verbose logs:

```bash
python dicom_to_png.py D:\data\sample.dcm -v
```

Folder mode with custom output root:

```bash
python dicom_to_png.py D:\data\dicom_root -o D:\data\png_root
```

## 6. What the Script Handles

- Reads DICOM with `pydicom`.
- Supports single file and recursive folder conversion.
- Supports single-frame and multi-frame input (select frame by index).
- Applies modality LUT and optional VOI LUT for grayscale images.
- Handles `MONOCHROME1` inversion for display-friendly PNG output.
- Saves grayscale PNG in 8-bit or 16-bit (default 16-bit for better intensity detail).

## 7. Troubleshooting

`ModuleNotFoundError`

Install dependencies:

```bash
pip install pydicom pillow numpy
```

`Input file does not exist`

- Check your file path.
- Use an absolute path.

`Unable to decode pixel data`

Install compressed transfer syntax decoders:

```bash
pip install pylibjpeg pylibjpeg-libjpeg
```
