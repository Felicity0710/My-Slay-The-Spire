param(
    [Parameter(Mandatory)]
    [string] $InputPath = "Data/cards.json",

    [string] $ArtFolder = "res://Assets/Cards",

    [switch] $Force,

    [switch] $DryRun
)

$Encoding = [System.Text.UTF8Encoding]::new($false)

if (-not (Test-Path -LiteralPath $InputPath)) {
    throw "Input file not found: $InputPath"
}

$jsonText = [System.IO.File]::ReadAllText($InputPath, $Encoding)
$data = $jsonText | ConvertFrom-Json -Depth 200

if (-not $data.PSObject.Properties.Name.Contains("cards")) {
    throw "Missing cards array in JSON."
}

$updated = 0
$normalized = 0

foreach ($card in $data.cards) {
    if (-not $card.id) { continue }

    $path = ""
    if ($null -ne $card.artPath) {
        $path = [string]$card.artPath
    }

    $needSet = $Force -or [string]::IsNullOrWhiteSpace($path)
    if (-not $needSet) {
        $normalizedPath = $path.Replace("\", "/").Trim()
        if ($normalizedPath -ne $path) {
            $card.artPath = $normalizedPath
            $normalized++
        }
        continue
    }

    $card.artPath = "$ArtFolder/$($card.id).png"
    $updated++
}

$out = $data | ConvertTo-Json -Depth 200

if ($DryRun) {
    Write-Host "DRY RUN: would update $updated cards, normalize $normalized paths."
    Write-Host "No file changed."
    return
}

[System.IO.File]::WriteAllText((Resolve-Path $InputPath), $out, $Encoding)
Write-Host "Updated cards.json to UTF-8."
Write-Host "Set artPath: $updated"
Write-Host "Normalized path separators: $normalized"
