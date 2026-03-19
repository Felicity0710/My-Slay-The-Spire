param(
    [string]$Project = "Tools\SlayHs.Agent\SlayHs.Agent.csproj"
)

$ErrorActionPreference = "Stop"

$tmp = Join-Path (Get-Location) "tempappdata"
Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path (Join-Path $tmp "NuGet") -Force | Out-Null

$packages = Join-Path $env:USERPROFILE ".nuget\packages"
$configPath = Join-Path $tmp "NuGet\NuGet.Config"
$configXml = "<configuration><packageSources><clear /><add key='local' value='$packages' /></packageSources></configuration>"
Set-Content -Path $configPath -Value $configXml -Encoding UTF8

$env:APPDATA = $tmp
$env:NUGET_PACKAGES = $packages

dotnet build $Project --configfile $configPath --source $packages --ignore-failed-sources
