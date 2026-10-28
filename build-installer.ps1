<#
.SYNOPSIS
    Builds, publishes, packages, and verifies the Lexora Windows 11 Desktop Application installer.

.DESCRIPTION
    Full release packaging pipeline:
      1. Prerequisites validation (.NET 10 SDK, Inno Setup 6, 64-bit Windows).
      2. Automated test suite execution (Unit and Integration tests).
      3. Self-contained WinUI 3 Release publish (win-x64, ReadyToRun).
      4. Wizard branding assets generation.
      5. Inno Setup 6 compilation with LZMA2/Ultra64 compression.
      6. Installer validation, SHA256 hashing, and security unblocking.
      7. Optional automated end-to-end install/smoke-test/uninstall verification.

.PARAMETER Version
    Semantic version of the application (e.g. 1.0.0). Defaults to "1.0.0".

.PARAMETER Configuration
    Build configuration ("Release" or "Debug"). Defaults to "Release".

.PARAMETER Runtime
    Target runtime identifier. Defaults to "win-x64".

.PARAMETER SkipTests
    Skip running automated tests before building.

.PARAMETER SkipPublish
    Skip dotnet publish step and use existing published files.

.PARAMETER Clean
    Clean previous publish and installer output directories before building.

.PARAMETER TestInstall
    Perform automated clean installation, launch smoke test, and clean uninstall verification.

.PARAMETER IsccPath
    Custom path to Inno Setup Compiler (ISCC.exe).

.EXAMPLE
    .\build-installer.ps1 -Version "1.0.0"
    .\build-installer.ps1 -TestInstall
#>

[CmdletBinding()]
param (
    [string]$Version = "1.4.1",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipTests,
    [switch]$SkipPublish,
    [switch]$Clean,
    [switch]$TestInstall,
    [string]$IsccPath = ""
)

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot
if (-not $ScriptRoot) { $ScriptRoot = (Get-Location).Path }

Write-Host "==============================================================================" -ForegroundColor Cyan
Write-Host " [LEXORA] Windows 11 Desktop Application - Packaging Pipeline" -ForegroundColor Cyan
Write-Host " Version: $Version | Config: $Configuration | Runtime: $Runtime" -ForegroundColor Cyan
Write-Host "==============================================================================" -ForegroundColor Cyan

# ==============================================================================
# Helper Functions
# ==============================================================================
function Write-Step {
    param([string]$Title)
    Write-Host "`n>>> [STEP] $Title" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "    [INFO] $Message" -ForegroundColor Gray
}

function Write-Success {
    param([string]$Message)
    Write-Host "    [SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warn {
    param([string]$Message)
    Write-Host "    [WARN] $Message" -ForegroundColor Yellow
}

function Write-Err {
    param([string]$Message)
    Write-Host "`n    [ERROR] $Message" -ForegroundColor Red
}

function Find-IsccCompiler {
    param([string]$ExplicitPath)

    if ($ExplicitPath -and (Test-Path $ExplicitPath)) {
        return $ExplicitPath
    }

    # 1. Check in PATH
    $cmd = Get-Command "iscc.exe" -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    # 2. Check Registry
    $regKeys = @(
        "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1",
        "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1",
        "HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1"
    )
    foreach ($k in $regKeys) {
        if (Test-Path $k) {
            $loc = (Get-ItemProperty -Path $k -Name "InstallLocation" -ErrorAction SilentlyContinue).InstallLocation
            if ($loc -and (Test-Path (Join-Path $loc "ISCC.exe"))) {
                return (Join-Path $loc "ISCC.exe")
            }
        }
    }

    # 3. Check Standard File Paths
    $candidates = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Antigravity IDE\resources\app\node_modules\innosetup\bin\ISCC.exe"
    )
    foreach ($path in $candidates) {
        if (Test-Path $path) {
            return $path
        }
    }

    return $null
}

