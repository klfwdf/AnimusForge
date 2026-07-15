@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
set "PATH_SCRIPT=%SCRIPT_DIR%resolve_bannerlord_paths.ps1"
set "BUILD_SCRIPT=%SCRIPT_DIR%build_single_module.ps1"
set "CONFIG=Debug"
set "BANNERLORD_ROOT="
set "WORKSHOP_CONTENT_DIR="
set "LAUNCH_GAME=1"
set "STEAM_EXE="
set "STEAM_GAME_ID=261550"

:parse_args
if "%~1"=="" goto args_done
if /I "%~1"=="--no-launch" (
    set "LAUNCH_GAME=0"
    shift
    goto parse_args
)
if /I "%~1"=="/no-launch" (
    set "LAUNCH_GAME=0"
    shift
    goto parse_args
)
echo [ERROR] Unknown argument: %~1
pause
exit /b 1

:args_done
if exist "%LOCALAPPDATA%\Microsoft\dotnet\sdk" (
    set "DOTNET_ROOT=%LOCALAPPDATA%\Microsoft\dotnet"
    set "PATH=%LOCALAPPDATA%\Microsoft\dotnet;%PATH%"
)

for /f "usebackq tokens=1,* delims==" %%A in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%PATH_SCRIPT%"`) do (
    if /I "%%A"=="BANNERLORD_ROOT" set "BANNERLORD_ROOT=%%B"
    if /I "%%A"=="WORKSHOP_CONTENT_DIR" set "WORKSHOP_CONTENT_DIR=%%B"
)

if not defined BANNERLORD_ROOT (
    echo [ERROR] Bannerlord root could not be detected.
    pause
    exit /b 1
)
if not exist "%BANNERLORD_ROOT%\Modules" (
    echo [ERROR] Bannerlord Modules directory not found:
    echo   "%BANNERLORD_ROOT%\Modules"
    pause
    exit /b 1
)

if not exist "%BUILD_SCRIPT%" (
    echo [ERROR] Unified build script not found:
    echo   "%BUILD_SCRIPT%"
    pause
    exit /b 1
)

echo [AnimusForge] Unified module build and overwrite started...
echo Project   : "%PROJECT_ROOT%"
echo Bannerlord: "%BANNERLORD_ROOT%"
if defined WORKSHOP_CONTENT_DIR echo Workshop  : "%WORKSHOP_CONTENT_DIR%"
echo Config    : "%CONFIG%"
echo.

if defined WORKSHOP_CONTENT_DIR (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -WorkshopContentDir "%WORKSHOP_CONTENT_DIR%" -Configuration "%CONFIG%" -Deploy
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -Configuration "%CONFIG%" -Deploy
)
set "ERR=%ERRORLEVEL%"
if not "%ERR%"=="0" (
    echo.
    echo [FAILED] Unified module build/overwrite failed. ExitCode=%ERR%
    pause
    exit /b %ERR%
)

echo.
echo [SUCCESS] Unified module overwritten:
echo   "%BANNERLORD_ROOT%\Modules\AnimusForge"
if "%LAUNCH_GAME%"=="1" (
    call :LaunchBannerlord
    if errorlevel 1 echo [WARNING] Bannerlord launch failed. Start it manually from Steam.
) else (
    echo Bannerlord launch skipped by --no-launch.
)
pause
exit /b 0

:LaunchBannerlord
echo.
echo Launching Bannerlord via Steam...
for /f "usebackq delims=" %%S in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$paths=@(); $keys=@('HKCU:\Software\Valve\Steam','HKLM:\SOFTWARE\WOW6432Node\Valve\Steam','HKLM:\SOFTWARE\Valve\Steam'); foreach($k in $keys){ try { $v=(Get-ItemProperty -Path $k -ErrorAction Stop).SteamExe; if($v){ $paths += [Environment]::ExpandEnvironmentVariables($v) } } catch {} }; $paths += 'C:\Program Files (x86)\Steam\steam.exe'; $paths += 'C:\Program Files\Steam\steam.exe'; $paths | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1"`) do set "STEAM_EXE=%%S"

tasklist /FI "IMAGENAME eq Bannerlord.exe" 2>nul | find /I "Bannerlord.exe" >nul
if not errorlevel 1 (
    echo [INFO] Bannerlord is already running. Skipping launch.
    exit /b 0
)

if defined STEAM_EXE (
    start "" "%STEAM_EXE%" -applaunch %STEAM_GAME_ID%
    if errorlevel 1 (
        echo [ERROR] Failed to launch Bannerlord via Steam.
        exit /b 1
    )
    echo [OK] Launched via Steam.
    exit /b 0
)

start "" "steam://rungameid/%STEAM_GAME_ID%"
if errorlevel 1 (
    echo [ERROR] Failed to launch the Steam URL.
    exit /b 1
)
echo [OK] Launched via Steam URL.
exit /b 0
