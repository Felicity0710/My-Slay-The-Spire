import json
import socket
from typing import Any, Dict, Optional


class GameBridgeClient:
    def __init__(self, host: str = "127.0.0.1", port: int = 47077, timeout: float = 10.0) -> None:
        self.host = host
        self.port = port
        self.timeout = timeout

    def get_snapshot(self) -> Dict[str, Any]:
        return self._send({"command": "get_snapshot"})

    def execute_action(
        self,
        kind: str,
        *,
        expected_state_version: Optional[int] = None,
        **action_fields: Any,
    ) -> Dict[str, Any]:
        payload: Dict[str, Any] = {
            "command": "execute_action",
            "action": {
                "kind": kind,
            },
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