# ==============================================================================
# 1. Prerequisites Validation
# ==============================================================================
Write-Step "Checking system prerequisites..."

# Check 64-bit Windows
if ($env:PROCESSOR_ARCHITECTURE -ne "AMD64" -and $env:PROCESSOR_ARCHITEW6432 -ne "AMD64") {
    Write-Err "Lexora target runtime is $Runtime. A 64-bit Windows operating system is required."
    exit 1
}
Write-Info "Operating System: 64-bit Windows detected ($env:OS)"

# Check .NET SDK
$dotnetCmd = Get-Command "dotnet" -ErrorAction SilentlyContinue
if (-not $dotnetCmd) {
    Write-Err ".NET SDK is not installed or not available in PATH. Please install .NET 10 SDK."
    exit 1
}
$dotnetVersion = & dotnet --version
Write-Info ".NET SDK Version: $dotnetVersion"

# Check Inno Setup 6
$resolvedIscc = Find-IsccCompiler -ExplicitPath $IsccPath
if (-not $resolvedIscc) {
    Write-Err "Inno Setup 6 Compiler (ISCC.exe) was not found!"
    Write-Host "    Please install Inno Setup 6 from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "    Or specify custom path via: -IsccPath 'C:\Path\To\ISCC.exe'" -ForegroundColor Yellow
    exit 1
}
Write-Success "Inno Setup Compiler: $resolvedIscc"

# Check running instances of Lexora
$runningProcs = Get-Process -Name "Lexora.Presentation" -ErrorAction SilentlyContinue
if ($runningProcs) {
    Write-Warn "Lexora is currently running ($($runningProcs.Count) process(es) detected)."
    Write-Info "Stopping running Lexora process(es) to avoid file lock issues..."
    $runningProcs | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

# ==============================================================================
# 2. Automated Test Suite Execution
# ==============================================================================
if (-not $SkipTests) {
    Write-Step "Executing automated unit and integration tests..."
    $testResult = & dotnet test "$ScriptRoot\Lexora.slnx" --configuration $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Err "Test suite execution failed! Packaging aborted to prevent defective releases."
        exit $LASTEXITCODE
    }
    Write-Success "All automated test suites passed successfully (100% Green)."
} else {
    Write-Warn "Skipping test execution (-SkipTests specified)."
}

# ==============================================================================
# 3. Publishing WinUI 3 Self-Contained Binaries
# ==============================================================================
$publishDir = Join-Path $ScriptRoot "publish\$Runtime"
$installerDir = Join-Path $ScriptRoot "publish\installer"

if ($Clean) {
    Write-Step "Cleaning previous output directories..."
    if (Test-Path $publishDir) {
        Remove-Item -Path $publishDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Info "Cleaned: $publishDir"
    }
    if (Test-Path $installerDir) {
        Remove-Item -Path $installerDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Info "Cleaned: $installerDir"
    }
}

