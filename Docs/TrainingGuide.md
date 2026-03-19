# 训练指南

这份指南面向刚 clone 仓库的新用户，覆盖从环境准备、采样、训练、回放到评估的最短可执行路径。

## 前置要求

- Windows PowerShell
- 主游戏和训练流程需要 .NET 8 SDK
- 如果还要运行 `Tools\SlayHs.Agent` 作为 MCP 或 bot，还需要 .NET 9 SDK
- Python 3.11+，并且可以通过 `python` 在 `PATH` 中访问
- Godot 4.5.1 Mono

可选的环境变量覆盖：

```powershell
$env:SLAY_THE_HS_PYTHON = 'C:\Path\To\python.exe'
$env:SLAY_THE_HS_GODOT_EXE = 'C:\Path\To\Godot_v4.5.1-stable_mono_win64.exe'
```

如果你使用 VS Code 配合 Godot Tools 插件，请在你自己的本地 user/workspace settings 中配置 Godot 可执行文件路径，不要把机器相关的绝对路径提交进仓库。

Python wrapper 的解释器查找优先级如下：

1. `SLAY_THE_HS_PYTHON`
2. `python`
3. `py -3`

## 快速开始

先在仓库根目录打开一个 PowerShell：

```powershell
cd C:\path\to\slay-the-hs
```

先构建一次主 C# 项目：

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

这一步构建的是主 Godot C# 项目。除非你还要运行 `Tools\SlayHs.Agent`，否则不需要额外的 .NET 9 SDK。

然后用 Godot 运行游戏。游戏启动后会在本地打开 TCP bridge：`127.0.0.1:47077`。

## 验证 Bridge 是否可用

游戏运行后，可以先用一个简单 bot 测试控制链路：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-python-bot.ps1
```

如果 bot 能在游戏里正常行动，说明 bridge 是通的，训练脚本也可以连上。

## 第 1 阶段：采集 Rule Bot Rollout

先采一份基础数据集：

```powershell
python .\Tools\python\training\rollout.py --episodes 50 --steps 200 --output .\Tools\python\replays\battle_rule_rollouts.jsonl
```

预期输出：

- 每局的进度日志
- 在 `Tools\python\replays\battle_rule_rollouts.jsonl` 生成一份采样数据

你也可以直接使用封装好的 stage-1 wrapper：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-training-stage1.ps1 -Episodes 50
```

## 第 1 阶段：训练 Tabular BC

训练基线版行为克隆策略：

```powershell
python .\Tools\python\training\train_bc.py --dataset .\Tools\python\replays\battle_rule_rollouts.jsonl --output .\Tools\python\checkpoints\battle_bc_policy.json
```

回放它：

```powershell
python .\Tools\python\training\run_bc_policy.py --policy .\Tools\python\checkpoints\battle_bc_policy.json --steps 200
```

如果你想一步跑完“采样 + tabular 训练 + 回放”，可以直接用：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-training-stage1.ps1 -Episodes 50 -Replay
```

## Neural BC

基于同一份 rollout 数据训练一个小型神经网络策略：

```powershell
python .\Tools\python\training\train_nn_bc.py --dataset .\Tools\python\replays\battle_rule_rollouts.jsonl --epochs 12 --hidden 64
```

回放它：

```powershell
python .\Tools\python\training\run_nn_bc_policy.py --policy .\Tools\python\checkpoints\battle_nn_bc_policy.json --steps 200
```

## PPO-lite 微调

以 neural BC checkpoint 为初始策略继续做 PPO-lite 微调：

```powershell
python .\Tools\python\training\train_ppo_lite.py --init-policy .\Tools\python\checkpoints\battle_nn_bc_policy.json --epochs 6 --episodes-per-epoch 10
```

回放它：

```powershell
python .\Tools\python\training\run_ppo_lite_policy.py --policy .\Tools\python\checkpoints\battle_ppo_lite_policy.json --steps 200
```

## 策略评估

比较不同策略时，尽量使用同一批 seeds：

```powershell
python .\Tools\python\training\evaluate_battle_policies.py --policy rule --episodes 20 --seed-base 1000
python .\Tools\python\training\evaluate_battle_policies.py --policy tabular --episodes 20 --seed-base 1000
python .\Tools\python\training\evaluate_battle_policies.py --policy nn --episodes 20 --seed-base 1000
python .\Tools\python\training\evaluate_battle_policies.py --policy ppo --episodes 20 --seed-base 1000
```

也可以把评估结果写成 JSON 报告：

```powershell
python .\Tools\python\training\evaluate_battle_policies.py --policy nn --episodes 50 --seed-base 1000 --output .\Tools\python\reports\nn_eval.json
```

## 多实例采样

Python client 已经支持 `SLAY_THE_HS_BRIDGE_PORT`。如果你想并行采样：

1. 启动第一个游戏实例，使用默认端口 `47077`
2. 启动第二个游戏实例，配置成 `47078`
3. 运行：

```powershell
python .\Tools\python\training\parallel_rollout.py --ports 47077,47078 --episodes 60
```

## MCP Server

如果你要跑的是 MCP sidecar，而不是 Python 训练流程，可以用：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-agent.ps1 -Mode mcp
```

`Tools\SlayHs.Agent` 的 target 是 `.NET 9`，所以使用这个命令前请先安装 .NET 9 SDK。

## 导出

如果 `godot.exe` 不在 `PATH` 里，可以先给导出脚本指定 Godot 路径：

```powershell
$env:SLAY_THE_HS_GODOT_EXE = 'C:\Path\To\Godot_v4.5.1-stable_mono_win64.exe'
.\Tools\export_windows_clean.bat
```

也可以直接把 Godot 可执行文件路径作为参数传进去：

```powershell
.\Tools\export_windows_clean.bat "C:\Path\To\Godot_v4.5.1-stable_mono_win64.exe"
```

## 常见错误

- `python` 找不到：
  安装 Python 后，重新打开 PowerShell，再运行 `python --version`
- bridge 连接被拒绝：
  先确认游戏已经启动，并且本地 bridge 正在运行
- model checkpoint 不存在：
  先训练对应阶段的模型，或者显式传 `--policy` / `--init-policy`
- Godot 导出失败：
  设置 `SLAY_THE_HS_GODOT_EXE`，或者直接给导出脚本传 Godot 路径
