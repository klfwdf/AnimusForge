@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "SCRIPT_DIR=%~dp0"
set "BUILD_DEPLOY_SCRIPT=%SCRIPT_DIR%一键编译并覆盖.bat"

echo [AnimusForge] Safe overwrite uses a fresh Bootstrap plus both implementation builds.
echo Output: Modules\AnimusForge
echo.

if not exist "%BUILD_DEPLOY_SCRIPT%" (
    echo [ERROR] Build/deploy script not found:
    echo   "%BUILD_DEPLOY_SCRIPT%"
    pause
    exit /b 1
)

call "%BUILD_DEPLOY_SCRIPT%" --no-launch %*
set "ERR=%ERRORLEVEL%"
if not "%ERR%"=="0" (
    echo [ERROR] Unified module overwrite failed.
    pause
    exit /b %ERR%
)

echo [SUCCESS] Unified module overwrite completed without launching the game.
pause
exit /b 0
