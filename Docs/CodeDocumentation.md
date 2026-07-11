# Slay The HS — 代码文档

## 目录

1. [架构概述](#1-架构概述)
2. [Autoload 全局服务](#2-autoload-全局服务)
3. [Data 数据层](#3-data-数据层)
4. [Systems 系统层](#4-systems-系统层)
5. [Scenes 场景层](#5-scenes-场景层)
6. [UI 组件层](#6-ui-组件层)
7. [Localization 多语言](#7-localization-多语言)
8. [External 外部接口](#8-external-外部接口)
9. [项目配置文件](#9-项目配置文件)

---

## 1. 架构概述

```
Scripts/
├── Autoload/          # 全局单例服务（引擎启动时自动注册）
│   ├── GameState.cs           # 整局游戏状态管理
│   ├── GameState.SaveLoad.cs  # 存档系统
│   ├── AppSettings.cs         # 游戏设置
│   ├── ExternalControlService.cs # TCP 外部控制桥接
│   ├── AudioManager.cs        # 音频管理器
│   └── SaveSystem.cs          # 存档文件读写
├── Data/              # 数据模型和 JSON 加载
│   ├── CardData.cs            # 卡牌数据模型 + 枚举
│   ├── CardCatalogPersistence.cs # 卡牌 JSON 持久化
│   ├── RelicData.cs           # 遗物数据模型
│   ├── PotionData.cs          # 药水数据模型
│   ├── EnemyUnit.cs           # 敌人运行时数据
│   ├── EnemyEncounterCatalog.cs # 敌人遭遇配置加载
│   ├── DeckPresetCatalog.cs   # 角色预设卡组
│   ├── AchievementCatalog.cs  # 成就定义
│   ├── MapProgressionRules.cs # 地图进度规则
│   ├── MapNodeType.cs         # 地图节点类型枚举
│   └── GameDataAccess.cs      # 文件读取抽象层
├── Systems/           # 纯 C# 逻辑（无 Godot 依赖）
│   ├── CombatResolver.cs      # 伤害计算
│   ├── CardEffectPipeline.cs  # 卡牌效果调度引擎
│   ├── TurnFlowResolver.cs    # 回合开始/结束逻辑
│   ├── DeckFlowResolver.cs    # 抽牌/洗牌逻辑
│   ├── IntentResolver.cs      # 敌人 AI 意图选择
│   ├── EnemyEncounterBuilder.cs # 战斗遭遇构建
│   ├── GameRng.cs             # 确定性随机数
│   ├── CombatVisualCatalog.cs # 敌人/遗物视觉配置
│   └── AchievementState.cs    # 成就追踪
├── Scenes/            # 各场景 C# 脚本
│   ├── BattleScene.cs         # 战斗主逻辑（+7 个 partial 文件）
│   ├── MapScene.cs            # 地图场景
│   ├── MainMenu.cs            # 主菜单
│   ├── CharacterSelectScene.cs # 角色选择
│   ├── EventScene.cs          # 随机事件
│   ├── IntroEventScene.cs     # 章节开幕
│   ├── ShopScene.cs           # 商店
│   ├── RestScene.cs           # 篝火休息
│   ├── RewardScene.cs         # 战后奖励
│   ├── VictoryScene.cs        # 胜利画面
│   ├── DefeatScene.cs         # 失败画面
│   ├── SettingsScene.cs       # 设置页面
│   ├── AchievementsScene.cs   # 成就展示
│   ├── BestiaryScene.cs       # 敌人图鉴
│   ├── RelicCompendiumScene.cs # 遗物图鉴
│   └── CardBrowserScene.cs    # 卡牌图鉴
├── UI/                # UI 组件
│   ├── CardView.cs            # 手牌渲染
│   ├── EnemyCardView.cs       # 敌人卡牌
│   ├── PlayerCardView.cs      # 玩家状态面板
│   ├── RunStatusOverlay.cs    # 状态栏浮层
│   ├── NodeSettingsOverlay.cs # 齿轮设置浮层
│   ├── RewardCardOptionView.cs # 奖励卡选项
│   └── AchievementPopup.cs    # 成就弹窗
├── Localization/      # 多语言
│   └── LocalizationService.cs # 本地化服务
└── External/          # 外部控制
    └── ExternalModels.cs      # TCP 协议 DTO
```

**核心设计原则**：`Scripts/Systems/` 下的所有类为纯 C# 静态类，不引用 Godot API，可在独立控制台测试项目中运行。

---

## 2. Autoload 全局服务

### 2.1 GameState (`Scripts/Autoload/GameState.cs`)

**职责**：整个 run 的中央状态管理器。

**核心属性**：

| 属性 | 类型 | 说明 |
|------|------|------|
| `MaxHp` | int | 玩家最大生命值 |
| `PlayerHp` | int | 玩家当前生命值 |
| `Floor` | int | 当前楼层 |
| `Act` | int | 当前章节 (1-3) |
| `Gold` | int | 金币 |
| `BattlesWon` | int | 胜场数 |
| `RunCompleted` | bool | 是否通关 |
| `DeckCardIds` | List\<string\> | 牌库卡牌 ID 列表 |
| `RelicIds` | List\<string\> | 已拥有遗物 ID 列表 |
| `PotionIds` | List\<string\> | 药水 ID 列表（最多 3 个） |
| `SelectedDeckPresetId` | string | 当前角色预设 ID |
| `PendingEncounterType` | MapNodeType | 待进入的遭遇类型 |
| `PendingEventId` | string | 待触发的事件 ID |

**核心方法**：

| 方法 | 说明 |
|------|------|
| `StartNewRun()` | 初始化新 run，生成地图 |
| `ChooseMapNode(column)` | 选节点触发遭遇 |
| `BeginEncounter(type)` | 开始战斗/事件/商店等 |
| `ResolveBattleVictory()` | 结算战斗奖励（金币+卡牌/药水/遗物选项） |
| `AddCardToDeck(id)` | 添加卡牌到牌库（自动处理熔岩蛋/剧毒蛋升级） |
| `AddRelic(id)` | 添加遗物（自动处理芒果等拾取效果） |
| `TryAddPotion(id)` | 添加药水（超上限返回 false） |
| `ApplyRestHeal()` | 篝火回复（自动处理腌肉/捕梦网遗物） |
| `BeginRandomEvent()` | 从 12 个事件中随机选择 |
| `RestHealAmount()` | 计算篝火回复量（基础 30%，腌肉 50%） |

**追踪字段**（用于成就检测）：

| 字段 | 说明 |
|------|------|
| `EliteKills` | 精英击杀数 |
| `EventsResolved` | 已完成事件数 |
| `MerchantRobbed` | 是否抢劫过商店 |
| `PerfectBattles` | 无伤战斗次数 |

### 2.2 AudioManager (`Scripts/Autoload/AudioManager.cs`)

**职责**：全局音频管理，BGM 和 SFX 的程序化合成。

**设计**：使用 Godot 内置 `AudioStreamGenerator` 进行实时音频合成，零外部文件依赖。

**BGM 系统**：
- 4 层音乐合成：Bass 低音 + Chord Pad 和弦 + Arpeggio 琶音 + Melody 旋律
- D 小调五声音阶 / C 弗里几亚音阶
- 10 个场景各有不同 BGM 预制

**API**：

| 方法 | 说明 |
|------|------|
| `PlayBgm(sceneId)` | 切换场景 BGM |
| `StopBgm()` | 停止 BGM |
| `PlaySfx(soundId)` | 播放一次性音效（24 种） |

**SFX 对照表**：

| ID | 频率 | 场景 |
|----|------|------|
| card_play | 880Hz | 出牌 |
| attack_hit | 220Hz | 攻击命中 |
| block_gain | 520Hz | 获得格挡 |
| damage_taken | 180Hz | 受伤 |
| enemy_death | 300Hz | 敌人死亡 |
| gold_gain | 1040Hz | 获得金币 |
| potion_drink | 600Hz | 喝药水 |

### 2.3 AppSettings (`Scripts/Autoload/AppSettings.cs`)

**职责**：游戏设置管理，连接显示/音频/FPS 等系统 API。

**核心属性**：

| 属性 | 说明 |
|------|------|
| `MasterVolumePercent` | 主音量 (0-100) |
| `MusicVolumePercent` | 音乐音量 (0-100) |
| `MaxFps` | 最大帧率 |
| `VSyncEnabled` | 垂直同步 |
| `ShowFpsCounter` | 显示帧率 |
| `WindowSize` | 窗口分辨率 |

**事件**：`SettingsChanged` — 设置变更时触发，AudioManager 订阅此事件同步音量。

---

## 3. Data 数据层

### 3.1 CardData (`Scripts/Data/CardData.cs`)

**职责**：卡牌数据模型定义，包含卡牌元数据、效果定义、升级配方和多语言解析。

**枚举类型**：

```csharp
enum CardKind { Attack, Skill }
enum CardEffectType { Damage, GainBlock, ApplyVulnerable, DrawCards, GainStrength, GainEnergy, Heal, DiscardCards }
enum CardEffectTarget { Player, SelectedEnemy, AllEnemies }
enum CardKeyword { Retain, Exhaust, Curious }
```

**CardData 属性**：

| 属性 | 说明 |
|------|------|
| `Id` | 唯一标识符（升级版以 + 结尾） |
| `Name` / `Description` | 英文名称和描述 |
| `DescriptionZh` | 中文描述（fallback） |
| `Kind` | Attack 或 Skill |
| `Cost` | 费用 (0-3) |
| `Effects` | 效果列表 (CardEffectData[]) |
| `Keywords` | 关键词列表 (Retain/Exhaust/Curious) |
| `ReplayCount` | 重放次数 |
| `Upgrade` | 升级配方 (CardUpgradeRecipe) |

**CardEffectData 属性**：

| 属性 | 说明 |
|------|------|
| `Type` | 效果类型 |
| `Target` | 效果目标 (Player/SelectedEnemy/AllEnemies) |
| `Amount` | 基础数值 |
| `Repeat` | 重复次数 |
| `UseAttackerStrength` | 是否受攻击者力量加成 |
| `UseTargetVulnerable` | 是否受目标易伤加成 |

**CardUpgradeRecipe**：定义升级时的修改（AmountDeltas、CostDelta、AddKeywords、ReplayCount）。

**多语言解析流程** (`ResolveLocalizedText`)：
1. 英文模式：直接返回 `description` 字段
2. 中文模式：先查 `zh_hans.json` 本地化键 → 不存在则使用 `descriptionZh` 字段

### 3.2 CardCatalogPersistence (`Scripts/Data/CardCatalogPersistence.cs`)

**职责**：卡牌 JSON 文件的加载、保存和校验。

**DTO 类**：`CardCatalogData`（根）、`CardEntryData`、`CardEffectEntryData`、`CardUpgradeEntryData`。

**校验规则**：cards 非空、ID 唯一、Kind/EffectType/EffectTarget 可解析为枚举、Repeat ≥ 1、Keywords 有效、starterDeck/rewardPool 引用有效、"strike" 必须存在作为 fallback。

### 3.3 RelicData (`Scripts/Data/RelicData.cs`)

**职责**：遗物数据模型和加载。

**属性**：`Id`、`Name`、`Description`、`Rarity`、`Archetype`。

**本地化**：`LocalizedName` / `LocalizedDescription` 直接检查 `LocalizationSettings.CurrentLanguage`，中文模式下从内置 `ZhText` 字典取值（63 个遗物全部内置中英双语）。

**静态方法**：`CreateById()`、`AllRelicIds()`、`GroupByRarity()`。

### 3.4 PotionData (`Scripts/Data/PotionData.cs`)

**职责**：药水数据模型。

**属性**：`Id`、`Name`/`NameZh`、`Description`/`DescriptionZh`、`HealAmount`、`StrengthAmount`、`EnergyAmount`、`BlockAmount`、`MaxHpAmount`。

**双语**：`DisplayName` / `DisplayDescription` 直接根据语言判定返回中/英文。

**10 种药水**：healing_potion (20HP)、greater_healing_potion (40HP)、strength_potion (+2力)、guard_potion (+15挡)、swift_potion (+2能)、fury_potion (+2力+1能)、block_potion (+25挡)、max_hp_potion (+8最大HP)、energy_potion (+3能)、vampire_potion (+2力+回5HP)。

### 3.5 EnemyUnit (`Scripts/Data/EnemyUnit.cs`)

**职责**：单个敌人的运行时数据（不持久化）。

**属性**：`ArchetypeId`、`Name`、`VisualId`、`Hp`、`MaxHp`、`Block`、`Strength`、`Vulnerable`、`IntentType`、`IntentValue`。

### 3.6 EnemyEncounterCatalog (`Scripts/Data/EnemyEncounterCatalog.cs`)

**职责**：从 `enemies.json` 加载敌人原型配置和遭遇规则。

**核心类型**：`EnemyArchetype`（原型）、`EncounterMember`（遭遇成员）、`StrengthFormula`（力量公式）。

**公式**：`baseValue + (floor + floorOffset) * floorMultiplier / floorDivisor`，保证 `minValue` 下限。

### 3.7 DeckPresetCatalog (`Scripts/Data/DeckPresetCatalog.cs`)

**职责**：角色预设卡组（3 个核心角色）。

**属性**：`Id`、`Name`、`Description`、`CardIds`、`Glyph`（图标）、`Accent`（主题色）、`MaxHp`、`StarterRelicId`。

### 3.8 AchievementCatalog (`Scripts/Data/AchievementCatalog.cs`)

**职责**：24 个成就的定义数据。每个成就包含 `Id`、`Icon`、`NameKey`、`DescriptionKey`、中英文 fallback。内置 `ZhText` 字典提供中文翻译。

### 3.9 GameDataAccess (`Scripts/Data/GameDataAccess.cs`)

**职责**：文件读取抽象层，使测试项目可在不依赖 Godot 引擎的情况下加载数据文件。

---

## 4. Systems 系统层

### 4.1 CombatResolver (`Scripts/Systems/CombatResolver.cs`)

**职责**：伤害计算。

**核心方法**：

```csharp
DamageResolution ResolveHit(int baseDamage, int attackerStrength, int targetVulnerable, int targetBlock, int targetHp)
```

**计算流程**：
1. `rawDamage = max(baseDamage + attackerStrength, 0)`
2. 易伤加成：`finalDamage = ceil(rawDamage * 1.5)`（若有易伤）
3. 格挡抵消：`blocked = min(block, finalDamage)`
4. 穿透伤害：`taken = max(finalDamage - blocked, 0)`

**返回值**：`DamageResolution`（finalDamage、blocked、taken、remainingBlock、remainingHp）。

### 4.2 CardEffectPipeline (`Scripts/Systems/CardEffectPipeline.cs`)

**职责**：卡牌效果的调度引擎。使用处理器注册表模式，支持通过 `RegisterOrReplaceHandler` 扩展新效果类型。

**接口**：`ICardEffectRuntime` — 定义了 7 种效果回调（ExecuteDamage、ExecuteGainBlock、ExecuteApplyVulnerable、ExecuteGainStrength、ExecuteGainEnergy、ExecuteHeal、ExecuteDiscardCards）。

**特殊处理**：DrawCards 和 DiscardCards 效果在 pipeline 中直接处理（无需回调）。支持 `Replay`（卡牌重复打出）和每个效果的 `Repeat` 参数。

### 4.3 TurnFlowResolver (`Scripts/Systems/TurnFlowResolver.cs`)

**职责**：回合开始/结束逻辑。

- 回合开始：能量恢复、格挡重置
- 回合结束：易伤层数衰减、手牌移入弃牌堆

### 4.4 DeckFlowResolver (`Scripts/Systems/DeckFlowResolver.cs`)

**职责**：抽牌和洗牌逻辑。

```csharp
DeckDrawResult DrawIntoHand(drawPile, discardPile, hand, count, handLimit, rng)
```

- 抽牌堆空时自动从弃牌堆洗入
- 手牌上限保护（默认 10 张）
- 返回抽到的牌、是否超上限、洗牌次数

### 4.5 IntentResolver (`Scripts/Systems/IntentResolver.cs`)

**职责**：敌人 AI 意图选择。为全部 29 种敌人原型提供独特的 `switch` 分支。

**意图类型**：`Attack`（攻击）、`Defend`（格挡）、`Buff`（强化）。

**设计原则**：
- 基础敌人：概率混合攻击/防御/Buff
- 特殊敌人：固定模式（如古代机械 2 回合蓄力 → 毁灭打击）
- Boss：多阶段模式（如腐化之心 5 回合 P1 → 3 回合 P2）

### 4.6 EnemyEncounterBuilder (`Scripts/Systems/EnemyEncounterBuilder.cs`)

**职责**：根据遭遇类型、楼层和章节构建敌人组。

```csharp
List<EnemyUnit> BuildEncounter(MapNodeType type, int floor, int act, Random rng)
```

**核心逻辑**：
- NormalBattle：1-3 个敌人（根据楼层）
- EliteBattle：1-2 个敌人
- Boss：按章节索引选择对应 Boss
- 从合格敌人池中随机选取子集

### 4.7 GameRng (`Scripts/Systems/GameRng.cs`)

**职责**：基于 xorshift64* 的确定性随机数生成器。继承 `System.Random` 以兼容现有代码。状态可序列化，支持快照/恢复。

### 4.8 CombatVisualCatalog (`Scripts/Systems/CombatVisualCatalog.cs`)

**职责**：敌人和遗物的视觉配置中心。

- 29 个敌人各有一个 `EnemyVisualProfile`（ID、名称、图片路径、舞台色调）
- 内置中英文敌人名称表 `EnemyDisplayNameZh`
- 内置中英文特征描述表 `TraitFallbacks` / `TraitFallbacksZh`
- 遗物图标路径映射

### 4.9 AchievementState (`Scripts/Systems/AchievementState.cs`)

**职责**：成就的持久化追踪。解锁状态保存到 `achievements.json`。

- `TryUnlock(id)`：解锁并标记本局新获得
- `EvaluateRunEnd(state)`：通关/失败时评估所有成就条件
- `GetNewThisRun()`：获取本局新解锁的成就（用于弹窗）

---

## 5. Scenes 场景层

### 5.1 BattleScene (`Scripts/Scenes/BattleScene.cs`)

**职责**：战斗场景的主控制器。最复杂的脚本，分为 8 个 partial 文件。

**核心常量**：

| 常量 | 值 | 说明 |
|------|-----|------|
| MaxEnergy | 3 | 每回合基础能量 |
| HandLimit | 10 | 手牌上限 |

**回合流程**：

1. **StartPlayerTurn()**：恢复能量、重置 counter、触发遗物、抽牌
2. **玩家出牌**：检查目标/费用/能量 → 扣除费用 → 触发遗物 → CardEffectPipeline → 更新状态
3. **EndTurn**：手牌移入弃牌 → 敌人执行意图 → 状态衰减
4. **胜利/失败检测**

**遗物集成**：在 SetupFromGameState、StartPlayerTurn、出牌、受伤、EndTurn 各处插入遗物效果检测和触发。

**卡牌出牌流程** (OnCardDropAttemptAsync)：

1. 检查有效目标
2. 遗物费用减免（dusty_scroll / spellbook / overclock_core / shadow_step）
3. 检查能量 ≥ 实际费用
4. 扣除能量
5. 递增计数器（cardsPlayedThisTurn / attacksPlayed 等）
6. 触发出牌遗物（echo_coin / shuriken / storm_feather 等）
7. 计算遗物攻击加成（whetstone + glass_meteor + pen_nib + hourglass + wrist_blade）
8. 执行 CardEffectPipeline
9. 处理 Exhaust / Dead Branch / Ember Chisel

### 5.2 MapScene (`Scripts/Scenes/MapScene.cs`)

**职责**：地图场景——显示路线图、节点交互、状态信息。

**功能**：
- 渲染 3 章 × 10 行的路线图
- 节点选择（NormalBattle / EliteBattle / Event / Rest / Shop）
- 支持缩放（滚轮）和拖拽
- 显示 HP / 金币 / 卡组 / 遗物 / 药水信息
- 语言切换后强制修正齿轮按钮文字

### 5.3 EventScene (`Scripts/Scenes/EventScene.cs`)

**职责**：随机事件场景——展示事件描述和选项，处理玩家选择。

**事件类型**：
- **正面**：远古神龛、神秘酿造、流浪治疗师、卡牌祭坛、神秘宝箱
- **风险**：黑市商人、黑暗熔炉、血之仪式、赌场
- **战斗**：强盗伏击、史莱姆坑、诅咒神像

**架构**：每个事件返回 `List<EventOption>`，选项带有回调。支持子菜单（如神龛取遗物的确认步骤）。所有文字通过 `Zh` 属性实现中英双语。

### 5.4 ShopScene (`Scripts/Scenes/ShopScene.cs`)

**职责**：商店——售卖卡牌/遗物/药水，提供移除卡牌服务和抢劫选项。

**功能**：
- 展示 8 张卡牌 + 4 个遗物 + 6 瓶药水
- 会员卡遗物自动 20% 折扣
- 购买/移除/抢劫操作
- 抢劫触发 MerchantFight

### 5.5 RestScene (`Scripts/Scenes/RestScene.cs`)

**职责**：篝火节点——选择休息（回复 HP）或锻造（升级卡牌）。

- 休息：回复 `MaxHp * 30%`（腌肉遗物 50%），捕梦网自动升级一张随机牌
- 锻造：从可升级卡牌中选择一张升级，支持右键预览升级后效果

### 5.6 RewardScene (`Scripts/Scenes/RewardScene.cs`)

**职责**：战后奖励——选择遗物/卡牌包/药水。

### 5.7 VictoryScene / DefeatScene (`Scripts/Scenes/VictoryScene.cs` / `DefeatScene.cs`)

**职责**：结局画面——展示通关/失败信息和运行统计，触发成就评估和弹窗。

---

## 6. UI 组件层

### 6.1 CardView (`Scripts/UI/CardView.cs`)

**职责**：手牌卡牌渲染。`PanelContainer` 子类，包含名称、类型标签、卡图、费用徽章、关键词芯片、描述文本。

**关键方法**：

| 方法 | 说明 |
|------|------|
| `Setup(card)` | 绑定卡牌数据并刷新显示 |
| `SetPreviewDescription(text)` | 设置拖拽时的动态预览描述 |
| `ClearPreviewDescription()` | 清除预览描述 |
| `RefreshText()` | 刷新所有文本显示（响应语言切换） |

**视觉反馈**：`SetFocusState(focused, dimmed)` 控制卡牌在悬停/非悬停时的明暗变化。

### 6.2 EnemyCardView (`Scripts/UI/EnemyCardView.cs`)

**职责**：敌人卡牌（Button 子类）。显示敌人立绘、HP 条、意图徽章、状态芯片。

**设计要点**：
- `Theme = null` 移除 Godot Button 默认主题，确保透明背景
- `PortraitBg` ColorRect 在 `CacheNodes()` 中立即设为透明
- 仅 hover 时有微弱光晕，选中状态无边框

### 6.3 PlayerCardView (`Scripts/UI/PlayerCardView.cs`)

**职责**：玩家状态面板（Control 子类）。显示角色立绘、HP 条和状态芯片。

**关键方法**：

| 方法 | 说明 |
|------|------|
| `SetPortrait(path)` | 加载角色战斗肖像 SVG |
| `Configure(player, inputLocked)` | 绑定玩家数据并刷新 |

### 6.4 RunStatusOverlay (`Scripts/UI/RunStatusOverlay.cs`)

**职责**：浮动状态栏——在多个场景（战斗、商店、事件等）中显示 HP、金币、卡组、遗物、药水。

### 6.5 NodeSettingsOverlay (`Scripts/UI/NodeSettingsOverlay.cs`)

**职责**：齿轮设置浮层（CanvasLayer）——在任意场景中通过齿轮按钮打开。提供显示/音频设置和返回主菜单/重新进入当前节点功能。所有文字使用直接语言判定实现双语。

### 6.6 AchievementPopup (`Scripts/UI/AchievementPopup.cs`)

**职责**：成就解锁弹窗——右上角滑入动画 + 自动消散。

---

## 7. Localization 多语言

### LocalizationService (`Scripts/Localization/LocalizationService.cs`)

**职责**：静态 key-value 多语言查找服务。

**语言**：`GameLanguage.En` 和 `GameLanguage.ZhHans`。

**加载机制**：
1. 优先从 Godot 资源系统读取 `res://Data/Localization/{en|zh_hans}.json`
2. 失败则通过 `ProjectSettings.GlobalizePath` 转换路径后用 `System.IO.File` 读取
3. 再失败则从 `AppContext.BaseDirectory` / `GetCurrentDirectory()` 搜索

**核心方法**：

| 方法 | 说明 |
|------|------|
| `Get(key, fallback)` | 按当前语言查找，未找到返回 fallback |
| `Format(key, fallback, args...)` | 格式化字符串版本 |
| `Load()` | 首次调用时延迟加载 JSON |
| `Reload()` | 清空缓存并重新加载 |

**语言切换**：`LocalizationSettings.ToggleLanguage()` → 触发 `LanguageChanged` 事件 → 各场景的 `RefreshText()` 更新 UI。

---

## 8. External 外部接口

### ExternalControlService (`Scripts/Autoload/ExternalControlService.cs`)

**职责**：TCP 桥接服务器，监听 `127.0.0.1:47077`，使用 JSON-line 协议。

**支持的命令**：`ping`、`get_snapshot`、`execute_action`（start_new_run、play_card、end_turn、choose_reward_type 等）。

### ExternalModels (`Scripts/External/ExternalModels.cs`)

**职责**：外部控制的 DTO 模型定义。

---

## 9. 项目配置文件

### cards.json (`Data/cards.json`)

**结构**：`{ "cards": [...], "starterDeck": [...], "rewardPool": [...], "characterPools": {...} }`

每个卡牌条目：`id`、`name`、`description`、`kind`、`cost`、`effects[]`、`descriptionZh`、`nameKey`、`descriptionKey`、`upgrade`（可选）、`keywords`（可选）。

### enemies.json (`Data/enemies.json`)

**结构**：`{ "archetypes": [...], "encounters": { "NormalBattle": [...], "EliteBattle": [...], ... } }`

### relics.json (`Data/relics.json`)

**结构**：`{ "relics": [...] }`，每个遗物：`id`、`name`、`description`、`rarity`、`archetype`。

### project.godot

**Autoload 注册**：GameState、AppSettings、ExternalControlService、AudioManager。
