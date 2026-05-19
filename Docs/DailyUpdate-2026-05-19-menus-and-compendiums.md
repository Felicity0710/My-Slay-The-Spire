# 今日更新日志（2026-05-19）主菜单重构 + 图鉴体系 + 多场景串联

## 一、今日概览

这一轮把"游戏外壳"全部接成完整流程：从主菜单进入选角色 → 战斗失败回到失败结算页可以一键再战 → 主菜单接入四大图鉴 + 完整设置场景。整个轮次按 4 个 Phase 推进：

- **Phase 1 — 战斗 / 商店 / 篝火小修复**：删牌弹窗不透明 + 按钮包裹 / 篝火 Skip 用按钮样式 / 战斗回合提示放大到 108pt 居中艺术字 / 商店删牌改成 CardView 网格
- **Phase 2 — 主菜单重构**：左侧菜单 + 右侧 logo，按存档状态切换 Start↔Continue，新增放弃存档按钮，删掉一堆 legacy 工具入口
- **Phase 3 — 图鉴 + 成就**：新建敌人图鉴、成就两个场景，验证已有的卡牌图鉴 / 遗物图鉴 nav
- **Phase 4 — 设置场景**：把语言开关挪进设置，整页采用 NodeSettingsOverlay 风格

附带：失败结算页 + 选角色场景（在前面的轮次已完成，这里成为主菜单 → 选角色 → Map / 战斗 → 失败 → 选角色的完整循环）。

---

## 二、Phase 1：战斗 / 商店 / 篝火小修复

