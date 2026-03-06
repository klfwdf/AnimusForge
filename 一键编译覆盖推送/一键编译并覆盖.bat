@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
cd /d "%SCRIPT_DIR%"
set "DEPLOY_SCRIPT=%SCRIPT_DIR%deploy_module.ps1"

set "BANNERLORD_ROOT=F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord"
set "CONFIG=Debug"
set "STEAM_EXE="
set "STEAM_GAME_ID=261550"

set "SRC_DEBUG=%PROJECT_ROOT%\bin\Debug\net472\AnimusForge.dll"
set "SRC_RELEASE=%PROJECT_ROOT%\bin\Release\net472\AnimusForge.dll"
set "SRC_DLL="

echo [AnimusForge] Build + Deploy + Launch started...
echo Script Dir : "%SCRIPT_DIR%"
echo Project Dir: "%PROJECT_ROOT%"
echo Bannerlord : "%BANNERLORD_ROOT%"
echo Config     : "%CONFIG%"
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet SDK not found in PATH.
    pause
    exit /b 1
)

if not exist "%BANNERLORD_ROOT%" (
    echo [ERROR] Bannerlord root not found:
    echo "%BANNERLORD_ROOT%"
    pause
    exit /b 1
)

echo [1/3] Build...
dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% /p:BannerlordRoot="%BANNERLORD_ROOT%"
set "ERR=%ERRORLEVEL%"
if not "%ERR%"=="0" (
    echo.
    echo [FAILED] Build step failed. ExitCode=%ERR%
    pause
    exit /b %ERR%
)

if exist "%SRC_DEBUG%" set "SRC_DLL=%SRC_DEBUG%"
if not defined SRC_DLL if exist "%SRC_RELEASE%" set "SRC_DLL=%SRC_RELEASE%"

if not defined SRC_DLL (
    echo [ERROR] Built DLL not found.
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

echo.
echo [2/3] Deploy...
echo Source DLL : "%SRC_DLL%"
powershell -NoProfile -ExecutionPolicy Bypass -File "%DEPLOY_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BuildDll "%SRC_DLL%" -BannerlordRoot "%BANNERLORD_ROOT%"
if errorlevel 1 (
    echo [ERROR] Deploy failed.
    pause
    exit /b 1
)

echo.
echo [3/3] Launch via Steam...
for /f "usebackq delims=" %%S in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$paths=@(); $keys=@('HKCU:\\Software\\Valve\\Steam','HKLM:\\SOFTWARE\\WOW6432Node\\Valve\\Steam','HKLM:\\SOFTWARE\\Valve\\Steam'); foreach($k in $keys){ try { $v=(Get-ItemProperty -Path $k -ErrorAction Stop).SteamExe; if($v){ $paths += [Environment]::ExpandEnvironmentVariables($v) } } catch {} }; $paths += 'C:\\Program Files (x86)\\Steam\\steam.exe'; $paths += 'C:\\Program Files\\Steam\\steam.exe'; $paths | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1"`) do set "STEAM_EXE=%%S"

tasklist /FI "IMAGENAME eq Bannerlord.exe" 2>nul | find /I "Bannerlord.exe" >nul
if not errorlevel 1 (
    echo [INFO] Bannerlord is already running. Skip launch.
    echo [SUCCESS] Build + Deploy completed.
    pause
    exit /b 0
)

if defined STEAM_EXE (
    start "" "%STEAM_EXE%" -applaunch %STEAM_GAME_ID%
    if errorlevel 1 (
        echo [ERROR] Failed to launch Bannerlord via Steam:
        echo   "%STEAM_EXE%" -applaunch %STEAM_GAME_ID%
        pause
        exit /b 1
    )
    echo [OK] Launched via Steam:
    echo   "%STEAM_EXE%" -applaunch %STEAM_GAME_ID%
) else (
    start "" "steam://rungameid/%STEAM_GAME_ID%"
    if errorlevel 1 (
        echo [ERROR] Failed to launch Steam URL:
        echo   steam://rungameid/%STEAM_GAME_ID%
        pause
        exit /b 1
    )
    echo [OK] Launched via Steam URL:
    echo   steam://rungameid/%STEAM_GAME_ID%
)

echo [SUCCESS] Build + Deploy + Launch completed.
pause
exit /b 0
