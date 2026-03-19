# 训练路线图

这份文档用于记录当前仓库推荐的训练方向，方便后续按同一条主线推进，避免一次改太多变量之后很难定位问题。

## 总目标

先把 `battle_only` 这条训练链路做稳，再扩展到地图、奖励、事件等更完整的 run 流程。

一句话版本：

1. 先稳定 battle-only 数据采样
2. 用固定种子基准测试 `rule / tabular / nn / ppo`
3. 把 neural BC 作为主要学习策略基座
4. 评估体系可靠之后再重点推进 PPO
5. battle-only 结果稳定后再扩展到完整 run

## 为什么走这条路线

当前仓库已经有一套比较完整的分阶段训练栈：

- rule bot 采样
- tabular BC
- neural BC
- PPO-lite 微调
- 重复 battle 测评

所以现在最稳的做法，不是直接冲全流程 RL，而是先把 `battle_only` 这一段做扎实。

这条路线的好处：

- 状态空间和动作空间更小
- 迭代速度更快
- 回归时更容易定位问题
- reward shaping 的效果更容易看清
- 更容易做 apples-to-apples 的公平比较

## 当前训练原则

尽量不要一次同时改动下面这些东西：

- observation 编码
- action 编码
- reward shaping
- 模型结构
- 训练算法

每一轮实验，优先只改一个主要变量，然后用同一套 benchmark seeds 重新评估。

## 阶段 1：建立可靠基线

目标：

- 确认 `rule -> rollout -> BC -> evaluation` 这条链路稳定可重复

成功标准：

- rollout 采样可以稳定完成，没有 bridge / runtime 异常
- tabular BC 可以训练并回放
- 固定种子评估能跑出可重复的数据

推荐命令：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-rollout.ps1 -Episodes 200 -Steps 200
```

```powershell
python .\Tools\python\training\train_bc.py --dataset .\Tools\python\replays\battle_rule_rollouts.jsonl --output .\Tools\python\checkpoints\battle_bc_policy.json
```

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-eval.ps1 -Policy rule -Episodes 50
```

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-eval.ps1 -Policy tabular -Episodes 50
```

这一阶段要记录的指标：

- win rate
- average steps
- average end HP
- 明显的策略失败模式

## 阶段 2：把 Neural BC 变成主基线

目标：

- 用 neural BC 替代 tabular BC，作为主要学习基线

原因：

- tabular BC 的价值主要是证明整条链路工作正常
- neural BC 才是后面真正值得继续优化的学习策略基座

成功标准：

- neural BC 至少和 tabular BC 一样稳定
- neural BC 在固定种子评估里不弱于 tabular
- 回放时 battle 行为看起来合理

推荐命令：

```powershell
python .\Tools\python\training\train_nn_bc.py --dataset .\Tools\python\replays\battle_rule_rollouts.jsonl --epochs 12 --hidden 64
```

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-eval.ps1 -Policy nn -Episodes 50
```

可选的多 seed 训练：

```powershell
python .\Tools\python\training\train_nn_bc.py --dataset .\Tools\python\replays\battle_rule_rollouts.jsonl --epochs 12 --hidden 64 --seeds 7,11,17
```

这一阶段主要比较：

- rule
- tabular
- nn

决策规则：

- 如果 `nn` 明显优于 `tabular`，后续就以 `nn` 作为默认学习策略基座
- 如果 `nn` 不稳定，先提升数据量和评估质量，不要急着继续改模型结构

## 阶段 3：从 Neural BC 出发做 PPO-lite 微调

目标：

- 验证 PPO-lite 能不能稳定超过 neural BC

重要原则：

- 评估体系没有稳定之前，PPO 的结论不可信
- 如果 PPO 没有明显超过 neural BC，不要第一反应就是加大模型；先回头看 reward 和 rollout 质量

成功标准：

- PPO 多次运行结果比较稳定
- PPO 在固定种子下能打平或超过 neural BC
- PPO 没有出现明显的 reward hacking 或奇怪退化

