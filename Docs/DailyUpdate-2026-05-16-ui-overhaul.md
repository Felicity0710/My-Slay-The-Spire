# 今日更新日志（2026-05-16）UI 大改：战斗 / 设置 / 商店

## 一、今日概览

这一轮把战斗、设置、商店三块 UI 一起翻修，统一视觉风格，集中调整入口位置：

- **统一 Settings 浮层**：把战斗里那套「分辨率 / FPS / VSync / 音量」搬出来，跟 Re-enter 合并成单一的 `NodeSettingsOverlay`，齿轮按钮挪到屏幕右上角，所有节点场景共用。
- **战斗界面重排**：左侧 sidebar 放遗物 + 药水；中间 Arena 不变；底部 HandArea 用 overlay 摆 4 个角的功能控件（左上能量、右上 End Turn、左下抽牌堆、右下弃牌 + 消耗堆）。Settings 入口收到屏幕右上。
- **商店界面重做**：左侧商人面板（占位头像 + 💰 金币 + 🗡 删牌大按钮 + 🗡 Rob 大按钮），右侧按 Cards / Relics / Potions 三段网格陈列商品；Leave 单独放在右下角。

整轮重做的方向是「分区清晰、入口固定、单一风格」。

## 二、统一 Settings 浮层

### 1) 节点结构

`Scenes/NodeSettingsOverlay.tscn` 整体重写为 CanvasLayer (Layer 100)：

```
NodeSettingsOverlay (CanvasLayer)
└─ Root (Control, 全屏, mouse_filter=Ignore)
   ├─ GearButton (按钮, 右上角 anchored)
   └─ Modal (默认 hidden, 全屏)
       ├─ ModalBackdrop (半透明黑底)
       └─ Dialog (560×540 居中)
           └─ DialogMargin → DialogVBox
               ├─ TitleLabel "Settings"
               ├─ SectionNodeLabel "Current node"
               ├─ HintLabel
               ├─ ReenterButton "Re-enter current node"
               ├─ HSeparator
               ├─ SectionDisplayLabel "Display"
               ├─ DisplayGrid (2 列)
               │   ├─ Resolution + ResolutionOption
               │   ├─ Max FPS + MaxFpsOption
               │   ├─ VSync + VsyncCheckBox
               │   └─ Show FPS + FpsCounterCheckBox
               ├─ HSeparator
               ├─ SectionAudioLabel "Audio"
               ├─ AudioGrid (2 列)
               │   ├─ Master Volume + Slider
               │   └─ Music Volume + Slider
               └─ CloseButton
```

### 2) 齿轮按钮的视觉

新增 3 个 `StyleBoxFlat` sub_resources（normal / hover / pressed）：
- 深蓝灰底 `#1F2A40` → hover 时变亮 `#324859`
- 金色描边 `#F7C75C` → hover 时 `#FFE48C`
- 圆角 10、阴影 4、内边距 14×8
- 文字 16pt、淡黄字色

齿轮按钮锚到屏幕右上：`offset_left = -148, offset_top = 16, offset_right = -16, offset_bottom = 60`，132×44 大小，一眼看见。

### 3) 行为逻辑

`Scripts/UI/NodeSettingsOverlay.cs` 接管原来 `BattleScene.Settings.cs` 的全部职责：
- 启动时填 Resolution / Max FPS 下拉项；把 `AppSettings.Instance` 中的当前值回写到 UI 控件。
- 把变化绑回 `AppSettings.Instance.SetWindowSize / SetMaxFps / SetVSyncEnabled / SetShowFpsCounter / SetMasterVolumePercent / SetMusicVolumePercent`。
- 「Re-enter current node」按钮：根据 `GameState.HasNodeEntrySnapshot` 启用/禁用；按下时 `RestoreNodeEntrySnapshot()` + `ChangeSceneToFile(GetNodeEntrySceneFilePath())`。
- 文案统一走 `LocalizationService.Get` 兜底，本地化键命名为 `ui.node_settings.*`。

### 4) BattleScene 旧 settings 退役

