# 今日更新日志（2026-05-20）商店 / 地图 / 战斗 打磨与 bug 修复

## 一、今日概览

这一轮没有大功能，全部是围绕"手感 + 视觉一致性 + bug 修复"的打磨，集中在三块：

- **商店**：点击商品本身即可购买、货架可滚动、商品数量翻倍、行列间距优化、删牌弹窗不透明 + 按钮包裹、抢商店掉精英奖励、购买后状态栏即时刷新、抢店后已购买物品的 Sold 状态修复。
- **地图**：移除顶部标题栏改用统一状态栏、查看卡组改成右上角浮动按钮、左键拖拽、缩放保持中心点不漂移、道路缩放不再抖动、进出结点保留缩放与位置、隐藏滚动条、去掉结点 hover tooltip。
- **战斗**：整体配色从冷蓝改成金棕、卡面重做、回合提示放大成居中艺术字。

外加一批连锁 bug 修复（状态栏遮挡、遗物图鉴空白、滚动条禁用导致的拖不动 / 布局溢出等）。

---

## 二、商店（ShopScene）

### 1) 点击商品本身购买
[ShopScene.cs:BuildItemTile](../Scripts/Scenes/ShopScene.cs)

以前必须精准点商品下方那个小价格按钮才能买。现在：
- 商品 tile 从 `PanelContainer` 改成 `Button`，整个 tile 表面都是购买热区
- tile 有 normal / hover 两套样式（`BuildTileStyle`），hover 时边框变亮、阴影加深
- 价格降级成纯展示的"价格徽章"（`PanelContainer + Label`，`MouseFilter = Ignore`）
- tile 内所有子节点设 `MouseFilter = Ignore`，点击穿透到 tile Button
- `ShopItem` 新增 `PriceLabel` 字段；售罄时改写 `PriceLabel.Text = "Sold"`，而不是改按钮文字

