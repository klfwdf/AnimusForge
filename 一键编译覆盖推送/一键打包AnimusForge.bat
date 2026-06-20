@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 936 >nul

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
cd /d "%SCRIPT_DIR%"

set "CONFIG=Debug"
set "PS_SCRIPT=%SCRIPT_DIR%package_mod.ps1"
set "PATH_SCRIPT=%SCRIPT_DIR%resolve_bannerlord_paths.ps1"
set "DEPLOY_SCRIPT=%SCRIPT_DIR%deploy_module.ps1"
set "BUILD_DEPS_SCRIPT=%SCRIPT_DIR%prepare_build_deps.ps1"
set "BANNERLORD_ROOT="
set "WORKSHOP_CONTENT_DIR="

for /f "usebackq tokens=1,* delims==" %%A in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%PATH_SCRIPT%"`) do (
    if /I "%%A"=="BANNERLORD_ROOT" set "BANNERLORD_ROOT=%%B"
    if /I "%%A"=="WORKSHOP_CONTENT_DIR" set "WORKSHOP_CONTENT_DIR=%%B"
)

set "BUILD_OUTPUT=%PROJECT_ROOT%\bin\%CONFIG%\net472"
set "BUILD_DEPS_DIR=%PROJECT_ROOT%\bin\%CONFIG%\build_deps"
set "ARTIFACT_DIR=%PROJECT_ROOT%\bin\%CONFIG%\dual_client_artifacts"
set "ARTIFACT_13=%ARTIFACT_DIR%\1.3.x\AnimusForge.dll"
set "ARTIFACT_14=%ARTIFACT_DIR%\1.4.5\AnimusForge.dll"

if not exist "%PS_SCRIPT%" (
    echo [ERROR] Package script not found:
    echo "%PS_SCRIPT%"
    pause
    exit /b 1
)

if not exist "%DEPLOY_SCRIPT%" (
    echo [ERROR] Deploy script not found:
    echo "%DEPLOY_SCRIPT%"
    pause
    exit /b 1
)

if not exist "%BUILD_DEPS_SCRIPT%" (
    echo [ERROR] Build dependency preparation script not found:
    echo "%BUILD_DEPS_SCRIPT%"
    pause
    exit /b 1
)

if not defined BANNERLORD_ROOT (
    echo [ERROR] Bannerlord root could not be auto-detected.
    echo Set BANNERLORD_ROOT to your "Mount & Blade II Bannerlord" folder and retry.
    pause
    exit /b 1
)

if not exist "%BANNERLORD_ROOT%" (
    echo [ERROR] Bannerlord root not found:
    echo "%BANNERLORD_ROOT%"
    pause
    exit /b 1
)

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet SDK not found in PATH.
    pause
    exit /b 1
)

echo [AnimusForge] Dual-version packaging started...
echo Script Dir : "%SCRIPT_DIR%"
echo Project Dir: "%PROJECT_ROOT%"
echo Bannerlord : "%BANNERLORD_ROOT%"
if defined WORKSHOP_CONTENT_DIR echo Workshop  : "%WORKSHOP_CONTENT_DIR%"
echo Config     : "%CONFIG%"
echo Build deps : "%BUILD_DEPS_DIR%"
echo.

echo [1/5] Preparing local build dependencies...
call :PrepareBuildDeps
if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo [2/5] Building AnimusForge for Bannerlord 1.3.x...
call :BuildVersion 1.3 "%ARTIFACT_13%"
if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo [3/5] Building AnimusForge for Bannerlord 1.4.5...
call :BuildVersion 1.4 "%ARTIFACT_14%"
if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo [4/5] Writing dual modules to Bannerlord Modules...
powershell -NoProfile -ExecutionPolicy Bypass -File "%DEPLOY_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -DualClientOutput -BuildDll13 "%ARTIFACT_13%" -BuildDll14 "%ARTIFACT_14%"
if errorlevel 1 (
    echo [ERROR] Dual module output failed.
    pause
    exit /b 1
)

echo.
echo [5/5] Packaging dual client modules...
powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" -BannerlordRoot "%BANNERLORD_ROOT%" -SourceModuleDir "%PROJECT_ROOT%\AnimusForge" -DualClientPackages -ExcludeOnnx %*
set "ERR=%ERRORLEVEL%"

if not "%ERR%"=="0" (
    echo [FAILED] Packaging failed. ExitCode=%ERR%
    pause
    exit /b %ERR%
)

echo.
echo [SUCCESS] Dual-version packages generated.
pause
exit /b 0

:PrepareBuildDeps
if defined WORKSHOP_CONTENT_DIR (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%BUILD_DEPS_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -WorkshopContentDir "%WORKSHOP_CONTENT_DIR%" -Configuration "%CONFIG%"
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%BUILD_DEPS_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -Configuration "%CONFIG%"
)
set "ERR=%ERRORLEVEL%"
if not "%ERR%"=="0" (
    echo [ERROR] Failed to prepare local build dependencies. ExitCode=%ERR%
    pause
    exit /b %ERR%
)
exit /b 0

:BuildVersion
set "API_VERSION=%~1"
set "TARGET_DLL=%~2"
set "TARGET_DIR=%~dp2"

if defined WORKSHOP_CONTENT_DIR (
    dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% /p:BannerlordApi=%API_VERSION% /p:BannerlordRoot="%BANNERLORD_ROOT%" /p:WorkshopContentDir="%WORKSHOP_CONTENT_DIR%" /p:AnimusForgeBinDir="%BUILD_DEPS_DIR%"
) else (
    dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% /p:BannerlordApi=%API_VERSION% /p:BannerlordRoot="%BANNERLORD_ROOT%" /p:AnimusForgeBinDir="%BUILD_DEPS_DIR%"
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
