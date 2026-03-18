from __future__ import annotations

import argparse
import math
import random
import sys
from pathlib import Path
from typing import Any, Dict, List

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from encoding.action_space import ACTION_DIM
from training.dataset import load_jsonl_dataset, save_json
from training.nn_policy import TinyPolicyNetwork, shuffled_indices, softmax


def build_samples(records: List[Dict]) -> List[Dict]:
    samples: List[Dict] = []
    for record in records:
        observation = record.get("observation")
        action_id = record.get("action_id")
        action_mask = record.get("action_mask")
        if not isinstance(observation, list) or action_id is None or not isinstance(action_mask, list):
            continue
        if len(action_mask) != ACTION_DIM:
            continue
        action_id = int(action_id)
        if action_id < 0 or action_id >= ACTION_DIM or not action_mask[action_id]:
            continue
        samples.append(
            {
                "observation": [float(value) for value in observation],
                "action_id": action_id,
                "action_mask": [1 if value else 0 for value in action_mask],
            }
        )
    return samples


def split_samples(samples: List[Dict], seed: int, valid_ratio: float) -> tuple[List[Dict], List[Dict]]:
    shuffled = list(samples)
    random.Random(seed).shuffle(shuffled)
    split_index = max(1, int(len(shuffled) * (1.0 - valid_ratio)))
    train_samples = shuffled[:split_index]
    valid_samples = shuffled[split_index:] or shuffled[: min(128, len(shuffled))]
    return train_samples, valid_samples


def accuracy(network: TinyPolicyNetwork, samples: List[Dict]) -> float:
    if not samples:
        return 0.0
    correct = 0
    for sample in samples:
        prediction = network.predict_action(sample["observation"], sample["action_mask"])
        if prediction == sample["action_id"]:
            correct += 1
    return correct / len(samples)


def average_cross_entropy(network: TinyPolicyNetwork, samples: List[Dict]) -> float:
    if not samples:
        return 0.0
    total = 0.0
    for sample in samples:
        logits = network.forward(sample["observation"])["logits"]
        probabilities = softmax(logits, sample["action_mask"])
        target_prob = max(probabilities[sample["action_id"]], 1e-8)
        total += -math.log(target_prob)
    return total / len(samples)


def build_payload(
    network: TinyPolicyNetwork,
    *,
    dataset_path: str,
    sample_count: int,
    train_count: int,
    valid_count: int,
    learning_rate: float,
    epochs_requested: int,
    epochs_trained: int,
    seed: int,
    hidden_dim2: int,
    final_train_accuracy: float,
    final_valid_accuracy: float,
    final_valid_loss: float,
    best_epoch: int,
) -> Dict[str, Any]:
    payload = network.to_payload()
    payload.update(
        {
            "dataset_path": dataset_path,
            "sample_count": sample_count,
            "train_count": train_count,
            "valid_count": valid_count,
            "learning_rate": learning_rate,
            "epochs_requested": epochs_requested,
            "epochs_trained": epochs_trained,
            "seed": seed,
            "hidden_dim2": hidden_dim2,
            "final_train_accuracy": final_train_accuracy,
            "final_valid_accuracy": final_valid_accuracy,
            "final_valid_loss": final_valid_loss,
            "best_epoch": best_epoch,
        }
    )
    return payload


def train_single_run(
    samples: List[Dict],
    *,
    dataset_path: str,
    hidden_dim: int,
    hidden_dim2: int,
    epochs: int,
    learning_rate: float,
    seed: int,
    patience: int,
    min_delta: float,
    valid_ratio: float,
) -> Dict[str, Any]:
    if not samples:
        raise RuntimeError("Dataset is empty or contains no valid battle samples.")

    input_dim = len(samples[0]["observation"])
    train_samples, valid_samples = split_samples(samples, seed, valid_ratio)

    network = TinyPolicyNetwork(
        input_dim=input_dim,
        hidden_dim=hidden_dim,
        hidden_dim2=hidden_dim2,
        output_dim=ACTION_DIM,
        seed=seed,
    )

    best_payload: Dict[str, Any] | None = None
    best_valid_acc = float("-inf")
    best_valid_loss = float("inf")
    best_epoch = 0
    wait = 0

    for epoch in range(epochs):
        total_loss = 0.0
        count = 0
        for index in shuffled_indices(len(train_samples), seed + epoch):
            sample = train_samples[index]
            loss = network.train_step(
                observation=sample["observation"],
                target_action_id=sample["action_id"],
                action_mask=sample["action_mask"],
                learning_rate=learning_rate,
            )
            total_loss += loss
            count += 1

        avg_train_loss = total_loss / max(count, 1)
        train_acc = accuracy(network, train_samples)
        valid_acc = accuracy(network, valid_samples)
        valid_loss = average_cross_entropy(network, valid_samples)
        print(
            f"seed {seed} epoch {epoch + 1}/{epochs}:",
            f"train_loss={avg_train_loss:.4f}",
            f"train_acc={train_acc:.3f}",
            f"valid_acc={valid_acc:.3f}",
            f"valid_loss={valid_loss:.4f}",
        )

        improved_acc = valid_acc > best_valid_acc + min_delta
        improved_loss = abs(valid_acc - best_valid_acc) <= min_delta and valid_loss < best_valid_loss - 1e-6
        if improved_acc or improved_loss or best_payload is None:
            best_valid_acc = valid_acc
            best_valid_loss = valid_loss
            best_epoch = epoch + 1
            best_payload = build_payload(
                network,
                dataset_path=dataset_path,
                sample_count=len(samples),
                train_count=len(train_samples),
                valid_count=len(valid_samples),
                learning_rate=learning_rate,
                epochs_requested=epochs,
                epochs_trained=epoch + 1,
                seed=seed,
                hidden_dim2=hidden_dim2,
                final_train_accuracy=train_acc,
                final_valid_accuracy=valid_acc,
                final_valid_loss=valid_loss,
                best_epoch=best_epoch,
            )
            wait = 0
        else:
            wait += 1
            if wait >= patience:
                print(f"seed {seed}: early stop at epoch {epoch + 1}, best_epoch={best_epoch}")
                break

    assert best_payload is not None
    return {
        "seed": seed,
        "best_epoch": best_epoch,
        "best_valid_accuracy": best_valid_acc,
        "best_valid_loss": best_valid_loss,
        "payload": best_payload,
    }


