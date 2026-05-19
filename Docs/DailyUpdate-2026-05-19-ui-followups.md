# 今日更新日志（2026-05-19）UI 续作：设置 / 奖励 / 地图 / 事件 / 存档

## 一、今日概览

这一轮把 05-16 UI 大改后的一连串收尾工作打包推进：

- **设置界面再升级**：彻底重写 `NodeSettingsOverlay`，统一深棕 + 金边的"分组面板"视觉，按钮全部 normal/hover/disabled 状态化，并把每个分区加 emoji 头标。
- **战斗奖励重构**：把战后奖励改成"二级菜单"模型。一级菜单显示分类瓦片（卡牌 / 药水 / 遗物），点进去看具体选项，所有跳过按钮也都包裹成统一样式的按钮。
- **地图视觉重做**：节点缩小再加粗、按钮去黑底、未探索保持不透明、已探索半透明、路径改成黑色细虚线 + 微弯曲。
- **地图缩放修复**：缩放时图形不再"变形"，所有几何量按 `_zoom` 等比缩放，节点分布 / 路径形状保持稳定。
- **不期而遇模板化**：`EventScene` 改为"左图右选项"的通用模板，支持任意层级的子菜单导航，根菜单底部自动追加「离开」按钮，子菜单自动追加「返回」。
- **存档语义修正**：存档点从"进入节点前"挪到"节点完成、即将进入下一个节点之前"，跟玩家期望吻合。

---

## 二、设置界面（`NodeSettingsOverlay`）二次升级

