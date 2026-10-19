[CmdletBinding()]
param (
    [string]$TargetVersion = "1.0.0",
    [switch]$SkipTests,
    [switch]$JsonOutput
)

$ErrorActionPreference = "Stop"

Write-Host "=== Lexora Release Readiness Audit (Target Version: $TargetVersion) ===" -ForegroundColor Cyan

$unreleasedDir = "changelog/unreleased"
$fragments = @()
if (Test-Path $unreleasedDir) {
    $fragments = Get-ChildItem -Path $unreleasedDir -Filter "*.md" -File | Where-Object { $_.Name -ne ".gitkeep" -and $_.Name -ne "fragment-template.md" }
}

# 1. Check Quality Gates
$auditMode = if ($SkipTests) { "BuildOnly" } else { "Full" }
$qualityResult = & powershell -ExecutionPolicy Bypass -File .agents/skills/lexora-quality-audit/scripts/audit_quality_gates.ps1 -Mode $auditMode
$buildOk = ($LASTEXITCODE -eq 0)

# 2. Check Architecture
Write-Host "`n2. Running Clean Architecture Lint..." -ForegroundColor Yellow
$archResult = & powershell -ExecutionPolicy Bypass -File .agents/skills/clean-architecture-lint/scripts/lint_architecture.ps1
$archOk = ($LASTEXITCODE -eq 0)

# 3. Check i18n
Write-Host "`n3. Checking i18n Localization Parity..." -ForegroundColor Yellow
$i18nResult = & powershell -ExecutionPolicy Bypass -File .agents/skills/i18n-localization-sync/scripts/sync_localization.ps1 -CheckXaml
$i18nOk = ($LASTEXITCODE -eq 0)

# 4. Check SM-2
Write-Host "`n4. Verifying SM-2 Algorithm Engine..." -ForegroundColor Yellow
$sm2Result = & powershell -ExecutionPolicy Bypass -File .agents/skills/sm2-algorithm-verify/scripts/verify_sm2.ps1
$sm2Ok = ($LASTEXITCODE -eq 0)

$allPassed = $buildOk -and $archOk -and $i18nOk -and $sm2Ok

$report = [PSCustomObject]@{
    Timestamp          = (Get-Date).ToString("o")
    TargetVersion      = $TargetVersion
    BuildAndTestsOk    = $buildOk
    ArchitectureOk     = $archOk
    LocalizationOk     = $i18nOk
    Sm2EngineOk        = $sm2Ok
    UnreleasedFragments= $fragments.Count
    Status             = if ($allPassed) { "READY_FOR_RELEASE" } else { "NOT_READY" }
}

if ($JsonOutput) {
    $report | ConvertTo-Json -Depth 3
} else {
    Write-Host "`n=== Release Preparation Summary ===" -ForegroundColor Cyan
    Write-Host "Build & Tests: $(if ($buildOk) { 'PASSED' } else { 'FAILED' })"
    Write-Host "Clean Architecture: $(if ($archOk) { 'PASSED' } else { 'FAILED' })"
    Write-Host "i18n Parity: $(if ($i18nOk) { 'PASSED' } else { 'FAILED' })"
    Write-Host "SM-2 Engine: $(if ($sm2Ok) { 'PASSED' } else { 'FAILED' })"
    Write-Host "Pending Changelog Fragments: $($fragments.Count)"

    if ($allPassed) {
        Write-Host "`nRESULT: SOLUTION IS 100% READY FOR PACKAGING & RELEASE!" -ForegroundColor Green
        Write-Host "To generate installer, run: powershell -ExecutionPolicy Bypass -File ./build-installer.ps1 -Version `"$TargetVersion`"" -ForegroundColor Cyan
        exit 0
    } else {
        Write-Host "`nRESULT: RELEASE BLOCKED BY FAILED QUALITY GATES." -ForegroundColor Red
        exit 1
    }
}
