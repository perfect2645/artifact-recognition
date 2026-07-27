# PNG Splitter \(Split into 4 Parts\)

This project provides a lightweight Python script to split a single PNG image into 4 equal quadrants via one horizontal and one vertical center cut\. The script supports **high\-quality stretching** to scale each split piece to the original image size \(no blank/transparent background padding\) while preserving maximum image quality\.

Script file: `split_png_into_4.py`

## 1\. Requirements

- Python 3\.9\+ \(recommended\)

- Pillow library \(for image processing\)

## 2\. Installation

Open your terminal in the project folder and install the dependency:

```bash
pip install pillow
```

## 3\. Basic Usage

Run the script with your target PNG image path:

```bash
python split_png_into_4.py <input_png>
```

Simple example:

```bash
python split_png_into_4.py input.png
```

### Default Output Directory

All split images are automatically saved to the`output` folder inside the input file's parent directory:

`<input_folder>/output`

Absolute path example:

```bash
python split_png_into_4.py D:\cuifawei-d\workspace\2026\842768-freddie-widescreen\calibration-image-layout\tools\input\TG-18_PHLIPS.png
```

Corresponding output directory:

```Plain Text
D:\cuifawei-d\workspace\2026\842768-freddie-widescreen\calibration-image-layout\tools\input\output
```

## 4\. Output File Naming Rule

For an input file named `input.png`, the script generates 4 split images with fixed suffixes:

- `input_top_left.png`

- `input_top_right.png`

- `input_bottom_left.png`

- `input_bottom_right.png`

## 5\. Optional Arguments

### Custom Output Directory

Specify a custom folder to save split images with `-o/--output-dir`:

```bash
python split_png_into_4.py input.png -o output_images
```

### Enable Verbose Debug Logs

View detailed running logs with `-v/--verbose`:

```bash
python split_png_into_4.py input.png -v
```

### Stretch Split Pieces to Original Image Size \(Core Feature\)

**New optimized feature**: Scale each cropped quadrant to the full original image resolution\. No transparent/white blank background, the split image fills the entire canvas\. Uses LANCZOS high\-quality resampling to minimize quality loss\.

Command usage:

```bash
python split_png_into_4.py input.png --stretch-full-size
```

Absolute path example:

```bash
python split_png_into_4.py D:\cuifawei-d\workspace\2026\842768-freddie-widescreen\calibration-image-layout\tools\input\TG-18_PHLIPS.png --stretch-full-size
```

### Combine Multiple Arguments

Custom output folder \+ full\-size stretching:

```bash
python split_png_into_4.py input.png -o output_images --stretch-full-size
```

Full debug logs \+ custom output \+ stretching:

```bash
python split_png_into_4.py input.png -o output_images -v --stretch-full-size
```

## 6\. Key Notes

- Only `.png` format input images are supported\.

- If the original image has an odd width/height, the 4 split quadrants will have slightly different native pixel sizes \(1\-pixel difference at most\)\.

- When using `--stretch-full-size`, uneven original quadrants will be forcibly scaled to identical full size, which may cause minor aspect ratio distortion \(expected\)\.

- High\-quality LANCZOS resampling is used for stretching to retain image details and avoid blurriness\.

- All output images are saved in lossless PNG format to preserve original quality\.

## 7\. Troubleshooting

### ModuleNotFoundError: No module named 'PIL'

The Pillow library is not installed\. Run the installation command:

```bash
pip install pillow
```

### Input file does not exist

- Double\-check your input file path for typos\.

- Use absolute file paths for stability\.

- Wrap paths with spaces in double quotes\.

Absolute path example with quotes:

```bash
python split_png_into_4.py "D:/my images/input.png"
```

> （注：部分内容可能由 AI 生成）
