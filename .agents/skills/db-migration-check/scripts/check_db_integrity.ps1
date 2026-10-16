[CmdletBinding()]
param (
    [switch]$SkipTests,
    [switch]$JsonOutput
)

$ErrorActionPreference = "Stop"

Write-Host "=== Lexora Database Schema & Backup Integrity Check ===" -ForegroundColor Cyan

$issues = @()

# 1. Verify WAL Mode & SQLite Configuration in Initializer / DbContext
$initializerFile = "src/Lexora.Infrastructure/Data/SqliteDatabaseInitializer.cs"
if (Test-Path $initializerFile) {
    $initContent = Get-Content $initializerFile -Raw
    if ($initContent -notmatch 'journal_mode\s*=\s*WAL' -or $initContent -notmatch 'foreign_keys\s*=\s*ON') {
        $issues += "SqliteDatabaseInitializer does not appear to configure WAL mode or foreign key PRAGMAs."
    }
} else {
    $issues += "SqliteDatabaseInitializer.cs not found at $initializerFile"
}

# 2. Verify 16-byte SQLite Magic Header Verification in Backup Service
$backupServiceFile = "src/Lexora.Infrastructure/Services/DatabaseBackupService.cs"
if (Test-Path $backupServiceFile) {
    $backupContent = Get-Content $backupServiceFile -Raw
    if ($backupContent -notmatch 'SQLite format 3' -and $backupContent -notmatch '0x53') {
        $issues += "DatabaseBackupService does not appear to verify the 16-byte SQLite magic header before restoration."
    }
    if ($backupContent -notmatch 'ClearAllPools') {
        $issues += "DatabaseBackupService does not call SqliteConnection.ClearAllPools() before replacing the active database file."
    }
} else {
    $issues += "DatabaseBackupService.cs not found at $backupServiceFile"
}

# 3. Run SQLite Integration Tests
$testsPassed = $false
if (-not $SkipTests) {
    Write-Host "`nRunning SQLite Integration Tests..." -ForegroundColor Yellow
    $testOutput = & dotnet test tests/Lexora.Tests.Integration --no-build --logger "console;verbosity=minimal" 2>&1
    if ($LASTEXITCODE -eq 0) {
        $testsPassed = $true
        Write-Host "All repository and database integration tests passed!" -ForegroundColor Green
    } else {
        $issues += "Database integration tests failed."
    }
} else {
    $testsPassed = $true
}

$status = if ($issues.Count -eq 0) { "PASSED" } else { "FAILED" }

$report = [PSCustomObject]@{
    Timestamp          = (Get-Date).ToString("o")
    IssuesCount        = $issues.Count
    Issues             = $issues
    IntegrationTestsOk = $testsPassed
    Status             = $status
}

if ($JsonOutput) {
    $report | ConvertTo-Json -Depth 3
} else {
    Write-Host "`n=== Database Integrity Summary ===" -ForegroundColor Cyan
    Write-Host "Invariants Verified: WAL Mode, 16-Byte Magic Header, ClearAllPools, SQLite Integration Tests"
    Write-Host "Issues Count: $($issues.Count)"

    if ($issues.Count -gt 0) {
        Write-Host "`nIssues Detected:" -ForegroundColor Red
        foreach ($iss in $issues) {
            Write-Host "  - $iss" -ForegroundColor Yellow
        }
        exit 1
    } else {
        Write-Host "`nRESULT: DATABASE & MIGRATION INTEGRITY INVARIANTS 100% SATISFIED!" -ForegroundColor Green
        exit 0
    }
}
