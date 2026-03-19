from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path
from typing import List

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from training.dataset import load_jsonl_dataset, save_jsonl


def split_counts(total: int, workers: int) -> List[int]:
    base = total // workers
    remainder = total % workers
    return [base + (1 if index < remainder else 0) for index in range(workers)]


def run_parallel_rollout(
    python_exe: str,
    ports: List[int],
    episodes: int,
    steps: int,
    output: str,
    fast_mode: bool,
) -> int:
    worker_counts = split_counts(episodes, len(ports))
    temp_outputs: List[Path] = []
    processes: List[subprocess.Popen[str]] = []

    try:
        for index, (port, worker_episodes) in enumerate(zip(ports, worker_counts)):
            if worker_episodes <= 0:
                continue

            temp_output = Path(output).with_name(f"{Path(output).stem}.worker{index}.jsonl")
            temp_outputs.append(temp_output)
            command = [
                python_exe,
                str(Path("Tools/python/training/rollout.py")),
                "--host",
                "127.0.0.1",
                "--port",
                str(port),
                "--episodes",
                str(worker_episodes),
                "--steps",
                str(steps),
                "--fast-mode" if fast_mode else "--no-fast-mode",
                "--output",
                str(temp_output),
            ]
            processes.append(subprocess.Popen(command, cwd=ROOT.parent, text=True))

        exit_codes = [process.wait() for process in processes]
        if any(code != 0 for code in exit_codes):
            raise RuntimeError(f"One or more rollout workers failed: {exit_codes}")

        merged = []
        for temp_output in temp_outputs:
            if temp_output.exists():
                merged.extend(load_jsonl_dataset(temp_output))
        save_jsonl(output, merged)
        print(f"merged {len(merged)} samples from {len(temp_outputs)} workers into {output}")
        return 0
    finally:
        for temp_output in temp_outputs:
            if temp_output.exists():
                temp_output.unlink()


def main() -> int:
    parser = argparse.ArgumentParser(description="Collect rollout data from multiple running game instances in parallel.")
    parser.add_argument("--python", default=sys.executable)
    parser.add_argument("--ports", required=True, help="Comma-separated bridge ports, e.g. 47077,47078")
    parser.add_argument("--episodes", type=int, default=60)
    parser.add_argument("--steps", type=int, default=200)
    parser.add_argument("--fast-mode", action=argparse.BooleanOptionalAction, default=True)
    parser.add_argument("--output", default=str(Path("Tools/python/replays/battle_rule_rollouts_parallel.jsonl")))
    args = parser.parse_args()

    ports = [int(item.strip()) for item in args.ports.split(",") if item.strip()]
    if not ports:
        raise RuntimeError("At least one port is required.")

    return run_parallel_rollout(args.python, ports, args.episodes, args.steps, args.output, args.fast_mode)


if __name__ == "__main__":
    raise SystemExit(main())
