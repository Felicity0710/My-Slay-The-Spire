# Training Guide

This guide is for a fresh clone of the repository. It covers the shortest path from setup to rollout collection, training, replay, and evaluation.

## Prerequisites

- Windows PowerShell
- .NET 8 SDK for the main Godot project and training flow
- .NET 9 SDK if you also want to run `Tools\SlayHs.Agent` as MCP or bot
- Python 3.11+ available as `python` in `PATH`
- Godot 4.5.1 Mono

Optional overrides:

```powershell
$env:SLAY_THE_HS_PYTHON = 'C:\Path\To\python.exe'
$env:SLAY_THE_HS_GODOT_EXE = 'C:\Path\To\Godot_v4.5.1-stable_mono_win64.exe'
```

If you use VS Code with the Godot Tools extension, configure the Godot executable path in your local user/workspace settings instead of committing a machine-specific path into the repo.

The Python wrappers use this priority:

1. `SLAY_THE_HS_PYTHON`
2. `python`
3. `py -3`

## Quick Start

Open a PowerShell window in the repo root:

```powershell
cd C:\path\to\slay-the-hs
```

Build the C# project once:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

This builds the main Godot C# project. It does not require the separate .NET 9 SDK unless you also plan to run the MCP sidecar in `Tools\SlayHs.Agent`.

Run the game from Godot. The game starts a local TCP bridge on `127.0.0.1:47077`.

## Verify The Bridge

After the game is running, test the control loop with a simple bot:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-python-bot.ps1
```

If the bot can act in the game, the bridge is working and training scripts can connect.

## Stage 1: Collect Rule-Bot Rollouts

Collect a first dataset:

```powershell
python .\Tools\python\training\rollout.py --episodes 50 --steps 200 --output .\Tools\python\replays\battle_rule_rollouts.jsonl
```

Expected output:

- per-episode progress logs
- a dataset file at `Tools\python\replays\battle_rule_rollouts.jsonl`

You can also use the stage-1 wrapper:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-training-stage1.ps1 -Episodes 50
```

## Stage 1: Train Tabular BC

Train the baseline behavior-cloning policy:

```powershell
python .\Tools\python\training\train_bc.py --dataset .\Tools\python\replays\battle_rule_rollouts.jsonl --output .\Tools\python\checkpoints\battle_bc_policy.json
```

Replay it:

```powershell
python .\Tools\python\training\run_bc_policy.py --policy .\Tools\python\checkpoints\battle_bc_policy.json --steps 200
```

Or run collection + tabular training + replay in one command:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-training-stage1.ps1 -Episodes 50 -Replay
```

## Neural BC

Train a small neural policy from the same rollout file:

```powershell
python .\Tools\python\training\train_nn_bc.py --dataset .\Tools\python\replays\battle_rule_rollouts.jsonl --epochs 12 --hidden 64
```

Replay it:

```powershell
python .\Tools\python\training\run_nn_bc_policy.py --policy .\Tools\python\checkpoints\battle_nn_bc_policy.json --steps 200
```

## PPO-lite Fine-Tuning

Fine-tune from the neural BC checkpoint:

```powershell
python .\Tools\python\training\train_ppo_lite.py --init-policy .\Tools\python\checkpoints\battle_nn_bc_policy.json --epochs 6 --episodes-per-epoch 10
```

Replay it:

```powershell
python .\Tools\python\training\run_ppo_lite_policy.py --policy .\Tools\python\checkpoints\battle_ppo_lite_policy.json --steps 200
```

## Evaluate Policies

Use the same seed range when comparing policies:

```powershell
python .\Tools\python\training\evaluate_battle_policies.py --policy rule --episodes 20 --seed-base 1000
python .\Tools\python\training\evaluate_battle_policies.py --policy tabular --episodes 20 --seed-base 1000
python .\Tools\python\training\evaluate_battle_policies.py --policy nn --episodes 20 --seed-base 1000
python .\Tools\python\training\evaluate_battle_policies.py --policy ppo --episodes 20 --seed-base 1000
```

You can also write a JSON report:

```powershell
python .\Tools\python\training\evaluate_battle_policies.py --policy nn --episodes 50 --seed-base 1000 --output .\Tools\python\reports\nn_eval.json
```

## Multi-Instance Sampling

The Python client already supports `SLAY_THE_HS_BRIDGE_PORT`. To collect in parallel:

1. Start one game instance on the default bridge port `47077`.
2. Start another instance configured to use `47078`.
3. Run:

```powershell
python .\Tools\python\training\parallel_rollout.py --ports 47077,47078 --episodes 60
```

## MCP Server

If you want the MCP sidecar instead of Python training, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-agent.ps1 -Mode mcp
```

`Tools\SlayHs.Agent` targets `.NET 9`, so install the .NET 9 SDK before using this command.

## Export

If `godot.exe` is not on `PATH`, point the export script at it:

```powershell
$env:SLAY_THE_HS_GODOT_EXE = 'C:\Path\To\Godot_v4.5.1-stable_mono_win64.exe'
.\Tools\export_windows_clean.bat
```

You can also pass the executable directly:

```powershell
.\Tools\export_windows_clean.bat "C:\Path\To\Godot_v4.5.1-stable_mono_win64.exe"
```

## Common Failures

- `python` not found:
  Install Python, reopen PowerShell, and re-run `python --version`.
- bridge connection refused:
  Start the game first and confirm it is running locally.
- model checkpoint not found:
  Train the matching stage first, or pass `--policy` / `--init-policy` explicitly.
- Godot export failed:
  Set `SLAY_THE_HS_GODOT_EXE` or pass the Godot executable path to the export script.
