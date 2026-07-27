"""Data models for artifact recognition messages and results."""

from __future__ import annotations

from dataclasses import asdict, dataclass
from datetime import datetime
from enum import Enum
from typing import Any, Optional


class ArtifactStatus(str, Enum):
    PENDING = "Pending"
    PROCESSING = "Processing"
    COMPLETED = "Completed"
    FAILED = "Failed"


@dataclass
class ArtifactPayload:
    source_dicom_image_path: str
    converted_bitmap_image_path: str = ""
    status: ArtifactStatus = ArtifactStatus.PENDING
    has_artifact: bool = False
    comments: Optional[str] = None
    created_time: Optional[str] = None

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> "ArtifactPayload":
        source_path = payload.get("sourceDicomImagePath") or payload.get("source_dicom_image_path")
        if not source_path:
            raise ValueError("Payload missing sourceDicomImagePath")

        raw_status = payload.get("status", ArtifactStatus.PENDING.value)
        try:
            status = ArtifactStatus(raw_status)
        except ValueError:
            status = ArtifactStatus.PENDING

        created_time = payload.get("createdTime") or payload.get("created_time")
        if isinstance(created_time, datetime):
            created_text = created_time.isoformat()
        else:
            created_text = str(created_time) if created_time else None

        return cls(
            source_dicom_image_path=str(source_path),
            converted_bitmap_image_path=str(
                payload.get("convertedBitmapImagePath") or payload.get("converted_bitmap_image_path") or ""
            ),
            status=status,
            has_artifact=bool(payload.get("hasArtifact") or payload.get("has_artifact") or False),
            comments=payload.get("comments"),
            created_time=created_text,
        )

    def to_dict(self) -> dict[str, Any]:
        data = asdict(self)
        return {
            "sourceDicomImagePath": data["source_dicom_image_path"],
            "convertedBitmapImagePath": data["converted_bitmap_image_path"],
            "status": self.status.value,
            "hasArtifact": data["has_artifact"],
            "comments": data["comments"],
            "createdTime": data["created_time"],
        }
