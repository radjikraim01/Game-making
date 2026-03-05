@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%publish_release.ps1"

if not exist "%PS_SCRIPT%" (
    echo [ERROR] Missing script: "%PS_SCRIPT%"
    pause
    exit /b 1
)

echo Running release packaging...
powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" %*
if errorlevel 1 (
    echo.
    echo [ERROR] Packaging failed.
    pause
    exit /b 1
)

echo.
echo [OK] Packaging completed.
echo Output folder: "%SCRIPT_DIR%dist\DungeonEscape-win-x64"
echo Output zip:    "%SCRIPT_DIR%dist\DungeonEscape-win-x64.zip"
pause
exit /b 0
