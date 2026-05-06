@echo off
setlocal EnableExtensions EnableDelayedExpansion

chcp 65001 >nul
set "LANG=en_US.UTF-8"
set "LC_ALL=C"

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
cd /d "%PROJECT_ROOT%"

set "DRY_RUN=0"
set "COMMIT_MSG="
set "GIT_EXCLUDE_PATH=AnimusForge/ONNX/reranker"

:parse_args
if "%~1"=="" goto args_done

if /I "%~1"=="--dry-run" (
    set "DRY_RUN=1"
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

where git >nul 2>nul
if errorlevel 1 (
    echo [ERROR] git not found in PATH.
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

if not defined COMMIT_MSG (
    set /p "INPUT_MSG=Enter commit note (optional): "
    if defined INPUT_MSG set "COMMIT_MSG=!INPUT_MSG!"
)

if not defined COMMIT_MSG (
    set "NOW=%DATE% %TIME%"
    set "COMMIT_MSG=auto: update %NOW%"
)

set "COMMIT_MSG_SAFE=%COMMIT_MSG:"='%"


echo [Git] One-click push started...
echo Repo   : "%PROJECT_ROOT%"
echo Branch : "%BRANCH%"
echo Origin : "%ORIGIN_URL%"
echo Exclude: "%GIT_EXCLUDE_PATH%"
if "%DRY_RUN%"=="1" echo Mode   : DRY RUN (no add / no commit / no push)
echo.

if "%DRY_RUN%"=="1" (
    echo [Preview] Current changed files:
    git -c core.quotepath=false status --short
    echo.
    echo [Preview] Commit message:
    echo   "%COMMIT_MSG%"
    echo.
    echo [Preview] Would run:
    echo   git rm -r --cached --ignore-unmatch -- "%GIT_EXCLUDE_PATH%"
    echo   git add -A
    echo   if changes exist: git commit -m "%COMMIT_MSG_SAFE%"
    echo   if no changes   : skip commit
    echo   if origin/%BRANCH% is ahead: git rebase "origin/%BRANCH%"
    echo   git push -u origin "%BRANCH%"
    echo.
    echo [SUCCESS] Dry-run completed.
    pause
    exit /b 0
)

echo [Preflight] Checking origin/%BRANCH% connectivity...
git ls-remote --exit-code origin "refs/heads/%BRANCH%" >nul
if errorlevel 1 (
    echo [ERROR] Cannot reach origin/%BRANCH%. No commit was created.
    echo Check network / GitHub access / credentials, then retry.
    pause
    exit /b 1
)

git fetch origin "%BRANCH%" >nul
if errorlevel 1 (
    echo [ERROR] Failed to refresh origin/%BRANCH%. No commit was created.
    pause
    exit /b 1
)

echo.

echo [1/4] Staging changes...
git rm -r --cached --ignore-unmatch -- "%GIT_EXCLUDE_PATH%" >nul 2>nul
if errorlevel 1 (
    echo [ERROR] Failed to exclude Git path:
    echo   "%GIT_EXCLUDE_PATH%"
    pause
    exit /b 1
)

git add -A
if errorlevel 1 (
    echo [ERROR] git add failed.
    pause
    exit /b 1
)

echo [2/4] Creating commit...
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

echo [3/4] Syncing with origin/%BRANCH%...
git merge-base --is-ancestor "origin/%BRANCH%" HEAD
if errorlevel 1 (
    echo [INFO] origin/%BRANCH% has new commit^(s^). Rebasing local work...
    git rebase "origin/%BRANCH%"
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
    echo [INFO] Local branch already contains origin/%BRANCH%.
)

for /f "delims=" %%A in ('git rev-list --count "origin/%BRANCH%..HEAD"') do set "AHEAD_COUNT=%%A"
if not defined AHEAD_COUNT set "AHEAD_COUNT=0"
if "%AHEAD_COUNT%"=="0" (
    echo [SUCCESS] Nothing to push. origin/%BRANCH% is already up to date.
    pause
    exit /b 0
)

echo [4/4] Pushing to origin/%BRANCH%...
git push -u origin "%BRANCH%"
if errorlevel 1 (
    echo [ERROR] git push failed.
    pause
    exit /b 1
)

echo [SUCCESS] Push completed.
pause
exit /b 0
