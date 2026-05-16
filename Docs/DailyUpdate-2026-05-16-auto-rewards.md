# 今日更新日志（2026-05-16）战斗奖励：多组掉落 + 每组单选

## 一、今日概览

把战斗奖励从「遗物 / 卡牌包 / 药水 三选一」改成「每类各掉落多个、每类各最多挑 1 个或跳过」，并把金币改成被动直接进账（不再放进选择 UI 里）。

- **金币**：胜利时直接 `AddGold`。普通 ~22（18–27），紧急作战 ~50（40–59）。
- **卡牌**：掉落多张，**每场最多拿 1 张或跳过**。普通 3 张候选，紧急作战 4 张（升级率额外 +0.20）。
- **药水**：掉落多瓶，**每场最多拿 1 瓶或跳过**。普通 2 瓶（全池随机），紧急作战 3 瓶（高阶池：Strength / Guard / Fury）。
- **遗物**：仅紧急作战（Elite / Boss）会掉，掉 2 个，**最多挑 1 个或跳过**。
- 充能符 / 鲜血瓶仍然被动加血，照旧。

所有三类候选物同时显示在战后界面，玩家在各组独立按按钮拿或跳过；最后按一次 Continue 回地图。

## 二、新增 / 修改

### 1) `GameState` — 滚 offers，不再自动入仓库

`Scripts/Autoload/GameState.cs`：

- 简化后的 `BattleRewardSummary`（只剩**被动**部分）：
  ```csharp
  public bool IsEliteTier;
  public int GoldGained;
  public int HealedFromCharm;
  public int HealedFromBloodVial;
  ```
  原来记录"已发的卡 / 药水 / 遗物"那些字段全部移除，因为现在卡 / 药水 / 遗物都是 offers，由玩家自己挑。
- 新增公开字段 `List<string> PendingPotionRewardOptions = new();`，跟现有的 `PendingRewardOptions` (cards) 和 `PendingRelicOptions` 并列。
- 新增私有方法 `RollBattleRewardOffers()`：
  - 清空三组 pending 列表。
  - 算 isElite = `Encounter == EliteBattle || Encounter == Boss`。
  - 应用 charm / blood_vial 回血到 `PlayerHp`，把回血量记进 summary。
  - 直接 `AddGold(...)`，金币不进 picker。
  - 滚卡：从 reward pool 去 strike/defend、洗牌取 3（精英 4）；每张过 `MaybeUpgradeCardId(chance)`，精英在 Act 基础升级率 +0.20（夹到 [0,1]）。结果写入 `PendingRewardOptions`，**不加进牌库**。
  - 滚药水：精英从 `EliteTierPotionPool` 取 3，普通从全池取 2，写入 `PendingPotionRewardOptions`。
  - 滚遗物：仅精英；从 `RelicData.AllRelicIds()` 去掉已拥有、洗牌取 2，写入 `PendingRelicOptions`。
  - 保存 `LastBattleReward`（只剩金币 + 回血信息）。
- `ResolveBattleVictory()` 现在三行：`BattlesWon++; RollBattleRewardOffers(); AdvanceFloor();`
- 新增公开方法供 `RewardScene` 调用：
  - `bool TakeRewardCardOption(int idx)`：把第 idx 张候选卡加进 `DeckCardIds`，清空 cards offer。
  - `bool TakeRewardPotionOption(int idx)`：尝试加入潜在 idx 的药水（药水栏满时返回 false 但仍清空 offer——玩家做出了"我要这瓶但放不下"的选择，跳过即可）。
  - `bool TakeRewardRelicOption(int idx)`：加入 relic。
  - `SkipRewardCards()` / `SkipRewardPotions()` / `SkipRewardRelics()`：单独清空一类。
  - `ClearBattleRewardOffers()`：玩家按 Continue 时全部清空（兜底）。
- 每个 `TakeReward*` 方法都在选完后**清空整组 offer**，从而强制实现"每组最多 1 个"。

### 2) `RewardScene.tscn` — 三段 picker

`Scenes/RewardScene.tscn` 整体重写。结构：

```
RewardScene
└─ Margin
   └─ Scroll
      └─ RootVBox
         ├─ BannerWrap / Title
         ├─ SummaryLabel              (Gold +N + charm/blood_vial 信息)
         ├─ CardSection
         │   ├─ CardSectionLabel       (Pick 1 card or skip)
         │   ├─ CardOptionsRow         (HBox, 运行时填入卡牌按钮)
         │   └─ CardSkipRow / CardSkipButton
         ├─ PotionSection              (同结构，运行时填入药水按钮)
         ├─ RelicSection               (同结构，运行时填入遗物按钮；普通战隐藏)
         └─ BottomRow / ContinueButton
```

外层加了 `ScrollContainer` 以防分辨率紧时三段塞不下。

### 3) `RewardScene.cs` — picker 驱动

`Scripts/Scenes/RewardScene.cs` 整体重写：

- `_Ready` 拿到所有节点引用，挂三段 skip 按钮 + Continue 按钮的信号。
- `RebuildAll()` → 刷新标题/概要 + 各段 picker。挑完一组（不论是 Take 还是 Skip）只刷新对应段，其它段保持当前状态。
- 各段 `Rebuild*()` 在 OptionsRow 下动态生成 Button：
  - 卡牌 Button 文本：`<Name>\nCost: N\n\n<Description>`，触发 `state.TakeRewardCardOption(idx)`。
  - 药水 Button 文本：`<Name>\n\n<Description>`；如果背包满，整组按钮 Disabled，并把段标题换成「Potion belt is full — skip or replace nothing」。
  - 遗物 Button 文本：`<LocalizedName>\n\n<LocalizedDescription>`。
