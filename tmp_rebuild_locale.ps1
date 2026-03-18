$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function ConvertFrom-JsonSafe {
    param([string]$Text, [string]$SourceLabel)

    try {
        return $Text | ConvertFrom-Json
    }
    catch {
        throw "JSON parse failed: $SourceLabel`n$($_.Exception.Message)"
    }
}

function LoadDictionary {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return [ordered]@{}
    }

    try {
    $text = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
        $obj = ConvertFrom-JsonSafe -Text $text -SourceLabel $Path
        $dict = [ordered]@{}

        foreach ($prop in $obj.PSObject.Properties) {
            $dict[$prop.Name] = $prop.Value
        }

        return $dict
    }
    catch {
        Write-Host "WARNING: cannot parse $Path, fallback to empty dictionary."
        return [ordered]@{}
    }
}

function AddIfAbsent {
    param(
        [System.Collections.Specialized.OrderedDictionary]$Target,
        [string]$Key,
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Key) -or [string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    if (-not $Target.Contains($Key)) {
        $Target[$Key] = $Value
    }
}

function MergeLocalizationFromCode {
    param(
        [string]$RootDir,
        [System.Collections.Specialized.OrderedDictionary]$En,
        [System.Collections.Specialized.OrderedDictionary]$Zh
    )

    $files = Get-ChildItem -Recurse -File -Path (Join-Path $RootDir 'Scripts')
    $pattern = 'LocalizationService\.(?:Get|Format)\(\s*"([^"]+)"\s*,\s*"([^"\\]*(?:\\.[^"\\]*)*)"'

    foreach ($file in $files) {
        $text = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
        $matches = [regex]::Matches($text, $pattern)

        foreach ($match in $matches) {
            $key = $match.Groups[1].Value
            $value = $match.Groups[2].Value
            if ([string]::IsNullOrWhiteSpace($key) -or [string]::IsNullOrWhiteSpace($value)) {
                continue
            }

            AddIfAbsent -Target $En -Key $key -Value $value

            if (-not $Zh.Contains($key)) {
                # Keep zh entry as key's default text. For English-only fallback keys,
                # this is still valid (runtime can be improved by replacing manually later).
                $Zh[$key] = $value
            }
        }
    }
}

# 1) Rebuild cards json from git HEAD
$cardsSource = git show HEAD:Data/cards.json
if (-not $cardsSource) {
    throw 'Failed to read HEAD:Data/cards.json via git.'
}

$cards = ConvertFrom-JsonSafe -Text $cardsSource -SourceLabel 'HEAD:Data/cards.json'
if ($null -eq $cards -or -not $cards.PSObject.Properties.Name.Contains('cards')) {
    throw 'HEAD cards.json is missing `cards` array.'
}

foreach ($card in $cards.cards) {
    if (-not $card.PSObject.Properties.Name.Contains('artPath')) {
        Add-Member -InputObject $card -MemberType NoteProperty -Name 'artPath' -Value '' | Out-Null
    }
    if (-not $card.PSObject.Properties.Name.Contains('nameKey')) {
        Add-Member -InputObject $card -MemberType NoteProperty -Name 'nameKey' -Value '' | Out-Null
    }
    if (-not $card.PSObject.Properties.Name.Contains('descriptionKey')) {
        Add-Member -InputObject $card -MemberType NoteProperty -Name 'descriptionKey' -Value '' | Out-Null
    }

    if ([string]::IsNullOrWhiteSpace($card.artPath)) {
        $card.artPath = "res://Assets/Cards/$($card.id).png"
    }
    else {
        $card.artPath = $card.artPath.Trim()
    }

    $card.nameKey = "card.$($card.id).name"
    $card.descriptionKey = "card.$($card.id).description"

    if ([string]::IsNullOrWhiteSpace($card.descriptionZh)) {
        $card.descriptionZh = $card.description
    }
}

$cards | ConvertTo-Json -Depth 100 | Set-Content -Path 'Data/cards.json' -Encoding UTF8

# 2) Build localization dictionaries
$en = LoadDictionary -Path 'Data/Localization/en.json'
$zh = LoadDictionary -Path 'Data/Localization/zh_hans.json'

MergeLocalizationFromCode -RootDir '.' -En $en -Zh $zh

# Essential keys that may not be caught by regex extraction.
$extra = [ordered]@{
    'map.node.normal' = 'Normal'
    'map.node.elite' = 'Elite'
    'map.node.event' = 'Event'
    'map.node.rest' = 'Rest'
    'map.node.shop' = 'Shop'
    'map.node.unknown' = 'Unknown'
    'map.node_symbol.normal' = "`u{2694}"
    'map.node_symbol.elite' = "`u{2620}"
    'map.node_symbol.event' = "`u{25c6}"
    'map.node_symbol.rest' = "`u{2665}"
    'map.node_symbol.shop' = '$'
    'map.node_symbol.unknown' = "`u{ff1f}"
    'ui.map.status_select_path' = 'Choose a path and select next node to climb.'
}

foreach ($kv in $extra.GetEnumerator()) {
    AddIfAbsent -Target $en -Key $kv.Key -Value $kv.Value
    AddIfAbsent -Target $zh -Key $kv.Key -Value $kv.Value
}

foreach ($card in $cards.cards) {
    if ([string]::IsNullOrWhiteSpace($card.id)) {
        continue
    }

    $nameKey = "card.$($card.id).name"
    $descKey = "card.$($card.id).description"

    AddIfAbsent -Target $en -Key $nameKey -Value $card.name
    AddIfAbsent -Target $en -Key $descKey -Value $card.description

    if (-not $zh.Contains($nameKey)) {
        $zh[$nameKey] = $card.name
    }
    if (-not $zh.Contains($descKey)) {
        if (-not [string]::IsNullOrWhiteSpace($card.descriptionZh)) {
            $zh[$descKey] = $card.descriptionZh
        }
        else {
            $zh[$descKey] = $card.description
        }
    }
}

$en | ConvertTo-Json -Depth 100 | Set-Content -Path 'Data/Localization/en.json' -Encoding UTF8
$zh | ConvertTo-Json -Depth 100 | Set-Content -Path 'Data/Localization/zh_hans.json' -Encoding UTF8

Write-Host "Rebuilt cards.json => $($cards.cards.Count) cards"
Write-Host "Saved en.json => $($en.Count) keys"
Write-Host "Saved zh_hans.json => $($zh.Count) keys"
