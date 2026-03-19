from __future__ import annotations

import argparse
import sys
from pathlib import Path
from typing import Dict

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from game_bridge import GameBridgeClient
from training.policy_runtime import choose_tabular_action, load_tabular_policy


def main() -> int:
    parser = argparse.ArgumentParser(description="Run a tabular behavior cloning policy against battle-test mode.")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=47077)
    parser.add_argument("--fast-mode", action=argparse.BooleanOptionalAction, default=True)
    parser.add_argument("--policy", default=str(Path("Tools/python/checkpoints/battle_bc_policy.json")))
    parser.add_argument("--steps", type=int, default=200)
    args = parser.parse_args()

    policy = load_tabular_policy(args.policy)

    client = GameBridgeClient(host=args.host, port=args.port, default_fast_mode=args.fast_mode)
    result = client.execute_action("start_battle_test_run", fast_mode=args.fast_mode)
    snapshot = result.get("snapshot")
    if not snapshot:
        raise RuntimeError("Missing snapshot on reset.")

    for step in range(args.steps):
        action = choose_tabular_action(policy, snapshot)
        if action is None:
            legal_actions = snapshot.get("legalActions") or []
            if not legal_actions:
                print("no legal action, stop")
                return 0
            first = legal_actions[0]
            action = {"kind": first.get("kind")}
            action.update(first.get("parameters") or {})

        print(f"bc step {step}: {action}")
        response = client.execute_action(
            action["kind"],
            expected_state_version=snapshot.get("stateVersion"),
            fast_mode=args.fast_mode,
            **{key: value for key, value in action.items() if key != "kind"},
        )
        if not response.get("ok"):
            if "State version mismatch" in str(response.get("message")) and response.get("snapshot"):
                snapshot = response["snapshot"]
                continue
            raise RuntimeError(f"Bridge action failed: {response.get('message')}")
        snapshot = response.get("snapshot")
        if not snapshot:
            raise RuntimeError("Missing snapshot after step.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
