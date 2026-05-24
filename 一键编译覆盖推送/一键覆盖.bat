@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
cd /d "%SCRIPT_DIR%"
set "DUAL_BUILD_DEPLOY_SCRIPT=%SCRIPT_DIR%一键编译并覆盖.bat"

echo [AnimusForge] One-click deploy redirected to dual-version build + overwrite...
echo Script Dir : "%SCRIPT_DIR%"
echo Project Dir: "%PROJECT_ROOT%"

if not exist "%DUAL_BUILD_DEPLOY_SCRIPT%" (
    echo [ERROR] Dual build deploy script not found:
    echo   "%DUAL_BUILD_DEPLOY_SCRIPT%"
    pause
    exit /b 1
)

echo.
echo [INFO] Single unversioned deploy is disabled.
echo [INFO] Output will only go to:
echo   AnimusForge_1_3_x
echo   AnimusForge_1_4_5
echo.

call "%DUAL_BUILD_DEPLOY_SCRIPT%" --no-launch
if errorlevel 1 (
    echo [ERROR] Dual-version build + overwrite failed.
    pause
    exit /b 1
)

echo [SUCCESS] Dual-version deploy completed.
pause
exit /b 0
