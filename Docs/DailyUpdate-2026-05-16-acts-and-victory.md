# 今日更新日志（2026-05-16）三幕制 + 胜利结算 + 按层升级率

## 一、今日概览

接上一份 [DailyUpdate-2026-05-16-probabilistic-upgrades.md](DailyUpdate-2026-05-16-probabilistic-upgrades.md)，今天做了两件主线工作：

1. **三幕制**：原本玩家可以无限爬塔，现在打通 3 个完整 Act 后游戏结束。
2. **按层升级率**：之前是恒定 25%。现在按当前 Act 决定升级概率（10% / 25% / 40%），越深的层升级卡越常见。

## 二、新增 / 修改

### 1) `MapProgressionRules` 静态规则类

`Scripts/Data/MapProgressionRules.cs`（新文件）

```csharp
public static class MapProgressionRules
{
    public const int MaxActs = 3;
    public const int RowsPerAct = 8;

    public static double UpgradeChanceForAct(int act)
    {
        if (act <= 1) return 0.10;
        if (act == 2) return 0.25;
        return 0.40;
    }
}
```

把"地图前进的常量 + 升级率公式"集中到一个纯静态类，便于直接单元测试，也方便以后调参（线性插值、爬塔越高翻倍等都改这一个文件）。

### 2) GameState 状态与流程

`Scripts/Autoload/GameState.cs`：

- 私有 `MapRows = MapProgressionRules.RowsPerAct`（直接复用常量，避免硬编码）。
- 新增公有状态：
  - `int Act { get; private set; } = 1;`
  - `bool RunCompleted { get; private set; }`
- `StartNewRun` 把 `Act = 1; RunCompleted = false` 一并重置。
- `AdvanceFloor()` 改写：当 `CurrentMapRow >= MapRows` 时：
  - 计算 `nextAct = Act + 1`。
  - 若 `nextAct > MaxActs` → `RunCompleted = true`，**不再生成新地图**，方法直接返回；玩家位置保持在"刚踏出最终节点"的状态以便快照/外部接口仍合理。
  - 否则 `Act = nextAct; GenerateMap();`。
- 移除常量 `DefaultUpgradeChance`；改为成员方法 `CurrentUpgradeChance()` 走 `MapProgressionRules.UpgradeChanceForAct(Act)`。
- `MaybeUpgradeCardId(string id, double chance = -1.0)`：`chance < 0` 时回退到 `CurrentUpgradeChance()`，所有现有调用点都不传 chance，所以自动跟着 Act 走。

### 3) `VictoryScene`

- `Scenes/VictoryScene.tscn` 新增：黑色背景 + 大字 `Victory` 标题 + 副标题（"You have cleared all 3 acts."）+ 一行简报 + 返回主菜单按钮。
- `Scripts/Scenes/VictoryScene.cs`：
  - `_Ready` 把 `UiPhase` 切到 `"victory"`。
  - 简报展示 `Floors climbed / Battles won / Final HP / Gold`，全部从 `GameState` 读。
  - 监听 `LocalizationSettings.LanguageChanged` 重新刷新文案。
  - 「Back to Main Menu」按钮 → 切回 `MainMenu.tscn`。

### 4) `MapScene` 入口检测

- `_Ready` 一开始就拿 `GameState`，若 `state.RunCompleted == true`，`CallDeferred(GoToVictoryScene)` 后直接返回，跳过整个地图布局逻辑（避免空地图引发崩溃）。
- `GoToVictoryScene()` 简单地切到 `res://Scenes/VictoryScene.tscn`。
- `RefreshUi` 的 run-info 头里加了 `Act X/3` 字段，排在 `Floor` 前面，便于第一眼看到当前进度。

### 5) 关键流程闭环

无论玩家在第 3 幕的最后一个节点走的是哪个分支：

