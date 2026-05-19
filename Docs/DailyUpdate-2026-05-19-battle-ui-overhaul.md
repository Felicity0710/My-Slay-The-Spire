# 今日更新日志（2026-05-19）战斗 UI 全面翻修：紧凑视图 + 攻击动画 + 飘字

## 一、今日概览

这一轮把战斗界面从「信息冗余 + 静态卡片」改成「紧凑视图 + 动态攻防」。三个 Phase 串起来推进：

- **Phase 1 — 信息瘦身**：删除重复 HUD、行动日志，隐藏玩家/敌人名字、敌人描述（保留 tooltip）。
- **Phase 2 — 紧凑卡片**：玩家和敌人都改用统一的紧凑视图（意图徽章 / 头顶血条 / 立绘 / 图标 buff 行），敌人数量多时自动缩放 + 分两行；舞台背景大框去掉、双方 Y 轴对齐、敌人右移与玩家关于手牌中线对称。
- **Phase 3 — 攻击动画 + 伤害飘字**：玩家/敌人攻击时实体冲向对方-命中-回位三段式动画；伤害数字下落飘字；多敌人攻击串行播放而非同帧爆炸。

附带一堆重大 bug 修复：`EffectsLayer.visible = false` 历史问题（所有视觉特效一直被隐藏）、闭包捕获已释放 Control、敌人防御/buff 飘字定位错误。

---

## 二、Phase 1：信息瘦身

