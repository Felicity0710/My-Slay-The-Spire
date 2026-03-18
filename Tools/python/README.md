# Python Control Layer

This folder adds a pure Python layer on top of the in-game TCP bridge at `127.0.0.1:47077`.

Files:

- `game_bridge.py`: low-level request client
- `slay_env.py`: gym-style wrapper with `reset()` and `step()`
- `gym_env.py`: optional `gymnasium.Env` adapter
- `simple_bot.py`: small rule-based bot
- `example_loop.py`: tiny rollout example

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
