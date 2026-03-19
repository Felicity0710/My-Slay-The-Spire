from __future__ import annotations

import argparse
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from game_bridge import GameBridgeClient
from training.policy_runtime import choose_policy_action
from training.ppo_lite_policy import TinyActorCritic


def main() -> int:
    parser = argparse.ArgumentParser(description="Run a PPO-lite policy against battle-test mode.")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=47077)
    parser.add_argument("--fast-mode", action=argparse.BooleanOptionalAction, default=True)
    parser.add_argument("--policy", default=str(Path("Tools/python/checkpoints/battle_ppo_lite_policy.json")))
    parser.add_argument("--steps", type=int, default=200)
    args = parser.parse_args()

    import json

    with Path(args.policy).open("r", encoding="utf-8") as handle:
        payload = json.load(handle)
    network = TinyActorCritic.from_payload(payload)

    client = GameBridgeClient(host=args.host, port=args.port, default_fast_mode=args.fast_mode)
    result = client.execute_action("start_battle_test_run", fast_mode=args.fast_mode)
    snapshot = result.get("snapshot")
    if not snapshot:
        raise RuntimeError("Missing snapshot on reset.")

    for step in range(args.steps):
        if snapshot.get("sceneType") == "map" and (snapshot.get("run") or {}).get("battlesWon", 0) > 0:
            print("ppo-lite stop: battle-test run finished and returned to map")
            return 0

        action = choose_policy_action("ppo", snapshot, ppo_policy=network)
        if action is None:
            print("ppo-lite stop: no action")
            return 0

        print(f"ppo-lite step {step}: {action}")
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
