# 今日更新日志（2026-05-16）卡牌升级与关键字机制

## 一、今日概览

今日围绕"卡牌升级 + 杀戮尖塔风关键字"做了一整轮系统性扩展，分 7 个阶段从数据层一路推到玩家可见的 UI。最终交付：

- 卡牌升级（数据驱动 + `id+` 后缀解析）。
- 4 个关键字：保留（Retain）/ 消耗（Exhaust）/ 重放（Replay N）/ 奇巧（Curious）。
- 战斗中新增消耗堆（Exhaust Pile）可视化。
- 篝火节点改造为「治疗 30% Max HP」或「升级一张卡」二选一。
- 5 张样板升级卡 + 1 张新增 `triage` 卡。
- 6 条新单元测试，主项目和测试 0 错误，46 / 46 通过。

后续概率性升级卡掉落（商店、战斗奖励、事件）作为下一阶段任务保留。

## 二、功能与改动

### 1) 卡牌升级机制（数据层）

- `Scripts/Data/CardData.cs`
  - 新增 `CardUpgradeRecipe`：`CostDelta` / `ReplayCountOverride` / `AmountDeltas[]` / `AddKeywords[]` / `DescriptionOverride` / `DescriptionZhOverride`。
  - `CardData` 增加 `Upgrade`（基础卡上挂着的配方）、`IsUpgraded`、`BaseId`。
  - `GetLocalizedName()` 在 `IsUpgraded` 时自动追加 `+`，中英文名都生效（复用基础卡的 `NameKey`）。
  - `CreateById("xxx+")` 识别 `+` 后缀 → 查找基础卡 → `BuildUpgradedClone` 应用 recipe：逐效果加 `AmountDeltas`、`CostDelta` 改 cost（夹到 ≥0）、合并关键字去重、覆写描述。升级版 `Id="xxx+"`、`BaseId="xxx"`、`Upgrade=null`（防止二次升级）。
  - `CloneCard` 保留 `Upgrade` / `IsUpgraded` / `BaseId`。
- `Scripts/Data/CardCatalogPersistence.cs`
  - `CardEntryData.Upgrade` 子配置 + 新增 `CardUpgradeEntryData` DTO。
  - 校验 `Upgrade.AddKeywords` 字符串合法、`Upgrade.ReplayCount >= 0`。
- DeckCardIds 中升级卡以 `"strike+"` 之类的形式保存，运行时解析。

### 2) 关键字数据通路

- `CardKeyword` 枚举：`Retain` / `Exhaust` / `Curious`。
- `CardData.Keywords`（`IReadOnlyList<CardKeyword>`）和 `ReplayCount`（`int`，默认 1）。
- JSON 端字段 `keywords: [...]` 和 `replayCount: N`，可选。
- 加载器读 JSON，验证器拒绝非法关键字字符串和 `replayCount < 1`。

### 3) 消耗（Exhaust）+ 消耗堆 UI

- `Scripts/Scenes/BattleScene.cs` 加 `_exhaustPile`，每场战斗开始清空。
- 出牌后按关键字分流：`Exhaust` → 进 `_exhaustPile` + 日志「{name} is exhausted」；否则进 `_discardPile`。
- `Scenes/BattleScene.tscn` 在牌堆按钮行加 `ExhaustPileButton`。
- `Scripts/Scenes/BattleScene.PileViewer.cs` 把旧的 `bool _pileViewerShowsDrawPile` 重构成 `PileViewerTarget { Draw, Discard, Exhaust }` 枚举，模态标题 / 按钮文本 / 自然排序方向都按枚举派生。
- `Scripts/External/ExternalModels.cs` + `BattleScene.ExternalApi.cs` 给 `BattleSnapshot` 加 `ExhaustPileCount`。
- `Scripts/UI/CardView.cs` 在卡牌正面加 `_keywordLabel`（费用下方），自动渲染关键字 + `Replay xN`，本地化键为 `ui.keyword.{xxx}`，缺失则用枚举名兜底。
- 示例：`glass_cannon+` 升级到 16 伤害 + 自身易伤 + Exhaust。

### 4) 保留（Retain）

