$ErrorActionPreference = 'Stop'

$nugetPackages = Join-Path $env:USERPROFILE '.nuget/packages'
$project = 'slay the hs.csproj'

# 统一使用本地 NuGet 包目录，避免 NuGet.Config 权限问题导致的 SDK 解析失败
$env:NUGET_PACKAGES = $nugetPackages

if (-not (Test-Path $project)) {
    throw "未找到项目文件：$project，请在项目根目录运行此脚本。"
}

dotnet restore $project
dotnet build $project -c Debug
