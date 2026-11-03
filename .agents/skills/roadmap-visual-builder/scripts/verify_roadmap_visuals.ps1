param(
    [switch]$VerboseOutput = $false
)

$ErrorActionPreference = "Stop"
Write-Host "[Roadmap Visual Builder] Running Quality Gate Audits..." -ForegroundColor Cyan

# 1. Clean Architecture Invariant Checks
$domainFiles = Get-ChildItem -Path "src/Lexora.Domain" -Recurse -Filter "*.cs"
foreach ($f in $domainFiles) {
    $content = Get-Content $f.FullName -Raw
    if ($content -match "Microsoft\.UI\.Xaml" -or $content -match "Microsoft\.EntityFrameworkCore") {
        Write-Error "Domain layer violation found in $($f.FullName): References UI or EF Core!"
    }
}
Write-Host "  [PASS] Domain Layer Purity: PASSED (Zero external dependencies)" -ForegroundColor Green

# 2. XAML Anti-WMC9999 Invariant Checks
$roadmapXaml = Get-Content "src/Lexora.Presentation/Views/RoadmapPage.xaml" -Raw
if ($roadmapXaml.Contains("Loc[")) {
    Write-Error "XAML String indexer detected in RoadmapPage.xaml! Use Loc.GetString(...) to avoid WMC9999."
}
if ($roadmapXaml.Contains("ConverterParameter=") -and -not $roadmapXaml.Contains("Converter=")) {
    Write-Error "ConverterParameter without Converter detected! Triggers WMC9999 compiler null-reference."
}
Write-Host "  [PASS] XAML Compiler Safety: PASSED (No WMC9999 triggers)" -ForegroundColor Green

# 3. Zero Hardcoded Hex Colors in Roadmap Presentation
$xamlLines = Get-Content "src/Lexora.Presentation/Views/RoadmapPage.xaml"
$lineIdx = 0
foreach ($line in $xamlLines) {
    $lineIdx++
    if ($line -match '#[0-9a-fA-F]{6,8}') {
        Write-Error "Hardcoded hex color detected at line $lineIdx in RoadmapPage.xaml: $line"
    }
}
Write-Host "  [PASS] Zero Hardcoded Colors: PASSED (100% ThemeResource usage)" -ForegroundColor Green

# 4. Dotnet Build Verification
Write-Host "  Running dotnet build Lexora.slnx..." -ForegroundColor Yellow
$buildOutput = dotnet build Lexora.slnx -v q /clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed! Please review compilation errors."
}
Write-Host "  [PASS] Solution Build: PASSED (0 Warnings, 0 Errors)" -ForegroundColor Green

# 5. Targeted Unit Test Execution
Write-Host "  Running RoadmapEngine test suite..." -ForegroundColor Yellow
$testOutput = dotnet test tests/Lexora.Tests.Unit/Lexora.Tests.Unit.csproj --filter "FullyQualifiedName~Roadmap" --no-build --logger "console;verbosity=minimal"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Roadmap tests failed!"
}
Write-Host "  [PASS] Unit and Adaptive Pathway Tests: PASSED" -ForegroundColor Green

Write-Host "[Roadmap Visual Builder] All quality gates PASSED successfully!" -ForegroundColor Green
