@echo off
setlocal EnableExtensions

set "PACKAGE_SCRIPT="
for %%F in ("%~dp0*AnimusForge.bat") do (
    if exist "%%~fF" set "PACKAGE_SCRIPT=%%~fF"
)

if not defined PACKAGE_SCRIPT (
    echo [ERROR] Base AnimusForge package script was not found.
    pause
    exit /b 1
)

call "%PACKAGE_SCRIPT%" -BumpMicro %*
set "ERR=%ERRORLEVEL%"
endlocal & exit /b %ERR%
