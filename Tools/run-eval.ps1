param(
    [ValidateSet("rule", "tabular", "nn", "ppo")]
    [string]$Policy = "rule",
    [int]$Episodes = 20,
    [int]$Steps = 200,
    [Nullable[int]]$SeedBase = 1000,
    [string]$Output = "",
    [string]$BridgeHost = "127.0.0.1",
    [int]$Port = 47077,
    [switch]$NoFastMode
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "Resolve-Python.ps1")

$arguments = @(
    Join-Path $PSScriptRoot "python\training\evaluate_battle_policies.py"
    "--policy", $Policy
    "--episodes", $Episodes
    "--steps", $Steps
    "--host", $BridgeHost
    "--port", $Port
)

if ($SeedBase -ne $null) {
    $arguments += @("--seed-base", $SeedBase)
}

if (-not [string]::IsNullOrWhiteSpace($Output)) {
    $arguments += @("--output", $Output)
}

if ($NoFastMode) {
    $arguments += "--no-fast-mode"
}
else {
    $arguments += "--fast-mode"
}

Invoke-ProjectPython @arguments
