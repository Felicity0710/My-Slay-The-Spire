from __future__ import annotations

import argparse
import sys
from pathlib import Path
from typing import Dict, List

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from encoding.action_space import build_action_mask, encode_action
from encoding.observation import encode_battle_observation, hash_battle_observation
from encoding.reward import compute_battle_only_reward
from game_bridge import GameBridgeClient
from simple_bot import choose_action
from training.dataset import save_jsonl


def collect_rollouts(
    episodes: int,
    max_steps: int,
    output_path: str,
    *,
    host: str,
    port: int,
    fast_mode: bool,
) -> int:
    client = GameBridgeClient(host=host, port=port, default_fast_mode=fast_mode)
    all_records: List[Dict] = []

    for episode in range(episodes):
        response = client.execute_action("start_battle_test_run", fast_mode=fast_mode)
        snapshot = response.get("snapshot")
        if not snapshot:
            raise RuntimeError("Missing snapshot on reset.")

        for step in range(max_steps):
            action = choose_action(snapshot)
            if action is None:
                break

            record_index = None
            if snapshot.get("sceneType") == "battle":
                encoded = encode_battle_observation(snapshot)
                action_id = encode_action(action)
                if action_id is not None:
                    all_records.append(
                        {
                            "episode": episode,
                            "step": step,
                            "observation": encoded,
                            "observation_key": hash_battle_observation(encoded),
                            "action_id": action_id,
                            "action": action,
                            "action_mask": build_action_mask(snapshot),
                            "reward": 0.0,
                            "scene_type": snapshot.get("sceneType"),
                            "terminal": False,
                        }
                    )
                    record_index = len(all_records) - 1

            previous_snapshot = snapshot
            result = client.execute_action(
                action["kind"],
                expected_state_version=snapshot.get("stateVersion"),
                fast_mode=fast_mode,
                **{key: value for key, value in action.items() if key != "kind"},
            )
            if not result.get("ok"):
                if "State version mismatch" in str(result.get("message")) and result.get("snapshot"):
                    snapshot = result["snapshot"]
                    continue
                raise RuntimeError(f"Bridge action failed: {result.get('message')}")

            snapshot = result.get("snapshot")
            if not snapshot:
                raise RuntimeError("Missing snapshot after step.")

            if record_index is not None:
                all_records[record_index]["reward"] = compute_battle_only_reward(previous_snapshot, snapshot)
                all_records[record_index]["terminal"] = bool(snapshot.get("run", {}).get("playerHp", 1) <= 0)

            if snapshot.get("run", {}).get("playerHp", 1) <= 0:
                break

        print(f"rollout episode {episode} done")

    save_jsonl(output_path, all_records)
    print(f"saved {len(all_records)} battle samples to {output_path}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Collect battle-only rollout samples using the rule bot.")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=47077)
    parser.add_argument("--fast-mode", action=argparse.BooleanOptionalAction, default=True)
    parser.add_argument("--episodes", type=int, default=20)
    parser.add_argument("--steps", type=int, default=200)
    parser.add_argument("--output", default=str(Path("Tools/python/replays/battle_rule_rollouts.jsonl")))
    args = parser.parse_args()
    return collect_rollouts(
        args.episodes,
        args.steps,
        args.output,
        host=args.host,
        port=args.port,
        fast_mode=args.fast_mode,
    )


if __name__ == "__main__":
    raise SystemExit(main())