- Skip 按钮直接调对应 `state.Skip*` 方法 + 刷段。
- 如果某段的 offer 列表已空（包括开局就空——比如普通战的 relic），整段 `Visible = false`，不占空间。
- Continue 按钮调 `ClearBattleRewardOffers()` + 切回 MapScene。这是兜底，万一玩家没点 Skip 也没选就 Continue，过期的 offer 不会泄漏到下一场。

### 4) `ExternalControlService` 兼容垫片

RewardScene 上的几个静态垫片：

- `TryChooseRewardTypeExternally(rewardType)` → no-op（新流程没有 type 切换）。
- `TryChooseRewardCardExternally(idx, cardId)` → 实际转 `TakeRewardCardOption`，支持按 cardId 反查 idx；返回 OK 或 "Invalid".
- `TrySkipRewardExternally()` → 等同 Continue。
- `BuildRewardSnapshot()` → 仍输出 `RewardSnapshot`，Mode = `"multi_picker"`，含当前 `PendingRewardOptions` 和 `PendingRelicOptions`（潜在的潜药水暂未走 snapshot，后续 ExternalModels 加 PotionOptions 字段时再补）。

## 三、本地化键

```
ui.reward.title_normal               -> "Battle Reward"
ui.reward.title_elite                -> "Elite Reward"
ui.reward.continue                   -> "Continue"
ui.reward.gold_line                  -> "Gold +{0}"
ui.reward.charm_heal                 -> "Lucky Charm healed {0} HP."
ui.reward.blood_vial_heal            -> "Blood Vial healed {0} HP."
ui.reward.card_section_label         -> "Pick 1 card or skip"
ui.reward.skip_cards                 -> "Skip cards"
ui.reward.potion_section_label       -> "Pick 1 potion or skip"
ui.reward.potion_section_label_full  -> "Potion belt is full — skip or replace nothing"
ui.reward.skip_potions               -> "Skip potions"
ui.reward.relic_section_label        -> "Pick 1 relic or skip"
ui.reward.skip_relics                -> "Skip relic"
```

## 四、构建与验证

- `powershell -ExecutionPolicy Bypass -File .\build.ps1`：**0 错误**。
- `dotnet run --project .\Tests\CombatLogicTests\CombatLogicTests.csproj`：**50/50**。
- 推荐手动验证路径：
  1. 普通战胜利 → RewardScene 显示金币 +X、3 张候选卡、2 瓶候选药水。**没有**遗物段。
  2. 点其中一张卡 → 卡段消失（只能拿一张）；其它两段还在。
  3. 点 Skip potions → 药水段消失。
  4. 按 Continue → 回地图。Deck 应该多了刚才那张卡，药水栏未变。
  5. 精英战胜利 → 金币 +X、4 张卡、3 瓶高阶药水、2 个遗物。
  6. 故意药水栏吃满后再打一场 → 药水段标题切换为「belt is full — skip or replace nothing」，所有药水按钮 Disabled，只能点 Skip。
  7. Continue 后再进下一场 → 不会看到上一场没拿走的候选物（offer 已被 ClearBattleRewardOffers 清空）。

## 五、调参点

```
GameState.RollBattleRewardOffers 内部硬编码：
  normal gold:           _rng.Next(18, 28)     -> 18..27
  elite  gold:           _rng.Next(40, 60)     -> 40..59
  normal card offers:    3
  elite  card offers:    4
  elite  upgrade bonus:  +0.20  (Math.Clamp 限定 [0,1])
  normal potion offers:  2 (全池)
  elite  potion offers:  3 (Strength / Guard / Fury)
  elite  relic offers:   2 (未拥有列表)
```

## 六、涉及文件

```text
Scripts/Autoload/GameState.cs                 (BattleRewardSummary 收缩 / PendingPotionRewardOptions / Roll + Take/Skip 系列)
Scripts/Scenes/RewardScene.cs                 (整体重写为三段 picker)
Scenes/RewardScene.tscn                       (重建为带 Scroll 的三段布局)
Scripts/Scenes/BattleScene.cs                 (沿用 LastBattleReward 读 charm/blood_vial 回血量；其余不变)
```

## 七、后续可改进

- **快照接口**：`RewardSnapshot` 目前没有 `PotionOptions` 字段，外部 bot 看不到药水候选。补这个字段需要同时改 `Scripts/External/ExternalModels.cs` + `RewardScene.BuildRewardSnapshot` + `ExternalControlService.BuildRewardSnapshotFromState`。
- **药水替换**：药水栏满时现在只能跳过。可在按钮上加「Replace X potion?」二级菜单。
- **预览**：卡牌按钮目前是纯文本。可换成完整 `CardView` 节点（参照 `RewardCardOptionView.tscn`）。
- **遗物预览动画**：遗物拿到手时可以触发一次小高亮，目前是静默加入 `Relics` 列表。
- **可观测性**：`LastBattleReward` 只记录金币和回血。如果想给训练管线一份"本场最终拿了什么"的日志，可以新增 `LastBattleAcquired`，在 Take* 调用里 append。
