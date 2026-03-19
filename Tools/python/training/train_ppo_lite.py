from __future__ import annotations

import argparse
import random
import sys
from pathlib import Path
from typing import Any, Dict, List

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from encoding.action_space import ACTION_DIM, build_action_mask, decode_action, choose_first_legal_decoded
from encoding.observation import encode_battle_observation
from encoding.reward import compute_battle_only_reward
from game_bridge import GameBridgeClient
from simple_bot import choose_action as choose_rule_action
from training.dataset import save_json
from training.nn_policy import TinyPolicyNetwork
from training.ppo_lite_policy import TinyActorCritic, compute_gae, normalize


def fallback_action(snapshot: Dict[str, Any]) -> Dict[str, Any] | None:
    fallback = choose_first_legal_decoded(snapshot)
    if fallback is not None:
        return fallback[1]
    return choose_rule_action(snapshot)


def collect_episode(
    client: GameBridgeClient,
    network: TinyActorCritic,
    rng: random.Random,
    max_steps: int,
    fast_mode: bool,
) -> Dict[str, Any]:
    response = client.execute_action("start_battle_test_run", fast_mode=fast_mode)
    snapshot = response.get("snapshot")
    if not snapshot:
        raise RuntimeError("Missing snapshot on reset.")

    records: List[Dict[str, Any]] = []
    battle_actions = 0

    for step in range(max_steps):
        run = snapshot.get("run") or {}
        scene_type = snapshot.get("sceneType")
        if scene_type == "map" and run.get("battlesWon", 0) > 0:
            break
        if run.get("playerHp", 0) <= 0:
            break

        if scene_type == "battle":
            observation = encode_battle_observation(snapshot)
            action_mask = build_action_mask(snapshot)
            action_id, probability, value = network.sample_action(observation, action_mask, rng)
            action = decode_action(action_id, snapshot) if action_id is not None else None
            if action is None:
                action = fallback_action(snapshot)
            record_index = len(records)
            records.append(
                {
                    "observation": observation,
                    "action_mask": action_mask,
                    "action_id": action_id,
                    "old_probability": probability,
                    "value": value,
                    "reward": 0.0,
                    "done": False,
                }
            )
            battle_actions += 1
        else:
            action = choose_rule_action(snapshot)
            record_index = None

        if action is None:
            break

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

        if record_index is not None and records[record_index]["action_id"] is not None:
            reward = compute_battle_only_reward(previous_snapshot, snapshot)
            done = bool((snapshot.get("run") or {}).get("playerHp", 0) <= 0 or snapshot.get("sceneType") != "battle")
            records[record_index]["reward"] = reward
            records[record_index]["done"] = done

    end_run = snapshot.get("run") or {}
    return {
        "records": records,
        "won": (snapshot.get("sceneType") == "map" and end_run.get("battlesWon", 0) > 0),
        "end_hp": end_run.get("playerHp", 0),
        "battle_actions": battle_actions,
    }


