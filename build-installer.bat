@echo off
setlocal enabledelayedexpansion

title Lexora - Build & Package Installer Pipeline

echo ==============================================================================
echo  [LEXORA] Windows 11 Desktop Application - Packaging Pipeline
echo ==============================================================================
echo.
echo  Starting automated packaging process:
echo   1. Run full unit and integration test suite
echo   2. Publish self-contained WinUI 3 binaries (win-x64)
echo   3. Generate branded wizard banners (Large banner & Small icon)
echo   4. Compile Setup.exe with Inno Setup 6
echo.
echo ==============================================================================
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-installer.ps1" %*
set BUILD_STATUS=%ERRORLEVEL%

echo.
if %BUILD_STATUS% equ 0 (
    echo [OK] Packaging completed successfully!
    echo Setup file is located in: %~dp0publish\installer\
) else (
    echo [ERROR] Packaging failed with exit code %BUILD_STATUS%.
)

echo.
echo Press any key to exit...
pause >nul
exit /b %BUILD_STATUS%
