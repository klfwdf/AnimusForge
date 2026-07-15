@echo off
setlocal EnableExtensions EnableDelayedExpansion

chcp 65001 >nul
set "LANG=en_US.UTF-8"
set "LC_ALL=C"

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
cd /d "%PROJECT_ROOT%"

set "PATH_SCRIPT=%SCRIPT_DIR%resolve_bannerlord_paths.ps1"
set "BUILD_SCRIPT=%SCRIPT_DIR%build_single_module.ps1"
set "CONFIG=Debug"
set "DRY_RUN=0"
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

:parse_args
if "%~1"=="" goto args_done

if /I "%~1"=="--dry-run" (
    set "DRY_RUN=1"
    shift
    goto parse_args
)

echo [ERROR] Unknown argument: %~1
pause
exit /b 1

:args_done

echo [AnimusForge] Pre-work Pull + Build started...
echo Repo      : "%PROJECT_ROOT%"
echo Bannerlord: "%BANNERLORD_ROOT%"
if defined WORKSHOP_CONTENT_DIR echo Workshop  : "%WORKSHOP_CONTENT_DIR%"
echo Config    : "%CONFIG%"
if "%DRY_RUN%"=="1" echo Mode      : DRY RUN
echo.

where git >nul 2>nul
if errorlevel 1 (
    echo [ERROR] git not found in PATH.
    pause
    exit /b 1
)

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet SDK not found in PATH.
    pause
    exit /b 1
)

if not exist "%BUILD_SCRIPT%" (
    echo [ERROR] Unified build script not found:
    echo "%BUILD_SCRIPT%"
    pause
    exit /b 1
)

git rev-parse --is-inside-work-tree >nul 2>nul
if errorlevel 1 (
    echo [ERROR] Current directory is not a git repository:
    echo "%PROJECT_ROOT%"
    pause
    exit /b 1
)

for /f "delims=" %%B in ('git branch --show-current') do set "BRANCH=%%B"
if not defined BRANCH (
    echo [ERROR] Cannot determine current branch.
    pause
    exit /b 1
)
if /I not "%BRANCH%"=="main" (
    echo [ERROR] This unified-module toolchain only allows pulls on branch "main".
    echo Current branch: "%BRANCH%"
    pause
    exit /b 1
)

for /f "delims=" %%U in ('git remote get-url origin 2^>nul') do set "ORIGIN_URL=%%U"
if not defined ORIGIN_URL (
    echo [ERROR] Remote 'origin' not found.
    pause
    exit /b 1
)

if not exist "%BANNERLORD_ROOT%" (
    echo [ERROR] Bannerlord root not found:
    echo "%BANNERLORD_ROOT%"
    pause
    exit /b 1
)

echo Branch : "%BRANCH%"
echo Origin : "%ORIGIN_URL%"
echo.

if "%DRY_RUN%"=="1" (
    echo [Preview] Current changed files:
    git -c core.quotepath=false status --short
    echo.
    echo [Preview] Would run:
    echo   git ls-remote --exit-code origin "refs/heads/%BRANCH%"
    echo   git fetch origin "%BRANCH%"
    echo   if origin/%BRANCH% has new commit^(s^): git rebase --autostash "origin/%BRANCH%"
    echo   sequential isolated builds: implementation 1.3, implementation 1.4, Bootstrap
    echo   powershell -File "%BUILD_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -Configuration "%CONFIG%" -Stage
    echo.
    echo [SUCCESS] Dry-run completed. No build, deploy, commit, or push was run.
    pause
    exit /b 0
)

echo [1/2] Pulling latest origin/%BRANCH%...
git ls-remote --exit-code origin "refs/heads/%BRANCH%" >nul
if errorlevel 1 (
    echo [ERROR] Cannot reach origin/%BRANCH%.
    echo Check network / GitHub access / credentials, then retry.
    pause
    exit /b 1
)

git fetch origin "%BRANCH%"
if errorlevel 1 (
    echo [ERROR] git fetch failed.
    pause
    exit /b 1
)

for /f "delims=" %%A in ('git rev-list --count "origin/%BRANCH%..HEAD"') do set "AHEAD_COUNT=%%A"
for /f "delims=" %%B in ('git rev-list --count "HEAD..origin/%BRANCH%"') do set "BEHIND_COUNT=%%B"
if not defined AHEAD_COUNT set "AHEAD_COUNT=0"
if not defined BEHIND_COUNT set "BEHIND_COUNT=0"

echo Local ahead : %AHEAD_COUNT%
echo Local behind: %BEHIND_COUNT%
echo.

if not "%BEHIND_COUNT%"=="0" (
    echo [INFO] Rebasing local work on origin/%BRANCH% with autostash...
    git rebase --autostash "origin/%BRANCH%"
    if errorlevel 1 (
        echo [ERROR] git rebase failed. Resolve conflicts, then run:
        echo   git rebase --continue
        echo or abort with:
        echo   git rebase --abort
        echo After resolving, rerun this script.
        pause
        exit /b 1
    )
) else (
    echo [INFO] No remote commits to pull.
)

echo.
echo [2/2] Building both implementations and Bootstrap...
if defined WORKSHOP_CONTENT_DIR (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -WorkshopContentDir "%WORKSHOP_CONTENT_DIR%" -Configuration "%CONFIG%" -Stage
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -Configuration "%CONFIG%" -Stage
)
set "ERR=%ERRORLEVEL%"
if not "%ERR%"=="0" (
    echo.
    echo [WARNING] Pull completed, but build failed. ExitCode=%ERR%
    echo [WARNING] This usually means the local Bannerlord/mod dependency setup is incomplete.
    echo [WARNING] The pull/rebase step was already successful, so this script will not fail the pull.
    pause
    exit /b 0
)

echo [SUCCESS] Pre-work Pull + unified Build completed. No deploy, commit, or push was run.
pause
exit /b 0
