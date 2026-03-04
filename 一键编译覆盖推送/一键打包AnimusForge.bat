@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%package_mod.ps1"

if not exist "%PS_SCRIPT%" (
    echo [ERROR] Script not found: "%PS_SCRIPT%"
    pause
    exit /b 1
)

echo [AnimusForge] Packaging started...
powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" %*
set "ERR=%ERRORLEVEL%"

if not "%ERR%"=="0" (
    echo [FAILED] Packaging failed. ExitCode=%ERR%
    pause
    exit /b %ERR%
)

echo [SUCCESS] Packaging completed.
pause
exit /b 0
