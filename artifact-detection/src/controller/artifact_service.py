"""Core orchestration for artifact recognition requests."""

from __future__ import annotations

from dataclasses import replace
from datetime import datetime
from pathlib import Path

from controller.artifact_inference import ArtifactClassifier
from controller.artifact_models import Artifact, ArtifactStatus, RecognitionStatus
from controller.dicom_converter import convert_dicom_to_bitmap


class ArtifactRecognitionService:
    def __init__(self, classifier: ArtifactClassifier, bitmap_output_dir: Path) -> None:
        self.classifier = classifier
        self.bitmap_output_dir = bitmap_output_dir

    def process(self, artifact: Artifact) -> Artifact:
        processing_artifact = replace(
            artifact, 
            recognition_status=RecognitionStatus.PROCESSING,
            update_time=datetime.now().astimezone()
        )
        source_path = Path(processing_artifact.input_path)
        if not source_path.exists():
            return replace(
                processing_artifact,
                recognition_status=RecognitionStatus.FAILED,
                comments=f"Source DICOM not found: {source_path}",
                update_time=datetime.now().astimezone()
            )

        output_path = self._resolve_output_path(processing_artifact, source_path)
        converted_output_path = str(convert_dicom_to_bitmap(source_path, output_path))

        result = self.classifier.predict(Path(converted_output_path))
        artifact_status = (
            ArtifactStatus.ARTIFACT_EXISTS if bool(result["hasArtifact"]) else ArtifactStatus.NO_ARTIFACT
        )
        comments = (
            f"predictedClass={result['predictedClass']}; "
            f"artifactProb={result['probabilities']['artifact']:.4f}; "
            f"noArtifactProb={result['probabilities']['noArtifact']:.4f}"
        )

        return replace(
            processing_artifact,
            output_path=converted_output_path,
            artifact_status=artifact_status,
            recognition_status=RecognitionStatus.COMPLETED,
            comments=comments,
            update_time=datetime.now().astimezone()
        )

    def _resolve_output_path(self, artifact: Artifact, source_path: Path) -> Path:
        if artifact.output_path:
            return Path(artifact.output_path)
        return self.bitmap_output_dir / f"{source_path.stem}.png"
