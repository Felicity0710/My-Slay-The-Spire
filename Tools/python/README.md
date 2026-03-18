# Python Control Layer

This folder adds a pure Python layer on top of the in-game TCP bridge at `127.0.0.1:47077`.

Files:

- `game_bridge.py`: low-level request client
- `slay_env.py`: gym-style wrapper with `reset()` and `step()`
- `gym_env.py`: optional `gymnasium.Env` adapter
- `simple_bot.py`: small rule-based bot
- `example_loop.py`: tiny rollout example
- `encoding/`: observation, action, reward encoding
- `training/`: rollout collection, tabular BC training, and policy replay

## Run With Your Python

If `python` is not visible in the current terminal session, use the absolute interpreter path:

```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\simple_bot.py
```

```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\example_loop.py
```

Or use the wrapper script:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\run-python-bot.ps1
```

## Gymnasium Adapter

If you want to plug this into a standard RL stack, install `gymnasium` in your Python and use:

```python
from gym_env import GymSlayHsEnv

env = GymSlayHsEnv()
obs, info = env.reset()
action = env.sample_action()
```

## Typical Training Direction

The usual split is:

- Godot/C#: authoritative game state and action execution
- Python: data collection, bot logic, RL environment wrappers, model training

`SlayHsEnv` intentionally avoids third-party dependencies so you can later adapt it to `gymnasium` or your preferred training stack.

## First Training Stage

The first stage is `battle_only`:

1. Collect rule-bot rollouts
```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\rollout.py --episodes 50
```

2. Train a lightweight tabular behavior-cloning policy
```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\train_bc.py
```

3. Replay the cloned policy
```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\run_bc_policy.py
```

## Neural BC Upgrade

After you have rollout data, you can train a small neural policy without installing extra packages:

1. Train the tiny MLP BC model
```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\train_nn_bc.py --epochs 12 --hidden 64
```

You can also try a larger or deeper model:

```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\train_nn_bc.py --epochs 12 --hidden 128 --hidden2 128
```

2. Replay the neural policy
```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\run_nn_bc_policy.py
```

Or run the whole stage with one wrapper:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\run-training-stage1.ps1 -Episodes 50 -Replay
```

This first-stage BC policy is intentionally simple. It proves out:

- fixed battle observation encoding
- fixed action ids and masks
- rollout collection
- training artifact generation

Later we can replace the tabular policy with PPO or a neural policy without changing the bridge contract.

## What Exists Now

The current training stack is intentionally small and staged:

- `encoding/observation.py`: battle snapshot -> fixed float vector
- `encoding/action_space.py`: legal actions -> fixed action ids and masks
- `encoding/reward.py`: first-pass battle-only reward shaping
- `training/rollout.py`: collect expert data from the rule bot
- `training/train_bc.py`: train a tabular behavior-cloning baseline
- `training/run_bc_policy.py`: replay the learned baseline against battle-test mode
- `training/train_nn_bc.py`: train a tiny neural BC policy
- `training/run_nn_bc_policy.py`: replay the tiny neural BC policy
- `training/train_ppo_lite.py`: PPO-lite fine-tuning from the BC policy
- `training/run_ppo_lite_policy.py`: replay the PPO-lite policy
- `training/evaluate_battle_policies.py`: benchmark rule, tabular BC, and neural BC on repeated battle tests
- `training/parallel_rollout.py`: collect rollout data from multiple running game instances

This means the repo now has a real first-stage training loop:

1. open the game
2. collect battle-only trajectories
3. fit a baseline policy
4. replay and compare behavior

The next logical upgrade is replacing the tabular BC policy with a neural policy or PPO while keeping the same observation/action contract.

## Benchmark Policies

You can now compare policies on repeated `battle_test` runs:

```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\evaluate_battle_policies.py --policy rule --episodes 10
```

```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\evaluate_battle_policies.py --policy tabular --episodes 10
```

```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\evaluate_battle_policies.py --policy nn --episodes 10
```

```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\evaluate_battle_policies.py --policy ppo --episodes 10
```

For apples-to-apples comparisons across policies, run the evaluator with a fixed seed range:

```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\evaluate_battle_policies.py --policy nn --episodes 50 --seed-base 1000
```

## PPO-lite Fine-Tuning

After training `battle_nn_bc_policy.json`, you can fine-tune it with PPO-lite:

```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\train_ppo_lite.py --epochs 6 --episodes-per-epoch 10
```

Replay it with:

```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\run_ppo_lite_policy.py
```

This prints per-episode results and a final summary including:

- win rate
- average steps
- average end HP
- reward type distribution

## Multi-Instance Sampling

The game bridge now supports overriding the port with the `SLAY_THE_HS_BRIDGE_PORT` environment variable.

Example for two local instances:

```powershell
$env:SLAY_THE_HS_BRIDGE_PORT=47077
```

Launch one game instance, then in another terminal:

```powershell
$env:SLAY_THE_HS_BRIDGE_PORT=47078
```

Launch the second instance. After that, you can collect rollouts in parallel:

```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\parallel_rollout.py --ports 47077,47078 --episodes 60
```

All major training and evaluation scripts also support `--host`, `--port`, and `--fast-mode` / `--no-fast-mode`.

If you want an externally controlled run to play at normal speed instead of fast mode, pass `--no-fast-mode`:

```powershell
& 'C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe' Tools\python\training\evaluate_battle_policies.py --policy nn --episodes 1 --no-fast-mode
```
