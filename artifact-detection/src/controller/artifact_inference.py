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
    """Create the two-class ResNet-18 architecture expected by the checkpoint."""
    # the checkpoint supplies all trained weights, so no pretrained weights are loaded here.
    model = models.resnet18(weights=None)
    # replace the original ImageNet head with logits for the two artifact classes
    model.fc = nn.Linear(model.fc.in_features, 2)
    return model


class ArtifactClassifier:
    """Load a trained artifact classifier and run inference for a single image."""
    def __init__(self, model_path: Path, image_size: int = 224) -> None:
        """ initialize the model, checkpoint weights, device, and image preprocessing pipeline.

        Args:
            model_path (Path): Path to a checkpoint containing ``model_state_dict``,
                optionally, the ``image_size`` used during training.

            image_size (int): Fallback input size when the checkpoint does not contain an ``image_size``. Defaults to 224.
          """
        self.model_path = model_path

        # prefer CUDA when available; otherwise use CPU
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        # recreate the training architecture on the selected device before loading weights.
        self.model = build_model().to(self.device)
        # load tensors directly onto the inference device to avoid an extra device transfer.
        checkpoint: dict[str, Any] = torch.load(model_path, map_location=self.device)
        self.model.load_state_dict(checkpoint["model_state_dict"])
        # evaluation mode disables training-only behavior such as dropout and batch normalization.
        self.model.eval()
        # use the training image size if available.
        self.image_size = int(checkpoint.get("image_size", image_size))
        # Apply the same shape, tensor convention, and channel normalization to every image.
        self.transform = transforms.Compose(
            [
                transforms.Resize((self.image_size, self.image_size)),
                transforms.ToTensor(),
                transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
            ]
        )

    def predict(self, image_path: Path) -> dict[str, Any]:
        """Classify an image and return its class, artifact flag, and probabilities.
            Args:
                image_path (Path): Path to the image to classify.

            Returns:
                dict[str, Any]: A dictionary containing the predicted class index and name,
                 a boolean artifact result, and probabilities for both classes.
        """

        # force three-channel RGB so every supported source image has a consistent shape.
        image = Image.open(image_path).convert("RGB")
        # transform C x H x W into a one-image batch and move it to the model's device.
        x = self.transform(image).unsqueeze(0).to(self.device)
        # disable gradient tracking because inference does not perform backpropagation.
        with torch.no_grad():
            # the model returns one raw logits per class.
            logits = self.model(x)
            # convert logits into normalized probabilities. and copy them back to python memory.
            probabilities = torch.softmax(logits, dim=1).squeeze(0).cpu().tolist()
            # select the class with the largest logit as the prediction.
            predicted_index = int(torch.argmax(logits, dim=1).item())

        # keep this transport-friendly shape for ArtifactRecognitionService.
        return {
            "predictedIndex": predicted_index,
            "predictedClass": CLASS_NAMES[predicted_index],
            "hasArtifact": predicted_index == 1,
            "probabilities": {
                "noArtifact": float(probabilities[0]),
                "artifact": float(probabilities[1]),
            },
        }
