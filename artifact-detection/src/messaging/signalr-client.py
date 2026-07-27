"""SignalR client CLI entrypoint for receiving messages from an ASP.NET Core WebAPI hub."""

from __future__ import annotations

import argparse
import logging
import signal
import sys
import time
from typing import Any

from signalr_client import SignalRReceiver


def _parse_args() -> argparse.Namespace:
	parser = argparse.ArgumentParser(description="Receive messages from a .NET SignalR hub.")
	parser.add_argument(
		"--hub-url",
		default="http://localhost:5000/hubs/updates",
		help="SignalR hub URL exposed by the WebAPI server.",
	)
	parser.add_argument(
		"--events",
		nargs="+",
		default=["ReceiveMessage"],
		help="Hub event names to subscribe to.",
	)
	parser.add_argument(
		"--access-token",
		default=None,
		help="Optional bearer token for authenticated hubs.",
	)
	parser.add_argument(
		"--log-level",
		default="INFO",
		choices=["DEBUG", "INFO", "WARNING", "ERROR", "CRITICAL"],
		help="Logging level.",
	)
	return parser.parse_args()


def main() -> int:
	args = _parse_args()
	logging.basicConfig(
		level=getattr(logging, args.log_level),
		format="%(asctime)s | %(levelname)s | %(message)s",
	)

	receiver = SignalRReceiver(
		hub_url=args.hub_url,
		events=args.events,
		access_token=args.access_token,
	)

	def _shutdown(_signum: int, _frame: Any) -> None:
		receiver.stop()

	signal.signal(signal.SIGINT, _shutdown)
	if hasattr(signal, "SIGTERM"):
		signal.signal(signal.SIGTERM, _shutdown)

	receiver.start()

	try:
		while True:
			time.sleep(1)
	except KeyboardInterrupt:
		pass
	finally:
		receiver.stop()

	return 0


if __name__ == "__main__":
	sys.exit(main())
