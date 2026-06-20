@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
cd /d "%SCRIPT_DIR%"

set "DEPLOY_SCRIPT=%SCRIPT_DIR%deploy_module.ps1"
set "PATH_SCRIPT=%SCRIPT_DIR%resolve_bannerlord_paths.ps1"
set "BUILD_DEPS_SCRIPT=%SCRIPT_DIR%prepare_build_deps.ps1"

set "CONFIG=Debug"
set "BANNERLORD_ROOT="
set "WORKSHOP_CONTENT_DIR="
set "LAUNCH_GAME=1"
set "DUAL_BUILD=0"
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

if /I "%~1"=="--dual" (
    set "DUAL_BUILD=1"
    shift
    goto parse_args
)

echo [ERROR] Unknown argument: %~1
echo Usage: "%~nx0" [--no-launch] [--dual]
pause
exit /b 1

:args_done

for /f "usebackq tokens=1,* delims==" %%A in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%PATH_SCRIPT%"`) do (
    if /I "%%A"=="BANNERLORD_ROOT" set "BANNERLORD_ROOT=%%B"
    if /I "%%A"=="WORKSHOP_CONTENT_DIR" set "WORKSHOP_CONTENT_DIR=%%B"
)

set "BUILD_OUTPUT=%PROJECT_ROOT%\bin\%CONFIG%\net472"
set "BUILD_DEPS_DIR=%PROJECT_ROOT%\bin\%CONFIG%\build_deps"
set "ARTIFACT_DIR=%PROJECT_ROOT%\bin\%CONFIG%\dual_client_artifacts"
set "ARTIFACT_13=%ARTIFACT_DIR%\1.3.x\AnimusForge.dll"
set "ARTIFACT_14=%ARTIFACT_DIR%\1.4.5\AnimusForge.dll"

if "%DUAL_BUILD%"=="1" (
    echo [AnimusForge] Dual-version build + overwrite started...
) else (
    echo [AnimusForge] Bannerlord 1.3.x build + overwrite started...
)
echo Script Dir : "%SCRIPT_DIR%"
echo Project Dir: "%PROJECT_ROOT%"
echo Bannerlord : "%BANNERLORD_ROOT%"
if defined WORKSHOP_CONTENT_DIR echo Workshop  : "%WORKSHOP_CONTENT_DIR%"
echo Config     : "%CONFIG%"
if "%DUAL_BUILD%"=="1" (
    echo Build API  : 1.3 + 1.4
) else (
    echo Build API  : 1.3
)
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

if not exist "%DEPLOY_SCRIPT%" (
    echo [ERROR] Deploy script not found:
    echo   "%DEPLOY_SCRIPT%"
    pause
    exit /b 1
)

if not exist "%BUILD_DEPS_SCRIPT%" (
    echo [ERROR] Build dependency preparation script not found:
    echo   "%BUILD_DEPS_SCRIPT%"
    pause
    exit /b 1
)

echo [1/4] Preparing local build dependencies...
if defined WORKSHOP_CONTENT_DIR (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%BUILD_DEPS_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -WorkshopContentDir "%WORKSHOP_CONTENT_DIR%" -Configuration "%CONFIG%"
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%BUILD_DEPS_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -Configuration "%CONFIG%"
)
if errorlevel 1 (
    echo [ERROR] Failed to prepare local build dependencies.
    pause
    exit /b 1
)

echo.
echo [2/4] Building AnimusForge for Bannerlord 1.3.x...
call :BuildVersion 1.3 "%ARTIFACT_13%"
if errorlevel 1 exit /b %ERRORLEVEL%

if not "%DUAL_BUILD%"=="1" (
    echo.
    echo [3/3] Overwriting Bannerlord 1.3.x module...
    powershell -NoProfile -ExecutionPolicy Bypass -File "%DEPLOY_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -Client13Output -BuildDll13 "%ARTIFACT_13%"
    if errorlevel 1 (
        echo [ERROR] 1.3.x module overwrite failed.
        pause
        exit /b 1
    )

    echo.
    echo [SUCCESS] Bannerlord 1.3.x module overwritten:
    echo   "%BANNERLORD_ROOT%\Modules\AnimusForge_1_3_x"
    echo.
    echo Enable AnimusForge_1_3_x for Bannerlord 1.3.x.
    if "%LAUNCH_GAME%"=="1" (
        call :LaunchBannerlord
        if errorlevel 1 (
            echo [WARNING] Bannerlord launch failed. You can start it manually from Steam.
        )
    ) else (
        echo Bannerlord launch skipped by --no-launch.
    )
    pause
    exit /b 0
)

echo.
echo [3/4] Building AnimusForge for Bannerlord 1.4.5...
call :BuildVersion 1.4 "%ARTIFACT_14%"
if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo [4/4] Overwriting dual modules in Bannerlord Modules...
powershell -NoProfile -ExecutionPolicy Bypass -File "%DEPLOY_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -DualClientOutput -BuildDll13 "%ARTIFACT_13%" -BuildDll14 "%ARTIFACT_14%"
if errorlevel 1 (
    echo [ERROR] Dual module overwrite failed.
    pause
    exit /b 1
)

