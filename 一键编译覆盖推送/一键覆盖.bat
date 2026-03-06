@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
cd /d "%SCRIPT_DIR%"
set "DEPLOY_SCRIPT=%SCRIPT_DIR%deploy_module.ps1"

set "SRC_DEBUG=%PROJECT_ROOT%\bin\Debug\net472\AnimusForge.dll"
set "SRC_RELEASE=%PROJECT_ROOT%\bin\Release\net472\AnimusForge.dll"
set "SRC_DLL="

if exist "%SRC_DEBUG%" set "SRC_DLL=%SRC_DEBUG%"
if not defined SRC_DLL if exist "%SRC_RELEASE%" set "SRC_DLL=%SRC_RELEASE%"

echo [AnimusForge] One-click deploy started...
echo Script Dir : "%SCRIPT_DIR%"
echo Project Dir: "%PROJECT_ROOT%"

if not defined SRC_DLL (
    echo [ERROR] Source DLL not found.
    echo Tried:
    echo   "%SRC_DEBUG%"
    echo   "%SRC_RELEASE%"
    pause
    exit /b 1
)

if not exist "%DEPLOY_SCRIPT%" (
    echo [ERROR] Deploy script not found:
    echo   "%DEPLOY_SCRIPT%"
    pause
    exit /b 1
)

echo Source DLL : "%SRC_DLL%"
echo.

echo [1/1] Deploying full AnimusForge module...
powershell -NoProfile -ExecutionPolicy Bypass -File "%DEPLOY_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BuildDll "%SRC_DLL%"
if errorlevel 1 (
    echo [ERROR] Deploy failed.
    pause
    exit /b 1
)

echo [SUCCESS] Deploy completed.
pause
exit /b 0
