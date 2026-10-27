
import logging
from collections.abc import Callable
from threading import Event
from urllib.error import URLError


LOGGER = logging.getLogger(__name__)

def start_with_retry(
    start: Callable[[], None],
    shutdown_requested: Event,
) -> bool:
    while not shutdown_requested.is_set():
        try:
            start()
        except URLError as exc:
            if not isinstance(exc.reason, ConnectionRefusedError):
                raise

            LOGGER.warning("Connection refused, retrying in 5 seconds... %s", exc.reason)
            shutdown_requested.wait(5)
        else:
            return True

    return False