### 1) 删除顶部冗余 HUD
[BattleScene.tscn:269](../Scenes/BattleScene.tscn#L269) — `TopHpLabel`（生命）和 `HandCountLabel`（手牌 | 抽牌堆 | 弃牌堆 | 敌人数）设 `visible = false`。这些信息已经在玩家卡片血条、抽牌堆/弃牌堆按钮、敌人区域里直接展示，重复显示纯粹是干扰。

保留：`TurnLabel`（回合数）+ `TestVictoryButton`。

C# 端没动 — `_topHpLabel.Text = ...` 等赋值仍在执行但写到不可见标签，零成本。

### 2) 删除行动日志
[BattleScene.tscn:698](../Scenes/BattleScene.tscn#L698) — `LogOverlay` 整个面板 `visible = false`。`Log()` 调用仍在执行（仍写日志结构），但不在画面上显示。

### 3) 隐藏玩家/敌人名称 + 敌人描述
- [EnemyCardView.cs](../Scripts/UI/EnemyCardView.cs)：`_nameLabel.Visible = false`、`_traitLabel.Visible = false`
- [PlayerCardView.cs](../Scripts/UI/PlayerCardView.cs)：`_nameLabel.Visible = false`
- 信息全部塞进 Card 的 `TooltipText`，鼠标悬停 0.5s 后显示：`{名字}\n{描述}\nNext intent: {意图详情}`

### 4) Tooltip 触发 bug 修复
**问题**：把 name/trait 隐藏后，鼠标悬停敌人卡发现 tooltip 也不出现。

**根因**：EnemyCardView 根节点是 `Button`，TooltipText 设置正确；但子节点 Margin / VBox / PortraitBg / Portrait / IntentLabel 默认 `MouseFilter = Stop`，会**吞掉** hover 事件不往上冒泡 → Button 永远收不到 hover → tooltip 不触发。

**修复** [EnemyCardView.cs:42-51](../Scripts/UI/EnemyCardView.cs#L42)：`CacheNodes` 里把所有内部结构控件统一设 `MouseFilter = Pass`，hover 穿透到根 Button。点击照常 — Pass 模式让事件先尝试当前节点再冒泡。

---

## 三、Phase 2：紧凑卡片视图

### 1) 设计目标

旧版 EnemyCardView 是 196×300 的"大卡面"，每只敌人占满一格，3 只就拥挤、4+ 就溢出。新版瘦身到 156×218，去掉冗余信息行，关键属性更显眼。

### 2) 敌人卡布局
[Scenes/EnemyCardView.tscn](../Scenes/EnemyCardView.tscn) — 从上到下：

```
┌────────────────────┐
│  ⚔  8              │ ← IntentBadge：emoji 图标 + 数字（色彩按意图类型）
├────────────────────┤
│ ████████████ 41/61 │ ← HpBar 红色填充 + 居中数字 overlay
├────────────────────┤
│                    │
│     [立绘 96px]    │ ← Portrait + TargetGlow
│                    │
├────────────────────┤
│ 🛡5  💪2  🩸1      │ ← StatusRow：图标徽章 + 层数
└────────────────────┘
```

**意图图标** [EnemyCardView.cs:152-160](../Scripts/UI/EnemyCardView.cs#L152)：
| 类型 | 图标 | 颜色 |
| --- | --- | --- |
| Attack | ⚔ | 红 `fca5a5` |
| Defend | 🛡 | 蓝 `93c5fd` |
| Buff | 💪 | 紫 `d8b4fe` |
| Other | ❓ | 灰 |

数字去掉前缀 `ATK / BLK / STR +`（图标已表达类型），保持简洁。

**状态徽章** [EnemyCardView.cs:217-264](../Scripts/UI/EnemyCardView.cs#L217)：`StatusChip(icon, stacks, accent, tooltip)` 静态方法，三种 buff 图标统一：
- 🛡 Block / 💪 Strength / 🩸 Vulnerable
- 0 层时整个徽章不显示
- 悬停徽章看完整描述（"格挡：吸收受到的伤害。(5)"）

### 3) 玩家卡同模板
[Scenes/PlayerCardView.tscn](../Scenes/PlayerCardView.tscn) + [Scripts/UI/PlayerCardView.cs](../Scripts/UI/PlayerCardView.cs)：
- 无意图徽章（玩家没有"意图"）
- 头顶绿色 HP 条（区别于敌人的红色）
- 立绘用 🧙 64pt emoji 占位（后续可换 PNG）
- 复用 `EnemyCardView.StatusChip` 静态方法 → 两边样式 100% 一致

### 4) 动态缩放
[BattleScene.cs:773-781](../Scripts/Scenes/BattleScene.cs#L773)，根据存活敌人数量调整 `cardScale`：

| 敌人数 | 列数 | 卡缩放 |
| --- | --- | --- |
| 1 | 1 | 1.00× |
| 2 | 2 | 1.00× |
| 3 | 3 | 0.92× |
| 4 | 2 (2×2) | 0.88× |
| 5 | 3 (3+2) | 0.82× |
| 6+ | 3 | 0.76× |

缩放通过 `Configure` 的 `rosterScale` 参数传入，跟 hover/selected 的 1.015×/1.035× 倍率**正确组合**（之前 PunchPanel 写法在某些路径下会冲突）。

### 5) 舞台布局重排
[BattleScene.tscn:9, 410-449](../Scenes/BattleScene.tscn#L9)：

- **去掉大框**：新增 `StyleBoxEmpty_arena_unit`，PlayerPanel 和 EnemyDropArea 都用它覆盖 panel 样式 → 那两个深蓝圆角背景消失
- **Y 轴对齐**：双方 anchor_top/bottom 统一 0.20-0.78
- **DropVBox** alignment=1 + EnemyRosterGrid `size_flags_vertical=4`(ShrinkCenter) → 敌人垂直居中
- **EnemyRosterGrid** `size_flags_horizontal=4`(ShrinkCenter) → 网格内容水平居中，不论 1 个还是 5 个敌人都对齐到右侧固定中心点
- **敌人间距**：h_separation 6→28，v_separation 6→20
- **左右对称**：
  - PlayerPanel anchor 0.04-0.30 → 中心 0.17
  - EnemyDropArea anchor 0.66-1.00 → 中心 0.83
  - 关于手牌中线 X=0.5 **完美对称**（0.17 + 0.83 = 1.0）

---

## 四、Phase 3：攻击动画 + 伤害飘字

### 1) Y 轴 punch 偏移
[BattleScene.cs:203-208](../Scripts/Scenes/BattleScene.cs#L203)：之前只有 `_playerPunchX / _enemyPunchX`（X 轴抖动），新增 `_playerPunchY / _enemyPunchY`。`_Process` 里每帧 Lerp 回 0、叠加在呼吸位移上。这样攻击者可以沿任意方向冲，而不仅是水平方向。

### 2) 三段式攻击动画

**玩家攻击敌人** [BattleScene.VisualEffects.cs:152-189](../Scripts/Scenes/BattleScene.VisualEffects.cs#L152) — `PlayPlayerAttackAnimation(target, onImpact)`：
- A. 出击 220ms：tween `_playerPunchX/Y` 朝目标方向冲 70px（Cubic ease-out）
- B. 命中：`onImpact()` 回调触发伤害飘字 + 斜线 + 闪光 + hit-stop
- C. 回位 360ms：tween 回 0（Cubic ease-out）
- 总时长 ≈ 580ms，单 tween + Finished 链式回调，**不需要 async**

**敌人攻击玩家** [BattleScene.cs:1006-1044](../Scripts/Scenes/BattleScene.cs#L1006) — `PlayEnemyAttackAnimation(enemyIndex, playerTarget, damage)`：
- 三段同上，但直接 tween 敌人卡 `Position`（每只敌人在 GridContainer 各占一格，无共享 punch 变量）
- 三段都 `await ToSignal(tween, Finished)` — 这是关键

### 3) 敌人回合改成 async
[BattleScene.cs:934](../Scripts/Scenes/BattleScene.cs#L934)：`ExecuteEnemyTurn()` → `async Task ExecuteEnemyTurn()`，攻击时 `await PlayEnemyAttackAnimation(...)`。

**为什么必须 await**：之前同步实现下 N 个敌人在同一帧全攻击，所有 lunge tween 同时启动 → 视觉混乱；紧跟着 `StartPlayerTurn` 触发 RefreshUi → 重建 EnemyCardView → 在飞的 tween 全部 orphan → **看不到任何动画**。

改 async 后每个敌人攻击都"打→等动画→下一个打"，多敌人回合像一套连续技动作。

### 4) 伤害下落飘字
[BattleScene.VisualEffects.cs:191-237](../Scripts/Scenes/BattleScene.VisualEffects.cs#L191) — `SpawnFallingDamage(target, damage, tint)`：
- 数字模型为 Label，黑色 6px outline 保证在红色血条上可读
- 字号按伤害分档：< 12 用 34pt，>= 12 用 42pt
- 单 tween：1.0s 下落 80px，淡出延后 0.35s 才开始（前 0.4s 完全清晰），0.6s 内淡完
- 时长充裕便于阅读

### 5) 标准上升飘字也延长
[BattleScene.VisualEffects.cs:251-258](../Scripts/Scenes/BattleScene.VisualEffects.cs#L251) — `SpawnFloatingText` 时长从 0.46s 拉到 ≈1.03s：
- 0→0.18s：弹出 + 上飘 22px
- 0.18→1.03s：继续上飘到 80px + 回缩
- 0.43→1.03s：淡出（延迟 0.25s 后开始，0.6s 完成）
- 影响所有正面飘字：玩家 +Block / +STR / +EN / +HP，敌人 +Block / +STR，药水 buff 文字

---

## 五、关键 Bug 修复

### Bug 1：`EffectsLayer.visible = false` 隐藏了所有视觉特效

**严重程度**：历史遗留，影响所有视觉效果。

**症状**：伤害飘字、斜线、护盾环、符文等特效"实现失败"，肉眼完全看不到。

**根因** [BattleScene.tscn:757](../Scenes/BattleScene.tscn#L757)：`EffectsLayer` 是所有视觉特效的 TopLevel 容器，但场景文件里写死了 `visible = false`。TopLevel 节点继承父节点 Visible → 所有 Spawn* 创建的特效（即使用 TopLevel=true）全部隐藏。

**修复**：删除 `visible = false`。所有特效一夜回归。

### Bug 2：敌人受伤飘字闭包捕获过期 Control

**症状**：玩家攻击敌人时，伤害数字不出现（但同一动画里的斜线、闪光正常）。

**根因** [BattleScene.CardEffects.cs:202](../Scripts/Scenes/BattleScene.CardEffects.cs#L202)：
1. `PlayPlayerAttackAnimation(effectTarget, onImpact)` 启动 220ms lunge tween
2. ApplyDamageToEnemy 末尾同步调用 `RefreshEnemyRuntimeStatusUi` → `UpdateEnemySelectionUi`
3. `UpdateEnemySelectionUi` **销毁并重建**所有 EnemyCardView（用新 HP 数据）
4. 220ms 后 lunge 完成、`onImpact` 回调触发，但闭包里捕获的 `effectTarget`（旧的 PortraitBg）已被 QueueFree
5. 在已释放的 Control 上 `GetGlobalRect()` 返回垃圾值 → label 飞到 (0,0) 看不见

**修复** [BattleScene.CardEffects.cs:189-204](../Scripts/Scenes/BattleScene.CardEffects.cs#L189)：闭包只捕获 `capturedIndex`（int），回调内通过 `EnemyEffectTarget(capturedIndex)` **重新查询**新生成的 portrait。SpawnSlashEffect / FlashPanel / PulseImpact 同样改用 freshTarget。

```csharp
var capturedIndex = enemyIndex;
PlayPlayerAttackAnimation(effectTarget, () =>
{
    var freshTarget = EnemyEffectTarget(capturedIndex);  // 重新解析
    SpawnFallingDamage(freshTarget, damageTaken, ...);
    // 其它特效也用 freshTarget
});
```

### Bug 3：敌人防御/Buff 飘字定位错误

**症状**：怪物 1 给自己上盾，飘字"+5 Block"出现在屏幕右上角的固定位置，而不是怪物 1 头上。非选中的怪物施法没有任何 shield/rune 特效。

**根因** [BattleScene.cs:972-985](../Scripts/Scenes/BattleScene.cs#L972)：
- 旧代码 `SpawnFloatingText(_enemyPanel, ...)` — `_enemyPanel` 是 Phase 2 之前的旧大卡面板（现在 `visible = false`），位置固定在屏幕右上角
- shield/rune 特效用 `if (i == _selectedEnemyIndex)` 门控，导致非选中敌人施法时没有视觉

**修复**：所有调用改用 `EnemyEffectTarget(i)`（施法者本人的 PortraitBg），去掉 selectedIndex 门控。

---

## 六、文件改动一览

| 文件 | 类别 | 关键改动 |
| --- | --- | --- |
| `Scenes/BattleScene.tscn` | 重构 | TopHud 瘦身、ActionLog 隐藏、PlayerPanel/EnemyDropArea 去框、Y 轴对齐、敌人右移对称、EffectsLayer 取消隐藏 |
| `Scenes/EnemyCardView.tscn` | 重写 | 156×218 紧凑布局：意图徽章 + HP 条 + 立绘 + 状态行 |
| `Scenes/PlayerCardView.tscn` | 重写 | 同上结构（无意图徽章），🧙 emoji 占位 |
| `Scripts/UI/EnemyCardView.cs` | 重写 | Configure 接受 rosterScale、ConfigureIntent、ConfigureHpBar、StatusChip 静态方法、MouseFilter=Pass 子控件 |
| `Scripts/UI/PlayerCardView.cs` | 重写 | 简化为 HP 条 + 状态行，复用 StatusChip |
| `Scripts/Scenes/BattleScene.cs` | 大量改动 | `_playerPunchY/_enemyPunchY` 字段、`ExecuteEnemyTurn` 改 async、`PlayEnemyAttackAnimation` 新方法、敌人列数/缩放算法、敌人防御/buff 锚点修复 |
| `Scripts/Scenes/BattleScene.CardEffects.cs` | 局部 | `ApplyDamageToEnemy` 改用 PlayPlayerAttackAnimation + capturedIndex 闭包 |
| `Scripts/Scenes/BattleScene.VisualEffects.cs` | 局部 | 新增 `PlayPlayerAttackAnimation`、`SpawnFallingDamage`、`SetPlayerPunchX/Y`；`SpawnFloatingText` 时长翻倍 |

---

## 七、验证

- `dotnet build "slay the hs.csproj"` → 0 errors（旧 nullable 警告与本次无关）
- 手动验证清单：
  - [ ] 单敌人战斗：玩家攻击 → 玩家向敌人冲、伤害数字下落、敌人闪红回血条
  - [ ] 多敌人战斗（4-5 只）：分两行排列，不重叠
  - [ ] 多敌人攻击玩家：每只敌人依次冲过来攻击，不是同帧爆炸
  - [ ] 怪物 N 给自己上盾：盾环 + 飘字都在怪物 N 头上，不是右上角
  - [ ] 鼠标悬停敌人立绘：0.5s 后显示 tooltip（名字 + 描述 + 下一意图）
  - [ ] 鼠标悬停 buff 徽章：显示具体描述 + 层数
  - [ ] 缩放/数量变化：敌人区域整体对齐玩家 Y 轴，关于手牌中线对称

---

## 八、技术债

短期内可以接受，但记下：

1. **`_enemyPanel` 引用未清理**：旧的大卡面板对象还在场景里（visible=false），有几处代码仍 `GetNode<Control>("%EnemyPanel")` 缓存。可以下一轮删除节点 + 删除 `_enemyPanel` 字段。
2. **`UpdateEnemySelectionUi` 重建所有卡**：每次 HP 变化都 QueueFree 重建。改成 in-place `Configure` 会更高效，且消除 Bug 2 类风险。这次靠"重新解析 index"绕过去，但根因还在。
3. **`_actionLog / _logText` 仍在运行**：日志记录代码继续执行但写到不可见 RichTextLabel。如果未来不需要日志查询功能可以彻底删除以减少分配。
4. **PunchPanel 仍在使用**：`PunchPanel` 旧 API 还被一些路径用（药水、状态效果等）。Phase 3 的 lunge 系统更通用，长期可以统一。
