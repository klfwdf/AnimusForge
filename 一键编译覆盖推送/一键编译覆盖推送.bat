@echo off
setlocal EnableExtensions EnableDelayedExpansion

chcp 65001 >nul
set "LANG=en_US.UTF-8"
set "LC_ALL=C"

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
cd /d "%SCRIPT_DIR%"

set "BANNERLORD_ROOT=F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord"
set "CONFIG=Debug"
set "GIT_DRY_RUN=0"
set "COMMIT_MSG="

:parse_args
if "%~1"=="" goto args_done

if /I "%~1"=="--git-dry-run" (
    set "GIT_DRY_RUN=1"
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

set "SRC_DEBUG=%PROJECT_ROOT%\bin\Debug\net472\Voxforge.dll"
set "SRC_RELEASE=%PROJECT_ROOT%\bin\Release\net472\Voxforge.dll"
set "SRC_DLL="

echo [Voxforge] Build + Deploy + Push started...
echo Script Dir : "%SCRIPT_DIR%"
echo Project Dir: "%PROJECT_ROOT%"
echo Bannerlord : "%BANNERLORD_ROOT%"
echo Config     : "%CONFIG%"
if "%GIT_DRY_RUN%"=="1" echo Git Mode   : DRY RUN
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet SDK not found in PATH.
    pause
    exit /b 1
)

where git >nul 2>nul
if errorlevel 1 (
    echo [ERROR] git not found in PATH.
    pause
    exit /b 1
)

if not exist "%BANNERLORD_ROOT%" (
    echo [ERROR] Bannerlord root not found:
    echo "%BANNERLORD_ROOT%"
    pause
    exit /b 1
)

echo [1/3] Build...
dotnet build "%PROJECT_ROOT%\Voxforge.csproj" -c %CONFIG% /p:BannerlordRoot="%BANNERLORD_ROOT%"
set "ERR=%ERRORLEVEL%"
if not "%ERR%"=="0" (
    echo.
    echo [FAILED] Build step failed. ExitCode=%ERR%
    pause
    exit /b %ERR%
)

if exist "%SRC_DEBUG%" set "SRC_DLL=%SRC_DEBUG%"
if not defined SRC_DLL if exist "%SRC_RELEASE%" set "SRC_DLL=%SRC_RELEASE%"

if not defined SRC_DLL (
    echo [ERROR] Built DLL not found.
    echo Tried:
    echo   "%SRC_DEBUG%"
    echo   "%SRC_RELEASE%"
    pause
    exit /b 1
)

echo.
echo [2/3] Deploy...
echo Source DLL : "%SRC_DLL%"

set "TARGET_LIST_FILE=%TEMP%\voxforge_targets_%RANDOM%_%RANDOM%.txt"
if exist "%TARGET_LIST_FILE%" del /f /q "%TARGET_LIST_FILE%" >nul 2>nul

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "$out='%TARGET_LIST_FILE%';" ^
  "$candidates = New-Object System.Collections.Generic.List[string];" ^
  "$drives = Get-PSDrive -PSProvider FileSystem | Select-Object -ExpandProperty Root;" ^
  "foreach($root in $drives){" ^
  "  $root = $root.TrimEnd('\');" ^
  "  $candidates.Add($root + '\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\Voxforge\bin\Win64_Shipping_Client\Voxforge.dll');" ^
  "  $candidates.Add($root + '\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Voxforge\bin\Win64_Shipping_Client\Voxforge.dll');" ^
  "  $candidates.Add($root + '\Program Files\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Voxforge\bin\Win64_Shipping_Client\Voxforge.dll');" ^
  "};" ^
  "$hits = $candidates | Where-Object { Test-Path $_ } | Sort-Object -Unique;" ^
  "$hits | Set-Content -Path $out -Encoding Ascii;" ^
  "Write-Output ('FOUND=' + $hits.Count);"

if errorlevel 1 (
    echo [ERROR] Search target path failed.
    if exist "%TARGET_LIST_FILE%" del /f /q "%TARGET_LIST_FILE%" >nul 2>nul
    pause
    exit /b 1
)

set "FOUND_COUNT=0"
for /f "usebackq delims=" %%L in ("%TARGET_LIST_FILE%") do (
    set /a FOUND_COUNT+=1
)

if "%FOUND_COUNT%"=="0" (
    echo [ERROR] No Voxforge target DLL found.
    if exist "%TARGET_LIST_FILE%" del /f /q "%TARGET_LIST_FILE%" >nul 2>nul
    pause
    exit /b 1
)

for /f "usebackq delims=" %%L in ("%TARGET_LIST_FILE%") do (
    copy /Y "%SRC_DLL%" "%%L" >nul
    if errorlevel 1 (
        echo [ERROR] Copy failed: "%%L"
        if exist "%TARGET_LIST_FILE%" del /f /q "%TARGET_LIST_FILE%" >nul 2>nul
        pause
        exit /b 1
    )
    echo [OK] Copied to: "%%L"

    powershell -NoProfile -ExecutionPolicy Bypass -Command ^
      "$src=(Get-FileHash '%SRC_DLL%' -Algorithm SHA256).Hash;" ^
      "$dst=(Get-FileHash '%%L' -Algorithm SHA256).Hash;" ^
      "Write-Output ('SRC='+$src);" ^
      "Write-Output ('DST='+$dst);" ^
      "Write-Output ('MATCH=' + ($src -eq $dst));"
    if errorlevel 1 (
        echo [ERROR] Hash verify failed: "%%L"
        if exist "%TARGET_LIST_FILE%" del /f /q "%TARGET_LIST_FILE%" >nul 2>nul
        pause
        exit /b 1
    )
    echo ---
)

if exist "%TARGET_LIST_FILE%" del /f /q "%TARGET_LIST_FILE%" >nul 2>nul

echo.
echo [3/3] Push to GitHub...
cd /d "%PROJECT_ROOT%"

git rev-parse --is-inside-work-tree >nul 2>nul
if errorlevel 1 (
    echo [ERROR] Not a git repository:
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
    set /p "INPUT_MSG=请输入本次推送备注（可留空）: "
    if defined INPUT_MSG set "COMMIT_MSG=!INPUT_MSG!"
)

if not defined COMMIT_MSG (
    set "NOW=%DATE% %TIME%"
    set "COMMIT_MSG=auto: build+deploy+push %NOW%"
)

set "COMMIT_MSG_SAFE=%COMMIT_MSG:"='%"


echo Branch : "%BRANCH%"
echo Origin : "%ORIGIN_URL%"

if "%GIT_DRY_RUN%"=="1" (
    echo [Preview] Current changed files:
    git -c core.quotepath=false status --short
    echo.
    echo [Preview] Commit message:
    echo   "%COMMIT_MSG%"
    echo.
    echo [Preview] Would run:
    echo   git add -A
    echo   git commit -m "%COMMIT_MSG_SAFE%"
    echo   git push -u origin "%BRANCH%"
    echo.
    echo [SUCCESS] Build + Deploy + Push (Git dry-run) completed.
    pause
    exit /b 0
)

git add -A
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
    echo [INFO] No staged changes to commit.
)

git push -u origin "%BRANCH%"
if errorlevel 1 (
    echo [ERROR] git push failed.
    pause
    exit /b 1
)

echo [SUCCESS] Build + Deploy + Push completed.
pause
exit /b 0
