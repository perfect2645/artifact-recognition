"""
Data models for artifact recognition messages and business entities.
Includes core artifact entity, message wrapper and status enumerations.
"""

from __future__ import annotations

from dataclasses import asdict, dataclass
from datetime import datetime
from enum import Enum
from typing import Any, Optional


class ArtifactStatus(Enum):
    """Enumeration representing the existence state of an artifact."""
    UNKNOWN = 0
    NO_ARTIFACT = 1
    ARTIFACT_EXISTS = 2


class RecognitionStatus(Enum):
    """Enumeration representing the processing state of artifact recognition."""
    PENDING = 0
    PROCESSING = 1
    COMPLETED = 2
    CANCELLED = 3
    FAILED = 4


@dataclass(frozen=True)
class Artifact:
    """
    Core business entity representing an artifact task.
    Corresponds to the C# `Artifact` record, with value-based equality and immutability.
    """
    artifact_id: str
    name: str
    input_path: str
    output_path: str
    update_time: Optional[datetime]
    artifact_status: ArtifactStatus
    recognition_status: RecognitionStatus

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> "Artifact":
        """
        Create an Artifact instance from a raw dictionary.
        Supports both snake_case (Python style) and PascalCase (C# style) keys.

        Args:
            data: Raw dictionary containing artifact fields.

        Returns:
            Constructed Artifact instance.

        Raises:
            ValueError: If required fields are missing or invalid.
        """

        def _get_value(snake_key: str, pascal_key: str) -> Any:
            """Fallback helper: return snake_case value if key exists, else fall back to PascalCase key."""
            if snake_key in data:
                return data[snake_key]
            return data.get(pascal_key)

        # Extract base fields with cross naming convention compatibility
        artifact_id = _get_value("artifact_id", "artifactId")
        name = _get_value("name", "Name")
        input_path = _get_value("input_path", "inputPath")
        output_path = _get_value("output_path", "outputPath")

        # Required field validation
        if input_path is None:
            raise ValueError("Payload missing required field: input_path")

        # Parse ISO format datetime string
        raw_update_time = _get_value("update_time", "updateTime")
        update_time = datetime.fromisoformat(raw_update_time) if raw_update_time else None

        # Convert raw integer values to enum instances
        raw_artifact_status = _get_value("artifact_status", "artifactStatus")
        artifact_status = ArtifactStatus(raw_artifact_status)

        raw_recognition_status = _get_value("recognition_status", "recognitionStatus")
        recognition_status = RecognitionStatus(raw_recognition_status)

        # Frozen dataclass must be fully constructed in one call
        return cls(
            artifact_id=artifact_id,
            name=name,
            input_path=input_path,
            output_path=output_path,
            update_time=update_time,
            artifact_status=artifact_status,
            recognition_status=recognition_status,
        )

    def to_dict(self) -> dict[str, Any]:
        """Convert the Artifact instance to a snake_case dictionary for serialization."""
        return asdict(self)


@dataclass(frozen=True)
class ArtifactMessage:
    """
    Outer message wrapper for real-time artifact communication.
    Corresponds to C# `ArtifactMessage` record and `IRealTimeMessage<Artifact>` interface.
    Contains transport metadata (sender, topic) and the inner business payload.
    """
    sender: str
    topic: str
    message: Artifact

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> "ArtifactMessage":
        """
        Create an ArtifactMessage instance from a raw message dictionary.
        Automatically extracts and delegates parsing of the inner Artifact payload.
        Supports both snake_case and PascalCase keys for outer fields.

        Args:
            payload: Raw dictionary containing full message envelope data.

        Returns:
            Constructed ArtifactMessage instance.

        Raises:
            ValueError: If the inner message field is missing or not a dictionary.
        """

        def _get_value(snake_key: str, pascal_key: str) -> Any:
            """Fallback helper: return snake_case value if key exists, else fall back to PascalCase key."""
            if snake_key in payload:
                return payload[snake_key]
            return payload.get(pascal_key)

        # Extract outer message metadata
        sender = _get_value("sender", "Sender") or ""
        topic = _get_value("topic", "Topic") or ""

        # Extract inner business payload dictionary
        message_dict = _get_value("message", "Message")
        if not isinstance(message_dict, dict):
            raise ValueError("Invalid payload: 'message' field must be a dictionary")

        # Delegate inner payload parsing to Artifact class (single responsibility principle)
        artifact = Artifact.from_dict(message_dict)

        return cls(
            sender=sender,
            topic=topic,
            message=artifact,
        )

    def to_dict(self) -> dict[str, Any]:
        """Convert the ArtifactMessage instance to a snake_case dictionary for serialization."""
        return {
            "sender": self.sender,
            "topic": self.topic,
            "message": self.message.to_dict(),
        }