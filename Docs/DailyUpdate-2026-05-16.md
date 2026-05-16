# 今日更新日志（2026-05-16）

## 一、今日概览

今日主要完成了商店节点的从无到有：把之前的"空过"商店节点改造成一个完整的可交互商店，引入金币系统、抢劫商店触发的特殊 Boss 战，以及商人逃跑后的过渡动画。

## 二、功能新增

### 1) 金币系统

- 在 `GameState` 中新增 `Gold` 字段：
  - 新开局起始 99 金币。
  - 普通战斗胜利奖励 15–25 金币，精英战斗胜利奖励 25–39 金币（在 `ResolveBattleVictory` 中根据 `PendingEncounterType` 区分）。
- 提供 `AddGold` / `TrySpendGold` / `TryRemoveCardFromDeck` 公共方法供商店与其他场景使用。
- 在地图界面 `RunInfoLabel` 中加入 `Gold N` 显示，与 HP、Deck、Potions 等并列。

### 2) 商店节点：基础交互

- 新增独立场景 `Scenes/ShopScene.tscn` + `Scripts/Scenes/ShopScene.cs`，UI 采用最简洁的列表+按钮布局。
- 进入商店时随机生成下列售卖物，价格随机：
  - 3 张随机卡牌（排除 strike / defend，基于卡牌 Cost 浮动定价）。
  - 最多 2 个玩家未拥有的遗物（高价）。
  - 最多 2 瓶随机药水（中等价）。
- 额外服务：一次性"删除一张手牌"，固定 75 金币（点击后弹出牌库列表覆盖层，可选择具体卡牌确认移除）。
- 购买交互：金币不足、药水栏已满等异常都有状态文本提示，购买成功后按钮置为 `Sold`。
- 离开商店后调用 `ResolveShopExit` 推进地图楼层。

### 3) 抢劫商店：触发特殊 Boss 战

- 在商店中点击「抢劫商店」不再直接获得免费物品，而是触发一场与商人之间的特殊战斗：
  - 新增 `MapNodeType.MerchantFight`（仅作内部 Encounter 标识，不会出现在地图上）。
  - 在 `Data/enemies.json` 中新增 `merchant` 原型与 `MerchantFight` 遭遇规则（baseHp 55，hpPerFloor 5，力量随楼层缓增）。
  - 商人意图通过 `IntentResolver.RollDefaultIntent` 自动产出攻击/防御/增益组合；视觉资源缺省回退到 `cultist` 立绘，作为占位实现。
- 抢劫流程：
  - 抢劫前：ShopScene 把当前商店库存（含已售状态、是否已用删牌服务）写入 `GameState.ShopSnapshot`，调用 `BeginMerchantFight` 后切换到 `BattleScene`。
  - 抢赢：`BattleScene.OnVictoryAsync` 检测 `PendingEncounterType == MerchantFight` 后走特殊分支——不进入战后奖励，不推进楼层，调用 `ResolveMerchantFightVictory` 设置 `MerchantFled = true` 和 `PendingMerchantFightVictory = true`，然后切回 `ShopScene`。
  - 抢输：沿用现有 HP≤0 的死亡分支，回到主菜单，等同战斗失败（游戏结束）。
- 抢赢返回商店：
  - ShopScene `_Ready` 检测 `PendingMerchantFightVictory && ShopSnapshotHasData`，按快照恢复物品列表（保留已售/已用删卡服务的状态），设为 `_robbed = true`，所有未售物品和删卡服务一律按免费显示。
  - 顶部状态文本切换为「你击败了商人！可以自由拿走商店里剩下的东西。」。
  - 处理完后调用 `ConsumePendingMerchantFightVictory` 和 `ClearShopSnapshot`，避免下次进入商店还触发恢复。

### 4) 商人逃跑后：地图过渡动画

- 抢劫胜利后 `MerchantFled` 保持为 true，之后地图上的所有商店节点都会变为「空商店」。
- 在 `Scenes/MapScene.tscn` 中新增 `MerchantFledOverlay`（全屏暗色遮罩 + 中心面板：标题 + 副标题），默认隐藏。
- 在 `MapScene.OnNodePressed` 的 Shop 分支：
  - 商人未逃走：照常切换到商店场景。
  - 商人已逃走：调用 `PlayMerchantFledTransition`，使用 Godot `CreateTween` 播放「淡入 0.35s → 停留 1.2s → 淡出 0.45s」的过渡动画，结束后再调用 `ResolveShopNode` 并刷新地图。
- 动画播放期间设置 `_isPlayingMerchantFledTransition = true`，避免连点其他节点；遮罩本身阻挡点击。

## 三、涉及模块（关键）

- 全局状态与商店快照：`Scripts/Autoload/GameState.cs`
- 战斗胜利分支：`Scripts/Scenes/BattleScene.cs`
- 地图节点跳转与过渡动画：`Scripts/Scenes/MapScene.cs`、`Scenes/MapScene.tscn`
- 商店场景与抢劫流程：`Scripts/Scenes/ShopScene.cs`、`Scenes/ShopScene.tscn`
- 节点类型枚举：`Scripts/Data/MapNodeType.cs`
- 商人 Boss 数据：`Data/enemies.json`

## 四、构建与验证

- `powershell -ExecutionPolicy Bypass -File .\build.ps1` 通过，0 错误（仅有项目中已存在的可空引用警告）。
- `dotnet run --project .\Tests\CombatLogicTests\CombatLogicTests.csproj` 通过：Total 40 / Failed 0。
- 推荐手动验证路径：
  1. 主菜单开新局，沿地图前进到一个商店节点。
  2. 在商店购买卡牌/遗物/药水、试用删牌服务。
  3. 点击「抢劫商店」，进入与商人的战斗：
     - 战胜后回到商店，确认所有物品和删牌服务变为免费。
     - 战败后回到主菜单，确认与普通战斗失败一致。
  4. 抢赢后继续推图，进入下一个商店节点，确认会播放「商人已逃走」过渡动画后自动跳过。

## 五、后续可改进

- 商人 Boss 当前仅复用默认意图组合 + cultist 立绘，后续可加入专属技能、专属立绘与战斗背景。
- 抢劫胜利后商店内容当前一次性恢复，可考虑加入"逃跑前丢下的少量额外货物"或"商人遗物"等差异化奖励。
- 当前抢劫战中点击战斗界面的 Back 按钮会直接回到地图，可以视为绕开战斗的口子，必要时可在战斗中禁用 Back 或改成弃权=失败。
