# Slay the HS 项目记忆文档

本文档用于给后续接手项目的人快速建立上下文。它不是详细教程，而是一张项目地图：先知道项目是什么、目录怎么分、常用命令是什么、哪些模块值得优先关注。

## 一句话概览

`slay-the-hs` 是一个基于 Godot 4.5 + C# 的卡牌 Roguelike 课程项目，核心流程是：

主菜单 -> 地图 -> 战斗 -> 战后奖励 -> 继续地图/下一步。

当前项目重点是游戏本体、数据驱动内容和基础工程质量。仓库里确实包含 AI 训练、Bot、MCP/Agent 等实验性内容，但如果只是维护课程项目或准备展示，可以先不用关注 AI 训练部分。

## 技术栈

- 引擎：Godot 4.5.1 Mono / .NET
- 主项目：C#，目标框架 `net8.0`
- 测试项目：`Tests/CombatLogicTests`，目标框架 `net8.0`
- 可选 Agent：`Tools/SlayHs.Agent`，目标框架 `net9.0`
- 可选训练脚本：Python 3，位于 `Tools/python`

主场景配置在 `project.godot`：

- 项目名：`slay the hs`
- 默认主场景：`Scenes/MainMenu.tscn`
- Autoload：
  - `GameState`
  - `AppSettings`
  - `ExternalControlService`

## 目录结构速记

```text
Assets/                 美术资源，包含卡牌图、图标等
Data/                   数据驱动内容，卡牌、敌人、遗物、本地化 JSON
Docs/                   项目文档、演示文档、环境配置、训练说明
Scenes/                 Godot 场景文件 .tscn
Scripts/                游戏本体 C# 脚本
Tests/                  独立测试工程，主要验证战斗逻辑
Tools/                  构建、导出、Agent、Python 控制与训练脚本
```

`Scripts/` 下的重点分区：

```text
Scripts/Autoload/       全局状态、设置、外部控制服务
Scripts/Data/           卡牌、敌人、遗物等数据模型与读取
Scripts/External/       外部控制接口模型
Scripts/Localization/   本地化服务
Scripts/Scenes/         各场景的主逻辑脚本
Scripts/Systems/        战斗、牌堆、意图、遭遇等核心规则系统
Scripts/UI/             卡牌视图、敌方卡牌视图、奖励卡牌选项等 UI 组件
```

## 游戏本体优先看哪里

如果你要理解主流程，建议按这个顺序看：

1. `Scripts/Autoload/GameState.cs`
   - 全局运行状态、玩家牌组、遗物、药水、地图/奖励流程相关状态。

2. `Scripts/Scenes/MainMenu.cs`
   - 入口界面，负责进入地图、编辑器等主要入口。

3. `Scripts/Scenes/MapScene.cs`
   - 地图流程，选择节点并进入战斗、事件或奖励。

4. `Scripts/Scenes/BattleScene.cs` 及其 partial 文件
   - 战斗界面与战斗交互，是最大的一组场景逻辑。
   - 相关文件包括：
     - `BattleScene.CardEffects.cs`
     - `BattleScene.CardPreview.cs`
     - `BattleScene.ExternalApi.cs`
     - `BattleScene.PileViewer.cs`
     - `BattleScene.Potions.cs`
     - `BattleScene.Settings.cs`
     - `BattleScene.VisualEffects.cs`

5. `Scripts/Scenes/RewardScene.cs`
   - 战后奖励结算，包括卡牌、遗物、药水奖励与跳过逻辑。

6. `Scripts/Systems/CombatResolver.cs`
   - 战斗规则核心，适合查伤害、格挡、回合结算等逻辑。

7. `Scripts/Systems/DeckFlowResolver.cs`
   - 抽牌、弃牌、牌堆流转。

8. `Scripts/Systems/IntentResolver.cs`
   - 敌人意图处理。

## 数据驱动内容

主要数据文件：

```text
Data/cards.json         卡牌数据
Data/enemies.json       敌人/遭遇数据
Data/relics.json        遗物数据
Data/Localization/      中英文文本
```

读取和模型相关脚本主要在：

```text
Scripts/Data/CardData.cs
Scripts/Data/EnemyUnit.cs
Scripts/Data/RelicData.cs
Scripts/Data/GameDataAccess.cs
```

