param(
    [ValidateSet("mcp", "bot")]
    [string]$Mode = "mcp"
)

$ErrorActionPreference = "Stop"

powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "build-agent.ps1")
dotnet run --project (Join-Path $PSScriptRoot "SlayHs.Agent\SlayHs.Agent.csproj") -- $Mode
