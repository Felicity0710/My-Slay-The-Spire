param(
    [int]$Episodes = 50,
    [int]$Steps = 200,
    [switch]$Replay
)

$ErrorActionPreference = "Stop"

$python = "C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe"
$repoRoot = Split-Path $PSScriptRoot -Parent
$dataset = Join-Path $repoRoot "Tools\python\replays\battle_rule_rollouts.jsonl"
$policy = Join-Path $repoRoot "Tools\python\checkpoints\battle_bc_policy.json"

Write-Host "Collecting battle-only rollouts..."
& $python (Join-Path $PSScriptRoot "python\training\rollout.py") --episodes $Episodes --steps $Steps --output $dataset

Write-Host "Training tabular behavior-cloning policy..."
& $python (Join-Path $PSScriptRoot "python\training\train_bc.py") --dataset $dataset --output $policy

if ($Replay) {
    Write-Host "Replaying trained policy..."
    & $python (Join-Path $PSScriptRoot "python\training\run_bc_policy.py") --policy $policy --steps $Steps
}
