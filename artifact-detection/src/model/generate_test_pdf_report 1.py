"""Generate test-score report and PDF visualizations using a trained .pth model.

This script selects fixed test samples:
- 15 images from no-artifact class
- 15 images from artifact class

Then it evaluates the model checkpoint and creates:
- test_score_report.json
- test_predictions.csv
- test_visual_report.pdf
"""

from __future__ import annotations

import argparse
import csv
import json
from pathlib import Path
from typing import Dict, List, Sequence

import matplotlib.pyplot as plt
import torch
from matplotlib.backends.backend_pdf import PdfPages
from PIL import Image
from torch import nn
from torchvision import models, transforms


IMAGE_EXTENSIONS = {".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff"}
CLASS_NAMES = ["no_artifact", "artifact"]


def list_images(folder: Path) -> List[Path]:
    return sorted([p for p in folder.rglob("*") if p.is_file() and p.suffix.lower() in IMAGE_EXTENSIONS])


def build_model() -> nn.Module:
    model = models.resnet18(weights=None)
    model.fc = nn.Linear(model.fc.in_features, 2)
    return model


def compute_metrics(preds: List[int], labels: List[int]) -> Dict[str, float]:
    n = max(1, len(labels))
    correct = sum(int(p == y) for p, y in zip(preds, labels))
    acc = correct / n

    tp = sum(int((p == 1) and (y == 1)) for p, y in zip(preds, labels))
    fp = sum(int((p == 1) and (y == 0)) for p, y in zip(preds, labels))
    fn = sum(int((p == 0) and (y == 1)) for p, y in zip(preds, labels))

    precision = tp / (tp + fp) if (tp + fp) > 0 else 0.0
    recall = tp / (tp + fn) if (tp + fn) > 0 else 0.0
    f1 = 2 * precision * recall / (precision + recall) if (precision + recall) > 0 else 0.0

    return {
        "accuracy": acc,
        "precision": precision,
        "recall": recall,
        "f1": f1,
    }


