# 今日更新日志（2026-05-16）每幕首尾节点 — Intro 与 Boss

## 一、今日概览

延续 [DailyUpdate-2026-05-16-acts-and-victory.md](DailyUpdate-2026-05-16-acts-and-victory.md)。三幕制已经有了，但每幕的"开始"和"结束"还没有显式节点；同时升级率虽然按 Act 分档，但中段还能跳过休息直接进入下一幕。这次补上：

- **每幕开头加一个 Intro 节点**：连接到第一排所有道路，玩家踏入即回满 HP，并触发一段（暂时只是占位）特殊事件。事件池与问号事件池**互不相交**——目前用 `IntroEventScene` 单独承载，未来扩展事件池只需替换该场景里的文案/选项。
- **每幕末尾加一个 Boss 节点**：所有路径最终都汇聚到这里。Boss 暂时是一个 95+12×Floor HP、强度按 Floor 缓涨的占位敌人，复用了 Elite Sentinel 的立绘和默认意图。
- **Boss 前一行强制为篝火**：玩家不可能"原地暴毙后直接进 Boss 战"——必然先经过一次 30% Max HP 的恢复机会，可选治疗或升级一张卡。

视觉上 Intro 在地图底部、Boss 在地图顶部，都作为单一节点居中显示，与原作的"出入口节点"一致。

## 二、新增 / 修改

### 1) 数据 & 常量

`Scripts/Data/MapNodeType.cs`
- 增加 `MapNodeType.Intro` 和 `MapNodeType.Boss` 两个枚举。

`Scripts/Data/MapProgressionRules.cs`
- `RowsPerAct` 由 8 改为 **10**：1 行 Intro + 8 行常规内容（其中倒数第二行为强制 Rest）+ 1 行 Boss。
- 注释清楚标出每个 row 的语义。

`Data/enemies.json`
- 新增 archetype `act_boss_placeholder`：baseHp 95、hpPerFloor 12、visualId 复用 `elite_sentinel`，作为占位 boss。
- 新增 encounter 类型 `Boss`：单只 `act_boss_placeholder`，Strength 按 `baseValue=2 + floor/2` 增长，最低 2。

### 2) GameState 地图生成

`Scripts/Autoload/GameState.cs`

- `GenerateMap` 重写：
  - Row 0：单元素列表 `[Intro]`。
  - Row 1..RowsPerAct-3：5 列随机内容（走原来的 `RollNodeType`）。
  - Row RowsPerAct-2（"boss 前一行"）：5 列**全部** `Rest`。
  - Row RowsPerAct-1：单元素 `[Boss]`。
- `MapConnections` 改为按相邻行实际宽度建图：
  - 上一行宽 1 / 下一行宽 5：唯一的源节点连接到全部 5 个目标列。
  - 上一行宽 5 / 下一行宽 1：每列源节点都指向目标的第 0 列（汇聚到 boss）。
  - 普通行走原有的「直走 + 左 / 右 55% 概率分叉」逻辑，但限制目标列在 `[0, dstCount)`。
- `EnforceProgressionLandmarks` 重新分布：
  - Row 1：强制一个 NormalBattle（紧接 Intro 之后给个热身）。
  - Row 3：强制 Shop。
  - Row `RowsPerAct-4`（当前为 row 6）：强制 EliteBattle（中段拦路）。
  - Row 0 / RowsPerAct-2 / RowsPerAct-1 不动（由生成阶段控制）。
- `CanChooseMapNode` 中原本硬编码 `CurrentMapColumn >= MapWidth` 的兜底，改为 `>= MapLayout[CurrentMapRow - 1].Count`，避免在 Intro 行（宽 1）误判。
- `MapNodeLabel` / `MapNodeSymbol` 加 `Intro`（★ / "Act Intro"）和 `Boss`（✪ / "Boss"）。

### 3) MapScene 渲染 & 交互

`Scripts/Scenes/MapScene.cs`

- `BuildNodePositions` 不再读 `MapLayout[0].Count` 作为统一宽度；改为**逐行**计算列布局。当行宽 = 1 时，节点直接居中（X = mapWidth × 0.5），不再加 jitter，确保 Intro/Boss 视觉上是单一中心节点。
- `NodeTint` 新增两个颜色：
  - `Intro` → 冷青蓝 `(0.55, 0.85, 0.95)`。
  - `Boss` → 危险红 `(0.95, 0.20, 0.22)`。
- `OnNodePressed` switch 加两个 case：
  - **Intro**：`state.PlayerHp = state.MaxHp;` 当场回满血，然后切到 `IntroEventScene.tscn`。
  - **Boss**：`state.BeginEncounter(MapNodeType.Boss);` 然后切到 `BattleScene.tscn`。
- 注意 `MapConnections` 已自然约束玩家路径——任何走法最终都会到达 row N-2 的 Rest，再到 row N-1 的 Boss。

### 4) BattleScene 把 Boss 当 Elite

`Scripts/Scenes/BattleScene.cs`
- `SetupFromGameState` 里：
  ```csharp
  _isElite = _state.PendingEncounterType == MapNodeType.EliteBattle
      || _state.PendingEncounterType == MapNodeType.Boss;
  ```
- `_isElite` 会被 `IntentResolver.RollEnemyIntent` 用来取更暴力的攻击/防御/增益数值。Boss 占位敌人的 archetype 没在意图分支表里，所以会走 `RollDefaultIntent`，但 `_isElite=true` 让它走 elite-grade 的随机区间。

### 5) Intro 事件场景（占位）

新增 `Scenes/IntroEventScene.tscn` + `Scripts/Scenes/IntroEventScene.cs`：

