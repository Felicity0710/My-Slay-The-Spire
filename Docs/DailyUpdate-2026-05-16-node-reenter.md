# 今日更新日志（2026-05-16）节点设置 — 重新进入当前结点

## 一、今日概览

给每个节点（战斗 / 商店 / 普通事件 / 篝火 / 幕间事件）加了一个统一的「设置」浮层，内含一个功能：**重新进入当前结点**。点击后：

- 回滚到「**点击这个节点之前**」的存档（即玩家还在地图上、还没踏进该结点）；
- 所有随机数（RNG state）一起回滚；
- 切场景到 MapScene，玩家可以再次点同一个节点（或选别的可达节点）。
- 再次点同一个节点 → 随机结果与第一次**完全一致**（同样的商店货品 / 同样的敌人 / 同样的事件），因为 `state.ChooseMapNode`、`BeginRandomEvent`、`BuildInventory` 等会按相同的 RNG state 重放。

举个例子：进商店 → 选「抢商店」打了一半 → 按「Re-enter」→ 回到地图，状态如同还没点商店 → 再点同一个商店 → 看到的还是那家店、同样的商品、同样的价格 → 这次选「买东西」而不是抢。

实现思路完全按需求来：在 `MapScene.OnNodePressed` 最开头（在 `ChooseMapNode` 之前）保存一份 snapshot（含 RNG state、HP、Gold、Deck、Map 位置、各种 Pending 字段）。按 Re-enter 时把 snapshot 写回 GameState、再 ChangeScene 到 MapScene。

## 二、新增 / 修改

### 1) `GameRng` —— 可快照的确定性随机数

`Scripts/Systems/GameRng.cs`（新文件）

- 继承自 `System.Random`，所以现有所有签名为 `Random rng` 的方法（`IntentResolver.RollEnemyIntent` / `DeckFlowResolver.ShuffleInPlace` / `CardUpgradeRules.MaybeUpgrade` 等）零改动接受它。
- 状态是一个 `ulong State { get; set; }`，xorshift64* 算法。把当前 state 抄下来即可"快照"，写回去即"还原"。
- 重写了 `Next() / Next(int) / Next(int,int) / NextDouble() / Sample()`，覆盖了项目里实际用到的全部入口。基类内部的状态留空，不浪费正确性。

### 2) `GameState` —— 唯一 RNG + 快照 / 还原

`Scripts/Autoload/GameState.cs`

- `_rng` 类型从 `Random` 改为 `GameRng`；新增公开只读属性 `public GameRng Rng => _rng;` 供场景共享。
- `ResetRandom(int? seed)` 改为构造 `GameRng((ulong)seed)`。
- 新增嵌套类 `NodeEntrySnapshot`，覆盖所有「一场单节点中可能被改的状态」：
  - `PlayerHp / MaxHp / Gold`
  - 集合：`DeckCardIds / Relics(=RelicIds) / PotionIds`（**深拷贝**——`new List<string>(...)`）
  - `PotionCharges / Floor / Act / RunCompleted / MerchantFled`
  - 地图位置：`CurrentMapRow / CurrentMapColumn / PendingMapColumn / PendingEncounterType / PendingEventId`
  - 计数器：`BattlesWon`
  - **`RngState`** — 节点入场那一刻 `_rng.State` 的副本
  - 商店相关：`ShopSnapshot` 列表（深拷贝 entries）+ `ShopSnapshotHasData / ShopSnapshotRemoveServiceUsed / PendingMerchantFightVictory`
  - 战斗奖励候选：`PendingRewardOptions / PendingPotionRewardOptions / PendingRelicOptions`
  - `SceneFilePath` — 当时所在场景的 `res://...` 路径，供 Re-enter 还原跳转目标
- 新增方法：
  - `SaveNodeEntrySnapshot(string sceneFilePath)`
  - `RestoreNodeEntrySnapshot()` → 返回 bool（无快照时 false）
  - `GetNodeEntrySceneFilePath()`
  - `ClearNodeEntrySnapshot()`
  - 公开属性 `HasNodeEntrySnapshot`

### 3) `NodeSettingsOverlay` —— 通用浮层

新文件：
- `Scenes/NodeSettingsOverlay.tscn`
- `Scripts/UI/NodeSettingsOverlay.cs`

