@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
cd /d "%SCRIPT_DIR%"

set "BANNERLORD_ROOT=F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord"
set "CONFIG=Debug"

set "SRC_DEBUG=%PROJECT_ROOT%\bin\Debug\net472\AnimusForge.dll"
set "SRC_RELEASE=%PROJECT_ROOT%\bin\Release\net472\AnimusForge.dll"
set "SRC_DLL="

echo [AnimusForge] Build + Deploy started...
echo Script Dir : "%SCRIPT_DIR%"
echo Project Dir: "%PROJECT_ROOT%"
echo Bannerlord : "%BANNERLORD_ROOT%"
echo Config     : "%CONFIG%"
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet SDK not found in PATH.
    pause
    exit /b 1
)

if not exist "%BANNERLORD_ROOT%" (
    echo [ERROR] Bannerlord root not found:
    echo "%BANNERLORD_ROOT%"
    pause
    exit /b 1
)

echo [1/2] Build...
dotnet build "%PROJECT_ROOT%\AnimusForge.csproj" -c %CONFIG% /p:BannerlordRoot="%BANNERLORD_ROOT%"
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
echo [2/2] Deploy...
echo Source DLL : "%SRC_DLL%"

set "TARGET_LIST_FILE=%TEMP%\animusforge_targets_%RANDOM%_%RANDOM%.txt"
if exist "%TARGET_LIST_FILE%" del /f /q "%TARGET_LIST_FILE%" >nul 2>nul

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "$out='%TARGET_LIST_FILE%';" ^
  "$candidates = New-Object System.Collections.Generic.List[string];" ^
  "$drives = Get-PSDrive -PSProvider FileSystem | Select-Object -ExpandProperty Root;" ^
  "foreach($root in $drives){" ^
  "  $root = $root.TrimEnd('\');" ^
  "  $candidates.Add($root + '\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge\bin\Win64_Shipping_Client\AnimusForge.dll');" ^
  "  $candidates.Add($root + '\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge\bin\Win64_Shipping_Client\AnimusForge.dll');" ^
  "  $candidates.Add($root + '\Program Files\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge\bin\Win64_Shipping_Client\AnimusForge.dll');" ^
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
    echo [ERROR] No AnimusForge target DLL found.
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

echo [SUCCESS] Build + Deploy completed.
pause
exit /b 0
