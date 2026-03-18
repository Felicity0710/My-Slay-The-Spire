param(
    [Parameter(Mandatory)]
    [string[]] $TargetJson = @("Data/cards.json"),

    [switch] $AllowBom
)

$Utf8 = [System.Text.UTF8Encoding]::new($false)
$pass = $true

function Test-InvalidJson {
    param([string] $Path)

    $text = [System.IO.File]::ReadAllText($Path, $Utf8)
    try {
        $null = $text | ConvertFrom-Json -Depth 200
        return $true
    } catch {
        Write-Host "JSON parse failed: $Path"
        Write-Host $_.Exception.Message
        return $false
    }
}

function Test-FileEncoding {
    param([string] $Path)
    $bytes = [System.IO.File]::ReadAllBytes($Path)

    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        if (-not $AllowBom) {
            Write-Host "BOM found (not recommended): $Path"
        }
    }

    try {
        $text = $Utf8.GetString($bytes)
        $roundTrip = $Utf8.GetBytes($text)

        if ($bytes.Length -ne $roundTrip.Length) {
            Write-Host "Byte length changed after UTF-8 roundtrip: $Path"
            return $false
        }

        for ($i = 0; $i -lt $bytes.Length; $i++) {
            if ($bytes[$i] -ne $roundTrip[$i]) {
                Write-Host "UTF-8 byte mismatch at $i: $Path"
                return $false
            }
        }

        return $true
    } catch {
        Write-Host "Encoding decode failed: $Path"
        Write-Host $_.Exception.Message
        return $false
    }
}

foreach ($path in $TargetJson) {
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Host "Not found: $path"
        $pass = $false
        continue
    }

    Write-Host "Checking: $path"
    if (-not (Test-FileEncoding -Path $path)) { $pass = $false }
    if (-not (Test-InvalidJson -Path $path)) { $pass = $false }
}

if (-not $pass) {
    throw "Guard check failed."
}

Write-Host "Guard check passed."