- `Scripts/Scenes/BattleScene.cs`
  - 把回合结束的 `TurnFlowResolver.MoveHandToDiscard` 替换为本地 `MoveNonRetainedHandToDiscard()`：从后向前遍历手牌，带 `Retain` 的留在手里，并打橙色日志「{name} is retained」。
- 示例：`defend+` = 8 格挡 + Retain。

### 5) 重放（Replay N）— Echo 语义

- `Scripts/Systems/CardEffectPipeline.cs` 把效果循环外层再套一圈 `for (var r = 0; r < replays; r++)`，`replays = max(1, card.ReplayCount)`。普通卡（=1）行为不变。
- `BattleScene` 打牌时若 `ReplayCount > 1`，多打一条青色日志「{name} replays x{N}」。
- `DrawCount` 在多次循环中累加，所以一张「抽 1 + Replay 2」的卡总共抽 2 张。
- 示例：`quick_slash+` = 7 伤害 + 抽 1 张 + Replay 2（实际造成 14 伤害 + 抽 2 张）。

### 6) 奇巧（Curious）+ `DiscardCards` 效果

- `CardEffectType` 新增 `DiscardCards`。
- `ICardEffectRuntime` 加 `ExecuteDiscardCards`；pipeline 静态构造里注册 handler。
- `Scripts/Scenes/BattleScene.CardEffects.cs`
  - `BattleCardEffectExecutor.ExecuteDiscardCards` 转给 `BattleScene.ResolveDiscardCardsEffect`。
  - `ResolveDiscardCardsEffect`：从手牌里排除当前打出的卡，部分 Fisher–Yates 随机抽 `effect.Amount` 张；逐张移出手牌；带 `Curious` 的递归调用 `CardEffectPipeline.Execute` 自动结算；最后按自身关键字进消耗堆或弃牌堆。
  - Curious 自播带来的 `DrawCount` 在 v1 故意忽略，避免抽牌堆嵌套。
- 测试 stub（`RecordingEffectExecutor` / `CountingRuntime`）都补上了 `ExecuteDiscardCards`。
- 数据：
  - 新卡 `triage`（0 费 Skill，抽 2 张 + 随机弃 1 张）加入 cards.json + rewardPool。
  - `bone_shrapnel+` = 6 伤害 + 1 易伤 + Curious。

### 7) 篝火节点：治疗 / 升级 二选一

- `Scripts/Autoload/GameState.cs`
  - 新增 `RestHealAmount()`（`max(1, MaxHp * 3 / 10)`，即 30% 最大生命）、`ApplyRestHeal` / `ApplyRestSkip` / `DeckCardIsUpgradable(index)` / `ApplyRestUpgrade(deckIndex)`。
  - 旧 `ResolveRestNode()` 保留为兼容外壳，内部调 `ApplyRestHeal`。
- `Scripts/Scenes/MapScene.cs` Rest 分支不再原地治疗，改为跳转 `res://Scenes/RestScene.tscn`。
- 新建 `Scenes/RestScene.tscn` + `Scripts/Scenes/RestScene.cs`：
  - 显示 HP X/Max，三个主按钮 Rest（动态显示治疗量）/ Smith / Skip。
  - Smith 打开一个 `ItemList` 覆盖层，列出所有满足 `DeckCardIsUpgradable` 的卡（已升级 / 无升级配方的卡不出现）；无可升级时显示空状态并禁用 Confirm。
  - Confirm → `ApplyRestUpgrade(deckIndex)` → 切回 MapScene；Skip → `ApplyRestSkip`；Rest → `ApplyRestHeal`。
  - UiPhase 进入时切「rest」，退出回「map」。
- 目前可升级的样板卡：`strike` / `defend` / `quick_slash` / `glass_cannon` / `bone_shrapnel`。

## 三、新增 / 修改的关键文件