注意事项：

- 文本配置文件统一使用 UTF-8。
- 修改 JSON 后建议检查是否还能被正常解析。
- `Docs/DevGuidelines.md` 里有编码和 JSON 工具约定。
- `Tools/Json/` 下有辅助脚本，适合批量处理卡牌数据或编码问题。

## 常用命令

在仓库根目录执行。

构建主项目：

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

运行战斗逻辑测试：

```powershell
dotnet run --project .\Tests\CombatLogicTests\CombatLogicTests.csproj
```

用 Godot 启动：

1. 使用 Godot 4.5.1 Mono 打开根目录下的 `project.godot`
2. 等待 C# 编译完成
3. 运行默认主场景 `Scenes/MainMenu.tscn`

## 展示和验收相关

课程展示优先看：

- `Docs/ProjectDemo.md`
- `Docs/EnvironmentSetupGuide.md`
- `Docs/DailyUpdate-2026-04-21.md`

10 分钟 Demo 推荐展示主线：

1. 主菜单进入地图
2. 地图选择战斗节点
3. 战斗中打牌、获得格挡/造成伤害、使用药水
4. 战斗结束进入奖励
5. 展示 JSON 数据驱动内容
6. 展示构建和测试命令

## 关于 AI / 训练部分

仓库中有一套外部控制和训练实验链路，主要集中在：

```text
Scripts/Autoload/ExternalControlService.cs
Scripts/Scenes/BattleScene.ExternalApi.cs
Scripts/External/
Tools/SlayHs.Agent/
Tools/python/
Tools/run-agent.ps1
Tools/run-python-bot.ps1
Tools/run-training-stage1.ps1
Tools/run-eval.ps1
```

这部分的作用大致是：

- 游戏本体通过本地 TCP bridge 暴露状态和动作接口。
- Python 侧可以控制游戏、采样 battle-only 数据、训练简单策略。
- C# Agent 可作为 MCP 或 Bot sidecar 使用。

但是，对后续课程维护者来说：

**如果你的目标是修游戏、补功能、准备演示或交付课程作业，可以先完全忽略 AI 训练部分。**

只有在明确要继续做 Bot、强化学习、策略评估或外部自动控制时，才需要阅读：

- `Docs/TrainingGuide.md`
- `Docs/TrainingRoadmap.md`
- `Tools/python/README.md`
- `Tools/AGENT_CONTROL.md`

换句话说，AI 部分是附加实验栈，不是理解和运行游戏本体的前置条件。

## 当前维护重点建议

优先级从高到低：

1. 保证 Godot 主流程能跑通。
2. 保证 `Data/*.json` 数据可读、文本不乱码。
3. 保证战斗和奖励流程稳定。
4. 修改规则时同步补充或运行 `Tests/CombatLogicTests`。
5. 展示前跑一遍构建和测试。
6. AI/训练相关内容除非被明确要求，否则不要作为主线维护任务。

## 常见修改入口

- 新增或调整卡牌：改 `Data/cards.json`，必要时查 `CardData.cs` 和 `CardEffectPipeline.cs`。
- 调整敌人：改 `Data/enemies.json`，查 `EnemyEncounterCatalog.cs`、`EnemyEncounterBuilder.cs`。
- 调整遗物：改 `Data/relics.json`，查 `RelicData.cs` 和奖励相关场景。
- 改战斗规则：优先看 `CombatResolver.cs`、`TurnFlowResolver.cs`、`DeckFlowResolver.cs`。
- 改战斗界面：看 `BattleScene.cs` 及其 partial 文件。
- 改奖励流程：看 `RewardScene.cs`。
- 改地图流程：看 `MapScene.cs`、`MapCanvas.cs`。
- 改主菜单：看 `MainMenu.cs` 和 `Scenes/MainMenu.tscn`。

## 接手时的最短检查清单

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
dotnet run --project .\Tests\CombatLogicTests\CombatLogicTests.csproj
```

然后用 Godot 打开 `project.godot`，从主菜单走一遍：

```text
主菜单 -> 地图 -> 战斗 -> 奖励
```

如果这条链路能跑通，后续开发就可以围绕具体模块继续推进。
