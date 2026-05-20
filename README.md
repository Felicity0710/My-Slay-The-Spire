# Slay The HS

像素风格卡牌构筑 Roguelike，灵感来源于《Slay the Spire》。基于 Godot 4.5 (.NET/C#) 构建。

## 快速开始

环境配置详见 [Docs/EnvironmentSetupGuide.md](Docs/EnvironmentSetupGuide.md)。

```powershell
# 构建项目
.\build.ps1

# 启动 Godot 编辑器
Godot_v4.5.2-stable_mono_win64.exe

# 运行测试
dotnet run --project .\Tests\CombatLogicTests\CombatLogicTests.csproj
```

Godot 启动后在项目管理器中导入 `project.godot`，然后按 `F5` 运行。

## 项目结构

```
slay-the-hs/
├── Assets/           # 静态资源（卡牌插画、图标）
├── Data/             # JSON 数据（卡牌、敌人、遗物、多语言）
├── Scenes/           # Godot 场景文件 (.tscn)
├── Scripts/
│   ├── Autoload/     # 全局单例（GameState, AppSettings, ExternalControlService）
│   ├── Data/         # 数据模型
│   ├── Scenes/       # 场景脚本
│   ├── Systems/      # 纯游戏逻辑（无 Godot 依赖）
│   └── UI/           # UI 组件
├── Tests/            # 控制台测试
├── Tools/
│   └── SlayHs.Agent/ # MCP Agent（外部 AI 控制桥接）
└── Docs/             # 文档
```

## 技术栈

| 层级 | 技术 |
|------|------|
| 游戏引擎 | Godot 4.5 (.NET / C#) |
| 运行时 | .NET 8.0 |
| Agent | .NET 9.0 (MCP 协议) |
| 测试 | 独立控制台项目 (net8.0) |
| 多语言 | en / zh-Hans (JSON key-value) |

## 核心架构

详见 [Docs/Architecture.md](Docs/Architecture.md)。

- **Autoload 服务**: GameState（运行状态）、AppSettings（设置）、ExternalControlService（TCP 桥接，端口 47077）
- **Systems 层**: 纯 C# 逻辑，不依赖 Godot，可在测试中独立运行
- **外部控制**: 游戏通过 TCP 桥接暴露完整控制接口，支持 MCP 协议的 AI Agent / Bot

## 游戏演示模式

游戏支持以下演示/测试入口：

```powershell
# Battle Test — 直接进入战斗测试
dotnet run --project .\Tests\CombatLogicTests\CombatLogicTests.csproj

# MCP Agent — AI 通过 MCP 协议控制游戏
powershell -ExecutionPolicy Bypass -File .\Tools\run-agent.ps1 -Mode mcp

# Simple Bot — 简单 Bot 自动操控
powershell -ExecutionPolicy Bypass -File .\Tools\run-agent.ps1 -Mode bot
```

## 许可

仅供学习与测试用途。