### 1) 视觉
- Dialog 加 `StyleBoxFlat_dialog`：深棕底 (#0F0D0A 98% 不透明) + 3px 金边 + 圆角 18 + 阴影 14px。
- 四个分区（当前节点 / 显示 / 音频 / 本局）每个都用 **Header + Body 两层 Panel**：
  - Header：上圆角 + 下边线，金色 emoji 标题（📍 / 🖥 / 🔊 / 🏃）
  - Body：下圆角 + 半透明深棕底，内含真实控件
- 按钮分四种语义样式：
  - **Primary**（蓝灰底 + 金边）→ ↺ 重新进入
  - **Danger**（暗红底 + 红边）→ ← 返回主菜单
  - **Close**（深紫底 + 银边）→ ✕ 关闭，居中 220×46
  - **Gear**（屏幕右上）→ ⚙ 设置，保留 05-16 的浮按钮风格

### 2) 行为
- 标题/按钮文本由 [`NodeSettingsOverlay.cs:99-121`](../Scripts/UI/NodeSettingsOverlay.cs#L99-L121) 的 `RefreshText` 统一注入，emoji 前缀写在 C# 端，所以中英文都能拿到统一图标。
- 弹窗扩到 640×640 容纳新增分组。
- 增补本地化键：`ui.node_settings.title/section_*/hint/reenter/menu_exit/close` + `ui.options.max_fps.unlimited`。

---

## 三、战后奖励二级菜单

### 1) 设计目标
旧版把卡牌 / 药水 / 遗物三类奖励一字排开占满屏幕。问题：
- 一打开就要一次性消化所有信息
- "跳过"按钮散在各个分区，视觉混乱
- 与 Slay the Spire 的体验差距大

### 2) 新结构（一级 → 二级）
| 层级 | 内容 |
| --- | --- |
| 一级 | 💰 金币 / ❤ 治疗摘要 + 三张大瓦片（🎴 卡牌 / 🧪 药水 / 💎 遗物，只在有奖励时显示）+ 大的 ✓ Continue |
| 二级 | ← Back + 类别标题（🎴 选 1 张卡牌 / 🧪 选 1 瓶药水 / 💎 选 1 个遗物）+ 具体选项 + ✕ 跳过此类 |

- 卡牌二级用 `CardView` 网格（与查看卡组一致）；药水 / 遗物用样式化按钮卡。
- 任何"选完"或"跳过"操作自动回到一级菜单，让玩家继续选别的类别。
- 选项瓦片在没有选项时呈 disabled 灰态，依然显示 emoji 但不可点击。
- 按钮颜色语义：
  - 分类瓦片 / 卡牌：蓝灰金边
  - 药水：绿系（药剂感）
  - 遗物：深棕金边
  - 跳过 / 返回：暖灰
  - 继续：绿色强调

### 3) 关键文件
- 场景：[`Scenes/RewardScene.tscn`](../Scenes/RewardScene.tscn)
- 逻辑：[`Scripts/Scenes/RewardScene.cs`](../Scripts/Scenes/RewardScene.cs)
- 新增本地化：`ui.reward.category_*`、`ui.reward.title_normal/elite`、`ui.reward.continue/back`、`ui.reward.card/potion/relic_section_label` 等

### 4) 外部 API 兼容
`TryChooseRewardTypeExternally / TryChooseRewardCardExternally / TrySkipRewardExternally / BuildRewardSnapshot` 全部保留并对接到新流程，agent / E2E 测试无需调整。

---

## 四、地图视觉与缩放

### 1) 节点
- 尺寸：当前可选 82×82、未来不可选 72×72、已探索 62×62（全部 ×`_zoom`）
- 字号：56 / 50 / 42（全部 ×`_zoom`）
- 默认 Button 的方框深底全部用 `StyleBoxEmpty` 覆盖（normal/hover/pressed/disabled/focus 五种），节点只剩 emoji 图标
- 透明度分档：
  - `row < currentRow`（已探索）→ alpha 0.6
  - `row == currentRow` 且不可点 → alpha 0.75
  - 其它（当前可选 + 所有未来）→ alpha 1.0
- 选中可选节点轻度 Lightened(0.25)；其它行不再 Lightened，避免饱和度被洗成视觉透明

### 2) 路径
- `MapCanvas` 把所有连接画成**黑色虚线**：alpha 已探索 0.22 / 当前 0.85 / 未来 0.55
- LineWidth 1.1（更细）、DashLength 6 / GapLength 5
- 曲线由 `BuildCurve` 用二次 Bezier 算出，控制点的偏移方向由中点哈希决定 → 相邻边自动错开
- CurveOffsetRatio 收到 0.09，曲度内敛，与紧致的节点分布配合

### 3) 节点抖动（jitter）
- xStep × 0.18、yStep × 0.14（之前是 0.34/0.28，太大导致重叠）
- 抖动仍是 `Noise(row, col, salt)` 的确定性噪声，同一地图永远同一形状

### 4) 缩放等比缩放修复（关键）
**问题**：之前 `horizontalMargin = 72f`、`verticalMargin = 50f` 是固定常量，但 `mapWidth/mapHeight` 会乘 `_zoom`。zoom=2 时，边距占比从 7% 变成 3.5%，节点可用区域相对扩张 → 同一节点在不同 zoom 下被推到不同相对位置，整张图"形状"变了。

**修复**：[`MapScene.cs:469-477`](../Scripts/Scenes/MapScene.cs#L469-L477)
```csharp
var horizontalMargin = 72f * _zoom;
var verticalMargin = 50f * _zoom;
var clampPadX = 16f * _zoom;
var clampPadY = 12f * _zoom;
```
现在所有几何量都按 zoom 等比缩放，地图就是纯视觉缩放 — 节点分布与路径形状完全不变。

### 5) 右侧滚动条
ScrollContainer 隐藏掉了垂直滚动条（在前次改动里已通过 vertical_scroll_mode 控制）。

---

## 五、不期而遇模板化（`EventScene`）

### 1) 模板布局
```
EventScene (Control)
└─ Margin → MainHBox
   ├─ LeftColumn (45%)
   │   └─ ImageFrame (PanelContainer，深底 + 金边 + 圆角)
   │       └─ ImageCenter → ImageStack
   │           ├─ EventTexture (TextureRect，默认隐藏)
   │           └─ PlaceholderIcon (Label，160pt emoji)
   └─ RightColumn (55%)
       ├─ HeaderPanel
       │   └─ TitleLabel + DescLabel
       ├─ BreadcrumbLabel (子菜单显示导航轨迹)
       └─ OptionsScroll → OptionsVBox (按钮垂直堆叠)
```

### 2) 数据驱动 + 多级菜单
EventScene 不再为每个事件写专门 UI，而是用一个内部数据结构：

```csharp
class EventOption {
    string Label;
    Action OnSelected;        // 可以触发结算，也可以推一个新菜单
    OptionVariant Variant;    // Default / Leave / Back
}
```

导航通过 `_menuStack` 维护：
- `PushMenu(options, breadcrumb)` → 进入子菜单，面包屑追加一段
- `PopMenu()` → 回到上一级
- `RenderCurrentMenu()` → 重建按钮列表，并**自动**追加：
  - 根菜单底部：✕ 离开（红色 Leave 样式）
  - 子菜单底部：← 返回（暖灰 Back 样式）

### 3) 选项样式
三种 variant：
- **Default**（蓝灰底 + 金边）→ 普通选项
- **Leave**（暗红底 + 红边）→ 离开
- **Back**（深棕底 + 米黄边）→ 返回上一级

按钮均 ExpandFill 横向铺满、字号 16、最小高度 60、文字自动换行。

### 4) 已迁移事件
- **远古祭坛 (shrine)**：根菜单两个选项
  - 🙏 祈祷 → 直接结算（+5 Max HP）
  - 💎 拾取遗物 → **打开子菜单**「确认 — 失去 8 HP, 获得随机遗物」→ 再次确认才结算（多级菜单 demo）
- **可疑商人 (dealer)**：单层菜单，购买卡牌一个选项 + 自动 Leave

未来加新事件只需：
1. 在 `BindEvent()` switch 里加一个 case
2. 写一个 `BuildXxxRoot()` 返回 `List<EventOption>`
3. 需要子菜单就 `OnSelected = () => PushMenu(BuildXxxSub(), breadcrumb: "...")`

### 5) 占位图
现在没有事件 PNG，使用 160pt emoji 占位（🏛 / 🎲）。`ApplyHeader` 检查 `_eventTexture.Texture`，有真实图时自动切换显示。

### 6) 本地化新增
`event.shrine.option_relic_open / option_relic_confirm / crumb_relic`、`event.option.leave / back`。

---

## 六、存档语义修正

### 1) 旧行为（不符合预期）
"进入节点 A → 打完 → 退出 → 重启" 后回到**进入节点 A 之前**的状态。等于打了 A 的进度被吞掉。

### 2) 期望
"打完 A → 即将进入 B → 退出 → 重启"应该回到**即将进入 B 之前**，也就是当前的"地图主菜单 + 已经获得 A 奖励"的状态。

### 3) 实现思路（待落地）
> 注：本节为本轮约定的语义，具体代码改动会在下一次提交中跟进。

- 存档时机不再钉在"MapScene 节点点击的瞬间"，而是钉在"节点结算完毕、回到 MapScene 的瞬间"
- 等价于：当 `GameState.SetUiPhase("map")` 触发，并且 `PendingEncounterType == None` 时（即没有正在进行中的节点）→ 落盘

### 4) 与 Re-enter 区别
- Re-enter（设置 → 重新进入）使用的是节点入场快照（`NodeEntrySnapshot`），用来局内回滚一个节点
- 存档使用的是节点完成后的快照，是跨会话的进度持久化

---

## 七、文件改动一览

| 文件 | 性质 | 关键改动 |
| --- | --- | --- |
| `Scenes/NodeSettingsOverlay.tscn` | 重写 | Dialog + 4 个分组 + 4 种按钮样式 |
| `Scripts/UI/NodeSettingsOverlay.cs` | 改写 RefreshText | emoji 前缀注入 |
| `Scenes/RewardScene.tscn` | 重写 | banner + 一级类别面板 + 二级详情面板 + 大 Continue |
| `Scripts/Scenes/RewardScene.cs` | 重写 | RewardCategory 枚举 + 菜单切换 + 样式化瓦片构造 |
| `Scripts/Scenes/MapScene.cs` | 局部 | 节点尺寸/字号/透明度、StyleBoxEmpty、margin/clamp ×_zoom |
| `Scripts/Scenes/MapCanvas.cs` | 局部 | LineWidth/DashLength、CurveOffsetRatio |
| `Scenes/EventScene.tscn` | 重写 | 通用左图右选项模板 |
| `Scripts/Scenes/EventScene.cs` | 重写 | 数据驱动 + 多级菜单 + 三种按钮样式 |
| `Data/Localization/zh_hans.json` / `en.json` | 增补 | reward / node_settings / event 三类新键 |

---

## 八、验证

- `dotnet build "slay the hs.csproj"` → 0 errors（仅旧 nullable 警告）
- 测试：`dotnet test Tests/CombatLogicTests/CombatLogicTests.csproj` 跑所有测试
- 手动验证清单：
  - [ ] Map：缩放在 0.6 / 1.0 / 2.0 切换，节点相对位置 / 路径形状不变
  - [ ] Map：未探索节点饱和不透明，已探索节点 0.6 透明，当前节点最亮
  - [ ] Reward：分类瓦片 disabled 灰态；选完一类自动回一级
  - [ ] Reward：Continue / Back / Skip 都是大按钮，无裸文本
  - [ ] Settings：分区 emoji 显示正常；Return-to-menu 按红色样式
  - [ ] Event：shrine 选"拾取遗物"后看到子菜单 + Back；选 Pray 直接结算
  - [ ] Event：dealer 单层菜单也有 Leave 按钮