def parse_seeds(seed: int, seeds_text: str) -> List[int]:
    if not seeds_text.strip():
        return [seed]
    parsed = [int(item.strip()) for item in seeds_text.split(",") if item.strip()]
    return parsed or [seed]


def checkpoint_path_for_seed(output_path: str, seed: int, multi_run: bool) -> str:
    if not multi_run:
        return output_path
    path = Path(output_path)
    return str(path.with_name(f"{path.stem}.seed{seed}{path.suffix}"))


def train_neural_behavior_clone(
    dataset_path: str,
    output_path: str,
    *,
    hidden_dim: int,
    hidden_dim2: int,
    epochs: int,
    learning_rate: float,
    seed: int,
    seeds_text: str,
    patience: int,
    min_delta: float,
    valid_ratio: float,
) -> int:
    records = load_jsonl_dataset(dataset_path)
    samples = build_samples(records)
    if not samples:
        raise RuntimeError("Dataset is empty or contains no valid battle samples.")

    seeds = parse_seeds(seed, seeds_text)
    results: List[Dict[str, Any]] = []
    for current_seed in seeds:
        result = train_single_run(
            samples,
            dataset_path=dataset_path,
            hidden_dim=hidden_dim,
            hidden_dim2=hidden_dim2,
            epochs=epochs,
            learning_rate=learning_rate,
            seed=current_seed,
            patience=patience,
            min_delta=min_delta,
            valid_ratio=valid_ratio,
        )
        results.append(result)
        seed_output = checkpoint_path_for_seed(output_path, current_seed, len(seeds) > 1)
        save_json(seed_output, result["payload"])
        print(
            f"saved seed {current_seed} checkpoint to {seed_output}",
            f"(valid_acc={result['best_valid_accuracy']:.3f}, valid_loss={result['best_valid_loss']:.4f})",
        )

    best_result = max(results, key=lambda item: (item["best_valid_accuracy"], -item["best_valid_loss"]))
    best_payload = dict(best_result["payload"])
    best_payload["candidate_seeds"] = seeds
    best_payload["selected_seed"] = best_result["seed"]
    best_payload["selection_metric"] = {
        "best_valid_accuracy": best_result["best_valid_accuracy"],
        "best_valid_loss": best_result["best_valid_loss"],
    }
    best_payload["all_results"] = [
        {
            "seed": item["seed"],
            "best_epoch": item["best_epoch"],
            "best_valid_accuracy": item["best_valid_accuracy"],
            "best_valid_loss": item["best_valid_loss"],
        }
        for item in results
    ]
    save_json(output_path, best_payload)
    print(
        f"saved best neural BC policy to {output_path}",
        f"(seed={best_result['seed']}, valid_acc={best_result['best_valid_accuracy']:.3f}, valid_loss={best_result['best_valid_loss']:.4f})",
    )
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Train a tiny neural behavior-cloning policy without third-party deps.")
    parser.add_argument("--dataset", default=str(Path("Tools/python/replays/battle_rule_rollouts.jsonl")))
    parser.add_argument("--output", default=str(Path("Tools/python/checkpoints/battle_nn_bc_policy.json")))
    parser.add_argument("--hidden", type=int, default=64)
    parser.add_argument("--hidden2", type=int, default=0)
    parser.add_argument("--epochs", type=int, default=8)
    parser.add_argument("--lr", type=float, default=0.03)
    parser.add_argument("--seed", type=int, default=7)
    parser.add_argument("--seeds", default="", help="Comma-separated seed list. When set, trains all seeds and saves the best model.")
    parser.add_argument("--patience", type=int, default=4)
    parser.add_argument("--min-delta", type=float, default=0.002)
    parser.add_argument("--valid-ratio", type=float, default=0.1)
    args = parser.parse_args()
    return train_neural_behavior_clone(
        dataset_path=args.dataset,
        output_path=args.output,
        hidden_dim=args.hidden,
        hidden_dim2=args.hidden2,
        epochs=args.epochs,
        learning_rate=args.lr,
        seed=args.seed,
        seeds_text=args.seeds,
        patience=args.patience,
        min_delta=args.min_delta,
        valid_ratio=args.valid_ratio,
    )


if __name__ == "__main__":
    raise SystemExit(main())