- `Scripts/Scenes/BattleScene.Settings.cs` 砍成两段壳函数（`SetupSettingsUi()` no-op、`RefreshBattleStaticText()` 只更新 EndTurn / Back / TestVictory / LogTitle / TurnBanner 文案）。
- `BattleScene.cs` 删了 18 个跟 settings 相关的字段、对应的 `GetNode<...>("%...")` 行、`_settingsButton.Pressed += OnOpen...` 信号；保留旧的 `%SettingsButton` 和 `%SettingsModal` 节点（已设为 `visible = false`），用 `GetNodeOrNull` 防御性引用，避免 tscn 改完老代码崩。

## 三、战斗界面重排

### 1) 新结构

`Scenes/BattleScene.tscn` 重写为 `MainMargin → MainHBox = LeftSidebar | MainVBox`。

```
MainHBox
├─ LeftSidebar (PanelContainer, 150px)
│   └─ LeftSidebarVBox
│       ├─ "Relics" 标题
│       ├─ RelicScroll → RelicIcons (VBox, 图标列)
│       ├─ HSeparator
│       ├─ "Potions" 标题
│       └─ PotionBar (VBox, 三个药水槽按钮)
└─ MainVBox
    ├─ TopHud (PanelContainer)
    │   └─ TopHudRow (HP / Turn / Hand info / TestVictory / Spacer / Back hidden)
    ├─ Arena (Control, 自适应)
    │   ├─ ArenaFarBg / ArenaMidBg / ArenaFrontFog 视差背景
    │   ├─ PlayerPanel (锚 0.04~0.30, 0.18~0.78, 不再用负 offset)
    │   └─ EnemyDropArea (锚 0.55~0.96, 0.10~0.92)
    └─ HandArea (Control, 高 300)
        ├─ HandPanel (full rect)
        │   ├─ HandContainer
        │   └─ DrawAnchor
        ├─ EnergyDisplay (左上 112×112 圆形, 金色描边)
        │   └─ EnergyValueLabel "3/3" 44pt
        ├─ DrawPileButton (左下 96×80, 🂠 Draw)
        ├─ DiscardPileButton (右下偏左 96×80, ♻ Discard)
        ├─ ExhaustPileButton (右下角 96×80, ✕ Exhaust)
        └─ EndTurnButton (右上 196×64, 红底 22pt 醒目按钮)
```

### 2) 新增的 StyleBoxFlat 子资源

| 名字 | 用途 | 颜色 |
|------|------|------|
| `_panel` | 顶栏 / 玩家 / 敌人 / 侧栏 | 深蓝灰 + 浅蓝边 |
| `_hand` | HandPanel 背景 | 极暗灰蓝 + 微蓝边 + 圆角 12 |
| `_energy` | 能量圆形 | 棕褐底 + 金色 3px 边 + 圆角 56 + 金色外发光 |
| `_end_turn` / `_end_turn_hover` | 结束回合大按钮 | 深红底 + 米白边 + 内阴影暖色发光 |
| `_pile` | 抽 / 弃 / 消耗堆按钮 | 深蓝灰 + 加粗浅蓝边 + 圆角 12 |

### 3) C# 改动

- `BattleScene.cs` 新增 `_energyValueLabel` 字段、`_arenaBasePositionsCaptured` 标志和 `EnsureArenaBasePositionsCaptured()` 方法。
- `_Process` 的「呼吸/晃动」动画在 base 位置未捕获前直接 return，避免把 PlayerPanel / EnemyDropArea 钉到 (0, 0)。
- `RefreshUi()` 在更新旧的 `_energyLabel.Text`（隐藏的兼容节点）的同时，把新的 `_energyValueLabel.Text` 设成 `"{_energy}/{MaxEnergy}"` —— 字号 44pt 金色，圆形面板里显示。
- `_relicIcons` 字段从 `HBoxContainer` 改成基类 `BoxContainer`，因为新的 RelicIcons 是 VBoxContainer。

### 4) BackButton & TestVictoryButton 重新摆位

- `BackButton` 之前和 Settings 齿轮重合在右上角 → 隐藏（`visible = false`），节点保留兼容 `_backButton.Pressed += BackToMap`。
- `TestVictoryButton` 重新显示，但挪到 TopHudRow 的左半段（HandCountLabel 后、TopSpacer 前），跟 Settings 齿轮拉开距离。

