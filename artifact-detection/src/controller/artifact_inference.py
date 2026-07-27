"""Artifact model loading and single-image inference."""

from __future__ import annotations

from pathlib import Path
from typing import Any

import torch
from PIL import Image
from torch import nn
from torchvision import models, transforms


CLASS_NAMES = ["no_artifact", "artifact"]


def build_model() -> nn.Module:
    model = models.resnet18(weights=None)
    model.fc = nn.Linear(model.fc.in_features, 2)
    return model


class ArtifactClassifier:
    def __init__(self, model_path: Path, image_size: int = 224) -> None:
        self.model_path = model_path
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        self.model = build_model().to(self.device)

        checkpoint: dict[str, Any] = torch.load(model_path, map_location=self.device)
        self.model.load_state_dict(checkpoint["model_state_dict"])
        self.model.eval()

        self.image_size = int(checkpoint.get("image_size", image_size))
        self.transform = transforms.Compose(
            [
                transforms.Resize((self.image_size, self.image_size)),
                transforms.ToTensor(),
                transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
            ]
        )

    def predict(self, image_path: Path) -> dict[str, Any]:
        image = Image.open(image_path).convert("RGB")
        x = self.transform(image).unsqueeze(0).to(self.device)
        with torch.no_grad():
            logits = self.model(x)
            probabilities = torch.softmax(logits, dim=1).squeeze(0).cpu().tolist()
            predicted_index = int(torch.argmax(logits, dim=1).item())

        return {
            "predictedIndex": predicted_index,
            "predictedClass": CLASS_NAMES[predicted_index],
            "hasArtifact": predicted_index == 1,
            "probabilities": {
                "noArtifact": float(probabilities[0]),
                "artifact": float(probabilities[1]),
            },
        }