结构：
```
NodeSettingsOverlay (CanvasLayer, layer=100)
└─ Root (Control, 全屏)
   ├─ GearButton ("Settings", 屏幕左上角 16×16 锚定)
   └─ Modal (默认 hidden)
      ├─ ModalBackdrop (半黑遮罩)
      └─ Dialog (440×280 居中 Panel)
         ├─ TitleLabel "Settings"
         ├─ HintLabel  "Rewind this node back to..."
         ├─ ReenterButton "Re-enter current node"
         └─ CloseButton "Close"
```

行为：
- `GearButton.Pressed` → 显示 modal，并根据 `state.HasNodeEntrySnapshot` 决定 `ReenterButton` 是否禁用。
- `ReenterButton.Pressed` → 读 `GetNodeEntrySceneFilePath()` → `RestoreNodeEntrySnapshot()` → 关 modal → `ChangeSceneToFile(scenePath)`。
- `CloseButton.Pressed` → 隐藏 modal。
- 所有文案走 `LocalizationService.Get(...)`，键见末尾。

### 4) 场景接入

**快照点**：唯一一处，写在 `MapScene.OnNodePressed` 最开头，**在 `ChooseMapNode` 之前**：

```csharp
private void OnNodePressed(int column)
{
    if (_isPlayingMerchantFledTransition) return;
    var state = GetNode<GameState>("/root/GameState");

    // Snapshot map state right before the click is consumed.
    state.SaveNodeEntrySnapshot("res://Scenes/MapScene.tscn");

    if (!state.ChooseMapNode(column, out var nodeType)) return;
    // ... 后面切场景的 switch ...
}
```

这一行的位置保证 snapshot 里：
- `PendingMapColumn = -1`、`PendingEncounterType` 还是上一场的值（无关紧要，下次点击会覆盖）；
- `PendingEventId` 是空（事件 ID 尚未骰）；
- HP / Deck / Relics / Potions 是地图上的状态；
- `_rng.State` 是地图上、点击之前的位置 —— 重放任何点击都会得到相同结果。

**浮层接入**：5 个节点场景都在 `_Ready` 里一行：
```csharp
AddChild(GD.Load<PackedScene>("res://Scenes/NodeSettingsOverlay.tscn").Instantiate());
```

涉及：
- `ShopScene._Ready` — 把 `_rng` 从私有 `new Random()` 改为 `state.Rng` 复用，使商店的洗牌 / 价格 jitter 跟随主 RNG。
- `BattleScene._Ready` — 同上，让敌人骰点和初始抽牌也走 `state.Rng`。
- `EventScene._Ready` / `RestScene._Ready` / `IntroEventScene._Ready` — 只挂浮层。

### 5) `BattleScene` 原有 settings modal 保留

战斗里原本就有一个分辨率/音量/FPS 等图形设置 modal。这次没有把它合并到 `NodeSettingsOverlay`，而是**叠加**——战斗界面现在会同时有两个齿轮（左上角是新的 Node Re-enter；右上角原战斗 settings 走老路径）。后续可以再合并；先以最小侵入保证两套都能工作。

## 三、为什么 RNG 改为继承 `System.Random`

原本想自己定义一个独立的 `GameRng` 类，但项目里 `Random` 作为参数类型出现在很多签名上（`IntentResolver.RollEnemyIntent(..., Random rng)`、`DeckFlowResolver.ShuffleInPlace<T>(IList<T>, Random)`、`CardUpgradeRules.MaybeUpgrade(..., Random rng)`、各处战斗测试 `new Random(seed)`）。让 `GameRng : Random` 就让 `state.Rng` 在所有这些点直接传入即可，零修改。基类构造里传 `base(0)` 仅是必须的样板调用，内部状态用不到。

测试里继续用 `new Random(seed)` 不受影响，因为那些测试只需要"可重复"而不需要"可快照"。

## 四、本地化键

```
ui.node_settings.gear     -> "Settings"
ui.node_settings.title    -> "Settings"
ui.node_settings.hint     -> "Rewind this node back to the state it was in when you stepped onto it."
ui.node_settings.reenter  -> "Re-enter current node"
ui.node_settings.close    -> "Close"
```

