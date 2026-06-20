@echo off
setlocal EnableExtensions EnableDelayedExpansion

chcp 65001 >nul
set "LANG=en_US.UTF-8"
set "LC_ALL=C"

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
cd /d "%PROJECT_ROOT%"

set "PATH_SCRIPT=%SCRIPT_DIR%resolve_bannerlord_paths.ps1"
set "BUILD_DEPS_SCRIPT=%SCRIPT_DIR%prepare_build_deps.ps1"
set "CONFIG=Debug"
set "DRY_RUN=0"
set "DUAL_BUILD=0"
set "GIT_REQUIRED=0"
set "HAS_GIT=0"
set "HAS_GIT_REPO=0"
set "HAS_GIT_REMOTE=0"
set "BANNERLORD_ROOT="
set "WORKSHOP_CONTENT_DIR="

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

if /I "%~1"=="--dual" (
    set "DUAL_BUILD=1"
    shift
    goto parse_args
)

if /I "%~1"=="--git-required" (
    set "GIT_REQUIRED=1"
    shift
    goto parse_args
)

echo [ERROR] Unknown argument: %~1
echo Usage: "%~nx0" [--dry-run] [--dual] [--git-required]
pause
exit /b 1

:args_done
set "BUILD_DEPS_DIR=%PROJECT_ROOT%\bin\%CONFIG%\build_deps"

echo [AnimusForge] Pre-work Pull + Build started...
echo Repo      : "%PROJECT_ROOT%"
echo Bannerlord: "%BANNERLORD_ROOT%"
if defined WORKSHOP_CONTENT_DIR echo Workshop  : "%WORKSHOP_CONTENT_DIR%"
echo Config    : "%CONFIG%"
echo Build deps: "%BUILD_DEPS_DIR%"
if "%DUAL_BUILD%"=="1" (
    echo Build API : 1.3 + 1.4
) else (
    echo Build API : 1.3
)
if "%DRY_RUN%"=="1" echo Mode      : DRY RUN
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet SDK not found in PATH.
    pause
    exit /b 1
)

where git >nul 2>nul
if not errorlevel 1 (
    set "HAS_GIT=1"
    git rev-parse --is-inside-work-tree >nul 2>nul
    if not errorlevel 1 set "HAS_GIT_REPO=1"
)

if "%HAS_GIT_REPO%"=="0" (
    if "%GIT_REQUIRED%"=="1" (
        echo [ERROR] Current directory is not a git repository:
        echo "%PROJECT_ROOT%"
        pause
        exit /b 1
    )
    if "%HAS_GIT%"=="0" (
        echo [WARNING] git not found in PATH. Pull step will be skipped.
    ) else (
        echo [WARNING] No .git metadata found for this working copy. Pull step will be skipped.
    )
    echo.
)

if not exist "%BANNERLORD_ROOT%" (
    echo [ERROR] Bannerlord root not found:
    echo "%BANNERLORD_ROOT%"
    pause
    exit /b 1
)

if not exist "%BUILD_DEPS_SCRIPT%" (
    echo [ERROR] Build dependency preparation script not found:
    echo "%BUILD_DEPS_SCRIPT%"
    pause
    exit /b 1
)

if "%HAS_GIT_REPO%"=="1" (
    call :LoadGitContext
    if errorlevel 1 exit /b %ERRORLEVEL%
)

if "%DRY_RUN%"=="1" (
    if "%HAS_GIT_REPO%"=="1" (
        echo [Preview] Current changed files:
        git -c core.quotepath=false status --short
        echo.
        if "%HAS_GIT_REMOTE%"=="1" (
            echo [Preview] Would run:
            echo   git ls-remote --exit-code origin "refs/heads/%BRANCH%"
            echo   git fetch origin "%BRANCH%"
            echo   if origin/%BRANCH% has new commit^(s^): git rebase --autostash "origin/%BRANCH%"
        ) else (
            echo [Preview] Git pull would be skipped because remote 'origin' is not configured.
        )
    ) else (
        echo [Preview] Git pull would be skipped because this is not a git working tree.
    )
    echo [Preview] Would prepare build dependencies:
    echo   powershell -NoProfile -ExecutionPolicy Bypass -File "%BUILD_DEPS_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -Configuration "%CONFIG%"
    call :PrintBuildPreview 1.3
    if "%DUAL_BUILD%"=="1" call :PrintBuildPreview 1.4
    echo.
    echo [SUCCESS] Dry-run completed. No pull or build was run.
    pause
    exit /b 0
)

if "%HAS_GIT_REPO%"=="1" if "%HAS_GIT_REMOTE%"=="1" (
    echo [1/2] Pulling latest origin/%BRANCH%...
    call :PullLatest
    if errorlevel 1 exit /b %ERRORLEVEL%
) else (
    echo [1/2] Pull skipped.
)

echo.
echo [2/3] Preparing local build dependencies...
call :PrepareBuildDeps
if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo [3/3] Building project for Bannerlord 1.3.x...
call :BuildApi 1.3
if errorlevel 1 exit /b %ERRORLEVEL%

if "%DUAL_BUILD%"=="1" (
    echo.
    echo [3/3] Building project for Bannerlord 1.4.5...
    call :BuildApi 1.4
    if errorlevel 1 exit /b %ERRORLEVEL%
)

echo.
echo [SUCCESS] Pre-work Pull + Build completed. No deploy, commit, or push was run.
pause
exit /b 0

:LoadGitContext
for /f "delims=" %%B in ('git branch --show-current') do set "BRANCH=%%B"
if not defined BRANCH (
    echo [ERROR] Cannot determine current branch.
    pause
    exit /b 1
)
if /I not "%BRANCH%"=="main" (
    echo [ERROR] This 1.3.x toolchain only allows pulls on branch "main".
    echo Current branch: "%BRANCH%"
    pause
    exit /b 1
)

for /f "delims=" %%U in ('git remote get-url origin 2^>nul') do set "ORIGIN_URL=%%U"
if not defined ORIGIN_URL (
    echo [WARNING] Remote 'origin' not found. Pull step will be skipped.
    echo.
    exit /b 0
)
set "HAS_GIT_REMOTE=1"

echo Branch : "%BRANCH%"
echo Origin : "%ORIGIN_URL%"
echo.
exit /b 0

:PullLatest
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
exit /b 0

:PrintBuildPreview
set "API_VERSION=%~1"
if defined WORKSHOP_CONTENT_DIR (
    echo   dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% /p:BannerlordApi=%API_VERSION% /p:BannerlordRoot="%BANNERLORD_ROOT%" /p:WorkshopContentDir="%WORKSHOP_CONTENT_DIR%" /p:AnimusForgeBinDir="%BUILD_DEPS_DIR%"
) else (
    echo   dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% /p:BannerlordApi=%API_VERSION% /p:BannerlordRoot="%BANNERLORD_ROOT%" /p:AnimusForgeBinDir="%BUILD_DEPS_DIR%"
)
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

:BuildApi
set "API_VERSION=%~1"
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
exit /b 0
