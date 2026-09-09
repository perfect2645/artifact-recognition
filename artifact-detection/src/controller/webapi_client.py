"""HTTP client for pushing artifact recognition results back to WebAPI."""

from __future__ import annotations

import json
import logging
from typing import Any
from urllib import request


LOGGER = logging.getLogger(__name__)


class WebApiClient:
    def __init__(self, result_url: str, access_token: str | None = None) -> None:
        self.result_url = result_url
        self.access_token = access_token

    def post_result(self, payload: dict[str, Any]) -> None:
        body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        headers = {
            "Content-Type": "application/json; charset=utf-8",
            "Content-Length": str(len(body)),
        }
        if self.access_token:
            headers["Authorization"] = f"Bearer {self.access_token}"

        req = request.Request(self.result_url, data=body, headers=headers, method="PUT")

        LOGGER.debug("Posting result to WebAPI: url=%s, body=%s", self.result_url, body.decode("utf-8"))

        try:
            with request.urlopen(req) as response:
                response_body = response.read().decode("utf-8", errors="replace")
                if response.status >= 400:
                    raise RuntimeError(
                        f"WebAPI returned status {response.status}: {response_body}"
                    )
        except request.HTTPError as exc:
            response_body = exc.read().decode("utf-8", errors="replace")
            raise RuntimeError(
                f"WebAPI returned status {exc.code}: {response_body}"
            ) from exc
