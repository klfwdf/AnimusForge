@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%package_mod.ps1"
set "PATH_SCRIPT=%SCRIPT_DIR%resolve_bannerlord_paths.ps1"
set "BANNERLORD_ROOT="

for /f "usebackq tokens=1,* delims==" %%A in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%PATH_SCRIPT%"`) do (
    if /I "%%A"=="BANNERLORD_ROOT" set "BANNERLORD_ROOT=%%B"
)

if not exist "%PS_SCRIPT%" (
    echo [ERROR] Script not found: "%PS_SCRIPT%"
    pause
    exit /b 1
)

if not defined BANNERLORD_ROOT (
    echo [ERROR] Bannerlord root could not be auto-detected.
    echo Set BANNERLORD_ROOT to your "Mount & Blade II Bannerlord" folder and retry.
    pause
    exit /b 1
)

echo [AnimusForge] Packaging started...
echo Bannerlord : "%BANNERLORD_ROOT%"
powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" -BannerlordRoot "%BANNERLORD_ROOT%" %*
set "ERR=%ERRORLEVEL%"

if not "%ERR%"=="0" (
    echo [FAILED] Packaging failed. ExitCode=%ERR%
    pause
    exit /b %ERR%
)

echo [SUCCESS] Packaging completed.
pause
exit /b 0
