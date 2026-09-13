@echo off
setlocal

title Lexora - Build and Package Installer Pipeline

echo ==============================================================================
echo  [LEXORA] Windows 11 Desktop Application - Packaging Pipeline
echo ==============================================================================
echo.
echo  Starting automated packaging process:
echo   1. Run full unit and integration test suite (64 tests)
echo   2. Publish self-contained WinUI 3 binaries (win-x64)
echo   3. Generate branded wizard banners (Large banner and Small icon)
echo   4. Compile Setup.exe with Inno Setup 6
echo.
echo ==============================================================================
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-installer.ps1" %*
set BUILD_STATUS=%ERRORLEVEL%

echo.
if %BUILD_STATUS% equ 0 (
    echo ==============================================================================
    echo  [SUCCESS] Packaging completed successfully!
    echo  Setup file is located in: %~dp0publish\installer\
    echo ==============================================================================
    echo.
    echo  [INFO] Auto-exiting in 3 seconds... [Press any key to exit immediately]
    timeout /t 3 >nul 2>&1 || ping 127.0.0.1 -n 4 >nul
    exit /b 0
) else (
    echo ==============================================================================
    echo  [ERROR] Packaging failed with exit code %BUILD_STATUS%.
    echo  Please review the logs above for detailed error messages.
    echo ==============================================================================
    echo.
    echo  Press any key to exit...
    pause >nul
    exit /b %BUILD_STATUS%
)
