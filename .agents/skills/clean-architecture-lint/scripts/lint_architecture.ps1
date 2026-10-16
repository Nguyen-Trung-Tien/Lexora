[CmdletBinding()]
param (
    [switch]$JsonOutput
)

$ErrorActionPreference = "Stop"
$root = (Get-Location).Path

$violations = @()

# 1. Inspect Domain Layer (Must be 100% pure)
$domainCsFiles = Get-ChildItem -Path "src/Lexora.Domain" -Filter "*.cs" -Recurse -File | 
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

foreach ($f in $domainCsFiles) {
    $lines = Get-Content $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '^\s*using\s+(Microsoft\.EntityFrameworkCore|Lexora\.Application|Lexora\.Infrastructure|Lexora\.Presentation|Microsoft\.UI\.Xaml)') {
            $violations += [PSCustomObject]@{
                Layer    = "Lexora.Domain"
                File     = $f.FullName.Replace("$root\", "")
                Line     = $i + 1
                Rule     = "Domain purity violation: Domain cannot reference other layers or external frameworks."
                Snippet  = $line.Trim()
            }
        }
    }
}

# 2. Inspect Application Layer (Must reference ONLY Domain)
$appCsFiles = Get-ChildItem -Path "src/Lexora.Application" -Filter "*.cs" -Recurse -File | 
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

foreach ($f in $appCsFiles) {
    $lines = Get-Content $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '^\s*using\s+(Microsoft\.EntityFrameworkCore|Lexora\.Infrastructure|Lexora\.Presentation|Microsoft\.UI\.Xaml)') {
            $violations += [PSCustomObject]@{
                Layer    = "Lexora.Application"
                File     = $f.FullName.Replace("$root\", "")
                Line     = $i + 1
                Rule     = "Application boundary violation: Application cannot reference Infrastructure, Presentation, or EF Core."
                Snippet  = $line.Trim()
            }
        }
    }
}

# 3. Inspect Presentation Layer (Views & ViewModels must NEVER touch LexoraDbContext or EF Core)
$presFiles = Get-ChildItem -Path "src/Lexora.Presentation" -Filter "*.cs" -Recurse -File | 
    Where-Object { 
        $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and
        ($_.FullName -match '[\\/](Views|ViewModels)[\\/]')
    }

foreach ($f in $presFiles) {
    $lines = Get-Content $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '\bLexoraDbContext\b' -or $line -match '^\s*using\s+Microsoft\.EntityFrameworkCore') {
            $violations += [PSCustomObject]@{
                Layer    = "Lexora.Presentation"
                File     = $f.FullName.Replace("$root\", "")
                Line     = $i + 1
                Rule     = "Absolute Presentation Boundary: Views and ViewModels must NEVER reference LexoraDbContext or EF Core directly."
                Snippet  = $line.Trim()
            }
        }
    }
}

$summary = [PSCustomObject]@{
    Timestamp       = (Get-Date).ToString("o")
    ViolationsCount = $violations.Count
    Status          = if ($violations.Count -eq 0) { "PASSED" } else { "FAILED" }
    Violations      = $violations
}

if ($JsonOutput) {
    $summary | ConvertTo-Json -Depth 4
} else {
    Write-Host "=== Lexora Clean Architecture Boundary Lint Report ===" -ForegroundColor Cyan
    Write-Host "Total Boundary Violations: $($violations.Count)"
    
    if ($violations.Count -gt 0) {
        Write-Host "`nViolations detected:" -ForegroundColor Red
        foreach ($v in $violations) {
            Write-Host "[$($v.Layer)] $($v.File):$($v.Line)" -ForegroundColor Yellow
            Write-Host "  $($v.Rule)"
            Write-Host "  Snippet: $($v.Snippet)`n" -ForegroundColor Gray
        }
        exit 1
    } else {
        Write-Host "`nResult: CLEAN ARCHITECTURE INVARIANTS 100% PRESERVED!" -ForegroundColor Green
        exit 0
    }
}
