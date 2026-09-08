# WoodStreamPinger Build and Package Script (Installer)
[CmdletBinding()]
param (
    [ValidateSet("x64", "arm64", "all", "")]
    [string]$Architecture = "x64",

    [string]$VersionOverride = ""
)

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot
if (-not $ScriptRoot) {
    $ScriptRoot = (Get-Location).Path
}

# 1. ターゲットアーキテクチャの決定
$targets = @()
if ([string]::IsNullOrWhiteSpace($Architecture) -or $Architecture -eq "all") {
    $targets = @("x64", "arm64")
} else {
    $targets = @($Architecture)
}

# 2. csproj からバージョン取得
$csprojPath = Join-Path $ScriptRoot "WoodStreamPinger.csproj"
[xml]$csprojXml = Get-Content -Path $csprojPath -Raw -Encoding UTF8
$version = $csprojXml.Project.PropertyGroup.Version
if ($version -is [array]) {
    $version = $version[0]
}
if ([string]::IsNullOrWhiteSpace($version)) {
    $version = "1.0.0.0"
} else {
    $version = $version.Trim()
}
if (-not [string]::IsNullOrWhiteSpace($VersionOverride)) {
    $version = $VersionOverride.Trim()
}

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " WoodStreamPinger Installer Build Tool" -ForegroundColor Cyan
Write-Host " Version:       $version" -ForegroundColor Cyan
Write-Host " Target Arch:   $($targets -join ', ')" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 3. 出力先ディレクトリ: .\Installer
$installerDir = Join-Path $ScriptRoot "Installer"
if (-not (Test-Path -Path $installerDir)) {
    New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
}

# 4. Inno Setup コンパイラの検出
$isccCandidates = @(
    "C:\Users\tomok\AppData\Local\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    (Get-Command iscc -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source)
)

$isccPath = $null
foreach ($cand in $isccCandidates) {
    if ($cand -and (Test-Path -Path $cand)) {
        $isccPath = $cand
        break
    }
}

if (-not $isccPath) {
    Write-Error "Inno Setup Compiler (ISCC.exe) が見つかりませんでした。"
    exit 1
}
Write-Host "Using Inno Setup: $isccPath" -ForegroundColor Gray

$createdArtifacts = @()

# 5. 各アーキテクチャのビルド & インストーラ作成
foreach ($arch in $targets) {
    $rid = "win-$arch"
    Write-Host "`n>>> Processing Target: $arch ($rid) <<<" -ForegroundColor Yellow

    $publishDir = Join-Path $ScriptRoot "bin\Release\net10.0-windows10.0.26100.0\$rid\publish"
    
    Write-Host "Publishing self-contained release for $rid..." -ForegroundColor Cyan
    dotnet publish "$csprojPath" -c Release -r "$rid" --self-contained true -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -o "$publishDir"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed for $rid."
        exit $LASTEXITCODE
    }

    Write-Host "Building Inno Setup installer for $arch..." -ForegroundColor Cyan
    $issPath = Join-Path $ScriptRoot "setup.iss"
    
    & "$isccPath" "/DMyAppVersion=$version" "/DMyAppArch=$arch" "/DMySourceDir=$publishDir" "/DMyOutputDir=$installerDir" "$issPath"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Inno Setup compile failed for $arch."
        exit $LASTEXITCODE
    }

    $expectedInstaller = Join-Path $installerDir "WoodStreamPinger_Setup_v${version}_${arch}.exe"
    if (Test-Path -Path $expectedInstaller) {
        $createdArtifacts += (Get-Item -Path $expectedInstaller)
    }
}

Write-Host "`n==========================================" -ForegroundColor Green
Write-Host " Installer Packaging Completed!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
foreach ($item in $createdArtifacts) {
    $sizeMB = [math]::Round($item.Length / 1MB, 2)
    Write-Host "File: $($item.Name)" -ForegroundColor Green
    Write-Host "Path: $($item.FullName)" -ForegroundColor Green
    Write-Host "Size: $sizeMB MB`n" -ForegroundColor Green
}
