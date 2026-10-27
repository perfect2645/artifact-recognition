import sys
import unittest
from dataclasses import replace
from pathlib import Path
from unittest.mock import Mock
from urllib.error import URLError

SRC_DIR = Path(file).resolve().parents[1] / "src"
if str(SRC_DIR) not in sys.path:
sys.path.insert(0, str(SRC_DIR))
from controller.artifact_models import Artifact, ArtifactMessage, ArtifactStatus, RecognitionStatus
from controller.artifact_worker import ArtifactWorker
class ArtifactWorkerTests(unittest.TestCase):
def setUp(self) -> None:
self.service = Mock()
self.webapi_client = Mock()
self.worker = ArtifactWorker(
receiver=Mock(),
service=self.service,
webapi_client=self.webapi_client,
group_name="recognition-model",
join_group_method="JoinGroup",
)
self.message = ArtifactMessage(
sender="RecognitionService",
topic="Artifact",
message=Artifact(
artifact_id="00000000-0000-0000-0000-000000000001",
name="sample.dcm",
input_path=r"C:\test-data\sample.dcm",
output_path="",
update_time=None,
artifact_status=ArtifactStatus.UNKNOWN,
recognition_status=RecognitionStatus.PROCESSING,
comments=None,
),
)
def test_invalid_messages_are_logged_without_processing_or_posting(self) -> None:
for payload in ({}, {"message": {}}, ["invalid"]):
with self.subTest(payload=payload):
with self.assertLogs("artifact-worker", level="ERROR") as logs:
self.worker.handle_message("recognition", payload)
self.service.process.assert_not_called()
self.webapi_client.post_result.assert_not_called()
self.assertEqual(len(logs.records), 1)
self.assertIn("ValueError", logs.output[0])
self.assertNotIn("AttributeError", logs.output[0])
def test_processing_exception_posts_failed_model_result_once(self) -> None:
self.service.process.side_effect = ValueError("Invalid DICOM data")
with self.assertLogs("artifact-worker", level="ERROR"):
self.worker.handle_message("recognition", self.message.to_dict())
self.webapi_client.post_result.assert_called_once()
result = ArtifactMessage.from_dict(
self.webapi_client.post_result.call_args.args[0]
)
self.assertEqual(result.sender, "model")
self.assertEqual(result.topic, self.message.topic)
self.assertEqual(result.message.artifact_id, self.message.message.artifact_id)
self.assertEqual(result.message.input_path, self.message.message.input_path)
self.assertEqual(result.message.recognition_status, RecognitionStatus.FAILED)
self.assertEqual(
result.message.comments, "Artifact recognition failed: Invalid DICOM data"
)
self.assertIsNotNone(result.message.update_time)
self.assertEqual(self.message.sender, "RecognitionService")
self.assertEqual(
self.message.message.recognition_status, RecognitionStatus.PROCESSING
)
def test_successful_processing_posts_completed_result_once(self) -> None:
self.service.process.return_value = replace(
self.message.message, recognition_status=RecognitionStatus.COMPLETED
)
self.worker.handle_message("recognition", self.message.to_dict())
self.webapi_client.post_result.assert_called_once()
result = ArtifactMessage.from_dict(
self.webapi_client.post_result.call_args.args[0]
)
self.assertEqual(result.sender, "model")
self.assertEqual(result.message.recognition_status, RecognitionStatus.COMPLETED)
def test_failed_service_result_is_posted_once(self) -> None:
self.service.process.return_value = replace(
self.message.message,
recognition_status=RecognitionStatus.FAILED,
comments="Source DICOM not found",
)
self.worker.handle_message("recognition", self.message.to_dict())
self.webapi_client.post_result.assert_called_once()
result = ArtifactMessage.from_dict(
self.webapi_client.post_result.call_args.args[0]
)
self.assertEqual(result.sender, "model")
self.assertEqual(result.message.recognition_status, RecognitionStatus.FAILED)
self.assertEqual(result.message.comments, "Source DICOM not found")
def test_failed_result_delivery_is_not_retried(self) -> None:
self.service.process.side_effect = ValueError("Invalid DICOM data")
self.webapi_client.post_result.side_effect = URLError(
ConnectionRefusedError("API unavailable")
)
with self.assertLogs("artifact-worker", level="INFO") as logs:
self.worker.handle_message("recognition", self.message.to_dict())
self.webapi_client.post_result.assert_called_once()
self.assertFalse(
any("result posted" in record.getMessage() for record in logs.records)
)
if name == "main":
unittest.main()