- **战斗胜利**：`BattleScene.OnVictoryAsync` → `ResolveBattleVictory` → `AdvanceFloor` 设置 `RunCompleted` → `RewardScene` → `ExitToMap` → `MapScene._Ready` 检测 → `VictoryScene`。
- **篝火**：`RestScene` 的任一操作（Rest/Smith/Skip）都最终走 `AdvanceFloor`，然后回 `MapScene` → 同样命中胜利分支。
- **商店**：`ShopScene.OnLeavePressed → ResolveShopExit → AdvanceFloor → MapScene → VictoryScene`。
- **事件**：`EventScene` 任意选项 → `ResolveEventFinished → AdvanceFloor → MapScene → VictoryScene`。

商人战胜利那条特殊路径**不会**触发 RunCompleted（它走 `ResolveMerchantFightVictory`，根本没调 `AdvanceFloor`），符合预期——抢劫商店并不算"打通本层"。

## 三、新增单元测试

`Tests/CombatLogicTests/Program.cs`：

- `Upgrade chance scales with Act`：
  - `UpgradeChanceForAct(1) < UpgradeChanceForAct(2) < UpgradeChanceForAct(3)`。
  - `UpgradeChanceForAct(4) >= UpgradeChanceForAct(3)`（高于 3 的 act 也夹住）。
  - `UpgradeChanceForAct(0)` 不超过 act 1（边界）。
  - 全部落在 `[0, 1]`。
- `Map progression caps at 3 acts`：硬验证 `MaxActs == 3 && RowsPerAct == 8`，防止常量被无意改回旧值。

**测试统计**：Total 49 / Failed 0。

## 四、构建与验证

- `powershell -ExecutionPolicy Bypass -File .\build.ps1`：0 错误。
- `dotnet run --project .\Tests\CombatLogicTests\CombatLogicTests.csproj`：49/49 通过。
- 手动验证路径：
  1. 开新局 → 地图顶部应该看到 `Act 1/3   Floor 1   ...`。
  2. 一路打到地图最上方完成 8 个节点 → 进 Act 2，标签变成 `Act 2/3   Floor 9   ...`。
  3. 再打通 8 节点 → 进 Act 3，标签变成 `Act 3/3`。
  4. 打通 Act 3 任意终点节点（战斗 / 休息 / 商店 / 事件均可）→ 自动跳到 `VictoryScene`，按「Back to Main Menu」回到主菜单。
  5. 一边推进一边在战后奖励刷牌：Act 1 看到升级版的频率明显低（10%），Act 3 大量出 `+` 卡（40%）。

## 五、调参点

- 想加速 / 减速通关：改 `MapProgressionRules.MaxActs`。
- 想给每个 Act 更长地图：改 `MapProgressionRules.RowsPerAct`（同时检查 `EnforceProgressionLandmarks` 的硬编码行号是否需要相应调整）。
- 想换成线性升级率而非阶梯：把 `UpgradeChanceForAct` 改成 `0.05 + (act - 1) * 0.15` 之类。
- 想给"通关 Boss"专属遭遇：可以在 `AdvanceFloor` 看到末层 elite 时改用 `MerchantFight` 思路，加一个 `MapNodeType.ActBoss`。

## 六、涉及文件

```text
Scripts/Data/MapProgressionRules.cs       (新文件：MaxActs / RowsPerAct / UpgradeChanceForAct)
Scripts/Autoload/GameState.cs             (Act / RunCompleted / AdvanceFloor 三幕逻辑 / CurrentUpgradeChance)
Scripts/Scenes/MapScene.cs                (入口检测 RunCompleted / runInfo 加 Act 字段)
Scripts/Scenes/VictoryScene.cs            (新场景脚本)
Scenes/VictoryScene.tscn                  (新场景)
Tests/CombatLogicTests/Program.cs         (UpgradeChanceForAct + MapProgressionRules 测试)
```

## 七、后续可改进

- 把 Act 切换做成"进入下一幕"的过场动画或弹窗，而不是仅靠 UI 标签变化。
- 给胜利结算页加一些奖励或解锁内容（最少卡牌、试玩时间、随机种子等）。
- 商店里升级版加价已经是 +30g 的固定值；Act 3 概率涨到 40% 后，可以考虑把"升级版"的溢价改成按 Cost 百分比，避免高费升级卡价格相对便宜。
- 给 ExternalControl 接口加 `Act` / `RunCompleted` 快照字段，便于 Bot 训练时识别"已通关"。
