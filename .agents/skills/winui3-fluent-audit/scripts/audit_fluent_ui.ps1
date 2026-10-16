[CmdletBinding()]
param (
    [string]$TargetFile = "",
    [switch]$JsonOutput
)

$ErrorActionPreference = "Stop"
$presentationPath = "src/Lexora.Presentation"

$violations = @()

$xamlFiles = @()
$csFiles = @()

if ($TargetFile -and (Test-Path $TargetFile)) {
    if ($TargetFile.EndsWith(".xaml", [System.StringComparison]::OrdinalIgnoreCase)) {
        $xamlFiles += (Get-Item $TargetFile)
    } elseif ($TargetFile.EndsWith(".cs", [System.StringComparison]::OrdinalIgnoreCase)) {
        $csFiles += (Get-Item $TargetFile)
    }
} else {
    if (Test-Path $presentationPath) {
        # Exclude generated code and design system resource dictionaries where base palette colors are defined
        $xamlFiles = Get-ChildItem -Path $presentationPath -Filter "*.xaml" -Recurse -File | 
            Where-Object { 
                $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and 
                $_.Name -notin @("DesignSystem.xaml", "Generic.xaml") 
            }
        $csFiles = Get-ChildItem -Path $presentationPath -Filter "*.cs" -Recurse -File | 
            Where-Object { 
                $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and 
                $_.Name -notin @("ThemeResourceHelper.cs", "SafeDialogExtensions.cs") 
            }
    }
}

# 1. Audit XAML Files
foreach ($file in $xamlFiles) {
    $content = Get-Content $file.FullName -Raw
    $lines = Get-Content $file.FullName

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $lineNum = $i + 1

        # Skip comment lines
        $trimmed = $line.Trim()
        if ($trimmed.StartsWith("<!--") -and $trimmed.EndsWith("-->")) {
            continue
        }

        # Check for hardcoded hex colors
        if ($line -match '(?i)(?:Background|Foreground|BorderBrush|Fill|Stroke|Color)\s*=\s*["''](#[0-9A-Fa-f]{3,8})["'']') {
            $hex = $matches[1]
            $violations += [PSCustomObject]@{
                Type     = "HardcodedHexColor"
                File     = $file.FullName.Replace((Get-Location).Path + "\", "")
                Line     = $lineNum
                Detail   = "Found hardcoded hex color '$hex'. Must use Fluent {ThemeResource ...} brush."
                Snippet  = $line.Trim()
            }
        }

        # Check for rigid fixed widths (>= 600px)
        if ($line -match '(?i)\bWidth\s*=\s*["'']([6-9]\d{2}|\d{4,})["'']') {
            $widthVal = $matches[1]
            $violations += [PSCustomObject]@{
                Type     = "RigidFixedWidth"
                File     = $file.FullName.Replace((Get-Location).Path + "\", "")
                Line     = $lineNum
                Detail   = "Found rigid fixed width '$widthVal'. Use MaxWidth with HorizontalAlignment='Stretch' to prevent layout clipping."
                Snippet  = $line.Trim()
            }
        }
    }
}

# 2. Audit C# Code-Behind & ViewModels
foreach ($file in $csFiles) {
    $lines = Get-Content $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $lineNum = $i + 1

        # Check for Color.FromArgb with raw hex numbers or hardcoded SolidColorBrush
        if ($line -match '(?i)new\s+SolidColorBrush\s*\(\s*Color\.FromArgb' -or $line -match '(?i)ColorHelper\.FromArgb') {
            $violations += [PSCustomObject]@{
                Type     = "HardcodedColorInCode"
                File     = $file.FullName.Replace((Get-Location).Path + "\", "")
                Line     = $lineNum
                Detail   = "Found programmatic hardcoded Color creation. Retrieve theme brushes from App.Current.Resources or XAML ThemeResource."
                Snippet  = $line.Trim()
            }
        }

        # Check for ShowAsync on ContentDialog without XamlRoot mentioned in nearby scope
        if ($line -match '(?i)\.ShowAsync\(\)' -and $line -notmatch 'Task\.Yield') {
            # Check previous 10 lines for XamlRoot
            $hasXamlRoot = $false
            $start = [Math]::Max(0, $i - 10)
            for ($k = $start; $k -le $i; $k++) {
                if ($lines[$k] -match '(?i)XamlRoot') {
                    $hasXamlRoot = $true
                    break
                }
            }
            if (-not $hasXamlRoot) {
                $violations += [PSCustomObject]@{
                    Type     = "MissingXamlRootOnDialog"
                    File     = $file.FullName.Replace((Get-Location).Path + "\", "")
                    Line     = $lineNum
                    Detail   = "ContentDialog ShowAsync() call detected without explicit XamlRoot assignment in the preceding block."
                    Snippet  = $line.Trim()
                }
            }
        }
    }
}

$summary = [PSCustomObject]@{
    Timestamp        = (Get-Date).ToString("o")
    FilesScanned     = $xamlFiles.Count + $csFiles.Count
    ViolationsCount  = $violations.Count
    Status           = if ($violations.Count -eq 0) { "PASSED" } else { "FAILED" }
    Violations       = $violations
}

if ($JsonOutput) {
    $summary | ConvertTo-Json -Depth 5
} else {
    Write-Host "=== WinUI 3 & Fluent 2 Audit Report ===" -ForegroundColor Cyan
    Write-Host "Files Scanned: $($summary.FilesScanned) ($($xamlFiles.Count) XAML, $($csFiles.Count) C#)"
    Write-Host "Total Violations: $($summary.ViolationsCount)"
    
    if ($violations.Count -gt 0) {
        Write-Host "`nViolations detected:" -ForegroundColor Red
        foreach ($v in $violations) {
            Write-Host "[$($v.Type)] $($v.File):$($v.Line)" -ForegroundColor Yellow
            Write-Host "  Detail: $($v.Detail)"
            Write-Host "  Snippet: $($v.Snippet)`n" -ForegroundColor Gray
        }
        exit 1
    } else {
        Write-Host "`nResult: ZERO VIOLATIONS. Code strictly complies with Fluent 2 guidelines!" -ForegroundColor Green
        exit 0
    }
}