def train_ppo_lite(
    output_path: str,
    *,
    init_policy_path: str,
    host: str,
    port: int,
    fast_mode: bool,
    epochs: int,
    episodes_per_epoch: int,
    max_steps: int,
    learning_rate: float,
    clip_epsilon: float,
    gamma: float,
    gae_lambda: float,
    hidden_dim: int,
    seed: int,
) -> int:
    rng = random.Random(seed)
    init_payload = Path(init_policy_path)
    if init_payload.exists():
        bc_policy = TinyPolicyNetwork.from_payload(__import__("json").loads(init_payload.read_text(encoding="utf-8")))
        network = TinyActorCritic.from_bc_policy(bc_policy)
    else:
        network = TinyActorCritic(input_dim=10 + 3 * 9 + 10 * 10, hidden_dim=hidden_dim, output_dim=ACTION_DIM, seed=seed)

    client = GameBridgeClient(host=host, port=port, default_fast_mode=fast_mode)

    for epoch in range(epochs):
        batch: List[Dict[str, Any]] = []
        wins = 0
        end_hps: List[float] = []
        battle_actions = 0

        for _ in range(episodes_per_epoch):
            episode = collect_episode(client, network, rng, max_steps, fast_mode)
            records = episode["records"]
            if not records:
                continue
            rewards = [float(record["reward"]) for record in records]
            values = [float(record["value"]) for record in records]
            dones = [bool(record["done"]) for record in records]
            advantages, returns = compute_gae(rewards, values, dones, gamma, gae_lambda)
            advantages = normalize(advantages)

            for index, record in enumerate(records):
                if record["action_id"] is None:
                    continue
                batch.append(
                    {
                        "observation": record["observation"],
                        "action_mask": record["action_mask"],
                        "action_id": int(record["action_id"]),
                        "old_probability": float(record["old_probability"]),
                        "advantage": float(advantages[index]),
                        "target_return": float(returns[index]),
                    }
                )

            wins += 1 if episode["won"] else 0
            end_hps.append(float(episode["end_hp"]))
            battle_actions += int(episode["battle_actions"])

        if not batch:
            raise RuntimeError("No PPO samples were collected.")

        total_policy_loss = 0.0
        total_value_loss = 0.0
        for _ in range(3):
            rng.shuffle(batch)
            for sample in batch:
                result = network.ppo_update_step(
                    observation=sample["observation"],
                    action_id=sample["action_id"],
                    action_mask=sample["action_mask"],
                    old_probability=sample["old_probability"],
                    advantage=sample["advantage"],
                    target_return=sample["target_return"],
                    learning_rate=learning_rate,
                    clip_epsilon=clip_epsilon,
                    value_coef=0.5,
                )
                total_policy_loss += result["policy_loss"]
                total_value_loss += result["value_loss"]

        avg_policy_loss = total_policy_loss / (len(batch) * 3)
        avg_value_loss = total_value_loss / (len(batch) * 3)
        avg_end_hp = sum(end_hps) / max(len(end_hps), 1)
        win_rate = wins / max(episodes_per_epoch, 1)
        print(
            f"ppo epoch {epoch + 1}/{epochs}:",
            f"win_rate={win_rate:.3f}",
            f"avg_end_hp={avg_end_hp:.2f}",
            f"battle_actions={battle_actions}",
            f"policy_loss={avg_policy_loss:.4f}",
            f"value_loss={avg_value_loss:.4f}",
        )

    payload = network.to_payload()
    payload.update(
        {
            "init_policy_path": init_policy_path,
            "epochs": epochs,
            "episodes_per_epoch": episodes_per_epoch,
            "learning_rate": learning_rate,
            "clip_epsilon": clip_epsilon,
            "gamma": gamma,
            "gae_lambda": gae_lambda,
            "seed": seed,
        }
    )
    save_json(output_path, payload)
    print(f"saved PPO-lite policy to {output_path}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Train a zero-dependency PPO-lite policy for battle-only play.")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=47077)
    parser.add_argument("--fast-mode", action=argparse.BooleanOptionalAction, default=True)
    parser.add_argument("--output", default=str(Path("Tools/python/checkpoints/battle_ppo_lite_policy.json")))
    parser.add_argument("--init-policy", default=str(Path("Tools/python/checkpoints/battle_nn_bc_policy.json")))
    parser.add_argument("--epochs", type=int, default=8)
    parser.add_argument("--episodes-per-epoch", type=int, default=12)
    parser.add_argument("--steps", type=int, default=200)
    parser.add_argument("--lr", type=float, default=0.01)
    parser.add_argument("--clip", type=float, default=0.2)
    parser.add_argument("--gamma", type=float, default=0.99)
    parser.add_argument("--gae-lambda", type=float, default=0.95)
    parser.add_argument("--hidden", type=int, default=64)
    parser.add_argument("--seed", type=int, default=13)
    args = parser.parse_args()
    return train_ppo_lite(
        output_path=args.output,
        init_policy_path=args.init_policy,
        host=args.host,
        port=args.port,
        fast_mode=args.fast_mode,
        epochs=args.epochs,
        episodes_per_epoch=args.episodes_per_epoch,
        max_steps=args.steps,
        learning_rate=args.lr,
        clip_epsilon=args.clip,
        gamma=args.gamma,
        gae_lambda=args.gae_lambda,
        hidden_dim=args.hidden,
        seed=args.seed,
    )


if __name__ == "__main__":
    raise SystemExit(main())
