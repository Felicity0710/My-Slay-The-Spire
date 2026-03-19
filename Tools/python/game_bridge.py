import json
import os
import socket
from typing import Any, Dict, Optional


class GameBridgeClient:
    def __init__(
        self,
        host: str = "127.0.0.1",
        port: Optional[int] = None,
        timeout: float = 10.0,
        default_fast_mode: bool = True,
    ) -> None:
        self.host = host
        self.port = port if port is not None else int(os.environ.get("SLAY_THE_HS_BRIDGE_PORT", "47077"))
        self.timeout = timeout
        self.default_fast_mode = default_fast_mode

    def get_snapshot(self, *, fast_mode: Optional[bool] = None) -> Dict[str, Any]:
        payload: Dict[str, Any] = {"command": "get_snapshot"}
        if fast_mode is not None:
            payload["fastMode"] = fast_mode
        return self._send(payload)

    def execute_action(
        self,
        kind: str,
        *,
        expected_state_version: Optional[int] = None,
        fast_mode: Optional[bool] = None,
        **action_fields: Any,
    ) -> Dict[str, Any]:
        payload: Dict[str, Any] = {
            "command": "execute_action",
            "action": {
                "kind": kind,
            },
            "fastMode": self.default_fast_mode if fast_mode is None else fast_mode,
        }
        if expected_state_version is not None:
            payload["expectedStateVersion"] = expected_state_version

        for key, value in action_fields.items():
            if value is not None:
                payload["action"][key] = value

        return self._send(payload)

    def _send(self, payload: Dict[str, Any]) -> Dict[str, Any]:
        body = json.dumps(payload, ensure_ascii=False) + "\n"
        with socket.create_connection((self.host, self.port), timeout=self.timeout) as sock:
            sock.sendall(body.encode("utf-8"))
            received = self._readline(sock)

        if not received:
            raise RuntimeError("Game bridge returned an empty response.")

        return json.loads(received)

    def _readline(self, sock: socket.socket) -> str:
        chunks = bytearray()
        while True:
            piece = sock.recv(4096)
            if not piece:
                break
            chunks.extend(piece)
            if b"\n" in piece:
                break

        return chunks.decode("utf-8").strip()
