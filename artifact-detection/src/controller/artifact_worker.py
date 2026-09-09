"""SignalR-driven artifact recognition worker."""

from __future__ import annotations

import argparse
import logging
import signal
import sys
import time
from dataclasses import replace
from datetime import datetime
from enum import Enum
from pathlib import Path
from typing import Any

CURRENT_DIR = Path(__file__).resolve().parent
SRC_DIR = CURRENT_DIR.parent
if str(SRC_DIR) not in sys.path:
    sys.path.insert(0, str(SRC_DIR))

from controller.artifact_inference import ArtifactClassifier
from controller.artifact_models import ArtifactMessage, RecognitionStatus
from controller.artifact_service import ArtifactRecognitionService
from controller.webapi_client import WebApiClient
from messaging.signalr_client import SignalRReceiver


LOGGER = logging.getLogger("artifact-worker")

def _from_status(status: Enum) -> str:
    status_name = "".join(part.title() for part in status.name.split("_"))
    return f"{status.value} : {status_name}"

class ArtifactWorker:
    def __init__(
        self,
        receiver: SignalRReceiver,
        service: ArtifactRecognitionService,
        webapi_client: WebApiClient,
        group_name: str,
        join_group_method: str,
    ) -> None:
        self.receiver = receiver
        self.service = service
        self.webapi_client = webapi_client
        self.group_name = group_name
        self.join_group_method = join_group_method

    def start(self) -> None:
        self.receiver.start()
        if self.join_group_method:
            self.receiver.invoke(self.join_group_method, [self.group_name])

    def stop(self) -> None:
        self.receiver.stop()

    def handle_message(self, event_name: str, payload: ArtifactMessage) -> None:
        artifact_message: ArtifactMessage | None = None
        try:
            artifact_message = ArtifactMessage.from_dict(self._extract_payload(payload))

            self._log_artifact_message(
                "received", artifact_message, "event", event_name
            )
            artifact_message = replace(
                artifact_message,
                sender="model",
                message=self.service.process(artifact_message.message)
            )

            self._log_artifact_message(
                "proceeded", artifact_message, "event", event_name
            )

        except Exception as exc:
            self._handle_failure(artifact_message, exc)
            return

        self._post_result(artifact_message)       

    def _handle_failure(
        self,
        artifact_message: ArtifactMessage | None,
        exc: Exception
    ) -> None:
        error_text = f"Artifact recognition failed: {exc}"
        LOGGER.exception(
            "Artifact recognition failed: artifactId=%s; inputPath=%s",
            artifact_message.message.artifact_id if artifact_message else "N/A",
            artifact_message.message.input_path if artifact_message else "N/A"
        )

    @staticmethod
    def _extract_payload(payload: Any) -> dict[str, Any]:
        if isinstance(payload, dict):
            return payload
        raise ValueError("SignalR payload must be a JSON object")

    @staticmethod
    def _build_failed_message(artifact_message: ArtifactMessage, error_text: str) -> ArtifactMessage:
        failed_artifact = replace(
            artifact_message.message,
            recognition_status=RecognitionStatus.FAILED,
            comments=error_text,
            update_time=datetime.now().astimezone()
        )
        return replace(artifact_message, message=failed_artifact)

    @staticmethod
    def _log_artifact_message(
        action: str,
        artifact_message: ArtifactMessage,
        context_name: str,
        context_value: str,
    ) -> None:
        LOGGER.info(
            "Artifact %s: %s=%s; artifactId=%s; inputPath=%s "
            "ArtifactStatus=%s; RecognitionStatus=%s",
            action,
            context_name,
            context_value,
            artifact_message.message.artifact_id,
            artifact_message.message.input_path,
            _from_status(artifact_message.message.artifact_status),
            _from_status(artifact_message.message.recognition_status),
        )
    
    def _post_result(self, artifact_message: ArtifactMessage) -> None:
        try:
            self.webapi_client.post_result(artifact_message.to_dict())
        except Exception as exc:
            self._handle_failure(artifact_message, exc)
            return
        self._log_artifact_message(
            "result posted", artifact_message, "event", "post_result"
        )

def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="SignalR artifact recognition worker")
    parser.add_argument("--hub-url", default="https://localhost:7092/hubs/signalr")
    parser.add_argument("--event-name", default="recognition")
    parser.add_argument("--group-name", default="recognition-model")
    parser.add_argument("--join-group-method", default="JoinGroup")
    parser.add_argument("--result-url", default="https://localhost:7092/api/artifact")
    parser.add_argument("--model-path", type=Path, default=SRC_DIR / "model" / "best_model.pth")
    parser.add_argument("--bitmap-output-dir", type=Path, default=SRC_DIR / "runtime" / "converted")
    parser.add_argument("--access-token", default=None)
    parser.add_argument(
        "--log-level",
        default="DEBUG",
        choices=["DEBUG", "INFO", "WARNING", "ERROR", "CRITICAL"],
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    logging.basicConfig(
        level=getattr(logging, args.log_level),
        format="%(asctime)s | %(levelname)s | %(name)s | %(message)s",
    )

    classifier = ArtifactClassifier(args.model_path)
    service = ArtifactRecognitionService(classifier, args.bitmap_output_dir)
    webapi_client = WebApiClient(args.result_url, access_token=args.access_token)

    worker: ArtifactWorker

    def _on_message(event_name: str, payload: Any) -> None:
        worker.handle_message(event_name, payload)

    receiver = SignalRReceiver(
        hub_url=args.hub_url,
        events=[args.event_name],
        on_message=_on_message,
        access_token=args.access_token,
    )
    worker = ArtifactWorker(
        receiver=receiver,
        service=service,
        webapi_client=webapi_client,
        group_name=args.group_name,
        join_group_method=args.join_group_method,
    )

    def _shutdown(_signum: int, _frame: Any) -> None:
        worker.stop()

    signal.signal(signal.SIGINT, _shutdown)
    if hasattr(signal, "SIGTERM"):
        signal.signal(signal.SIGTERM, _shutdown)

    worker.start()
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        pass
    finally:
        worker.stop()

    return 0


if __name__ == "__main__":
    sys.exit(main())