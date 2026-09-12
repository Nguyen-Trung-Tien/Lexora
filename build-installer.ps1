# ==============================================================================
# Lexora - Inno Setup Build & Packaging Pipeline
# ==============================================================================
param (
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipTests,
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " [PACKAGING] Lexora Inno Setup Distribution Pipeline v$Version" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# 1. Locate Inno Setup Compiler (ISCC.exe)
$isccCandidates = @(
    "iscc.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$isccPath = $null
foreach ($candidate in $isccCandidates) {
    if (Get-Command $candidate -ErrorAction SilentlyContinue) {
        $isccPath = $candidate
        break
    } elseif (Test-Path $candidate) {
        $isccPath = $candidate
        break
    }
}

if (-not $isccPath) {
    Write-Host "`n[ERROR] Inno Setup Compiler (ISCC.exe) not found!" -ForegroundColor Red
    Write-Host "Please install Inno Setup 6 from https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n[FOUND] Inno Setup Compiler: $isccPath" -ForegroundColor Green

# 2. Run Test Suite (unless skipped)
if (-not $SkipTests) {
    Write-Host "`n[TEST] Step 1: Running unit and integration tests..." -ForegroundColor Green
    dotnet test Lexora.slnx --configuration $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[FAIL] Unit/Integration tests failed! Packaging aborted." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "`n[TEST] Skipping tests (-SkipTests specified)." -ForegroundColor Yellow
}

# 3. Publish WinUI 3 Project (Self-contained)
$publishDir = Join-Path $PSScriptRoot "publish\$Runtime"
$installerDir = Join-Path $PSScriptRoot "publish\installer"

if (-not $SkipPublish) {
    Write-Host "`n[PUBLISH] Step 2: Publishing WinUI 3 App (Self-contained, $Configuration, $Runtime)..." -ForegroundColor Green
    
    if (Test-Path $publishDir) {
        Write-Host "Cleaning previous publish directory: $publishDir" -ForegroundColor Yellow
        Remove-Item -Path $publishDir -Recurse -Force
    }

    dotnet publish "$PSScriptRoot\src\Lexora.Presentation\Lexora.Presentation.csproj" `
        -c $Configuration `
        -r $Runtime `
        --self-contained true `
        -p:WindowsAppSDKSelfContained=true `
        -o $publishDir

    if ($LASTEXITCODE -ne 0) {
        Write-Host "[FAIL] Publish failed! Packaging aborted." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "`n[PUBLISH] Skipping publish (-SkipPublish specified)." -ForegroundColor Yellow
    if (-not (Test-Path $publishDir)) {
        Write-Host "[ERROR] Publish directory not found: $publishDir" -ForegroundColor Red
        exit 1
    }
}

# 4. Compile Installer using Inno Setup
Write-Host "`n[INNO SETUP] Step 3: Compiling installer with Inno Setup 6..." -ForegroundColor Green

# Ensure Wizard branding bitmaps exist
$wizardSmall = Join-Path $PSScriptRoot "installer\wizard-small.bmp"
$wizardLarge = Join-Path $PSScriptRoot "installer\wizard-large.bmp"
if (-not (Test-Path $wizardSmall) -or -not (Test-Path $wizardLarge)) {
    Write-Host "[BRANDING] Generating Inno Setup wizard banner and icon images..." -ForegroundColor Cyan
    & "$PSScriptRoot\installer\generate-wizard-branding.ps1"
}

if (-not (Test-Path $installerDir)) {
    New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
}

$issFile = Join-Path $PSScriptRoot "installer\LexoraSetup.iss"

& $isccPath `
    "/DMyAppVersion=$Version" `
    "/DMyAppSourceDir=$publishDir" `
    "/DMyOutputDir=$installerDir" `
    "$issFile"

if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAIL] Inno Setup compilation failed!" -ForegroundColor Red
    exit 1
}

# 5. Output Result
$installerFile = Join-Path $installerDir "Lexora-Setup-v$Version.exe"
if (Test-Path $installerFile) {
    $fileInfo = Get-Item $installerFile
    $sizeMb = [math]::Round($fileInfo.Length / 1MB, 2)
    Write-Host "`n============================================================" -ForegroundColor Green
    Write-Host " [SUCCESS] Installer generated successfully!" -ForegroundColor Green
    Write-Host " Installer File: $installerFile" -ForegroundColor Cyan
    Write-Host " File Size     : $sizeMb MB" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Green
} else {
    Write-Host "`n[WARNING] Installer completed but file could not be verified at: $installerFile" -ForegroundColor Yellow
}
