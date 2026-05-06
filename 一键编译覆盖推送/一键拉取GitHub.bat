@echo off
setlocal EnableExtensions EnableDelayedExpansion

chcp 65001 >nul
set "LANG=en_US.UTF-8"
set "LC_ALL=C"

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
cd /d "%PROJECT_ROOT%"

set "DRY_RUN=0"

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
    echo [ERROR] This 1.3.x toolchain only allows pulls on branch "main".
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

echo [Git] One-click pull started...
echo Repo   : "%PROJECT_ROOT%"
echo Branch : "%BRANCH%"
echo Origin : "%ORIGIN_URL%"
if "%DRY_RUN%"=="1" echo Mode   : DRY RUN (no fetch / no pull)
echo.

set "HAS_CHANGES=0"
for /f "delims=" %%S in ('git status --porcelain') do set "HAS_CHANGES=1"

if "%DRY_RUN%"=="1" (
    echo [Preview] Current changed files:
    git -c core.quotepath=false status --short
    echo.
    echo [Preview] Would run:
    echo   git ls-remote --exit-code origin "refs/heads/%BRANCH%"
    echo   git fetch origin "%BRANCH%"
    echo   if clean and behind only: git merge --ff-only "origin/%BRANCH%"
    echo   if dirty or diverged    : git rebase --autostash "origin/%BRANCH%"
    echo.
    echo [SUCCESS] Dry-run completed.
    pause
    exit /b 0
)

echo [Preflight] Checking origin/%BRANCH% connectivity...
git ls-remote --exit-code origin "refs/heads/%BRANCH%" >nul
if errorlevel 1 (
    echo [ERROR] Cannot reach origin/%BRANCH%.
    echo Check network / GitHub access / credentials, then retry.
    pause
    exit /b 1
)

echo [1/2] Fetching origin/%BRANCH%...
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

if not "%AHEAD_COUNT%"=="0" if not "%BEHIND_COUNT%"=="0" (
    echo [INFO] Local and origin/%BRANCH% have diverged. Rebasing local commit^(s^) with autostash...
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
    echo [SUCCESS] Pull/rebase completed.
    pause
    exit /b 0
)

if "%BEHIND_COUNT%"=="0" (
    if "%AHEAD_COUNT%"=="0" (
        echo [SUCCESS] Already up to date.
    ) else (
        echo [INFO] Nothing to pull. Local %BRANCH% has %AHEAD_COUNT% unpushed commit^(s^).
    )
    pause
    exit /b 0
)

if "%HAS_CHANGES%"=="1" (
    echo [2/2] Rebasing with autostash because working tree has local changes...
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
    echo [2/2] Fast-forwarding local %BRANCH%...
    git merge --ff-only "origin/%BRANCH%"
    if errorlevel 1 (
        echo [ERROR] Fast-forward pull failed.
        pause
        exit /b 1
    )
)

echo [SUCCESS] Pull completed.
pause
exit /b 0
