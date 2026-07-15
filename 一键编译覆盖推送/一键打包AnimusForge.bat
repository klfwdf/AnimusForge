@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
set "PATH_SCRIPT=%SCRIPT_DIR%resolve_bannerlord_paths.ps1"
set "BUILD_SCRIPT=%SCRIPT_DIR%build_single_module.ps1"
set "PACKAGE_SCRIPT=%SCRIPT_DIR%package_mod.ps1"
set "CONFIG=Debug"
set "STAGE_MODULE=%PROJECT_ROOT%\bin\%CONFIG%\single_module_stage\AnimusForge"
set "BANNERLORD_ROOT="
set "WORKSHOP_CONTENT_DIR="

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

for %%F in ("%BUILD_SCRIPT%" "%PACKAGE_SCRIPT%") do (
    if not exist "%%~fF" (
        echo [ERROR] Required script not found:
        echo   "%%~fF"
        pause
        exit /b 1
    )
)

echo [AnimusForge] Unified module packaging started...
echo Project   : "%PROJECT_ROOT%"
echo Bannerlord: "%BANNERLORD_ROOT%"
if defined WORKSHOP_CONTENT_DIR echo Workshop  : "%WORKSHOP_CONTENT_DIR%"
echo Config    : "%CONFIG%"
echo.

echo [1/2] Building and assembling a project-local unified module...
if defined WORKSHOP_CONTENT_DIR (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -WorkshopContentDir "%WORKSHOP_CONTENT_DIR%" -Configuration "%CONFIG%" -Stage
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -Configuration "%CONFIG%" -Stage
)
set "ERR=%ERRORLEVEL%"
if not "%ERR%"=="0" (
    echo [FAILED] Unified module build/output failed. ExitCode=%ERR%
    pause
    exit /b %ERR%
)

echo.
echo [2/2] Creating one AnimusForge ZIP...
powershell -NoProfile -ExecutionPolicy Bypass -File "%PACKAGE_SCRIPT%" -ModuleDir "%STAGE_MODULE%" -SourceModuleDir "%PROJECT_ROOT%\AnimusForge" -ExcludeOnnx -ExcludeCustomPrompts %*
set "ERR=%ERRORLEVEL%"
if not "%ERR%"=="0" (
    echo [FAILED] Packaging failed. ExitCode=%ERR%
    pause
    exit /b %ERR%
)

echo.
echo [SUCCESS] One unified AnimusForge package was generated.
echo The game Modules directory was not modified.
pause
exit /b 0