```text
Scripts/Data/CardData.cs                       (CardKeyword / CardUpgradeRecipe / BuildUpgradedClone)
Scripts/Data/CardCatalogPersistence.cs         (CardUpgradeEntryData + 校验)
Scripts/Systems/CardEffectPipeline.cs          (Replay 外层循环 + DiscardCards handler)
Scripts/Scenes/BattleScene.cs                  (_exhaustPile / Retain 回合末 / Replay 日志)
Scripts/Scenes/BattleScene.CardEffects.cs      (ResolveDiscardCardsEffect + Curious 自播)
Scripts/Scenes/BattleScene.PileViewer.cs       (PileViewerTarget 三态枚举)
Scripts/Scenes/MapScene.cs                     (Rest 节点路由到 RestScene)
Scripts/Scenes/RestScene.cs                    (新场景：治疗 / 升级 二选一)
Scripts/Autoload/GameState.cs                  (RestHealAmount / ApplyRestHeal / ApplyRestUpgrade)
Scripts/UI/CardView.cs                         (关键字小标签 _keywordLabel)
Scripts/External/ExternalModels.cs             (BattleSnapshot.ExhaustPileCount)
Scripts/Scenes/BattleScene.ExternalApi.cs      (输出 ExhaustPileCount)
Scenes/BattleScene.tscn                        (新增 ExhaustPileButton)
Scenes/RestScene.tscn                          (新场景)
Data/cards.json                                (5 张升级配方 + 新卡 triage + rewardPool)
Tests/CombatLogicTests/Program.cs              (6 条新测试 + runtime stub 补口)
```

## 四、新增单元测试

```text
Upgraded card applies recipe deltas               # Strike+ 9 伤害 / IsUpgraded / BaseId
Upgraded card can add a keyword                   # glass_cannon+ 拿到 Exhaust + 16 伤害
Defend+ adds Retain keyword and boosts block      # defend+ 8 格挡 + Retain
Replay count repeats card effects                 # quick_slash+ 跑两遍效果 / DrawCount==2
Triage carries DrawCards + DiscardCards effects   # pipeline 派发 + DrawCount==2
Bone Shrapnel+ adds Curious keyword               # bone_shrapnel+ 6 伤害 + Curious
```

测试统计：**Total 46 / Failed 0**。

## 五、构建与验证

- `powershell -ExecutionPolicy Bypass -File .\build.ps1` 通过，0 错误（仅有项目原有的可空注解警告）。
- `dotnet run --project .\Tests\CombatLogicTests\CombatLogicTests.csproj` 通过：46 / 46。
- 手动验证路径：
  1. 开新局 → 走到 Rest 节点 → 进 RestScene。
  2. 选 Smith → 列表里挑 `strike` → Confirm → 回到地图，`View Deck` 看到 `strike+` 已替换原 strike。
  3. 进下一场战斗 → 打出 `strike+` → 9 伤害。
  4. 把 `glass_cannon` 升级为 `glass_cannon+`（同样在 Rest 升级）→ 打出 → 16 伤害 + 进消耗堆 → 点 Exhaust Pile 按钮可查看。
  5. `defend+`：回合末打 1 张留手；其他手牌正常进弃牌堆。
  6. `quick_slash+`：打出时日志显示「replays x2」，造成 14 伤害 + 抽 2 张。
  7. `triage` + `bone_shrapnel+` 同时在手：打 `triage` → 抽 2 + 随机弃 1，若弃中 `bone_shrapnel+` → 日志「curiously activates」+ 实际造成 6 伤害 + 1 易伤。

## 六、当前限制 & 后续工作

- 升级版卡牌的本地化采用「基础名 + `+`」的拼接策略；如果之后想给升级版独立中文名，可以新增 `card.{id}+.name` 键。
- Curious 自播的 `DrawCount` 当前丢弃，未在父 pipeline 累加。如果以后想让被弃的「抽卡 + Curious」卡也能补抽，需要把子 pipeline 的 result 桥回到外层。
- 升级版只能升一级；二阶升级（`strike++`）不支持。
- 商店 / 战斗奖励 / 事件按概率发升级卡尚未实现，作为下一阶段工作。
- 升级版的卡面文本目前是英文/中文双轨，新增升级时需要同时填两个字段；若以后想统一走本地化键再加 `card.{id}+.{name|description}` 即可。

## 七、向后兼容

- `cards.json` 中没有 `upgrade` / `keywords` / `replayCount` 字段的卡牌完全照旧工作。
- `DeckCardIds` 中没有 `+` 后缀的现存存档不受影响。
- `ResolveRestNode()` 保留为兼容外壳，老调用点不会变行为（恢复值不再是固定 18 HP，而是 30% MaxHp）。
