from __future__ import annotations

import argparse
import sys
from collections import Counter, defaultdict
from pathlib import Path
from typing import Dict

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from training.dataset import load_jsonl_dataset, save_json


def train_behavior_clone(dataset_path: str, output_path: str) -> int:
    records = load_jsonl_dataset(dataset_path)
    if not records:
        raise RuntimeError("Dataset is empty.")

    grouped: Dict[str, Counter] = defaultdict(Counter)
    global_counter: Counter = Counter()

    for record in records:
        observation_key = record.get("observation_key")
        action_id = record.get("action_id")
        if observation_key is None or action_id is None:
            continue
        grouped[str(observation_key)][int(action_id)] += 1
        global_counter[int(action_id)] += 1

    policy_table = {
        key: counter.most_common(1)[0][0]
        for key, counter in grouped.items()
        if counter
    }
    default_action = global_counter.most_common(1)[0][0]

    payload = {
        "type": "tabular_behavior_clone",
        "dataset_path": dataset_path,
        "state_count": len(policy_table),
        "sample_count": len(records),
        "default_action_id": default_action,
        "policy_table": policy_table,
    }
    save_json(output_path, payload)
    print(f"saved policy with {len(policy_table)} states to {output_path}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Train a lightweight tabular behavior cloning policy.")
    parser.add_argument("--dataset", default=str(Path("Tools/python/replays/battle_rule_rollouts.jsonl")))
    parser.add_argument("--output", default=str(Path("Tools/python/checkpoints/battle_bc_policy.json")))
    args = parser.parse_args()
    return train_behavior_clone(args.dataset, args.output)


if __name__ == "__main__":
    raise SystemExit(main())
