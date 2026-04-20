# 环境配置指南

本文档用于在 Windows 上快速完成 `slay-the-hs` 的开发环境准备。

## 1. 必备软件

- Godot 4.5（.NET/C# 版本）
- .NET SDK 8.x（游戏主项目和测试）
- .NET SDK 9.x（可选，仅 `Tools/SlayHs.Agent` 需要）
- Python 3（可选，仅训练/评估脚本需要）
- Git
- PowerShell（建议 5.1 或更新）

## 2. 版本自检

在项目根目录打开 PowerShell，执行：

```powershell
dotnet --list-sdks
python --version
py -3 --version
```

检查点：

- 输出中包含 8.x SDK
- 如果你要运行 Agent，输出中还需包含 9.x SDK
- `python` 或 `py -3` 至少有一个可用

## 3. 获取代码并构建

```powershell
git clone <your-repo-url>
cd slay-the-hs
.\build.ps1
```

也可以使用：

```powershell
.\build.bat
```

说明：

- `build.ps1` 会自动执行 `dotnet restore` + `dotnet build -c Debug`
- 脚本已将 `NUGET_PACKAGES` 指向用户目录，减少权限相关 NuGet 问题

## 4. 启动游戏

1. 使用 Godot 4.5 (.NET) 打开项目根目录下的 `project.godot`
2. 等待 C# 首次编译完成
3. 运行主场景（默认 `Scenes/MainMenu.tscn`）

## 5. 运行测试

```powershell
dotnet run --project .\Tests\CombatLogicTests\CombatLogicTests.csproj
```

说明：

- 测试项目目标框架为 `net8.0`
- 测试会读取 `Data/*.json`，请确保在项目根目录执行命令

## 6. 可选：Agent（MCP/Bot）

先启动游戏，再启动 Agent。游戏桥接端口默认为 `127.0.0.1:47077`。

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-agent.ps1 -Mode mcp
```

或：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-agent.ps1 -Mode bot
```

如果遇到 NuGet 配置权限问题，可先执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\build-agent.ps1
```

## 7. 可选：训练/评估脚本（Python）

如果 `python` 不在 PATH，可先设置：

```powershell
$env:SLAY_THE_HS_PYTHON = "C:\Path\To\python.exe"
```

运行评估：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-eval.ps1 -Policy rule -Episodes 20
```

运行 Stage 1 训练：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run-training-stage1.ps1 -Episodes 50 -Steps 200
```

## 8. 常见问题

### 8.1 PowerShell 拒绝执行脚本

使用一次性绕过执行策略：

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

### 8.2 找不到 Python

确认 `python` 或 `py -3` 可执行，或设置 `SLAY_THE_HS_PYTHON` 到明确解释器路径。

### 8.3 Agent 无法启动

确认已安装 .NET 9 SDK，并且游戏进程已启动（桥接端口可用）。

### 8.4 构建有警告

当前项目存在一批已知 nullable warning。只要构建结果为 `成功` 且无 error，可继续开发。
