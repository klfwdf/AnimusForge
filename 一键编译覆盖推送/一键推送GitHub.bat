@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
cd /d "%PROJECT_ROOT%"

set "DRY_RUN=0"
set "COMMIT_MSG="

if /I "%~1"=="--dry-run" (
    set "DRY_RUN=1"
    set "COMMIT_MSG=%~2"
) else (
    set "COMMIT_MSG=%~1"
)

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

for /f "delims=" %%U in ('git remote get-url origin 2^>nul') do set "ORIGIN_URL=%%U"
if not defined ORIGIN_URL (
    echo [ERROR] Remote 'origin' not found.
    pause
    exit /b 1
)

if not defined COMMIT_MSG (
    set "NOW=%DATE% %TIME%"
    set "COMMIT_MSG=auto: update %NOW%"
)

echo [Git] One-click push started...
echo Repo   : "%PROJECT_ROOT%"
echo Branch : "%BRANCH%"
echo Origin : "%ORIGIN_URL%"
if "%DRY_RUN%"=="1" echo Mode   : DRY RUN (no add / no commit / no push)
echo.

if "%DRY_RUN%"=="1" (
    echo [Preview] Current changed files:
    git status --short
    echo.
    echo [Preview] Commit message:
    echo   "%COMMIT_MSG%"
    echo.
    echo [Preview] Would run:
    echo   git add -A
    echo   git commit -m "%COMMIT_MSG%"
    echo   git push -u origin "%BRANCH%"
    echo.
    echo [SUCCESS] Dry-run completed.
    pause
    exit /b 0
)

echo [1/3] Staging changes...
git add -A
if errorlevel 1 (
    echo [ERROR] git add failed.
    pause
    exit /b 1
)

git diff --cached --quiet
if errorlevel 1 (
    echo [2/3] Creating commit...
    git commit -m "%COMMIT_MSG%"
    if errorlevel 1 (
        echo [ERROR] git commit failed.
        pause
        exit /b 1
    )
) else (
    echo [2/3] No staged changes to commit.
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