- 全屏暗色背景，居中显示：
  - 大字标题「Act N Intro」。
  - HP 行：`HP {PlayerHp}/{MaxHp}`（进入时已被 MapScene 回满）。
  - 一段叙述「A wandering soul finds you on the road and tends to your wounds...」。
  - 「Continue」按钮。
- `_Ready` 二次执行 `PlayerHp = MaxHp` 作安全网（万一通过非常规路径进入），保持幂等。
- 切 UiPhase 到 `"intro"`。
- Continue → `state.ResolveEventFinished()`（复用现有事件结算入口，会调用 `AdvanceFloor`）→ 回到 MapScene，玩家从 row 1 开始选路径。

之所以单独建场景而不复用 `EventScene`，是因为用户明确要求 intro 事件池与问号事件池**不相交**。当前实现里 intro 事件池为空（只有文案），未来扩展只需在这个场景内加按钮 / 选项即可，跟 question-mark 池物理隔离。

### 6) 测试

`Tests/CombatLogicTests/Program.cs`

- 更新 `Map progression caps at 3 acts`：把 `RowsPerAct == 8` 的断言改为 `== 10`。
- 新增 `Enemy catalog contains the Boss encounter`：
  - 校验 `EncounterMembersByType.ContainsKey(MapNodeType.Boss)`。
  - 用 `EnemyEncounterBuilder.BuildEncounter(MapNodeType.Boss, floor: 8)` 真实构造一次，确认 roster 非空且 boss 的 MaxHp > 0。

**测试统计**：Total 50 / Failed 0。

## 三、构建与验证

- `powershell -ExecutionPolicy Bypass -File .\build.ps1`：0 错误。
- `dotnet run --project .\Tests\CombatLogicTests\CombatLogicTests.csproj`：50/50。
- 手动验证路径：
  1. 开新局，地图最底部是一个青蓝色 ★ Intro 单节点。点击 → 满血 → IntroEventScene → Continue → 回到地图，玩家位置在 row 1。
  2. 沿任意一条路径向上推 → 中段会经过 Shop / Elite / 各种 Event。
  3. 接近顶部时强制经过一行 ♥ Rest（5 个 Rest 节点都可选）→ 进 RestScene → Heal or Smith。
  4. 篝火上方是单个红色 ✪ Boss 节点。点击 → BattleScene 出现一只 Act Boss（外形复用 Elite Sentinel）。
  5. 击败 Boss → RewardScene → 回到地图 → `AdvanceFloor` 已让 Act + 1 → 看到新地图，又是一个 ★ Intro 在底部。
  6. 第三幕 Boss 击败 → MapScene 检测 `RunCompleted` → 跳 VictoryScene。

## 四、涉及文件

```text
Scripts/Data/MapNodeType.cs                (新增 Intro / Boss)
Scripts/Data/MapProgressionRules.cs        (RowsPerAct 8 -> 10)
Scripts/Autoload/GameState.cs              (GenerateMap / EnforceProgressionLandmarks / CanChooseMapNode / Symbol&Label)
Scripts/Scenes/MapScene.cs                 (BuildNodePositions 变宽行 / NodeTint / OnNodePressed 新增 Intro+Boss 分支)
Scripts/Scenes/BattleScene.cs              (Boss 走 _isElite=true 分支)
Scripts/Scenes/IntroEventScene.cs          (新场景脚本)
Scenes/IntroEventScene.tscn                (新场景)
Data/enemies.json                          (新增 act_boss_placeholder 和 Boss encounter)
Tests/CombatLogicTests/Program.cs          (RowsPerAct=10 + 新增 Boss encounter 测试)
```

## 五、调参点 & 后续工作

- **Boss 平衡**：当前 baseHp 95 + 12*Floor。Act 3 Boss 是 Floor ~30，对应 HP 约 95 + 360 = **455** —— 占位值，预计偏强，等真正设计完三套 boss 后再调。
- **Intro 事件池**：现在只有一段文案 + 满血。下一步需要：
  - 设计 ≥ 3 个 intro-only 事件（如：随机加 1 个 relic、+10 Max HP、获得一张特定升级卡等）。
  - 在 GameState 加 `BeginIntroEvent()`，从独立 pool 滚一个 id。
  - IntroEventScene 按 id 渲染相应内容。
- **每幕 Boss 差异化**：占位是同一只敌人。未来：
  - 在 enemies.json 给每个 Act 一只独有的 boss archetype（不同 HP、技能集、立绘）。
  - 让 `EnemyEncounterBuilder` 支持按 `Floor` 或 `Act` 索引选择 boss。
  - 或者直接 3 个独立 encounter 类型（`BossAct1` / `BossAct2` / `BossAct3`），由 `BeginEncounter` 根据 `Act` 派发。
- **Boss 战胜利的特殊奖励**：现在沿用普通战斗的奖励池。可考虑：
  - 必出一个 relic（boss relic）。
  - 必出一张升级卡。
- **Intro 节点的「连接所有道路」可视化**：因为单个节点的 `MapConnections[0][0]` 已经是 `[0..4]`，5 条连线会同时从 Intro 发散到 row 1 的 5 个节点。如果觉得线条太密，可以在 MapCanvas 渲染时给从单节点出发的连线降低 alpha 或加曲线效果。
- **路径剪枝**：现在 row 8 强制 5 个 Rest，意味着无论玩家怎么走最后一行都是篝火。这是设计意图（用户明确要求），但视觉上 5 个并列的 Rest 略冗余。可以考虑让 row 8 也压缩成单一中心节点（参考 Intro/Boss 的渲染方式）—— 改 1 行：`rowTypes = new List<MapNodeType> { Rest };`，但要相应改 connections 和 BuildNodePositions 的 jitter。
