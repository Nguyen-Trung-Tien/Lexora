@echo off
setlocal enabledelayedexpansion

title Lexora - Windows 11 Desktop Application Installer Pipeline

echo ==============================================================================
echo  [LEXORA] Windows 11 Desktop Application - Enterprise Packaging Pipeline
echo ==============================================================================
echo.
echo  Automated Packaging Workflow:
echo   [1/6] Prerequisites validation (.NET 10 SDK, Inno Setup 6, 64-bit Windows)
echo   [2/6] Automated test suite execution (Unit and Integration tests)
echo   [3/6] Self-contained WinUI 3 Release publish (win-x64, ReadyToRun)
echo   [4/6] Payload optimization (stripping debug symbols, minimizing payload)
echo   [5/6] Inno Setup 6 compilation (LZMA2/Ultra64 solid compression)
echo   [6/6] Security unblocking (Zone.Identifier cleanup) and checksum verification
echo.
echo ==============================================================================
echo.

:: Locate PowerShell executable (prefers pwsh.exe if available, fallback to powershell.exe)
set "PS_EXE="
where.exe pwsh.exe >nul 2>&1
if !ERRORLEVEL! equ 0 (
    set "PS_EXE=pwsh.exe"
) else (
    where.exe powershell.exe >nul 2>&1
    if !ERRORLEVEL! equ 0 (
        set "PS_EXE=powershell.exe"
    )
)

if not defined PS_EXE (
    echo [ERROR] Neither pwsh.exe nor powershell.exe was found in system PATH.
    echo Please ensure Windows PowerShell or PowerShell 7 is installed.
    exit /b 1
)

:: Verify build-installer.ps1 exists
if not exist "%~dp0build-installer.ps1" (
    echo [ERROR] Packaging automation script missing: "%~dp0build-installer.ps1"
    exit /b 1
)

:: Execute the PowerShell packaging pipeline with all supplied arguments
"!PS_EXE!" -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-installer.ps1" %*
set "BUILD_STATUS=!ERRORLEVEL!"

echo.
if !BUILD_STATUS! equ 0 (
    echo ==============================================================================
    echo  [SUCCESS] Installer packaging completed successfully!
    echo  Setup executable: %~dp0publish\installer\
    echo ==============================================================================
    echo.
    if "%CI%"=="" (
        echo  [INFO] Auto-exiting in 5 seconds... (Press any key to exit immediately)
        timeout /t 5 >nul 2>&1 || ping 127.0.0.1 -n 6 >nul
    )
    exit /b 0
) else (
    echo ==============================================================================
    echo  [ERROR] Packaging failed with exit code !BUILD_STATUS!.
    echo  Please review the detailed logs above for troubleshooting.
    echo ==============================================================================
    echo.
    if "%CI%"=="" (
        echo  Press any key to exit...
        pause >nul
    )
    exit /b !BUILD_STATUS!
)
