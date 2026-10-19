[CmdletBinding()]
param (
    [ValidateSet("Added", "Changed", "Deprecated", "Removed", "Fixed", "Security")]
    [string]$Type = "Added",
    [string]$Description = "",
    [string]$PrOrIssue = "",
    [switch]$Consolidate,
    [string]$ReleaseVersion = ""
)

$ErrorActionPreference = "Stop"
$unreleasedDir = "changelog/unreleased"
$archiveDir = "changelog/archive"
$changelogFile = "CHANGELOG.md"

if (-not (Test-Path $unreleasedDir)) {
    New-Item -ItemType Directory -Path $unreleasedDir -Force | Out-Null
}
if (-not (Test-Path $archiveDir)) {
    New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null
}

# Mode 1: Consolidate unreleased fragments into CHANGELOG.md
if ($Consolidate) {
    if (-not $ReleaseVersion) {
        Write-Error "Please specify -ReleaseVersion (e.g. '1.1.0') when consolidating changelog."
        exit 1
    }

    $fragments = Get-ChildItem -Path $unreleasedDir -Filter "*.md" -File | Where-Object { $_.Name -ne ".gitkeep" -and $_.Name -ne "fragment-template.md" }
    if ($fragments.Count -eq 0) {
        Write-Host "No pending changelog fragments to consolidate in $unreleasedDir." -ForegroundColor Yellow
        exit 0
    }

    Write-Host "Consolidating $($fragments.Count) changelog fragments into release [$ReleaseVersion]..." -ForegroundColor Cyan

    $categories = @{
        "Added"      = [System.Collections.Generic.List[string]]::new()
        "Changed"    = [System.Collections.Generic.List[string]]::new()
        "Deprecated" = [System.Collections.Generic.List[string]]::new()
        "Removed"    = [System.Collections.Generic.List[string]]::new()
        "Fixed"      = [System.Collections.Generic.List[string]]::new()
        "Security"   = [System.Collections.Generic.List[string]]::new()
    }

    foreach ($frag in $fragments) {
        $content = Get-Content $frag.FullName
        $currentCat = ""
        foreach ($line in $content) {
            if ($line -match '^###\s+(Added|Changed|Deprecated|Removed|Fixed|Security)') {
                $currentCat = $matches[1]
                continue
            }
            if ($currentCat -and $line.Trim().StartsWith("- ")) {
                $categories[$currentCat].Add($line.Trim())
            }
        }
    }

    $releaseDate = (Get-Date).ToString("yyyy-MM-dd")
    $block = "`n## [$ReleaseVersion] - $releaseDate`n"

    foreach ($cat in @("Added", "Changed", "Deprecated", "Removed", "Fixed", "Security")) {
        if ($categories[$cat].Count -gt 0) {
            $block += "`n### $cat`n"
            foreach ($item in $categories[$cat]) {
                $block += "$item`n"
            }
        }
    }

    # Insert into CHANGELOG.md
    if (Test-Path $changelogFile) {
        $chLines = Get-Content $changelogFile -Raw
        if ($chLines -match '(##\s+\[Unreleased\][\r\n]+)') {
            $chLines = $chLines -replace '(##\s+\[Unreleased\][\r\n]+)', "`$1$block`n"
            Set-Content -Path $changelogFile -Value $chLines.Trim() -Encoding UTF8
            Write-Host "Updated $changelogFile with release $ReleaseVersion." -ForegroundColor Green
        } else {
            Add-Content -Path $changelogFile -Value $block -Encoding UTF8
        }
    }

    # Archive fragments
    $releaseArchiveSubdir = Join-Path $archiveDir "v$ReleaseVersion"
    New-Item -ItemType Directory -Path $releaseArchiveSubdir -Force | Out-Null
    foreach ($frag in $fragments) {
        Move-Item -Path $frag.FullName -Destination $releaseArchiveSubdir -Force
    }

    Write-Host "Consolidation complete! Fragments archived to $releaseArchiveSubdir." -ForegroundColor Green
    exit 0
}

# Mode 2: Create a new unreleased fragment
if (-not $Description) {
    Write-Error "Please specify -Description (e.g. 'Add smart fuzzy search algorithm for vocabulary terms')."
    exit 1
}

$slug = ($Description.ToLower() -replace '[^a-z0-9]+', '-').Trim('-')
if ($slug.Length -gt 40) {
    $slug = $slug.Substring(0, 40).Trim('-')
}
$timestamp = (Get-Date).ToString("yyyyMMdd-HHmmss")
$fragFileName = "$($Type.ToLower())-$timestamp-$slug.md"
$fragFilePath = Join-Path $unreleasedDir $fragFileName

$refString = if ($PrOrIssue) { " ($PrOrIssue)" } else { "" }
$fragContent = @"
### $Type
- $Description$refString
"@

Set-Content -Path $fragFilePath -Value $fragContent.Trim() -Encoding UTF8
Write-Host "Created changelog fragment at: $fragFilePath" -ForegroundColor Green
exit 0
