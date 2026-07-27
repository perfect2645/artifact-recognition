"""Core orchestration for artifact recognition requests."""

from __future__ import annotations

from pathlib import Path

from controller.artifact_inference import ArtifactClassifier
from controller.artifact_models import ArtifactPayload, ArtifactStatus
from controller.dicom_converter import convert_dicom_to_bitmap


class ArtifactRecognitionService:
    def __init__(self, classifier: ArtifactClassifier, bitmap_output_dir: Path) -> None:
        self.classifier = classifier
        self.bitmap_output_dir = bitmap_output_dir

    def process(self, artifact: ArtifactPayload) -> ArtifactPayload:
        artifact.status = ArtifactStatus.PROCESSING
        source_path = Path(artifact.source_dicom_image_path)
        if not source_path.exists():
            artifact.status = ArtifactStatus.FAILED
            artifact.comments = f"Source DICOM not found: {source_path}"
            return artifact

        output_path = self._resolve_output_path(artifact, source_path)
        artifact.converted_bitmap_image_path = str(convert_dicom_to_bitmap(source_path, output_path))

        result = self.classifier.predict(Path(artifact.converted_bitmap_image_path))
        artifact.has_artifact = bool(result["hasArtifact"])
        artifact.status = ArtifactStatus.COMPLETED
        artifact.comments = (
            f"predictedClass={result['predictedClass']}; "
            f"artifactProb={result['probabilities']['artifact']:.4f}; "
            f"noArtifactProb={result['probabilities']['noArtifact']:.4f}"
        )
        return artifact

    def _resolve_output_path(self, artifact: ArtifactPayload, source_path: Path) -> Path:
        if artifact.converted_bitmap_image_path:
            return Path(artifact.converted_bitmap_image_path)
        return self.bitmap_output_dir / f"{source_path.stem}.png"