echo.
echo [SUCCESS] Dual-version modules overwritten:
echo   "%BANNERLORD_ROOT%\Modules\AnimusForge_1_3_x"
echo   "%BANNERLORD_ROOT%\Modules\AnimusForge_1_4_5"
echo.
echo Enable only the AnimusForge module matching the current Bannerlord version.
if "%LAUNCH_GAME%"=="1" (
    call :LaunchBannerlord
    if errorlevel 1 (
        echo [WARNING] Bannerlord launch failed. You can start it manually from Steam.
    )
) else (
    echo Bannerlord launch skipped by --no-launch.
)
pause
exit /b 0

:BuildVersion
set "API_VERSION=%~1"
set "TARGET_DLL=%~2"
set "TARGET_DIR=%~dp2"

if defined WORKSHOP_CONTENT_DIR (
    dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% --no-incremental /p:BannerlordApi=%API_VERSION% /p:BannerlordRoot="%BANNERLORD_ROOT%" /p:WorkshopContentDir="%WORKSHOP_CONTENT_DIR%" /p:AnimusForgeBinDir="%BUILD_DEPS_DIR%"
) else (
    dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% --no-incremental /p:BannerlordApi=%API_VERSION% /p:BannerlordRoot="%BANNERLORD_ROOT%" /p:AnimusForgeBinDir="%BUILD_DEPS_DIR%"
)
set "ERR=%ERRORLEVEL%"
if not "%ERR%"=="0" (
    echo.
    echo [FAILED] Build failed for BannerlordApi=%API_VERSION%. ExitCode=%ERR%
    pause
    exit /b %ERR%
)

if not exist "%BUILD_OUTPUT%\AnimusForge.dll" (
    echo [ERROR] Built DLL not found:
    echo   "%BUILD_OUTPUT%\AnimusForge.dll"
    pause
    exit /b 1
)

if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"
copy /Y "%BUILD_OUTPUT%\AnimusForge.dll" "%TARGET_DLL%" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy build artifact:
    echo   "%TARGET_DLL%"
    pause
    exit /b 1
)

if exist "%BUILD_OUTPUT%\AnimusForge.pdb" (
    copy /Y "%BUILD_OUTPUT%\AnimusForge.pdb" "%TARGET_DIR%AnimusForge.pdb" >nul
)

for %%D in (
    0Harmony.dll
    Microsoft.ML.OnnxRuntime.dll
    System.Memory.dll
    System.Buffers.dll
    System.Runtime.CompilerServices.Unsafe.dll
    onnxruntime.dll
    onnxruntime_providers_shared.dll
) do (
    if exist "%BUILD_DEPS_DIR%\%%D" (
        copy /Y "%BUILD_DEPS_DIR%\%%D" "%TARGET_DIR%%%D" >nul
        if errorlevel 1 (
            echo [ERROR] Failed to copy runtime dependency:
            echo   "%BUILD_DEPS_DIR%\%%D"
            pause
            exit /b 1
        )
    )
)

for %%D in (
    0Harmony.dll
    Microsoft.ML.OnnxRuntime.dll
    System.Memory.dll
    System.Buffers.dll
    System.Runtime.CompilerServices.Unsafe.dll
) do (
    if not exist "%TARGET_DIR%%%D" (
        echo [ERROR] Captured artifact is missing runtime dependency:
        echo   "%TARGET_DIR%%%D"
        pause
        exit /b 1
    )
)

echo [OK] Captured BannerlordApi=%API_VERSION% artifact:
echo      "%TARGET_DLL%"
echo [OK] Captured runtime dependencies:
echo      "%TARGET_DIR%"
exit /b 0

:LaunchBannerlord
echo.
echo Launching Bannerlord via Steam...
for /f "usebackq delims=" %%S in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$paths=@(); $keys=@('HKCU:\\Software\\Valve\\Steam','HKLM:\\SOFTWARE\\WOW6432Node\\Valve\\Steam','HKLM:\\SOFTWARE\\Valve\\Steam'); foreach($k in $keys){ try { $v=(Get-ItemProperty -Path $k -ErrorAction Stop).SteamExe; if($v){ $paths += [Environment]::ExpandEnvironmentVariables($v) } } catch {} }; $paths += 'C:\\Program Files (x86)\\Steam\\steam.exe'; $paths += 'C:\\Program Files\\Steam\\steam.exe'; $paths | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1"`) do set "STEAM_EXE=%%S"

tasklist /FI "IMAGENAME eq Bannerlord.exe" 2>nul | find /I "Bannerlord.exe" >nul
if not errorlevel 1 (
    echo [INFO] Bannerlord is already running. Skip launch.
    exit /b 0
)

if defined STEAM_EXE (
    start "" "%STEAM_EXE%" -applaunch %STEAM_GAME_ID%
    if errorlevel 1 (
        echo [ERROR] Failed to launch Bannerlord via Steam:
        echo   "%STEAM_EXE%" -applaunch %STEAM_GAME_ID%
        exit /b 1
    )
    echo [OK] Launched via Steam:
    echo   "%STEAM_EXE%" -applaunch %STEAM_GAME_ID%
    exit /b 0
)

start "" "steam://rungameid/%STEAM_GAME_ID%"
if errorlevel 1 (
    echo [ERROR] Failed to launch Steam URL:
    echo   steam://rungameid/%STEAM_GAME_ID%
    exit /b 1
)
echo [OK] Launched via Steam URL:
echo   steam://rungameid/%STEAM_GAME_ID%
exit /b 0
