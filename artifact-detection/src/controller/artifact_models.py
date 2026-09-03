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

def _enum_json_value(value: Enum) -> str:
    return "".join(part.title() for part in value.name.split("_"))

def _parse_enum[EnumT: Enum](enum_type: type[EnumT], raw_value: object, field_name: str) -> EnumT:
    if isinstance(raw_value, enum_type):
        return raw_value
    if isinstance(raw_value, int) and not isinstance(raw_value, bool):
        try:
            return enum_type[raw_value]
        except KeyError as exc:
            raise ValueError(f"Invalid {field_name}: {raw_value}") from exc
    
    if isinstance(raw_value, str):
        text = raw_value.strip()
        if text.lstrip("+-").isdigit():
            return _parse_enum(enum_type, int(text), field_name)
        
        normalized_text = text.replace("_", "").casefold()
        for member in enum_type:
            if normalized_text in {
                member.name.replace("_", "").casefold(),
                _enum_json_value(member).casefold()
            }:
                return member

    raise ValueError(f"Invalid {field_name}: {raw_value}")


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
    comments: Optional[str]

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
        if isinstance(raw_update_time, datetime):
            update_time = raw_update_time
        elif isinstance(raw_update_time, str) and raw_update_time:
            update_time = datetime.fromisoformat(raw_update_time)
        elif raw_update_time is None or raw_update_time == "":
            update_time = None
        else:
            raise ValueError(f"Invalid update_time: {raw_update_time}")

        # Convert raw integer values to enum instances
        raw_artifact_status = _get_value("artifact_status", "artifactStatus")
        artifact_status = _parse_enum(ArtifactStatus, raw_artifact_status, "artifactStatus")

        raw_recognition_status = _get_value("recognition_status", "recognitionStatus")
        recognition_status = _parse_enum(RecognitionStatus, raw_recognition_status, "recognitionStatus")

        # Frozen dataclass must be fully constructed in one call
        return cls(
            artifact_id=artifact_id,
            name=name,
            input_path=input_path,
            output_path=output_path,
            update_time=update_time,
            artifact_status=artifact_status,
            recognition_status=recognition_status,
            comments=_get_value("comments", "Comments")
        )

    def to_dict(self) -> dict[str, Any]:
        """Convert the Artifact instance to a snake_case dictionary for serialization."""
        return {
            "artifact_id": self.artifact_id,
            "name": self.name,
            "input_path": self.input_path,
            "output_path": self.output_path,
            "update_time": self.update_time.isoformat() if self.update_time else None,
            "artifact_status": _enum_json_value(self.artifact_status),
            "recognition_status": _enum_json_value(self.recognition_status),
            "comments": self.comments
        }


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