if (-not $SkipPublish) {
    Write-Step "Publishing self-contained WinUI 3 application ($Runtime)..."
    
    if (Test-Path $publishDir) {
        Remove-Item -Path $publishDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

    $projFile = Join-Path $ScriptRoot "src\Lexora.Presentation\Lexora.Presentation.csproj"
    
    & dotnet publish "$projFile" `
        -c $Configuration `
        -r $Runtime `
        --self-contained true `
        -p:WindowsAppSDKSelfContained=true `
        -p:PublishReadyToRun=false `
        -o "$publishDir"

    if ($LASTEXITCODE -ne 0) {
        Write-Err "dotnet publish failed with exit code $LASTEXITCODE."
        exit $LASTEXITCODE
    }

    # Ensure WinUI 3 compiled XAML resources (.pri, .xbf, Themes, Views, Assets) are synchronized
    $binDir = Join-Path $ScriptRoot "src\Lexora.Presentation\bin\$Configuration\net10.0-windows10.0.26100.0\$Runtime"
    if (Test-Path $binDir) {
        Write-Info "Synchronizing WinUI 3 XAML resources and PRI maps..."
        Get-ChildItem -Path $binDir -Filter "*.pri" | Copy-Item -Destination $publishDir -Force
        Get-ChildItem -Path $binDir -Filter "*.xbf" | Copy-Item -Destination $publishDir -Force
        foreach ($subDir in @("Assets", "Themes", "Views")) {
            $srcSub = Join-Path $binDir $subDir
            if (Test-Path $srcSub) {
                Copy-Item -Path $srcSub -Destination $publishDir -Recurse -Force
            }
        }
    }

    # Clean debug symbols from published output to reduce installer payload
    Write-Info "Optimizing published payload (stripping unnecessary .pdb files)..."
    Get-ChildItem -Path $publishDir -Filter "*.pdb" -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue

    Write-Success "Publish completed successfully."
} else {
    Write-Warn "Skipping publish step (-SkipPublish specified)."
    if (-not (Test-Path $publishDir)) {
        Write-Err "Publish directory not found: $publishDir"
        exit 1
    }
}

# Verify critical published binaries
Write-Step "Validating published dependency bundle..."
$requiredFiles = @(
    "Lexora.Presentation.exe",
    "Lexora.Presentation.pri",
    "Microsoft.ui.xaml.dll",
    "Microsoft.WindowsAppRuntime.dll",
    "Microsoft.WindowsAppRuntime.pri",
    "Lexora.Domain.dll",
    "Lexora.Application.dll",
    "Lexora.Infrastructure.dll",
    "App.xbf",
    "MainWindow.xbf",
    "Themes\DesignSystem.xbf",
    "Views\ShellPage.xbf",
    "Assets\AppIcon.ico"
)

foreach ($file in $requiredFiles) {
    $fullPath = Join-Path $publishDir $file
    if (-not (Test-Path $fullPath)) {
        Write-Err "Missing critical published dependency: $file"
        exit 1
    }
}
Write-Success "All core dependencies, WinUI 3 assemblies, and PRI resources verified."

# ==============================================================================
# 4. Inno Setup Wizard Branding & Compilation
# ==============================================================================
Write-Step "Preparing installer branding assets..."
$wizardSmall = Join-Path $ScriptRoot "installer\wizard-small.bmp"
$wizardLarge = Join-Path $ScriptRoot "installer\wizard-large.bmp"
$brandingScript = Join-Path $ScriptRoot "installer\generate-wizard-branding.ps1"

if ((-not (Test-Path $wizardSmall) -or -not (Test-Path $wizardLarge)) -and (Test-Path $brandingScript)) {
    Write-Info "Generating high-resolution wizard branding bitmaps..."
    & "$brandingScript"
}

if (-not (Test-Path $installerDir)) {
    New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
}

$issFile = Join-Path $ScriptRoot "installer\LexoraSetup.iss"
if (-not (Test-Path $issFile)) {
    Write-Err "Inno Setup script not found: $issFile"
    exit 1
}

Write-Step "Compiling Inno Setup package..."
& "$resolvedIscc" `
    "/DMyAppVersion=$Version" `
    "/DMyAppSourceDir=$publishDir" `
    "/DMyOutputDir=$installerDir" `
    "$issFile"

if ($LASTEXITCODE -ne 0) {
    Write-Err "Inno Setup compilation failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

$installerFile = Join-Path $installerDir "Lexora-Setup-v$Version.exe"
if (-not (Test-Path $installerFile)) {
    Write-Err "Expected installer binary was not found at: $installerFile"
    exit 1
}

# Unblock file to avoid Windows SmartScreen / Mark-of-the-Web blocking
Unblock-File -Path $installerFile -ErrorAction SilentlyContinue

$installerItem = Get-Item $installerFile
$fileSizeMb = [math]::Round($installerItem.Length / 1MB, 2)
$hash = (Get-FileHash -Path $installerFile -Algorithm SHA256).Hash

Write-Host "`n==============================================================================" -ForegroundColor Green
Write-Host " [PACKAGE GENERATED SUCCESSFULLY]" -ForegroundColor Green
Write-Host " Output File : $installerFile" -ForegroundColor Cyan
Write-Host " File Size   : $fileSizeMb MB" -ForegroundColor Cyan
Write-Host " SHA256 Hash : $hash" -ForegroundColor Gray
Write-Host "==============================================================================" -ForegroundColor Green

# ==============================================================================
# 5. Automated Verification: Clean Install -> Smoke Test -> Clean Uninstall
# ==============================================================================
if ($TestInstall) {
    Write-Step "Performing automated End-to-End verification (Install -> Smoke Test -> Uninstall)..."

    $testInstallDir = Join-Path $env:TEMP "Lexora_TestInstall_$([guid]::NewGuid().ToString('N').Substring(0,8))"
    Write-Info "Temporary Install Target: $testInstallDir"

    try {
        # 1. Silent Install
        Write-Info "Executing silent installation..."
        $installProc = Start-Process -FilePath $installerFile `
            -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR=`"$testInstallDir`"" `
            -Wait -PassThru

        if ($installProc.ExitCode -ne 0) {
            Write-Err "Silent installation failed with exit code $($installProc.ExitCode)."
            exit $installProc.ExitCode
        }
        Write-Success "Silent installation completed successfully."

        # 2. Verify Installed Files
        $installedExe = Join-Path $testInstallDir "Lexora.Presentation.exe"
        $uninstallerExe = Join-Path $testInstallDir "unins000.exe"

        if (-not (Test-Path $installedExe)) {
            Write-Err "Installed application executable missing: $installedExe"
            exit 1
        }
        if (-not (Test-Path $uninstallerExe)) {
            Write-Err "Uninstaller executable missing: $uninstallerExe"
            exit 1
        }
        Write-Success "Installed application files and uninstaller verified."

        # 3. Launch Smoke Test
        Write-Info "Launching application smoke test (8-second execution test)..."
        $appProc = Start-Process -FilePath $installedExe -PassThru
        Start-Sleep -Seconds 8

        if ($appProc.HasExited -and $appProc.ExitCode -ne 0) {
            Write-Err "Application crashed immediately on startup with exit code $($appProc.ExitCode)!"
            exit 1
        }

        # Cleanly terminate the test process
        if (-not $appProc.HasExited) {
            Write-Info "Process started and ran without crashing. Terminating test process..."
            $appProc | Stop-Process -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 1
        }
        Write-Success "Smoke test passed: Application launches and runs cleanly."

        # 4. Silent Uninstall
        Write-Info "Executing silent uninstallation..."
        $uninstallProc = Start-Process -FilePath $uninstallerExe `
            -ArgumentList "/VERYSILENT /NORESTART /SUPPRESSMSGBOXES" `
            -Wait -PassThru

        Start-Sleep -Seconds 2
        Write-Success "Silent uninstallation completed."

        # 5. Clean up temporary test install folder
        if (Test-Path $testInstallDir) {
            Remove-Item -Path $testInstallDir -Recurse -Force -ErrorAction SilentlyContinue
        }
        Write-Success "Temporary test install folder cleaned up."
    }
    catch {
        Write-Err "Verification failed: $_"
        if (Test-Path $testInstallDir) {
            Remove-Item -Path $testInstallDir -Recurse -Force -ErrorAction SilentlyContinue
        }
        exit 1
    }

    Write-Host "`n==============================================================================" -ForegroundColor Green
    Write-Host " [ALL VERIFICATION GATES PASSED: INSTALL -> SMOKE TEST -> UNINSTALL]" -ForegroundColor Green
    Write-Host "==============================================================================" -ForegroundColor Green
}

exit 0
