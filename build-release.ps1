# ==============================================================================
# Lexora - Release Build and Distribution Automation Script
# ==============================================================================
param (
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "./publish/win-x64",
    [bool]$SelfContained = $true
)

$ErrorActionPreference = "Stop"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " [BUILD] Lexora English Learning Manager - Release Pipeline" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# 1. Clean previous publish folder
if (Test-Path $OutputDir) {
    Write-Host "[CLEAN] Cleaning existing output folder: $OutputDir" -ForegroundColor Yellow
    Remove-Item -Path $OutputDir -Recurse -Force
}

# 2. Run Test Suite
Write-Host "`n[TEST] Step 1: Running all Unit and Integration Tests..." -ForegroundColor Green
dotnet test Lexora.slnx --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAIL] Test run failed! Release build aborted." -ForegroundColor Red
    exit 1
}

# 3. Publish Presentation Project
Write-Host "`n[PUBLISH] Step 2: Publishing WinUI 3 Presentation App ($Configuration | $Runtime | SelfContained: $SelfContained)..." -ForegroundColor Green
dotnet publish src/Lexora.Presentation/Lexora.Presentation.csproj `
    -c $Configuration `
    -r $Runtime `
    --self-contained $SelfContained `
    -p:WindowsAppSDKSelfContained=$SelfContained `
    -o $OutputDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAIL] Publish failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n============================================================" -ForegroundColor Green
Write-Host " [SUCCESS] Release build completed successfully!" -ForegroundColor Green
Write-Host " Output directory: $OutputDir" -ForegroundColor Cyan
Write-Host " Executable: $OutputDir\Lexora.Presentation.exe" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Green