### 5) 玩家面板上移

PlayerPanel 锚点从 `top 0.42 ~ bottom 0.92` 改为 `top 0.18 ~ bottom 0.78`，并去掉了所有 offset。这样玩家立绘大概在 Arena 上半区，跟敌人在视觉上左右齐平。

## 四、商店界面重做

### 1) 新结构

`Scenes/ShopScene.tscn` 整体重写为 `Margin → MainHBox = MerchantPanel | ContentVBox`。

```
MainHBox
├─ MerchantPanel (PanelContainer, 240px, 全高)
│   └─ MerchantMargin → MerchantVBox
│       ├─ MerchantPortrait (TextureRect, 200 高, 占位 🧙 emoji)
│       ├─ MerchantNameLabel "Merchant" (22pt 金色)
│       ├─ HSeparator
│       ├─ GoldRow (💰 大图标 + GoldLabel 24pt 金色数字)
│       ├─ HSeparator
│       ├─ RemoveCardButton (96 高, 三态橙红主调)
│       ├─ MerchantFiller (expand)
│       ├─ HSeparator
│       └─ RobButton (78 高, 深红底 + 红色描边 + 红色外发光, "🗡 Rob the shop")
└─ ContentVBox
    ├─ TitleLabel "Shop" 32pt 金色
    ├─ StatusLabel
    ├─ ItemsScroll → SectionsVBox
    │   ├─ CardSection (浅棕 PanelContainer)
    │   │   ├─ "Cards" 20pt 金色标题
    │   │   └─ CardGrid (3 列)
    │   ├─ RelicSection (同款)
    │   │   ├─ "Relics" 标题
    │   │   └─ RelicGrid (4 列)
    │   └─ PotionSection (同款)
    │       ├─ "Potions" 标题
    │       └─ PotionGrid (4 列)
    └─ ActionRow
        ├─ ActionSpacer (expand)
        └─ LeaveButton (240×64, 深蓝灰底 + 浅蓝描边 + "Leave →")
```

### 2) 商品格子（`BuildItemTile`）

每个商品在代码里动态构造一个 `PanelContainer`：

| 类型 | 大小 | 描边色 | 内容 |
|------|------|--------|------|
| **Card** | 200×260 | 浅蓝 `#7DB1D2` | 名字 → Attack/Skill 染色小字 → 卡面立绘 (TextureRect 92 高) → Cost X 青色 → 描述 → 价格按钮 |
| **Relic** | 160×200 | 浅橙 `#F2A64C` | 遗物图标 (`CombatVisualCatalog.GetRelicIconPath`) → 名字 → 描述 → 价格按钮 |
| **Potion** | 160×200 | 浅绿 `#8CD899` | 圆形彩色色块 + 🧪 emoji（按 potion id 染色）→ 名字 → 描述 → 价格按钮 |

价格按钮统一格式：
- 普通: `💰 75`
- 抢劫后 / 免费: `Free`
- 已售: `Sold`

`Potion` 色块颜色映射：
- `healing_potion` 红色
- `strength_potion` 橙色
- `swift_potion` 绿色
- `guard_potion` 蓝色
- `fury_potion` 紫色

### 3) 删牌大按钮

`RemoveCardButton` 放在 MerchantPanel 中段：
- 三态 `StyleBoxFlat`：normal（深红橙底 + 金边 + 外发光）/ hover（亮橙）/ disabled（暗灰）。
- 文字两行：`"🗡 Remove a card\n💰 75"` 或 `"🗡 Remove a card\nFree"` 或 `"🗡 Remove service used"`（已用过禁用态）。
- 96×？大小，比普通按钮显眼。

### 4) Rob & Leave 配色

| 按钮 | 视觉风格 | 用意 |
|------|----------|------|
| **🗡 Rob the shop** | 深血红底 `#6A1414` + 红色描边 `#F24E37` + 红色外发光 | 危险 / 不可逆 |
| **Leave →** | 深蓝灰底 `#1A2E3D` + 浅蓝描边 `#8CC7D9` | 安全 / 平静 |

