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
    echo   if no changes   : git commit --allow-empty -m "%COMMIT_MSG_SAFE%"
    echo   git push -u origin "%BRANCH%"
    echo.
    echo [SUCCESS] Dry-run completed.
    pause
    exit /b 0
)

echo [1/3] Staging changes...
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

echo [2/3] Creating commit...
git diff --cached --quiet
if errorlevel 1 (
    git commit -m "%COMMIT_MSG_SAFE%"
    if errorlevel 1 (
        echo [ERROR] git commit failed.
        pause
        exit /b 1
    )
) else (
    echo [INFO] No file changes detected, creating empty commit...
    git commit --allow-empty -m "%COMMIT_MSG_SAFE%"
    if errorlevel 1 (
        echo [ERROR] git commit --allow-empty failed.
        pause
        exit /b 1
    )
)

echo [3/3] Pushing to origin/%BRANCH%...
git push -u origin "%BRANCH%"
if errorlevel 1 (
    echo [ERROR] git push failed.
    pause
    exit /b 1
)

echo [SUCCESS] Push completed.
pause
exit /b 0