def save_pdf(
    output_pdf: Path,
    test_paths: Sequence[Path],
    true_labels: Sequence[int],
    pred_labels: Sequence[int],
    probs: Sequence[List[float]],
    metrics: Dict[str, float],
) -> None:
    with PdfPages(output_pdf) as pdf:
        # Cover page with summary metrics.
        fig = plt.figure(figsize=(11.69, 8.27))
        fig.suptitle("Artifact Classification Test Report", fontsize=20, y=0.95)
        report_text = (
            f"Total test images: {len(test_paths)}\\n"
            f"Accuracy: {metrics['accuracy']:.4f}\\n"
            f"Precision (artifact): {metrics['precision']:.4f}\\n"
            f"Recall (artifact): {metrics['recall']:.4f}\\n"
            f"F1-score (artifact): {metrics['f1']:.4f}\\n"
        )
        fig.text(0.08, 0.75, report_text, fontsize=14, va="top")
        fig.text(0.08, 0.35, "Class mapping: 0=no_artifact, 1=artifact", fontsize=12)
        pdf.savefig(fig)
        plt.close(fig)

        # Image pages.
        images_per_page = 6
        for start in range(0, len(test_paths), images_per_page):
            end = min(start + images_per_page, len(test_paths))
            n_items = end - start

            fig, axes = plt.subplots(2, 3, figsize=(11.69, 8.27))
            axes = axes.flatten()
            for ax in axes:
                ax.axis("off")

            for ax, idx in zip(axes, range(start, end)):
                img = Image.open(test_paths[idx]).convert("RGB")
                ax.imshow(img)
                ax.axis("off")

                true_name = CLASS_NAMES[true_labels[idx]]
                pred_name = CLASS_NAMES[pred_labels[idx]]
                p_no, p_art = probs[idx]
                correct = pred_labels[idx] == true_labels[idx]
                status = "OK" if correct else "ERR"
                color = "green" if correct else "red"

                ax.set_title(
                    f"{status} | True={true_name}, Pred={pred_name}",
                    #f"P(no)={p_no:.3f}, P(art)={p_art:.3f}\\n"
                    #f"{test_paths[idx].name}",
                    fontsize=9,
                    color=color,
                )

            fig.suptitle(f"Test Samples {start + 1}-{end} / {len(test_paths)}", fontsize=14)
            plt.tight_layout(rect=[0, 0.02, 1, 0.95])
            pdf.savefig(fig)
            plt.close(fig)


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate test-score report and PDF")
    parser.add_argument(
        "--test-no-artifact-dir",
        type=Path,
        default=Path("test") / "无伪影",
    )
    parser.add_argument(
        "--test-artifact-dir",
        type=Path,
        default=Path("test") / "有伪影",
    )
    parser.add_argument("--model-path", type=Path, default=Path("artifacts_model") / "best_model.pth")
    parser.add_argument("--output-dir", type=Path, default=Path("artifacts_model"))
    parser.add_argument("--image-size", type=int, default=224)
    args = parser.parse_args()

    if not args.model_path.exists():
        raise FileNotFoundError(f"Model not found: {args.model_path}")

    test_no_images = list_images(args.test_no_artifact_dir)
    test_ar_images = list_images(args.test_artifact_dir)
    if not test_no_images:
        raise FileNotFoundError(f"No images found in: {args.test_no_artifact_dir}")
    if not test_ar_images:
        raise FileNotFoundError(f"No images found in: {args.test_artifact_dir}")

    test_paths = test_no_images + test_ar_images
    test_labels = [0] * len(test_no_images) + [1] * len(test_ar_images)

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Using device: {device}")

    model = build_model().to(device)
    checkpoint = torch.load(args.model_path, map_location=device)
    model.load_state_dict(checkpoint["model_state_dict"])
    model.eval()

    image_size = int(checkpoint.get("image_size", args.image_size))
    tfm = transforms.Compose(
        [
            transforms.Resize((image_size, image_size)),
            transforms.ToTensor(),
            transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
        ]
    )

    preds: List[int] = []
    probs: List[List[float]] = []

    with torch.no_grad():
        for p in test_paths:
            image = Image.open(p).convert("RGB")
            x = tfm(image).unsqueeze(0).to(device)
            logits = model(x)
            prob = torch.softmax(logits, dim=1).squeeze(0).cpu().tolist()
            pred = int(torch.argmax(logits, dim=1).item())
            preds.append(pred)
            probs.append(prob)

    metrics = compute_metrics(preds=preds, labels=test_labels)

    args.output_dir.mkdir(parents=True, exist_ok=True)

    # Save JSON report.
    report = {
        "model_path": str(args.model_path),
        "test_no_artifact_dir": str(args.test_no_artifact_dir),
        "test_artifact_dir": str(args.test_artifact_dir),
        "test_no_count": len(test_no_images),
        "test_artifact_count": len(test_ar_images),
        "total_test_images": len(test_paths),
        "class_mapping": {"0": "no_artifact", "1": "artifact"},
        "metrics": metrics,
    }
    json_path = args.output_dir / "test_score_report.json"
    with json_path.open("w", encoding="utf-8") as f:
        json.dump(report, f, ensure_ascii=False, indent=2)

    # Save CSV per-image predictions.
    csv_path = args.output_dir / "test_predictions.csv"
    with csv_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["image_path", "true_label", "pred_label", "true_name", "pred_name", "prob_no_artifact", "prob_artifact"])
        for p, y, pred, prob in zip(test_paths, test_labels, preds, probs):
            writer.writerow([
                str(p),
                y,
                pred,
                CLASS_NAMES[y],
                CLASS_NAMES[pred],
                f"{prob[0]:.6f}",
                f"{prob[1]:.6f}",
            ])

    # Save PDF visualization report.
    pdf_path = args.output_dir / "test_visual_report.pdf"
    save_pdf(
        output_pdf=pdf_path,
        test_paths=test_paths,
        true_labels=test_labels,
        pred_labels=preds,
        probs=probs,
        metrics=metrics,
    )

    print("\nTest score:")
    print(f"accuracy={metrics['accuracy']:.4f}")
    print(f"precision={metrics['precision']:.4f}")
    print(f"recall={metrics['recall']:.4f}")
    print(f"f1={metrics['f1']:.4f}")
    print(f"[SAVE] {json_path}")
    print(f"[SAVE] {csv_path}")
    print(f"[SAVE] {pdf_path}")


if __name__ == "__main__":
    main()