### 1) 商店删牌弹窗
[ShopScene.tscn:5-67](../Scenes/ShopScene.tscn#L5) + [ShopScene.cs:567-680](../Scripts/Scenes/ShopScene.cs#L567)

- **背景不透明**：`Backdrop.color` 从 `Color(0, 0, 0, 0.6)` 改成 **`Color(0.02, 0.02, 0.03, 1)`**。原本半透明会让背后的商品 UI 和当前弹窗内容混叠，玩家看着糊一片。
- **Dialog 加 panel 样式**：新增 `StyleBoxFlat_dialog` 子资源（深棕底 + 金边 + 圆角 18 + 阴影 14），覆盖在 Dialog PanelContainer 上，弹窗有了清晰的"对话框感"。
- **Cancel / Confirm 包成按钮 UI**：
  - Cancel：`StyleBoxFlat_cancel_normal` 暗灰底银边「✕ Cancel」，200×52
  - Confirm：`StyleBoxFlat_confirm_normal/hover` 暗红金光「✓ Confirm」，240×52，悬停时变亮红
- **选卡视觉**：原来是 ItemList 文本列表，已在前一轮改成 4 列 CardView 网格，每张卡用 PanelContainer 框，选中变红边 + 发红阴影。Confirm 在未选时 disabled。

### 2) 篝火 Skip 按钮
[RestScene.tscn:5-37, 253-267](../Scenes/RestScene.tscn#L253) + [RestScene.cs:110](../Scripts/Scenes/RestScene.cs#L110)

之前是 `flat = true` + 灰字「Skip」，看起来像普通文字链接。改成：
- 加 `StyleBoxFlat_skip_normal/hover` 暖灰金边样式
- 字体 16 → 18，尺寸 200×44 → 240×52，文字「Skip」→「**✕ Skip**」
- 跟 Reward 页面的 Skip 按钮风格统一

### 3) 战斗回合横幅升级
[BattleScene.tscn:646-680](../Scenes/BattleScene.tscn#L646) + [BattleScene.VisualEffects.cs:272-302](../Scripts/Scenes/BattleScene.VisualEffects.cs#L272)

旧版：顶部 300×48 小条，字号 22，普通滑入。新版：

- **大小**：`offset` 改成 ±480×±80 = **960×160**，居中（anchors_preset=8）
- **字号**：22pt → **108pt**
- **艺术字感**：12px 黑色描边 + 颜色按回合类型染色（玩家蓝 `38bdf8` / 敌人红 `f87171`）
- **动画**：
  - 入场：scale 0.4 → 1.0，Back ease（轻微 overshoot），0.32s
  - 保持 0.55s 完全可读
  - 出场：alpha → 0 + scale → 0.92，0.28s
- 类型 `_turnBanner` 已经是 `Control`，把场景的 PanelContainer 换成 Control 即可（去掉了背景边框，让大字直接浮在 Arena 上）

---

## 三、Phase 2：主菜单重构

### 1) 全新布局

[MainMenu.tscn](../Scenes/MainMenu.tscn) + [MainMenu.cs](../Scripts/Scenes/MainMenu.cs) 整体重写：

```
┌─────────────────────────┬──────────────────────────┐
│  SLAY THE HS            │                          │
│  像素卡牌肉鸽冒险       │                          │
│                         │                          │
│  ▶ 开始游戏             │           ⚔             │
│  ✕ 放弃存档（仅有存档） │     (大 emoji 320pt)    │
│  🏆 成就                │                          │
│  🐉 敌人图鉴            │      Slay the Tower      │
│  💎 遗物图鉴            │      (副标题 36pt)       │
│  🎴 卡牌图鉴            │                          │
│  ⚙ 设置                 │                          │
│  ✕ 退出游戏             │                          │
└─────────────────────────┴──────────────────────────┘
   左 42%                       右 58%
```

- **左面板**：金边深棕 PanelContainer，标题 / 副标题 / 按钮栈
- **右面板**：金边深棕 PanelContainer，居中大 emoji + 副标题（替代位置，后续可换真实 logo 贴图，`LogoImage` TextureRect 已经留好，把它的 Texture 赋值并 `Visible = true` 就会替换 emoji）

### 2) 按钮分语义样式
4 种 sub_resource：
- `StyleBoxFlat_button_primary` 绿底亮金边 → 开始/继续游戏（主操作）
- `StyleBoxFlat_button_ghost` 蓝灰金边 → 4 个图鉴 + 设置
- `StyleBoxFlat_button_danger` 暗红 → 放弃存档 + 退出游戏

每个都有对应 `_hover` 变体，hover 时边色变金/变白。

### 3) 存档感知 Start ↔ Continue ↔ Abandon
[MainMenu.cs:73-97](../Scripts/Scenes/MainMenu.cs#L73)

`RefreshSaveSlotState()` 在 `_Ready` 和语言切换后调用：

| 存档状态 | Start 按钮文本 | 放弃存档可见 |
| --- | --- | --- |
| 无存档 | ▶ 开始游戏 | 否 |
| 有存档 | ▶ 继续游戏 | **是**（红色 danger 样式） |

Start 按钮的 `Pressed` 一个 handler `OnStartOrContinuePressed`，内部按 `SaveSystem.HasSave()` 分发到 `BeginNewRun` 或 `ContinueExistingRun`。

### 4) 删掉的 legacy 部分
旧主菜单有一堆开发用入口（"直接战斗测试"、"测试卡组预设"下拉、"流派卡组编辑器"、"卡牌编辑器"、内嵌设置弹窗），全部清掉。Battle Test 等如果需要可以后续从设置或开发者菜单恢复，避免污染玩家面板。

---

## 四、Phase 3：图鉴 + 成就

### 1) 敌人图鉴
[Scenes/BestiaryScene.tscn](../Scenes/BestiaryScene.tscn) + [Scripts/Scenes/BestiaryScene.cs](../Scripts/Scenes/BestiaryScene.cs)

**布局**：
- 顶部：← Back（左）+ 🐉 Bestiary 标题面板（中）
- 主体水平分两栏：
  - **左 55%**：3 列敌人头像网格 ScrollContainer。每个头像 150×170，含 110×110 立绘 + 名字。按敌人 trait accent 描边（攻击红 / 防御蓝 / 增益紫 等）
  - **右 45%**：选中后显示
    - 大立绘 260×260 框（背景按 stage tint 染色，描边按 accent）
    - 名字（32pt，按 accent 染色）
    - Trait 描述（16pt 浅黄）
    - 未选中时显示「请从左边选择一个敌人」灰字占位

**数据接口**：新增 [`CombatVisualCatalog.AllEnemyIds()`](../Scripts/Systems/CombatVisualCatalog.cs#L88) 返回 `IEnumerable<string>`，让 UI 不需要硬编码敌人列表。

### 2) 成就场景
[Scripts/Data/AchievementCatalog.cs](../Scripts/Data/AchievementCatalog.cs) + [Scenes/AchievementsScene.tscn](../Scenes/AchievementsScene.tscn) + [Scripts/Scenes/AchievementsScene.cs](../Scripts/Scenes/AchievementsScene.cs)

**数据**：新建 `AchievementCatalog` 静态类，预设 12 个成就：

| Id | 图标 | 名字 | 触发条件（待实装） |
| --- | --- | --- | --- |
| first_blood | 🩸 | First Blood | 击杀第一个敌人 |
| first_floor | 🏛 | Beneath the Tower | 到达第 2 层 |
| act_one_clear / act_two_clear / the_finisher | 🗺/⛰/👑 | Act I/II/III Conqueror | 通关各幕 |
| perfectionist | ✨ | Perfectionist | 一场战斗不掉血 |
| merchant_robber | 🗡 | Merchant's Bane | 抢商人 |
| deck_master | 🎴 | Deck Master | 携 25 张牌 |
| relic_hoarder | 💎 | Relic Hoarder | 单局 5 个遗物 |
| elite_hunter | ⚔ | Elite Hunter | 击杀 3 个精英 |
| potion_brewer | 🧪 | Potion Brewer | 同时持有 3 瓶药水 |
| explorer | 🧭 | Explorer | 访问每种地图节点 |

**渲染**：每条用一个金边深底 PanelContainer（带阴影），左边 76×76 emoji 图标圆框 + 右边名字 + 描述 stacked。整个列表用 ScrollContainer 上下滚动。

**当前限制**：解锁状态没接通；可以在战斗 / 商店 / 关卡完成时调用 `AchievementService.Unlock("first_blood")` 等接口（待实现）。Catalog 已经为后续接入留好结构。

### 3) 卡牌图鉴 / 遗物图鉴
已有 `CardBrowserScene` 和 `RelicCompendiumScene`，两者的 BackButton 都已经指向 `res://Scenes/MainMenu.tscn`，新主菜单的按钮直接跳过去就能用，无需改动。

---

## 五、Phase 4：设置场景

[Scenes/SettingsScene.tscn](../Scenes/SettingsScene.tscn) + [Scripts/Scenes/SettingsScene.cs](../Scripts/Scenes/SettingsScene.cs)

### 1) 视觉风格
**完全复用 NodeSettingsOverlay 的 header/body 双层 panel 样式**：

```
┌─────── Dialog (640×自适应) ───────┐
│         ⚙ Settings                │
├───────────────────────────────────┤
│ 🌐 Language ─────────────────     │  ← header (上圆角 + 下边线)
│ ┌───────────────────────────────┐ │
│ │ [Language: English]           │ │  ← body (下圆角 + 半透明)
│ └───────────────────────────────┘ │
│ 🖥 Display ─────────────────      │
│ ┌───────────────────────────────┐ │
│ │ Resolution     [1920×1080 ▾] │ │
│ │ Max FPS        [60       ▾] │ │
│ │ VSync          [☑]            │ │
│ │ Show FPS       [☑]            │ │
│ └───────────────────────────────┘ │
│ 🔊 Audio ──────────────────       │
│ ┌───────────────────────────────┐ │
│ │ Master Volume  ━━━━━●━━━━━   │ │
│ │ Music Volume   ━━━━━●━━━━━   │ │
│ └───────────────────────────────┘ │
│            [ ← Back ]             │
└───────────────────────────────────┘
```

### 2) 语言切换从主菜单挪进来
旧主菜单上的 `LanguageButton`（按 `ToggleLanguage` 切换 ZhHans ↔ En）现在是设置场景的「🌐 Language」分组主操作按钮，按一下立即切换且会刷新所有打开窗口的文本（依赖 `LocalizationSettings.LanguageChanged` 事件）。

### 3) Display + Audio 完整接入 AppSettings
所有控件直接读写 `AppSettings.Instance` 单例：分辨率 / 最大帧率 / VSync / FPS counter / 主音量 / 音乐音量。切换后立即生效（窗口尺寸通过 `DisplayServer.WindowSetSize` 应用），跟 `NodeSettingsOverlay` 共用一套 settings 后端，所以两个入口的配置完全同步。

---

## 六、新增 / 修改文件一览

| 文件 | 类别 | 关键改动 |
| --- | --- | --- |
| `Scenes/ShopScene.tscn` | 改 | RemoveOverlay 不透明背景 + Dialog panel 样式 + Cancel/Confirm 包按钮 |
| `Scenes/RestScene.tscn` | 改 | SkipButton 样式化、文字「✕ Skip」 |
| `Scripts/Scenes/RestScene.cs` | 改 | Skip 按钮文本 emoji 前缀 |
| `Scenes/BattleScene.tscn` | 改 | TurnBanner 从 PanelContainer 改 Control，居中放大到 960×160，108pt 描边 |
| `Scripts/Scenes/BattleScene.VisualEffects.cs` | 改 | ShowTurnBanner 改用 scale-pop 动画 |
| `Scenes/MainMenu.tscn` | 重写 | 左菜单 + 右 logo 双栏布局 |
| `Scripts/Scenes/MainMenu.cs` | 重写 | 删掉 BattleTest/Deck 预设/Tools 等 legacy，留下 8 个核心入口 |
| `Scenes/BestiaryScene.tscn` | 新 | 敌人图鉴场景 |
| `Scripts/Scenes/BestiaryScene.cs` | 新 | 敌人图鉴逻辑 |
| `Scenes/AchievementsScene.tscn` | 新 | 成就场景 |
| `Scripts/Scenes/AchievementsScene.cs` | 新 | 成就场景逻辑 |
| `Scripts/Data/AchievementCatalog.cs` | 新 | 12 个成就数据 |
| `Scenes/SettingsScene.tscn` | 新 | 全屏设置场景 |
| `Scripts/Scenes/SettingsScene.cs` | 新 | 设置场景逻辑（含语言切换） |
| `Scripts/Systems/CombatVisualCatalog.cs` | 改 | 新增 `AllEnemyIds()` 公开方法 |
| `Data/Localization/zh_hans.json` / `en.json` | 增补 | 17 个新键（main_menu / bestiary / achievements / settings / common.back） |

---

## 七、流程串联

完整的玩家流程现在是：

```
MainMenu
  ├─ ▶ 开始游戏 → CharacterSelectScene → MapScene
  │                                          ├─ Battle → 胜：Reward → Map
  │                                          │           败：DefeatScene
  │                                          │                  ├─ ↻ 再来一把 → CharacterSelectScene
  │                                          │                  └─ ← 返回主菜单 → MainMenu
  │                                          ├─ Shop / Rest / Event / 节点 ...
  │                                          └─ Boss → 胜：VictoryScene
  ├─ ▶ 继续游戏 → 直接 ChangeSceneToFile(存档场景路径)
  ├─ ✕ 放弃存档 → SaveSystem.Delete()，刷新主菜单
  ├─ 🏆 成就 → AchievementsScene → ← MainMenu
  ├─ 🐉 敌人图鉴 → BestiaryScene → ← MainMenu
  ├─ 💎 遗物图鉴 → RelicCompendiumScene → ← MainMenu
  ├─ 🎴 卡牌图鉴 → CardBrowserScene → ← MainMenu
  ├─ ⚙ 设置 → SettingsScene → ← MainMenu
  └─ ✕ 退出游戏 → GetTree().Quit()
```

每个分支都能干净地回到主菜单（场景内有显式 Back 按钮），存档状态变化（StartNewRun / SaveSystem.Delete）都触发 RefreshSaveSlotState 重渲染按钮文字。

---

## 八、验证

- `dotnet build "slay the hs.csproj"` → 0 errors（仅旧 nullable 警告）
- 手动验证清单：
  - [ ] 主菜单无存档 → 显示「开始游戏」+ 无放弃存档按钮
  - [ ] 主菜单有存档 → 显示「继续游戏」+ 红色「✕ 放弃存档」
  - [ ] 主菜单 7 个其它按钮（4 个图鉴 + 设置 + 退出 + 开始）都能跳转
  - [ ] 设置里语言切换立即生效，所有打开的中英文标签都换
  - [ ] BestiaryScene：点头像 → 右边详情切换 + 头像高亮
  - [ ] AchievementsScene：列表可滚动，每条有图标 + 名字 + 描述
  - [ ] DefeatScene：← 主菜单 / ↻ 再来一把 → CharacterSelect 都能正常 nav
  - [ ] 战斗回合切换：玩家蓝大字 / 敌人红大字居中弹出 + 缩放

---

## 九、技术债 / 后续

1. **AchievementCatalog 解锁状态未接通**：catalog 准备好了，需要：
   - 添加 `AchievementService` autoload（记录已解锁 id 列表，持久化到磁盘）
   - 在关键节点调用 `.Unlock("id")`（击败首敌、Act 通关、商店抢劫等）
   - AchievementsScene 根据 unlocked 状态展示金色边框 vs 灰暗
2. **MainMenu Right Logo 还是 emoji**：`LogoImage` TextureRect 已经留好节点（默认 `visible=false`），只需要美术给一张大 PNG 然后设置 `Texture` 并切换可见性
3. **CharacterSelectScene 的 max HP 还是写死 80**：未来如果加角色专属起始 HP，需要把 `RefreshText` 里的 `"❤ Max HP: {0}", 80` 换成 `preset.MaxHp`
4. **Settings 没有保存按钮**：所有改动直接写入 AppSettings 单例（实时生效），但如果未来加"恢复默认 / 取消改动"功能就需要快照
5. **Battle 回合横幅在多敌人攻击时只显示一次**：是预期行为（只在 turn 切换时显示），不需要每个敌人都弹

