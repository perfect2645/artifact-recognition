"""HTTP client for pushing artifact recognition results back to WebAPI."""

from __future__ import annotations

import json
from typing import Any
from urllib import request


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

        req = request.Request(self.result_url, data=body, headers=headers, method="POST")
        with request.urlopen(req) as response:
            if response.status >= 400:
                raise RuntimeError(f"WebAPI returned status {response.status}")
