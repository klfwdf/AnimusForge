@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "PACKAGE_SCRIPT=%~dp0一键打包AnimusForge.bat"
if not exist "%PACKAGE_SCRIPT%" (
    echo [ERROR] Base AnimusForge package script was not found.
    pause
    exit /b 1
)

call "%PACKAGE_SCRIPT%" -BumpMicro %*
set "ERR=%ERRORLEVEL%"
endlocal & exit /b %ERR%
