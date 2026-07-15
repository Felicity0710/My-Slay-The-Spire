param(
    [switch]$RunTests
)

$ErrorActionPreference = 'Stop'

$nugetPackages = Join-Path $env:USERPROFILE '.nuget/packages'
$project = 'slay the hs.csproj'
$testProject = 'Tests\CombatLogicTests\CombatLogicTests.csproj'

# Use a user-local NuGet package cache to avoid machine-level config permission issues.
$env:NUGET_PACKAGES = $nugetPackages

if (-not (Test-Path $project)) {
    throw "Project file not found: $project. Run this script from the project root."
}

dotnet restore $project
dotnet build $project -c Debug

if ($RunTests) {
    dotnet run --project $testProject
}
