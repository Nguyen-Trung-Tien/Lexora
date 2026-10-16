[CmdletBinding()]
param (
    [switch]$JsonOutput
)

$ErrorActionPreference = "Stop"

Write-Host "Running SM-2 Spaced Repetition Engine Test Verification..." -ForegroundColor Cyan

$cmd = "test tests/Lexora.Tests.Unit --no-build --filter FullyQualifiedName~SpacedRepetition --logger console;verbosity=minimal"

try {
    $testOutput = & dotnet ((-split $cmd)) 2>&1
    $exitCode = $LASTEXITCODE

    $passedTests = 0
    $totalTests = 0

    foreach ($line in $testOutput) {
        if ($line -match 'Passed!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)') {
            $passedTests = [int]$matches[2]
            $totalTests = [int]$matches[4]
        }
    }

    $result = [PSCustomObject]@{
        Timestamp   = (Get-Date).ToString("o")
        Engine      = "Sm2SpacedRepetitionEngine"
        PassedTests = $passedTests
        TotalTests  = $totalTests
        Status      = if ($exitCode -eq 0) { "PASSED" } else { "FAILED" }
    }

    if ($JsonOutput) {
        $result | ConvertTo-Json -Depth 3
    } else {
        if ($exitCode -eq 0) {
            Write-Host "SM-2 Engine Tests Passed: $passedTests / $totalTests tests successful." -ForegroundColor Green
            Write-Host "Result: SM-2 ALGORITHM VERIFICATION PASSED (EF >= 1.3 invariant guaranteed)!" -ForegroundColor Green
            exit 0
        } else {
            Write-Host "SM-2 Engine Tests FAILED:" -ForegroundColor Red
            $testOutput | Write-Host -ForegroundColor Yellow
            exit 1
        }
    }
} catch {
    Write-Error "Execution error: $_"
    exit 1
}