推荐命令：

```powershell
python .\Tools\python\training\train_ppo_lite.py --init-policy .\Tools\python\checkpoints\battle_nn_bc_policy.json --epochs 6 --episodes-per-epoch 10
```

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-eval.ps1 -Policy ppo -Episodes 50
```

重点观察：

- reward hacking
- 只顾短期收益的退化行为
- 看起来提升了，但重跑之后又消失的“不稳定提升”

## 标准评估协议

只要你要比较不同策略，尽量都使用同一套评估参数。

基准比较集合：

- `rule`
- `tabular`
- `nn`
- `ppo`

推荐统一设置：

- `episodes = 50`
- `steps = 200`
- `seed-base = 1000`
- fast mode 开启

参考命令：

```powershell
python .\Tools\python\training\evaluate_battle_policies.py --policy rule --episodes 50 --steps 200 --seed-base 1000
```

```powershell
python .\Tools\python\training\evaluate_battle_policies.py --policy tabular --episodes 50 --steps 200 --seed-base 1000
```

```powershell
python .\Tools\python\training\evaluate_battle_policies.py --policy nn --episodes 50 --steps 200 --seed-base 1000
```

```powershell
python .\Tools\python\training\evaluate_battle_policies.py --policy ppo --episodes 50 --steps 200 --seed-base 1000
```

建议把结果写成报告文件：

```powershell
python .\Tools\python\training\evaluate_battle_policies.py --policy nn --episodes 50 --steps 200 --seed-base 1000 --output .\Tools\python\reports\nn_eval.json
```

## 数据策略

近期建议：

- 不要再把 20 到 50 局当成正式训练集规模
- serious 的 BC 比较，建议至少 200 到 500 局 rollout 起步

推荐顺序：

1. 先增大数据量
2. 再做固定种子对比
3. 最后才考虑调模型大小或 PPO 超参

原因：

- 早期很多 BC 不稳定，本质更像是数据质量或数据规模问题，而不是模型不够大

## 现在先不要做的事

在 battle-only 稳定前，尽量不要做这些：

- 直接做 full-run end-to-end RL
- battle / reward / map / event 一起学
- 在评估还不干净时就大幅升级模型结构
- 一轮里同时改 reward、observation 和 PPO 参数

## Battle-only 稳定之后的扩展路线

只有当 battle-only 结果足够稳定、可重复，才建议继续扩展到：

1. reward 选择策略
2. event 选项策略
3. map 路线策略
4. 最终再考虑 full-run 的分层控制器

后续更推荐的结构：

- battle policy 继续保持专门化
- 非 battle 阶段先从 rule-based 或 classifier-style policy 起步
- 等每一部分都可单独测试后，再组合成更高层的 run controller

## 接下来三轮建议迭代

### 第 1 轮

重点：

- 采更大的 rollout 数据
- 做 `rule` 和 `tabular` 的基线对比

检查清单：

- 采样 200 到 500 局
- 训练 tabular BC
- 跑固定种子评估
- 把基线数据记下来

### 第 2 轮

重点：

- 训练 neural BC
- 在同一套 seeds 下和 tabular 比

检查清单：

- 训练 neural BC
- 先回放一次，确认行为看起来正常
- 跑完整固定种子评估
- 判断 `nn` 是否成为新的默认学习基线

### 第 3 轮

重点：

- 从 neural BC 初始化 PPO
- 看 PPO 是否带来稳定提升

检查清单：

- 训练 PPO-lite
- 跑固定种子评估
- 和 `nn` 对比
- 如果提升弱或不稳定，先回头看 reward 和数据质量，不要急着继续换模型

## 工作规则

当结果变差时，先问自己：

1. 数据集变了吗？
2. 评估 seeds 变了吗？
3. reward 变了吗？
4. 是不是一口气改了多个训练变量？

如果第 4 个问题的答案是“是”，优先先简化实验，再重新跑。
