"""Reusable SignalR client helpers for the artifact recognition pipeline."""

from __future__ import annotations

import json
import logging
from typing import Any, Callable, Iterable, Optional

from signalrcore.hub_connection_builder import HubConnectionBuilder


LOGGER = logging.getLogger("signalr-client")


class SignalRReceiver:
	"""Receives hub events from a .NET SignalR endpoint."""

	def __init__(
		self,
		hub_url: str,
		events: Iterable[str],
		on_message: Optional[Callable[[str, Any], None]] = None,
		access_token: Optional[str] = None,
	) -> None:
		self.hub_url = hub_url
		self.events = [event for event in events if event.strip()]
		self.on_message = on_message or self._default_message_handler
		self._stopping = False

		options: dict[str, Any] = {}
		if access_token:
			options["access_token_factory"] = lambda: access_token

		builder = (
			HubConnectionBuilder()
			.with_url(
				hub_url,
				options=options,
			)
			.with_automatic_reconnect(
				{
					"type": "raw",
					"keep_alive_interval": 10,
					"reconnect_interval": 5,
					"max_attempts": 0,
				}
			)
		)

		self.connection = builder.build()

		self.connection.on_open(lambda: LOGGER.info("Connected to hub: %s", self.hub_url))
		self.connection.on_close(lambda: LOGGER.warning("Connection closed."))
		self.connection.on_error(lambda error: LOGGER.error("SignalR error: %s", error))

		if not self.events:
			raise ValueError("At least one event name is required.")

		for event_name in self.events:
			self.connection.on(event_name, self._build_event_handler(event_name))
			LOGGER.info("Subscribed to event: %s", event_name)

	def _build_event_handler(self, event_name: str) -> Callable[[Any], None]:
		def _handler(arguments: Any) -> None:
			payload = self._normalize_payload(arguments)
			self.on_message(event_name, payload)

		return _handler

	@staticmethod
	def _normalize_payload(arguments: Any) -> Any:
		if isinstance(arguments, list):
			if len(arguments) == 1:
				return arguments[0]
			return arguments
		return arguments

	@staticmethod
	def _default_message_handler(event_name: str, payload: Any) -> None:
		if isinstance(payload, (dict, list)):
			text = json.dumps(payload, indent=2, ensure_ascii=True)
		else:
			text = str(payload)
		print(f"[{event_name}] {text}")

	def start(self) -> None:
		LOGGER.info("Starting SignalR receiver...")
		self.connection.start()

	def stop(self) -> None:
		if self._stopping:
			return
		self._stopping = True
		LOGGER.info("Stopping SignalR receiver...")
		self.connection.stop()

	def invoke(self, method_name: str, arguments: Optional[list[Any]] = None) -> None:
		args = arguments or []
		LOGGER.info("Invoking hub method %s with %d argument(s)", method_name, len(args))
		self.connection.send(method_name, args)