## 五、新增 / 调整的测试

`Tests/CombatLogicTests/Program.cs`

- `GameRng restores identical stream from snapshotted state`：构造一个 `GameRng(seed=12345)`，烧掉 4 轮，取 `State` 作为快照，再连续滚 16×3 个值（含 `Next()` / `NextDouble()` / `Next(min,max)`）作为参考。然后把 `State` 写回快照值、重新滚 16×3 个值，**逐个比对**必须完全相等。

构建：**0 错误**。
测试：**Total 51 / Failed 0**。

## 六、手动验证路径

1. 开新局，走到一个商店 → 进去后看一眼当前商品 / 价格 / 删牌服务条件。点击左上角「Settings」→「Re-enter current node」→ 回到**地图**，玩家位置和点击前一致。
2. 在地图上**再次点击同一个商店节点** → 进入的商店**所有商品、价格、删除条件**与第一次完全一致。
3. 进商店 → 选「抢商店」打了一半 → Re-enter → 回地图 → 再次点商店 → 这次选「买东西」，商品和上次一模一样。
4. 进战斗 → 记住敌人 / 手牌起手 → Re-enter → 地图 → 再次点该节点 → 同样的敌人 + 同样的起手 + 同样的回合 1 意图。
5. 进事件 → 看是哪一个事件 → 任意选项 → Re-enter → 地图 → 再次点 → 同一个事件再次出现，可以换另一选项试效果。
6. 进篝火 → 升级了 Strike → Re-enter → 地图 → 再次点 → 这次选 Heal 而不是升级。
7. 进 Intro → Re-enter → 地图（玩家位置回退到 Intro 那一行的入口）→ 再次点 → 同样的 Intro 文案 + 满血效果。
8. 地图上**节点连接发散**的情况：Re-enter 回地图后可以选别的可达节点（比如本来去 Shop，悔棋去 Event），随机结果依然按 RNG state 复现。
9. 离开节点继续推图 → 在下一个节点再 Re-enter，**只回滚到这次入场点**（也就是最近的那次点击之前），不会跨多个节点回滚。

## 七、涉及文件

```text
Scripts/Systems/GameRng.cs                    (新文件：可快照 Random 子类)
Scripts/Autoload/GameState.cs                 (_rng -> GameRng / NodeEntrySnapshot / Save+Restore+Clear)
Scripts/Scenes/MapScene.cs                    (OnNodePressed 开头 SaveNodeEntrySnapshot — 唯一快照点)
Scripts/Scenes/ShopScene.cs                   (_rng 改为复用 state.Rng / 实例化 overlay)
Scripts/Scenes/BattleScene.cs                 (同上)
Scripts/Scenes/EventScene.cs                  (实例化 overlay)
Scripts/Scenes/RestScene.cs                   (同上)
Scripts/Scenes/IntroEventScene.cs             (同上)
Scripts/UI/NodeSettingsOverlay.cs             (新文件：浮层脚本)
Scenes/NodeSettingsOverlay.tscn               (新场景)
Tests/CombatLogicTests/Program.cs             (新增 GameRng 快照测试)
```

## 八、当前局限 / 后续工作

- **MapScene 不接入 Re-enter**：地图本身就是节点之间的中转，没有"再走一次地图"的语义；不在节点列表里。
- **战斗的两层 settings**：左上 Node Re-enter + 右上原图形设置。后续可以把图形选项移进 NodeSettingsOverlay 的同一个 Modal。
- **本地化键尚未补 zh_hans/en 翻译**：当前显示英文兜底。补到 `Data/Localization/*.json` 即可。
- **奖励界面 (`RewardScene`)** 没接 Re-enter——它不算节点本体，是战斗的延续；如果想"在 RewardScene 里也能回滚到战斗胜利前"，需要在 RewardScene._Ready 单独建一份"战斗胜利时"的快照（比"进入战斗节点时"更晚），目前未实现。
- **VictoryScene / MainMenu / MapScene**：跳出节点循环，没有 overlay，符合预期。
- **测试覆盖**：现在只验证 GameRng 状态可重放。`NodeEntrySnapshot` 涉及 Node-derived 类，需要起 Godot runtime 才能实例化；这部分依赖手动验证清单覆盖。
