# Slay The HS — 架构文档

## 概述

Slay The HS 是一款基于 Godot 4.5 (.NET/C#) 的像素风格卡牌构筑 Roguelike 游戏。灵感来源于《Slay the Spire》，支持中文/英文双语、外部 Agent 桥接控制（MCP 协议），以及确定性随机数用于可复现测试。

---

## 目录结构

```
slay-the-hs/
├── Assets/                  # 图片、图标等静态资源
│   ├── Cards/               # 卡牌插画
│   └── Icons/               # UI 图标
├── Data/                    # 数据驱动的 JSON 配置
│   ├── cards.json           # 卡牌定义
│   ├── enemies.json         # 敌人原型与遭遇配置
│   ├── relics.json          # 遗物定义
│   └── Localization/        # 多语言文本 (en.json, zh_hans.json)
├── Scenes/                  # Godot 场景文件 (.tscn)
├── Scripts/
│   ├── Autoload/            # 全局单例服务
│   ├── Data/                # 数据模型与加载
│   ├── External/            # 外部控制的 DTO 模型
│   ├── Localization/        # 多语言服务
│   ├── Scenes/              # 各场景 C# 脚本
│   ├── Systems/             # 纯逻辑系统（无 Godot 依赖）
│   └── UI/                  # UI 组件
├── Tests/
│   └── CombatLogicTests/    # 战斗逻辑的独立测试（控制台运行）
├── Tools/
│   ├── SlayHs.Agent/        # .NET 9 MCP/Bot Agent
│   ├── Json/                # JSON 工具
│   └── python/              # Python 训练/评估脚本
└── Docs/                    # 项目文档
```

---

## 架构分层

### 1. Autoload 全局服务

引擎启动时自动注册的单例（见 [project.godot](../project.godot) 的 `[autoload]` 段）。

#### GameState
- [Scripts/Autoload/GameState.cs](../Scripts/Autoload/GameState.cs)
- [Scripts/Autoload/GameState.SaveLoad.cs](../Scripts/Autoload/GameState.SaveLoad.cs)
- **职责**：整个 run 的中央状态管理器。持有 HP、金币、卡组、遗物、药水、地图、当前 UI 阶段、RNG 状态。
- **核心方法**：
  - `StartNewRun()` — 初始化新 run，生成地图
  - `ChooseMapNode(column)` → `BeginEncounter(type)` — 选节点触发遭遇
  - `ResolveBattleVictory()` — 结算奖励（金币 + 卡牌/药水/遗物选项）
  - `ApplyRestHeal()` / `ApplyRestUpgrade(index)` — 篝火操作
  - `SaveNodeEntrySnapshot()` / `RestoreNodeEntrySnapshot()` — 节点入场的快照/回退
- **UI 阶段** (`CurrentUiPhase`)：`main_menu` / `character_select` / `map` / `battle` / `reward` / `event` / `rest` / `shop`

#### AppSettings
- [Scripts/Autoload/AppSettings.cs](../Scripts/Autoload/AppSettings.cs)
- **职责**：游戏设置单例。音量、FPS、VSync、窗口大小、FPS 计数器。
- `SettingsChanged` 事件通知外部监听者。

#### ExternalControlService
- [Scripts/Autoload/ExternalControlService.cs](../Scripts/Autoload/ExternalControlService.cs)
- **职责**：TCP 桥接服务器，监听 `127.0.0.1:47077`，使用 JSON-line 协议。
- **协议**：支持的命令：
  - `ping` — 心跳 / 获取快照
  - `get_snapshot` — 获取完整游戏状态（场景类型、run 状态、合法动作列表）
  - `execute_action` — 执行动作（`start_new_run`, `start_battle_test_run`, `choose_map_node`, `play_card`, `end_turn`, `choose_reward_type`, `choose_reward_card`, `skip_reward`, `choose_event_option`）
- 每个请求带 `ExpectedStateVersion` 用于乐观并发控制。
- 支持 `FastMode`：Agent 控制时跳过动画。

---

### 2. Data 层 — 数据模型与加载

#### CardData
- [Scripts/Data/CardData.cs](../Scripts/Data/CardData.cs)
- 卡牌定义：费用 (`Cost`)、种类 (`Attack`/`Skill`)、效果列表 (`Effects[]`)、升级配方 (`Upgrade`)。
- 效果类型 (`CardEffectType`)：`Damage`, `GainBlock`, `ApplyVulnerable`, `DrawCards`, `GainStrength`, `GainEnergy`, `Heal`, `DiscardCards`。
- 效果目标 (`CardEffectTarget`)：`Player`, `SelectedEnemy`, `AllEnemies`。
- 关键词 (`CardKeyword`)：`Retain`, `Exhaust`, `Curious`。
- 数据源：`Data/cards.json`（回退到硬编码的基础卡组）。

#### CardUpgradeRules
- **职责**：升级逻辑。`MaybeUpgrade(cardId, chance, rng)` — 根据概率返回 `cardId+` 或不升级。

#### RelicData
- [Scripts/Data/RelicData.cs](../Scripts/Data/RelicData.cs)
- 遗物定义：Id、Name、Description、Rarity、Archetype。
- 数据源：`Data/relics.json`，支持环境变量 `SLAY_THE_HS_RELICS_JSON` 覆盖。
- Rarity: Starter / Common / Uncommon / Rare / Boss。

#### PotionData
- [Scripts/Data/PotionData.cs](../Scripts/Data/PotionData.cs)
- 5 种药水：healing_potion, strength_potion, swift_potion, guard_potion, fury_potion。

#### DeckPresetCatalog
- [Scripts/Data/DeckPresetCatalog.cs](../Scripts/Data/DeckPresetCatalog.cs)
- 7 套预设卡组原型：Balanced Starter / Infinite Cycle / Infinite Fireball / Death Legion / Berserker Slam / Fortress Control / Storm Engine。
- 每个预设包含 Glyph（角色图标）和 Accent（主题色）用于角色选择界面。

#### EnemyEncounterCatalog / EnemyUnit
- [Scripts/Data/EnemyEncounterCatalog.cs](../Scripts/Data/EnemyEncounterCatalog.cs) / [Scripts/Data/EnemyUnit.cs](../Scripts/Data/EnemyUnit.cs)
- 数据源：`Data/enemies.json`。
- 敌人原型：cultist, cultist_scout, cultist_shaman, cultist_guard, cultist_brute, elite_sentinel, merchant, act_boss_placeholder。
- 属性：`baseHp` + `hpPerFloor * floor` 计算血量。

#### MapNodeType / MapProgressionRules
- [Scripts/Data/MapNodeType.cs](../Scripts/Data/MapNodeType.cs) — 枚举：NormalBattle, EliteBattle, Event, Rest, Shop, MerchantFight, Intro, Boss。
- [Scripts/Data/MapProgressionRules.cs](../Scripts/Data/MapProgressionRules.cs) — 3 幕 × 10 行/幕，升级概率随幕增长 (10% → 25% → 40%)。

#### GameDataAccess
- [Scripts/Data/GameDataAccess.cs](../Scripts/Data/GameDataAccess.cs)
- 文件读取抽象层：同时支持 `res://` 资源路径和文件系统路径，使测试项目可以在不依赖 Godot 引擎的情况下加载数据文件。

---

### 3. Systems 层 — 纯游戏逻辑

所有 System 类为静态类，**不依赖 Godot API**，可以在独立测试项目中运行。

#### CombatResolver
- [Scripts/Systems/CombatResolver.cs](../Scripts/Systems/CombatResolver.cs)
- **职责**：伤害计算。
- `ResolveHit(baseDamage, attackerStrength, targetVulnerable, targetBlock, targetHp)`：
  1. 原始伤害 = baseDamage + strength
  2. 易伤加成 = 原始 × 1.5
  3. 格挡抵消
  4. 穿透伤害 → 扣血
- 返回 `DamageResolution`（finalDamage, blocked, taken, remainingBlock, remainingHp）。

#### CardEffectPipeline
- [Scripts/Systems/CardEffectPipeline.cs](../Scripts/Systems/CardEffectPipeline.cs)
- **职责**：卡牌效果的调度引擎。使用处理器注册表（handler registry）。
- 接口 `ICardEffectRuntime` 定义了 7 种效果回调：`ExecuteDamage`, `ExecuteGainBlock`, `ExecuteApplyVulnerable`, `ExecuteGainStrength`, `ExecuteGainEnergy`, `ExecuteHeal`, `ExecuteDiscardCards`。
- `DrawCards` 和 `DiscardCards` 效果在 pipeline 中直接处理（无需回调）。
- 支持 `Replay`（卡牌重复打出）。
- 支持通过 `RegisterOrReplaceHandler` 自定义/扩展效果处理器。

#### TurnFlowResolver
- [Scripts/Systems/TurnFlowResolver.cs](../Scripts/Systems/TurnFlowResolver.cs)
- **职责**：回合开始/结束逻辑。
- 回合开始：能量恢复（+1 如有 Lantern 遗物）、格挡重置（+8 如有 Anchor 遗物）、敌人格挡清零。
- 回合结束：易伤层数衰减、手牌移入弃牌堆。

#### DeckFlowResolver
- [Scripts/Systems/DeckFlowResolver.cs](../Scripts/Systems/DeckFlowResolver.cs)
- **职责**：抽牌逻辑。
- `DrawIntoHand(drawPile, discardPile, hand, count, handLimit, rng)`：
  - 抽牌堆空时自动从弃牌堆洗入。
  - 手牌上限保护。
  - 返回 `DeckDrawResult`（drawnCards, handLimitReached, reshuffleCount）。

#### IntentResolver
- [Scripts/Systems/IntentResolver.cs](../Scripts/Systems/IntentResolver.cs)
- **职责**：敌人 AI / 意图选择。
- 每种敌人原型有不同的行为模式：
  - `cultist_shaman`: 首回合 Buff，之后随机 Attack/Buff/Defend
  - `cultist_guard`: 首回合 Defend，之后混合 Attack/Defend/Buff
  - `cultist_brute`: 每 3 回合 Buff +2，其余攻击为主
  - `elite_sentinel`: 固定轮换 Attack → Defend → Buff
  - 默认: 根据是否是精英决定攻击偏重

#### EnemyEncounterBuilder
- [Scripts/Systems/EnemyEncounterBuilder.cs](../Scripts/Systems/EnemyEncounterBuilder.cs)
- **职责**：根据遭遇类型和楼层构建敌人组。

#### GameRng
- [Scripts/Systems/GameRng.cs](../Scripts/Systems/GameRng.cs)
- **职责**：基于 xorshift64* 的确定性随机数生成器，继承 `System.Random` 以兼容现有代码。状态可序列化，支持快照/恢复。

---

### 4. Scenes 层 — 游戏场景

#### 主菜单 (MainMenu)
- [Scripts/Scenes/MainMenu.cs](../Scripts/Scenes/MainMenu.cs)
- 入口：`Scenes/MainMenu.tscn`（`project.godot` 中设为 `run/main_scene`）。
- Start Game → CharacterSelectScene；Continue → 加载存档 → MapScene。
- Abandon Save 按钮仅在存档存在时显示。
- 快捷入口：Achievements / Bestiary / Relic Compendium / Card Compendium / Settings。

#### 角色选择 (CharacterSelectScene)
- [Scripts/Scenes/CharacterSelectScene.cs](../Scripts/Scenes/CharacterSelectScene.cs)
- 选择 7 套预设卡组之一，或进入 DeckEditorScene 自定义卡组。
- 可选的竞技场（Ascension）修饰符。

#### 地图 (MapScene)
- [Scripts/Scenes/MapScene.cs](../Scripts/Scenes/MapScene.cs)
- 视觉呈现地图，每行 5 列节点，底部 Intro → 顶部 Boss。
- 支持缩放（滚轮）和拖拽，单选节点跳转对应场景。
- 显示当前 HP/金币/遗物/卡组信息。

#### 战斗 (BattleScene)
- [Scripts/Scenes/BattleScene.cs](../Scripts/Scenes/BattleScene.cs)（主逻辑）
- 子文件拆分：`.CardEffects.cs` `.ExternalApi.cs` `.PileViewer.cs` `.Potions.cs` `.Settings.cs` `.VisualEffects.cs` `.CardPreview.cs`
- **回合循环**：
  1. 回合开始：恢复能量，触发遗物（Lantern/Anchor/Ember Ring/Iron Shell）
  2. 抽牌：`DeckFlowResolver.DrawIntoHand`
  3. 玩家出牌或 End Turn
  4. 每张牌 → `CardEffectPipeline` → `ICardEffectRuntime` 回调 → 战斗状态更新
  5. 敌人行动：按顺序执行意图（Attack/Defend/Buff）
  6. 回合结束：状态衰减，手牌入弃牌
- `BuildBattleSnapshot()` 和 `BuildLegalActions()` 为外部控制提供接口。

#### 奖励 (RewardScene)
- [Scripts/Scenes/RewardScene.cs](../Scripts/Scenes/RewardScene.cs)
- 奖励类型选择：relic / card_pack / potion / skip
- 选牌时展示 3-4 张卡牌供选择

#### 事件 (EventScene)
- [Scripts/Scenes/EventScene.cs](../Scripts/Scenes/EventScene.cs)
- 随机事件：Ancient Shrine（祈祷/取遗物）、Shady Dealer（买卡/拒绝）

#### 休息点 (RestScene)
- [Scripts/Scenes/RestScene.cs](../Scripts/Scenes/RestScene.cs)
- 二选一：Heal（恢复 30% 最大 HP）或 Upgrade（选择一张牌升级）

#### 商店 (ShopScene)
- [Scripts/Scenes/ShopScene.cs](../Scripts/Scenes/ShopScene.cs)
- 售卖卡牌/遗物/药水，支持 Remove Service（移除卡组中一张牌）。
- 可攻击商人触发 `MerchantFight`。

#### 其他场景
- VictoryScene / DefeatScene — 结局画面
- SettingsScene — 设置
- CardBrowserScene / CardEditorScene — 卡牌浏览器/编辑器（开发工具）
- BestiaryScene / AchievementsScene / RelicCompendiumScene — 图鉴

---

### 5. UI 组件层

- **CardView** — 手牌渲染，支持悬停/拖拽/播放动画
- **PlayerCardView** — 玩家状态面板（HP/格挡/力量/易伤）
- **EnemyCardView** — 敌人卡牌（意图显示、生命条、选中效果）
- **RewardCardOptionView** — 奖励卡选项
- **NodeSettingsOverlay** — 地图调试面板

---

### 6. 外部控制接口 (MCP Bridge)

#### 游戏侧 (ExternalControlService)
- TCP Server 监听 `127.0.0.1:47077`
- JSON-line 协议，每行一个完整 JSON 对象。
- 状态版本号：每次状态变化 `StateVersion` 递增，Agent 可携带 `ExpectedStateVersion` 防止并发冲突。

#### Agent 侧 (Tools/SlayHs.Agent/)
- [Tools/SlayHs.Agent/Program.cs](../Tools/SlayHs.Agent/Program.cs) — 入口，支持 `mcp` 和 `bot` 两种模式。
- [Tools/SlayHs.Agent/McpServer.cs](../Tools/SlayHs.Agent/McpServer.cs) — 实现 JSON-RPC 2.0 MCP 协议，暴露两个工具：
  - `get_game_snapshot` — 获取游戏状态快照（含合法动作列表）
  - `execute_game_action` — 执行一个合法动作
- [Tools/SlayHs.Agent/GameBridgeClient.cs](../Tools/SlayHs.Agent/GameBridgeClient.cs) — 游戏 TCP 客户端
- [Tools/SlayHs.Agent/SimpleBot.cs](../Tools/SlayHs.Agent/SimpleBot.cs) — 简单 Bot 逻辑

---

### 7. 多语言系统

- [Scripts/Localization/LocalizationService.cs](../Scripts/Localization/LocalizationService.cs)
- 静态 key-value 查找，支持英语 (`en`) 和简体中文 (`zh_hans`)。
- `LocalizationService.Get(key, fallback)`：当前语言无值时回退到英文。
- `LocalizationService.Format(key, fallback, args...)`：格式化字符串。

---

### 8. 存档系统

- [Scripts/Autoload/SaveSystem.cs](../Scripts/Autoload/SaveSystem.cs)
- 单槽存档（`savegame.json`），写入 Godot 用户数据目录。
- 包含完整运行状态：HP、金币、卡组、遗物、地图布局、RNG 状态。
- 存档时机：每次回到地图场景时自动保存。

---

## 核心流程

```
MainMenu
  ├── Start Game
  │     └── CharacterSelectScene (选预设 / 自定义卡组)
  │           └── StartNewRun → MapScene
  └── Continue
        └── 加载存档 → MapScene

MapScene
  ├── 点击 NormalBattle/EliteBattle → BattleScene
  ├── 点击 Event → EventScene
  ├── 点击 Rest → RestScene
  ├── 点击 Shop → ShopScene
  └── 点击 Intro/Boss → 自动触发

BattleScene
  ├── 玩家出牌 (CardEffectPipeline)
  ├── 结束回合 (敌人行动)
  ├── 胜利 → ResolveBattleVictory → RewardScene → MapScene
  └── 失败 → DefeatScene

3 幕通关 → VictoryScene
```

---

## 关键设计决策

| 决策 | 说明 |
|------|------|
| 纯 C# Systems | `Scripts/Systems/` 下的所有类不引用 Godot API，可在控制台测试中独立运行 |
| 确定性 RNG | `GameRng` 使用 xorshift64*，状态可序列化保存，支持 AI 训练的可复现性 |
| 数据驱动 | `cards.json` / `enemies.json` / `relics.json` 由 C# 类在运行时加载，回退到硬编码值 |
| 外部控制桥接 | TCP 服务器在游戏内常驻，支持 MCP 协议，使外部 AI/Agent 可控制游戏 |
| 单槽存档 | 简化实现，每次回到地图自动保存 |
| 卡牌效果管线 | 处理器注册表模式 (`CardEffectPipeline`)，易于扩展新效果类型 |
| 拆分战斗脚本 | BattleScene 按功能拆分为 8 个 partial class 文件 |