### 2) 货架可滚动 + 商品数量增加
- `ItemsScroll.vertical_scroll_mode` 改成 **3（SHOW_NEVER）**：滚轮 / 拖拽可上下浏览，滚动条隐藏
- 商品数量：卡牌 3 → **8**、遗物 2 → **4**、药水 2 → **6**（[ShopScene.cs:186/207/220](../Scripts/Scenes/ShopScene.cs#L186)）
- 卡牌、药水货架改成 **6 个一行**（`CardGrid` / `PotionGrid` columns = 6），遗物保持 4 列

### 3) 行列间距优化
- 三个货架网格：`h_separation` 14 → **24**、`v_separation` 14 → **22**
- 区块面板内边距 `StyleBoxFlat_section` content_margin 14 → **左右 20 / 上 18 / 下 20**
- 区块之间 `SectionsVBox.separation` 14 → **22**

### 4) 删牌弹窗
[ShopScene.tscn](../Scenes/ShopScene.tscn) + [ShopScene.cs](../Scripts/Scenes/ShopScene.cs)
- 背景从半透明 `(0,0,0,0.6)` 改成**完全不透明** `(0.02,0.02,0.03,1)`，不再透出背后的货架
- Dialog 加金边深棕 panel 样式
- 选卡区从 ItemList 文本列表改成 **4 列 CardView 网格**，选中变红边 + 发红阴影
- Cancel / Confirm 包成样式化按钮（银灰 ✕ Cancel / 暗红金光 ✓ Confirm），未选卡时 Confirm disabled

### 5) 抢商店掉落精英奖励
[GameState.cs](../Scripts/Autoload/GameState.cs) + [BattleScene.cs](../Scripts/Scenes/BattleScene.cs) + [RewardScene.cs](../Scripts/Scenes/RewardScene.cs)
- `RollBattleRewardOffers` 的 isElite 判定加入 `MerchantFight` → 抢商店成功掉**精英级**奖励池
- `ResolveMerchantFightVictory()` 末尾调 `RollBattleRewardOffers()`（楼层不前进）
- 抢店胜利分支：先 `ResolveMerchantFightVictory()` 再跳 `RewardScene`
- `RewardScene.OnContinuePressed`：检测 `PendingMerchantFightVictory` 标志 → 回 ShopScene（而非 MapScene）

### 6) 状态栏即时刷新
[ShopScene.cs:RefreshUi](../Scripts/Scenes/ShopScene.cs)
- 挂载时保存 `_statusOverlay` 引用
- `RefreshUi()` 末尾调 `_statusOverlay.Refresh()` → 买卡 / 买遗物 / 买药水 / 删牌后，顶部状态栏的卡组数 / 遗物图标 / 药水栏立即同步

---

## 三、地图（MapScene）

### 1) 顶部精简
[MapScene.tscn](../Scenes/MapScene.tscn)
- 移除"远古路线图"标题面板 + 描述文字
- 改用与战斗 / 非战斗结点统一的 `RunStatusOverlay` 浮层
- 旧的 StatsPanel / RelicRow / PotionRow 全部 `visible = false`（保留节点避免 GetNode 异常）
- 图例（普通战斗 / 精英战斗 / 事件 / 休息 / 商店）紧贴状态栏下方，地图主体上移腾出空间

### 2) 查看卡组按钮
- 从 TopBar 移出，改成右上角**浮动按钮**，位于设置齿轮正下方、同宽（132px）同色
- 用 `CanvasLayer (layer = 100)` 包裹 → 渲染在状态栏（layer 90）之上，不再被遮挡 / 吞点击
- 配色从冷蓝改成金棕（深棕底 + 金边 + 阴影），与齿轮成一组工具栏
- 文本缩短：「🎴 View Deck (11)」→「🎴 卡组 11」

### 3) 拖拽改左键
[MapScene.cs](../Scripts/Scenes/MapScene.cs)
- 拖拽键从右键改成 **左键**：落在地图空白区的左键拖动地图；落在结点上的左键被结点按钮消费用于选择，互不冲突

### 4) 缩放保持中心点不漂移
[MapScene.cs:ZoomBy / ApplyZoomCenter](../Scripts/Scenes/MapScene.cs)
- 缩放前记录视口中心点在画布上的**缩放无关坐标** `(ScrollH + viewW/2) / oldZoom`
- 缩放重建期间 `_suppressAutoFocus = true`，跳过自动行聚焦
- 缩放后 `ApplyZoomCenter` 把滚动设为 `unscaledCenter * newZoom - viewW/2` → 同一画布点回到屏幕正中
- 效果：滚轮缩放时地图围绕屏幕中心放大缩小，不再跳走

### 5) 道路缩放不再抖动
[MapCanvas.cs:BuildCurve](../Scripts/Scenes/MapCanvas.cs)
- 根因：曲线弯曲方向用**屏幕中点坐标**算哈希，缩放后中点变 → 哈希变 → 弯曲方向翻转 → 抖动
- 修复：line 元组加 `int Seed` 字段（由节点 row/col/nextCol 索引算出，缩放无关），`BuildCurve` 用 Seed 决定弯曲方向
- 不论怎么缩放，每条路的形状与弯向固定不变

### 6) 进出结点保留视图
[GameState.cs](../Scripts/Autoload/GameState.cs) + [MapScene.cs](../Scripts/Scenes/MapScene.cs)
- GameState（autoload，跨场景存活）新增 `HasMapViewState / MapZoom / MapScrollH / MapScrollV`
- 点结点离开地图前 `SaveMapViewState()` 存当前缩放 + 滚动
- 返回地图时 `ApplyInitialMapLayout` 先恢复 zoom 再建图，`CallDeferred(RestoreMapScroll)` 排在自动聚焦之后覆盖它
- `StartNewRun()` 重置（新开局首次进图仍走默认聚焦）

### 7) 杂项
- 隐藏地图水平滚动条（`scroll_mode` 3 = SHOW_NEVER，滚动正常、条隐藏）
- 取消结点 hover 时弹出的「01F - Event」tooltip

---

## 四、战斗界面（BattleScene）

### 1) 配色金棕统一
[BattleScene.tscn](../Scenes/BattleScene.tscn)
- 7 层 Arena 背景全部重染：远景深棕、中景暖棕、前景暖雾、左右辉光（左暖橙 / 右暗红区分敌我）、地平线改金色细线
- panel / button / chip / hand / banner / pile 六套 StyleBox：底色改深棕、边框改金色
- 主背景 `#070912` → `#0F0A05`

### 2) 卡面重做
[CardView.cs:BuildUi](../Scripts/UI/CardView.cs)
- 卡面板 bg `#18212b`（冷蓝灰）→ `#1C1610`（羊皮纸棕），边框 `#7aa8cf`（钢蓝）→ `#D9A552`（金）
- 费用徽章 bg / border 改琥珀色系
- 卡名 / 费用 / 描述文字改暖白 / 琥珀色
- 攻击红 / 技能蓝的小类别标签保留（语义色）

### 3) 回合提示
[BattleScene.tscn](../Scenes/BattleScene.tscn) + [BattleScene.VisualEffects.cs](../Scripts/Scenes/BattleScene.VisualEffects.cs)
- 「我方回合 / 敌方回合」从顶部 300×48 小条 → **居中 960×160 大字**，字号 22 → 108pt
- 12px 黑色描边 + 按回合类型染色（玩家蓝 / 敌人红）
- 动画改成「弹出缩放（Back ease）→ 保持 0.55s → 缩小淡出」

---

## 五、关键 Bug 修复

| Bug | 根因 | 修复 |
| --- | --- | --- |
| 非战斗场景被状态栏遮挡 | Shop / Event / Rest 内容从 y=0 起，被浮层盖住 | 各场景 `Margin.margin_top` 加到 168，内容落在状态栏下方 |
| 遗物图鉴进去空白 | 改版后 Title 移到 TopBar/TitlePanel，脚本仍按旧路径 GetNode → _Ready 抛异常 | 修正 `GetNode` 路径为 `Margin/Root/TopBar/TitlePanel/Title` |
| 地图右键拖不动 | 隐藏滚动条时把 `scroll_mode` 设成 0（DISABLED，完全禁用滚动），拖拽靠 ScrollVertical/Horizontal 实现 | 改成 3（SHOW_NEVER：滚动可用、条隐藏），并把拖拽键改左键 |
| 商店上盖下切 | 同上——`ItemsScroll` 滚动被禁用 → ScrollContainer 索取完整内容高度 → 撑爆屏幕 | `ItemsScroll.vertical_scroll_mode` 改 3，内容回到可用区域内 |
| 抢店后已购物品显示 Free | 重建 tile 时 `FormatPrice` 只看 `_robbed`，不看 `item.Sold` | `BuildItemTile` 末尾：若 `item.Sold` 强制 PriceLabel = "Sold" 并禁用 tile |
| 查看卡组按钮被状态栏遮挡 | 按钮在根 Control（layer 0），状态栏是 CanvasLayer（layer 90）盖在上面 | 用 `CanvasLayer (layer 100)` 包裹按钮 |

---

## 六、文件改动一览

| 文件 | 关键改动 |
| --- | --- |
| `Scenes/ShopScene.tscn` | 删牌弹窗不透明 + 样式、货架滚动、6 列、行列间距、区块内边距 |
| `Scripts/Scenes/ShopScene.cs` | tile 改 Button 整体可点、商品数量 8/4/6、状态栏刷新、Sold 修复、删牌网格 |
| `Scenes/MapScene.tscn` | 移除标题栏、ViewDeck 浮动 CanvasLayer、隐藏滚动条、margin 调整 |
| `Scripts/Scenes/MapScene.cs` | 左键拖拽、缩放保持中心、`_suppressAutoFocus`、视图状态保存/恢复、去 tooltip |
| `Scripts/Scenes/MapCanvas.cs` | line 元组加 Seed，曲线弯向缩放无关 |
| `Scenes/BattleScene.tscn` | 7 层背景 + 6 套 StyleBox 改金棕、回合横幅放大 |
| `Scripts/UI/CardView.cs` | 卡面 / 费用徽章 / 文字改金棕配色 |
| `Scripts/Scenes/BattleScene.VisualEffects.cs` | 回合横幅缩放弹出动画 |
| `Scripts/Autoload/GameState.cs` | 地图相机状态字段 + 抢店奖励 roll |
| `Scripts/Scenes/BattleScene.cs` / `RewardScene.cs` | 抢店胜利走 RewardScene → 回 ShopScene |
| `Scripts/Scenes/RelicCompendiumScene.cs` | 修正 Title GetNode 路径 |

---

## 七、验证

- `dotnet build "slay the hs.csproj"` → 0 errors（仅旧 nullable 警告）
- 手动验证清单：
  - [ ] 商店：点击卡牌 / 遗物 / 药水任意位置即可购买；货架可滚轮浏览；6 个一行；间距清晰
  - [ ] 商店：买物品后状态栏卡组数 / 遗物 / 药水即时更新
  - [ ] 抢商店：先买一件 → 抢店 → 该物品仍显示 Sold（不是 Free）；抢店后掉精英奖励
  - [ ] 地图：左键拖拽地图；滚轮缩放围绕屏幕中心、不漂移；道路缩放不抖动
  - [ ] 地图：进结点再出来，缩放比例和位置保持不变
  - [ ] 地图：查看卡组按钮在右上角、不被遮挡、可点击
  - [ ] 战斗：整体金棕配色；卡面是棕底金框；回合切换居中大字
  - [ ] 遗物图鉴可正常进入并显示内容