Rob 放在 `MerchantPanel` 最底部（屏幕左下角视觉位置），Leave 放在右侧 ContentVBox 底栏右对齐（屏幕右下角）。`ActionSpacer` 用 `size_flags_horizontal = 3` 把 Leave 推到最右。

### 5) C# 改动

`Scripts/Scenes/ShopScene.cs`：
- 新增字段：`_cardGrid` / `_relicGrid` / `_potionGrid` / 各 section panel 和 label 引用。
- 旧的 `ItemsVBox` + `RenderItems` 平铺方式废弃；`ItemsVBox` 节点仍保留但 `visible = false` 兼容。
- `RenderItems()` 改为按 `ShopItemKind` 分发到三个 GridContainer，并控制三段 section panel 的可见性（无该类商品时整段隐藏）。
- 新增 `BuildItemTile(ShopItem)` 构建每个格子。
- 新增 `TileBorderColor` / `PotionSwatchColor` / `TryLoadTexture` 辅助函数。
- `FormatPrice` 改成 `"💰 {price}"`（之前是 `"Buy {price}g"`）。
- `_goldLabel.Text` 简化为纯数字（💰 图标在旁边的 Label 里）。
- 删牌按钮文案换成两行：`🗡 Remove a card\n💰 75`。

## 五、构建与验证

- `powershell -ExecutionPolicy Bypass -File .\build.ps1`：**0 错误**。
- `dotnet run --project .\Tests\CombatLogicTests\CombatLogicTests.csproj`：**51/51 通过**。

手动验证清单：

1. **任意节点场景**（Battle / Shop / Event / Rest / Intro）右上角应该都有一个金色描边的「⚙ Settings」按钮，点开是同一个统一浮层；Re-enter 在浮层最上面、下面是 Display / Audio。
2. **战斗界面**：
   - 左栏看到遗物图标（带 tooltip）+ 药水按钮。
   - 玩家、敌人左右对称。
   - 手牌底部金色圆形显示能量、右上红色 End Turn 大按钮、左下 🂠 Draw、右下 ♻ Discard + ✕ Exhaust。
   - 顶栏左侧有 Test Victory 测试按钮；旧的 Back / 旧 Settings 已隐藏。
3. **商店界面**：
   - 左栏看到 🧙 商人占位 + 💰 金币 + 🗡 删牌（大）+ 🗡 Rob（红色大按钮，最底部）。
   - 右侧按 Cards / Relics / Potions 三段网格陈列，每个格子带价格按钮（`💰 N`）。
   - 右下角看到「Leave →」蓝色稳重按钮。

## 六、涉及文件

```text
Scenes/NodeSettingsOverlay.tscn         (重写 + 加 GearButton 三态 StyleBox)
Scripts/UI/NodeSettingsOverlay.cs       (吸收 BattleScene 的 settings 逻辑)
Scenes/BattleScene.tscn                 (整体重写为 MainHBox + LeftSidebar + HandArea)
Scripts/Scenes/BattleScene.cs           (删 settings 字段 / 加 EnsureArenaBasePositionsCaptured / 新 EnergyValueLabel)
Scripts/Scenes/BattleScene.Settings.cs  (砍成壳)
Scenes/ShopScene.tscn                   (重写: MerchantPanel + 三段网格 + Rob/Leave 各处)
Scripts/Scenes/ShopScene.cs             (新 BuildItemTile + 三组 grid + 💰 价格格式)
```

## 七、留给后续

- **商人头像**：目前是 🧙 emoji 占位，等做了真正的立绘资源直接给 `MerchantPortrait.Texture` 赋路径即可。
- **抽 / 弃 / 消耗堆**：图标用的是 Unicode `🂠 ♻ ✕`，未来可以换成 32×32 的 PNG 图标。
- **统一 Settings 浮层 vs 战斗里的隐藏 SettingsModal**：旧 SettingsModal 节点还在 BattleScene.tscn 里 `visible = false`，下次清理可以直接删掉。
- **本地化**：新引入的本地化键 `ui.node_settings.* / ui.shop.section_* / ui.card_kind.*` 现在都靠英文兜底，等翻译补到 `Data/Localization/*.json`。
