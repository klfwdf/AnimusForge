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
set "COMMIT_MSG="
set "GIT_EXCLUDE_PATH_1=AnimusForge/ONNX/reranker"
set "GIT_EXCLUDE_PATH_2=原版游戏本体代码1.3.x"
set "GIT_EXCLUDE_PATH_3=原版游戏本体代码1.4.5"
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

if /I "%~1"=="--msg" (
    if "%~2"=="" (
        echo [ERROR] --msg requires a value.
        pause
        exit /b 1
    )
    set "COMMIT_MSG=%~2"
    shift
    shift
    goto parse_args
)

if not defined COMMIT_MSG (
    set "COMMIT_MSG=%~1"
)

shift
goto parse_args

:args_done

if "%DRY_RUN%"=="1" if not defined COMMIT_MSG (
    set "NOW=%DATE% %TIME%"
    set "COMMIT_MSG=auto: pull+build+push %NOW%"
)

set "COMMIT_MSG_SAFE=%COMMIT_MSG:"='%"
set "BUILD_DEPS_DIR=%PROJECT_ROOT%\bin\%CONFIG%\build_deps"

echo [AnimusForge] Post-work Pull + Build + Push started...
echo Repo      : "%PROJECT_ROOT%"
echo Bannerlord: "%BANNERLORD_ROOT%"
if defined WORKSHOP_CONTENT_DIR echo Workshop  : "%WORKSHOP_CONTENT_DIR%"
echo Config    : "%CONFIG%"
echo Build deps: "%BUILD_DEPS_DIR%"
echo Exclude   : "%GIT_EXCLUDE_PATH_1%"
echo Exclude   : "%GIT_EXCLUDE_PATH_2%"
echo Exclude   : "%GIT_EXCLUDE_PATH_3%"
if "%DUAL_BUILD%"=="1" (
    echo Build API : 1.3 + 1.4
) else (
    echo Build API : 1.3
)
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
    echo [ERROR] This 1.3.x toolchain only allows pushes from branch "main".
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

if not exist "%BUILD_DEPS_SCRIPT%" (
    echo [ERROR] Build dependency preparation script not found:
    echo "%BUILD_DEPS_SCRIPT%"
    pause
    exit /b 1
)

echo Branch : "%BRANCH%"
echo Origin : "%ORIGIN_URL%"
echo.

if not "%DRY_RUN%"=="1" (
    call :EnsureNoStagedChanges
    if errorlevel 1 exit /b 1
)

if "%DRY_RUN%"=="1" (
    echo [Preview] Current changed files:
    git -c core.quotepath=false status --short
    echo.
    echo [Preview] Commit message:
    echo   "%COMMIT_MSG%"
    echo.
    echo [Preview] Would run:
    echo   git ls-remote --exit-code origin "refs/heads/%BRANCH%"
    echo   git fetch origin "%BRANCH%"
    echo   if origin/%BRANCH% has new commit^(s^): git rebase --autostash "origin/%BRANCH%"
    echo   powershell -NoProfile -ExecutionPolicy Bypass -File "%BUILD_DEPS_SCRIPT%" -ProjectRoot "%PROJECT_ROOT%" -BannerlordRoot "%BANNERLORD_ROOT%" -Configuration "%CONFIG%"
    if defined WORKSHOP_CONTENT_DIR (
        echo   dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% /p:BannerlordApi=1.3 /p:BannerlordRoot="%BANNERLORD_ROOT%" /p:WorkshopContentDir="%WORKSHOP_CONTENT_DIR%" /p:AnimusForgeBinDir="%BUILD_DEPS_DIR%"
        if "%DUAL_BUILD%"=="1" echo   dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% /p:BannerlordApi=1.4 /p:BannerlordRoot="%BANNERLORD_ROOT%" /p:WorkshopContentDir="%WORKSHOP_CONTENT_DIR%" /p:AnimusForgeBinDir="%BUILD_DEPS_DIR%"
    ) else (
        echo   dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% /p:BannerlordApi=1.3 /p:BannerlordRoot="%BANNERLORD_ROOT%" /p:AnimusForgeBinDir="%BUILD_DEPS_DIR%"
        if "%DUAL_BUILD%"=="1" echo   dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% /p:BannerlordApi=1.4 /p:BannerlordRoot="%BANNERLORD_ROOT%" /p:AnimusForgeBinDir="%BUILD_DEPS_DIR%"
    )
    echo   git add -A -- . ":(exclude)%GIT_EXCLUDE_PATH_2%/**" ":(exclude)%GIT_EXCLUDE_PATH_3%/**"
    echo   if changes exist: git commit -m "%COMMIT_MSG_SAFE%"
    echo   verify origin/%BRANCH% did not change after build
    echo   git push -u origin "%BRANCH%"
    echo.
    echo [SUCCESS] Dry-run completed. No build, deploy, commit, or push was run.
    pause
    exit /b 0
)

