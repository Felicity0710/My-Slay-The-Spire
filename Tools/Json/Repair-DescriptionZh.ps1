param(
    [Parameter(Mandatory)]
    [string] $InputPath = "Data/cards.json",

    [string] $BackupSuffix = ".bak",

    [switch] $DryRun,

    [switch] $ForceFallback
)

$Utf8 = [System.Text.UTF8Encoding]::new($false)
$Gbk = [System.Text.Encoding]::GetEncoding("GBK")

if (-not (Test-Path -LiteralPath $InputPath)) {
    throw "Input file not found: $InputPath"
}

function IsLikelyBroken {
    param([string] $Text)
    return (
        $Text.Contains("?") -or
        $Text.Contains([char]0xFFFD) -or
        $Text -match '[\uE000-\uF8FF]'
    )
}

$cardsObj = [System.IO.File]::ReadAllText($InputPath, $Utf8) | ConvertFrom-Json -Depth 200
if (-not $cardsObj.PSObject.Properties.Name.Contains("cards")) {
    throw "Missing cards array in JSON."
}

$changed = 0
$fallbacked = 0

foreach ($card in $cardsObj.cards) {
    if (-not ($card.PSObject.Properties.Name.Contains("descriptionZh") -and $card.PSObject.Properties.Name.Contains("description"))) {
        continue
    }

    $zh = [string]$card.descriptionZh
    $en = [string]$card.description
    $roundTrip = $Utf8.GetString($Gbk.GetBytes($zh))
    $roundTripClean = $roundTrip.Trim()

    if (IsLikelyBroken $roundTripClean) {
        if ($ForceFallback -and -not [string]::IsNullOrWhiteSpace($en)) {
            $card.descriptionZh = $en
            $changed++
            $fallbacked++
        }
        continue
    }

    # Avoid writing random replacements when recovery is not clearly better.
    if ($roundTripClean.Length -gt 1 -and $roundTripClean -ne $zh) {
        $card.descriptionZh = $roundTripClean
        $changed++
    }
}

if ($DryRun) {
    Write-Host "DRY RUN: would change $changed fields ($fallbacked fallbacked)."
    return
}

Copy-Item -LiteralPath $InputPath -Destination ($InputPath + $BackupSuffix) -Force
$out = $cardsObj | ConvertTo-Json -Depth 200
[System.IO.File]::WriteAllText((Resolve-Path $InputPath), $out, $Utf8)
Write-Host "Patched file written in UTF-8: $InputPath"
Write-Host "Modified descriptionZh: $changed"
Write-Host "Fallbacked to description: $fallbacked"
Write-Host "Backup: $InputPath$BackupSuffix"
