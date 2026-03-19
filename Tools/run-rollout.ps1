param(
    [int]$Episodes = 50,
    [int]$Steps = 200,
    [string]$Output = "",
    [string]$BridgeHost = "127.0.0.1",
    [int]$Port = 47077,
    [switch]$NoFastMode
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "Resolve-Python.ps1")

$repoRoot = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $repoRoot "Tools\python\replays\battle_rule_rollouts.jsonl"
}

$arguments = @(
    Join-Path $PSScriptRoot "python\training\rollout.py"
    "--episodes", $Episodes
    "--steps", $Steps
    "--host", $BridgeHost
    "--port", $Port
    "--output", $Output
)

if ($NoFastMode) {
    $arguments += "--no-fast-mode"
}
else {
    $arguments += "--fast-mode"
}

Invoke-ProjectPython @arguments
