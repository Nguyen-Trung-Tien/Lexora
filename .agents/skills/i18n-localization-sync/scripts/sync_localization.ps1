[CmdletBinding()]
param (
    [switch]$CheckXaml,
    [switch]$JsonOutput
)

$ErrorActionPreference = "Stop"
$locServiceFile = "src/Lexora.Presentation/Services/LocalizationService.cs"

if (-not (Test-Path $locServiceFile)) {
    Write-Error "LocalizationService.cs not found at '$locServiceFile'"
    exit 1
}

$lines = Get-Content $locServiceFile

$viKeys = [System.Collections.Generic.HashSet[string]]::new()
$enKeys = [System.Collections.Generic.HashSet[string]]::new()

$currentLang = ""

foreach ($line in $lines) {
    if ($line -match '^\s*\["vi-VN"\]\s*=') {
        $currentLang = "vi"
        continue
    }
    if ($line -match '^\s*\["en-US"\]\s*=') {
        $currentLang = "en"
        continue
    }
    if ($line -match '^\s*\},') {
        $currentLang = ""
        continue
    }

    if ($line -match '^\s*\["([a-zA-Z0-9_]+)"\]\s*=') {
        $key = $matches[1]
        if ($currentLang -eq "vi") {
            [void]$viKeys.Add($key)
        } elseif ($currentLang -eq "en") {
            [void]$enKeys.Add($key)
        }
    }
}

$missingInEn = @($viKeys | Where-Object { -not $enKeys.Contains($_) })
$missingInVi = @($enKeys | Where-Object { -not $viKeys.Contains($_) })

# Optional: Check XAML key usages
$missingInService = @()
if ($CheckXaml) {
    $presentationPath = "src/Lexora.Presentation"
    $xamlFiles = Get-ChildItem -Path $presentationPath -Filter "*.xaml" -Recurse -File | 
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

    foreach ($file in $xamlFiles) {
        $xLines = Get-Content $file.FullName
        for ($i = 0; $i -lt $xLines.Count; $i++) {
            $xLine = $xLines[$i]
            # Match Loc['Key'] or Loc.GetString('Key')
            if ($xLine -match "(?:Loc\['|Loc\.GetString\(')([a-zA-Z0-9_]+)'") {
                $usedKey = $matches[1]
                if (-not $viKeys.Contains($usedKey) -and -not $enKeys.Contains($usedKey)) {
                    $missingInService += [PSCustomObject]@{
                        Key     = $usedKey
                        File    = $file.FullName.Replace((Get-Location).Path + "\", "")
                        Line    = $i + 1
                        Snippet = $xLine.Trim()
                    }
                }
            }
        }
    }
}

$totalKeys = [Math]::Max($viKeys.Count, $enKeys.Count)
$isParityOk = ($missingInEn.Count -eq 0) -and ($missingInVi.Count -eq 0) -and ($missingInService.Count -eq 0)

$report = [PSCustomObject]@{
    Timestamp          = (Get-Date).ToString("o")
    TotalViKeys        = $viKeys.Count
    TotalEnKeys        = $enKeys.Count
    MissingInEnglish   = $missingInEn
    MissingInVietnamese= $missingInVi
    MissingInService   = $missingInService
    Status             = if ($isParityOk) { "PASSED" } else { "FAILED" }
}

if ($JsonOutput) {
    $report | ConvertTo-Json -Depth 4
} else {
    Write-Host "=== Lexora i18n Localization Parity Report ===" -ForegroundColor Cyan
    Write-Host "Total Keys in vi-VN: $($viKeys.Count)"
    Write-Host "Total Keys in en-US: $($enKeys.Count)"

    if ($missingInEn.Count -gt 0) {
        Write-Host "`nKeys present in vi-VN but MISSING in en-US ($($missingInEn.Count)):" -ForegroundColor Red
        foreach ($k in $missingInEn) {
            Write-Host "  - $k" -ForegroundColor Yellow
        }
    }

    if ($missingInVi.Count -gt 0) {
        Write-Host "`nKeys present in en-US but MISSING in vi-VN ($($missingInVi.Count)):" -ForegroundColor Red
        foreach ($k in $missingInVi) {
            Write-Host "  - $k" -ForegroundColor Yellow
        }
    }

    if ($missingInService.Count -gt 0) {
        Write-Host "`nKeys referenced in XAML but MISSING in LocalizationService ($($missingInService.Count)):" -ForegroundColor Red
        foreach ($m in $missingInService) {
            Write-Host "  [$($m.Key)] at $($m.File):$($m.Line)" -ForegroundColor Yellow
        }
    }

    if ($isParityOk) {
        Write-Host "`nResult: PERFECT LOCALIZATION PARITY (100% synchronized)!" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "`nResult: LOCALIZATION PARITY FAILED. Please synchronize keys." -ForegroundColor Red
        exit 1
    }
}