echo [1/4] Pulling latest origin/%BRANCH%...
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
echo [2/5] Preparing local build dependencies...
call :PrepareBuildDeps
if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo [3/5] Building project for Bannerlord 1.3.x...
if defined WORKSHOP_CONTENT_DIR (
    dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% /p:BannerlordApi=1.3 /p:BannerlordRoot="%BANNERLORD_ROOT%" /p:WorkshopContentDir="%WORKSHOP_CONTENT_DIR%" /p:AnimusForgeBinDir="%BUILD_DEPS_DIR%"
) else (
    dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% /p:BannerlordApi=1.3 /p:BannerlordRoot="%BANNERLORD_ROOT%" /p:AnimusForgeBinDir="%BUILD_DEPS_DIR%"
)
set "ERR=%ERRORLEVEL%"
if not "%ERR%"=="0" (
    echo.
    echo [FAILED] 1.3.x build failed. ExitCode=%ERR%
    pause
    exit /b %ERR%
)

if "%DUAL_BUILD%"=="1" (
    echo.
    echo [3/5] Building project for Bannerlord 1.4.5...
    if defined WORKSHOP_CONTENT_DIR (
        dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% /p:BannerlordApi=1.4 /p:BannerlordRoot="%BANNERLORD_ROOT%" /p:WorkshopContentDir="%WORKSHOP_CONTENT_DIR%" /p:AnimusForgeBinDir="%BUILD_DEPS_DIR%"
    ) else (
        dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% /p:BannerlordApi=1.4 /p:BannerlordRoot="%BANNERLORD_ROOT%" /p:AnimusForgeBinDir="%BUILD_DEPS_DIR%"
    )
    set "ERR=%ERRORLEVEL%"
    if not "!ERR!"=="0" (
        echo.
        echo [FAILED] 1.4.5 build failed. ExitCode=!ERR!
        pause
        exit /b !ERR!
    )
)

echo.
echo [4/5] Staging and committing changes...

if not defined COMMIT_MSG (
    set /p "INPUT_MSG=Enter commit note (optional): "
    if defined INPUT_MSG set "COMMIT_MSG=!INPUT_MSG!"
)

if not defined COMMIT_MSG (
    set "NOW=%DATE% %TIME%"
    set "COMMIT_MSG=auto: pull+build+push !NOW!"
)

set "COMMIT_MSG_SAFE=%COMMIT_MSG:"='%"

git rm -r --cached --ignore-unmatch -- "%GIT_EXCLUDE_PATH_1%" >nul 2>nul
if errorlevel 1 (
    echo [ERROR] Failed to exclude Git path:
    echo   "%GIT_EXCLUDE_PATH_1%"
    pause
    exit /b 1
)

git add -A -- . ":(exclude)%GIT_EXCLUDE_PATH_2%/**" ":(exclude)%GIT_EXCLUDE_PATH_3%/**"
if errorlevel 1 (
    echo [ERROR] git add failed.
    pause
    exit /b 1
)

git diff --cached --quiet
if errorlevel 1 (
    git commit -m "%COMMIT_MSG_SAFE%"
    if errorlevel 1 (
        echo [ERROR] git commit failed.
        pause
        exit /b 1
    )
) else (
    echo [INFO] No file changes detected, skipping commit.
)

echo.
echo [5/5] Pushing to origin/%BRANCH%...
git fetch origin "%BRANCH%" >nul
if errorlevel 1 (
    echo [ERROR] Failed to refresh origin/%BRANCH% before push.
    pause
    exit /b 1
)

git merge-base --is-ancestor "origin/%BRANCH%" HEAD
if errorlevel 1 (
    echo [ERROR] origin/%BRANCH% changed after the build.
    echo Rerun this script so it can rebase, rebuild, and then push the final code.
    pause
    exit /b 1
)

for /f "delims=" %%A in ('git rev-list --count "origin/%BRANCH%..HEAD"') do set "AHEAD_COUNT=%%A"
if not defined AHEAD_COUNT set "AHEAD_COUNT=0"
if "%AHEAD_COUNT%"=="0" (
    echo [SUCCESS] Nothing to push. origin/%BRANCH% is already up to date.
    pause
    exit /b 0
)

git push -u origin "%BRANCH%"
if errorlevel 1 (
    echo [ERROR] git push failed.
    pause
    exit /b 1
)

echo [SUCCESS] Post-work Pull + Build + Push completed. No deploy was run.
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

:EnsureNoStagedChanges
git diff --cached --quiet
set "STAGED_ERR=%ERRORLEVEL%"
if "%STAGED_ERR%"=="1" (
    echo [ERROR] There are already staged changes.
    echo This script stages and commits automatically, so clear stale staged changes first:
    echo   git restore --staged .
    pause
    exit /b 1
) else if not "%STAGED_ERR%"=="0" (
    echo [ERROR] git diff --cached failed.
    pause
    exit /b 1
)
exit /b 0
