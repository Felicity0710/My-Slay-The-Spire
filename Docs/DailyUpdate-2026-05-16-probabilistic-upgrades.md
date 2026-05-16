# 今日更新日志（2026-05-16）概率性升级卡掉落

## 一、今日概览

延续上一份「卡牌升级与关键字机制」文档（[`DailyUpdate-2026-05-16-cards.md`](DailyUpdate-2026-05-16-cards.md)），今天补上第二大阶段：把"获得卡牌"的三个入口（商店、战斗奖励、事件）接通升级抽签，并把升级判定逻辑抽到一个可被测的静态规则类里。

## 二、新增 / 修改

### 1) 静态规则类 `CardUpgradeRules`

`Scripts/Data/CardData.cs` 新增：

```csharp
public static class CardUpgradeRules
{
    public static bool CardIdHasUpgrade(string cardId);
    public static string MaybeUpgrade(string cardId, double chance, Random rng);
}
```

- `CardIdHasUpgrade` 判定 id 不是空、不以 `+` 结尾、且基础卡 `Upgrade` 配方非 null。
- `MaybeUpgrade` 按给定概率把 `"xxx"` 升级为 `"xxx+"`：
  - 不可升级 → 原样返回。
  - `chance <= 0` → 原样返回。
  - `chance >= 1` → 直接升级。
  - 其它 → 用 `rng.NextDouble() < chance` 决定。

把规则放到纯静态类有一个明确好处：可以脱离 Godot 实例化 `GameState` 单独测试。

### 2) GameState 委派

`Scripts/Autoload/GameState.cs`：
- `DefaultUpgradeChance = 0.25` 常量。
- `MaybeUpgradeCardId(string id, double chance = DefaultUpgradeChance)` 现在是 `CardUpgradeRules.MaybeUpgrade(...)` 的薄包装，用自己的 `_rng`。

### 3) 战斗奖励（`RollRewardOptions`）

`RollRewardOptions` 在挑出 build-enabler 保底卡和其它候选卡之后，每张都通过 `MaybeUpgradeCardId(...)` 走一次抽签，再写入 `PendingRewardOptions`：

```csharp
PendingRewardOptions.Add(MaybeUpgradeCardId(guaranteed));
...
for (var i = 0; i < cap; i++)
{
    PendingRewardOptions.Add(MaybeUpgradeCardId(pool[i]));
}
```

这样玩家在战后奖励界面看到的 3 张卡里就有 25% 概率出现 `xxx+` 版本。

### 4) 商店（`ShopScene.BuildInventory`）

`Scripts/Scenes/ShopScene.cs`：
- 把 `GameState` 的获取提前到方法开头，统一使用。
- 卡牌槽位先调用 `state.MaybeUpgradeCardId(...)` 拿到实际售卖 id，再用 `CardData.CreateById(rolledId)` 读卡。
- 价格规则：在原有 `45 + cost*10 + rng(0..16)` 基础上，若 `card.IsUpgraded` 再 +30 金币（升级版自然要更贵一些）。
- 因为 `ShopItem.Id` 直接存升级后的 id，购买、抢劫胜利后的快照恢复、`AddCardToDeck` 都自然带上 `+` 后缀。
- UI 端 `DescribeItem` 用 `card.GetLocalizedName()`，自动显示 `Strike+` / `打击+`。

### 5) 事件（`EventScene.DealerBuy`）

`Scripts/Scenes/EventScene.cs` 的「可疑商人」事件本来固定给 `quick_slash`，现在改为：

```csharp
state.AddCardToDeck(state.MaybeUpgradeCardId("quick_slash"));
```

所以 25% 的概率会直接给到 `quick_slash+`（伤害 7 + 抽 1 + Replay x2）。

## 三、测试

新增单元测试 `MaybeUpgradeCardId handles edge cases`：

- `MaybeUpgrade("strike", 1.0, rng)` → `"strike+"`。
- `MaybeUpgrade("strike", 0.0, rng)` → `"strike"`。
- `MaybeUpgrade("heavy_slash", 1.0, rng)` → `"heavy_slash"`（无升级配方）。
- `MaybeUpgrade("strike+", 1.0, rng)` → `"strike+"`（已升级，直通）。
- `MaybeUpgrade("", 1.0, rng)` → `""`。
- `CardIdHasUpgrade` 对 `strike` / `heavy_slash` / `strike+` 的判定符合预期。

**统计**：Total 47 / Failed 0。

## 四、构建与验证

- `powershell -ExecutionPolicy Bypass -File .\build.ps1` 通过，0 错误。
- `dotnet run --project .\Tests\CombatLogicTests\CombatLogicTests.csproj` 通过：47 / 47。
- 推荐手动验证路径：
  1. 开新局打几场战斗 → 战后奖励界面多刷几次，应能看到带 `+` 的卡（`Strike+` / `Defend+` / `Quick Slash+` / `Glass Cannon+` / `Bone Shrapnel+`）。
  2. 走到商店节点 → 商店里有概率出现 `+` 卡，标题带 `+`、价格相对未升级版高 30g。
  3. 在「可疑商人」事件选「购买卡牌」→ 大概率拿到 `quick_slash`，约 25% 概率拿到 `quick_slash+`。

## 五、参数调节点

- 全局升级概率：`GameState.DefaultUpgradeChance = 0.25`。后续如果想按楼层加成（杀戮尖塔的做法），把这个常量改成函数 `UpgradeChance(int floor)`，并让 `MaybeUpgradeCardId` 自行计算即可。
- 商店升级版加价：在 `ShopScene.BuildInventory` 中硬编码 `+30g`；可以视平衡调成基于 `card.Cost` 的百分比。

## 六、涉及文件

```text
Scripts/Data/CardData.cs                   (新增 CardUpgradeRules)
Scripts/Autoload/GameState.cs              (MaybeUpgradeCardId 委派 + RollRewardOptions 接入)
Scripts/Scenes/ShopScene.cs                (BuildInventory 接入 + 升级版加价)
Scripts/Scenes/EventScene.cs               (DealerBuy 接入)
Tests/CombatLogicTests/Program.cs          (新增 MaybeUpgradeCardId 边界测试)
```

## 七、后续可改进

- **按楼层调升级概率**：早期 5%、深处 35%，模拟 Slay the Spire 的「Ascension 越深升级越常见」。
- **保底机制**：连续 N 张奖励都没出升级时给一次必出（pity timer）。
- **遗物影响**：可以引入「下次奖励必出升级卡」的稀有遗物。
- **概率提示**：在战斗奖励 / 商店 UI 上加一个小问号 tooltip，告诉玩家当前升级率（教育向）。
- **概率 + 升级版定价的平衡测试**：跑一个 Monte-Carlo 模拟统计百场战斗的实际升级卡数量，反向调整 `DefaultUpgradeChance`。
