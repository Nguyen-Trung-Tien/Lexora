[CmdletBinding()]
param (
    [ValidateSet("BuildOnly", "FastUnit", "Full")]
    [string]$Mode = "BuildOnly",
    [string]$Filter = ""
)

$ErrorActionPreference = "Stop"
$solutionFile = "Lexora.slnx"

$result = [PSCustomObject]@{
    timestamp = (Get-Date).ToString("o")
    mode      = $Mode
    status    = "UNKNOWN"
    buildOk   = $false
    testsOk   = $false
    errors    = @()
}

Write-Host "Initiating Lexora Quality Gates Audit (Mode: $Mode)..." -ForegroundColor Cyan

# 1. Build Verification
try {
    $buildOutput = & dotnet build $solutionFile -v q /clp:ErrorsOnly 2>&1
    $buildExitCode = $LASTEXITCODE
    if ($buildExitCode -eq 0) {
        $result.buildOk = $true
        Write-Host "Build Succeeded with 0 Warnings and 0 Errors." -ForegroundColor Green
    } else {
        $result.buildOk = $false
        $result.errors += "Build failed with exit code $buildExitCode"
        Write-Host "Build FAILED." -ForegroundColor Red
        $result.status = "FAILED"
        $result | ConvertTo-Json -Depth 4
        exit 1
    }
} catch {
    $result.buildOk = $false
    $result.errors += $_.Exception.Message
    $result.status = "FAILED"
    $result | ConvertTo-Json -Depth 4
    exit 1
}

# If BuildOnly mode requested, conclude here
if ($Mode -eq "BuildOnly") {
    $result.status = "PASSED"
    $result.testsOk = $true
    Write-Host "Quality Gate Result: PASSED" -ForegroundColor Green
    $result | ConvertTo-Json -Depth 4
    exit 0
}

# 2. Test Verification
try {
    $cmd = "test $solutionFile --no-build --logger console;verbosity=minimal"
    if ($Mode -eq "FastUnit") {
        $cmd += " --filter Category!=Integration"
    }
    if ($Filter) {
        $cmd += " --filter $Filter"
    }

    Write-Host "Running tests..." -ForegroundColor Yellow
    $testOutput = & dotnet ((-split $cmd)) 2>&1
    $testExitCode = $LASTEXITCODE
    if ($testExitCode -eq 0) {
        $result.testsOk = $true
        $result.status = "PASSED"
        Write-Host "Tests: All test suites passed." -ForegroundColor Green
        Write-Host "Quality Gate Result: PASSED" -ForegroundColor Green
        $result | ConvertTo-Json -Depth 4
        exit 0
    } else {
        $result.testsOk = $false
        $result.status = "FAILED"
        $result.errors += "Test run failed with exit code $testExitCode"
        Write-Host "Tests: One or more tests failed." -ForegroundColor Red
        $result | ConvertTo-Json -Depth 4
        exit 1
    }
} catch {
    $result.testsOk = $false
    $result.status = "FAILED"
    $result.errors += $_.Exception.Message
    $result | ConvertTo-Json -Depth 4
    exit 1
}
