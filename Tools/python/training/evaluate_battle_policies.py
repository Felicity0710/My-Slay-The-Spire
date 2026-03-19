from __future__ import annotations

import argparse
import json
import statistics
import sys
from pathlib import Path
from typing import Any, Dict, List

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from game_bridge import GameBridgeClient
from training.policy_runtime import choose_policy_action, load_nn_policy, load_ppo_policy, load_tabular_policy


def run_episode(
    client: GameBridgeClient,
    policy_kind: str,
    *,
    max_steps: int,
    fast_mode: bool,
    battle_seed: int | None,
    tabular_policy: Dict | None,
    nn_policy: Any | None,
    ppo_policy: Any | None,
) -> Dict[str, Any]:
    result = client.execute_action("start_battle_test_run", fast_mode=fast_mode, seed=battle_seed)
    snapshot = result.get("snapshot")
    if not snapshot:
        raise RuntimeError("Missing snapshot on reset.")

    start_hp = (snapshot.get("run") or {}).get("playerHp", 0)
    reward_type = None

    for step in range(max_steps):
        scene_type = snapshot.get("sceneType")
        run = snapshot.get("run") or {}

        if scene_type == "map" and run.get("battlesWon", 0) > 0:
            return {
                "won": True,
                "steps": step,
                "seed": battle_seed,
                "start_hp": start_hp,
                "end_hp": run.get("playerHp", 0),
                "reward_type": reward_type,
            }

        if run.get("playerHp", 0) <= 0:
            return {
                "won": False,
                "steps": step,
                "seed": battle_seed,
                "start_hp": start_hp,
                "end_hp": 0,
                "reward_type": reward_type,
            }

        action = choose_policy_action(
            policy_kind,
            snapshot,
            tabular_policy=tabular_policy,
            nn_policy=nn_policy,
            ppo_policy=ppo_policy,
        )
        if action is None:
            return {
                "won": False,
                "steps": step,
                "seed": battle_seed,
                "start_hp": start_hp,
                "end_hp": run.get("playerHp", 0),
                "reward_type": reward_type,
                "stopped": True,
            }

        if action.get("kind") == "choose_reward_type":
            reward_type = action.get("rewardType")

        response = client.execute_action(
            action["kind"],
            expected_state_version=snapshot.get("stateVersion"),
            fast_mode=fast_mode,
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

    run = snapshot.get("run") or {}
    return {
        "won": False,
        "steps": max_steps,
        "seed": battle_seed,
        "start_hp": start_hp,
        "end_hp": run.get("playerHp", 0),
        "reward_type": reward_type,
        "truncated": True,
    }


def summarize(results: List[Dict[str, Any]]) -> Dict[str, Any]:
    wins = [1 if result.get("won") else 0 for result in results]
    steps = [int(result.get("steps", 0)) for result in results]
    end_hps = [float(result.get("end_hp", 0)) for result in results]

    reward_counts: Dict[str, int] = {}
    for result in results:
        reward_type = result.get("reward_type")
        if not reward_type:
            continue
        reward_counts[str(reward_type)] = reward_counts.get(str(reward_type), 0) + 1

    return {
        "episodes": len(results),
        "win_rate": sum(wins) / max(len(wins), 1),
        "avg_steps": statistics.fmean(steps) if steps else 0.0,
        "avg_end_hp": statistics.fmean(end_hps) if end_hps else 0.0,
        "reward_type_counts": reward_counts,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Evaluate battle policies on repeated battle-test runs.")
    parser.add_argument("--policy", choices=["rule", "tabular", "nn", "ppo"], default="rule")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=47077)
    parser.add_argument("--fast-mode", action=argparse.BooleanOptionalAction, default=True)
    parser.add_argument("--episodes", type=int, default=20)
    parser.add_argument("--steps", type=int, default=200)
    parser.add_argument("--seed-base", type=int, default=None, help="Use deterministic battle-test seeds starting from this value.")
    parser.add_argument("--tabular-path", default=str(Path("Tools/python/checkpoints/battle_bc_policy.json")))
    parser.add_argument("--nn-path", default=str(Path("Tools/python/checkpoints/battle_nn_bc_policy.json")))
    parser.add_argument("--ppo-path", default=str(Path("Tools/python/checkpoints/battle_ppo_lite_policy.json")))
    parser.add_argument("--output", default="")
    args = parser.parse_args()

    tabular_policy = load_tabular_policy(args.tabular_path) if args.policy == "tabular" else None
    nn_policy = load_nn_policy(args.nn_path) if args.policy == "nn" else None
    ppo_policy = load_ppo_policy(args.ppo_path) if args.policy == "ppo" else None

    client = GameBridgeClient(host=args.host, port=args.port, default_fast_mode=args.fast_mode)
    results: List[Dict[str, Any]] = []

    for episode in range(args.episodes):
        battle_seed = None if args.seed_base is None else args.seed_base + episode
        result = run_episode(
            client,
            args.policy,
            max_steps=args.steps,
            fast_mode=args.fast_mode,
            battle_seed=battle_seed,
            tabular_policy=tabular_policy,
            nn_policy=nn_policy,
            ppo_policy=ppo_policy,
        )
        results.append(result)
        print(
            f"episode {episode + 1}/{args.episodes}:",
            f"won={result.get('won')}",
            f"steps={result.get('steps')}",
            f"end_hp={result.get('end_hp')}",
            f"seed={result.get('seed')}",
            f"reward={result.get('reward_type')}",
        )

    summary = summarize(results)
    print(json.dumps(summary, ensure_ascii=False, indent=2))

    if args.output:
        payload = {
            "policy": args.policy,
            "summary": summary,
            "episodes": results,
        }
        Path(args.output).parent.mkdir(parents=True, exist_ok=True)
        Path(args.output).write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"saved evaluation to {args.output}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
