@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
cd /d "%SCRIPT_DIR%"

set "SRC_DEBUG=%PROJECT_ROOT%\bin\Debug\net472\Voxforge.dll"
set "SRC_RELEASE=%PROJECT_ROOT%\bin\Release\net472\Voxforge.dll"
set "SRC_DLL="

if exist "%SRC_DEBUG%" set "SRC_DLL=%SRC_DEBUG%"
if not defined SRC_DLL if exist "%SRC_RELEASE%" set "SRC_DLL=%SRC_RELEASE%"

echo [Voxforge] One-click deploy started...
echo Script Dir : "%SCRIPT_DIR%"
echo Project Dir: "%PROJECT_ROOT%"

if not defined SRC_DLL (
    echo [ERROR] Source DLL not found.
    echo Tried:
    echo   "%SRC_DEBUG%"
    echo   "%SRC_RELEASE%"
    pause
    exit /b 1
)

echo Source DLL : "%SRC_DLL%"
echo.

set "TARGET_LIST_FILE=%TEMP%\voxforge_targets_%RANDOM%_%RANDOM%.txt"
if exist "%TARGET_LIST_FILE%" del /f /q "%TARGET_LIST_FILE%" >nul 2>nul

echo [1/3] Searching Voxforge target locations...
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
    echo [ERROR] Failed while searching target paths.
    if exist "%TARGET_LIST_FILE%" del /f /q "%TARGET_LIST_FILE%" >nul 2>nul
    pause
    exit /b 1
)

set "FOUND_COUNT=0"
for /f "usebackq delims=" %%L in ("%TARGET_LIST_FILE%") do (
    set /a FOUND_COUNT+=1
)

if "%FOUND_COUNT%"=="0" (
    echo [ERROR] No Voxforge target DLL found under common Steam paths.
    echo Expected pattern:
    echo   *:\SteamLibrary\steamapps\common\Mount ^& Blade II Bannerlord\Modules\Voxforge\bin\Win64_Shipping_Client\Voxforge.dll
    if exist "%TARGET_LIST_FILE%" del /f /q "%TARGET_LIST_FILE%" >nul 2>nul
    pause
    exit /b 1
)

echo Found %FOUND_COUNT% target(s):
for /f "usebackq delims=" %%L in ("%TARGET_LIST_FILE%") do (
    echo   "%%L"
)
echo.

echo [2/3] Copying DLL to target(s)...
for /f "usebackq delims=" %%L in ("%TARGET_LIST_FILE%") do (
    copy /Y "%SRC_DLL%" "%%L" >nul
    if errorlevel 1 (
        echo [ERROR] Copy failed: "%%L"
        if exist "%TARGET_LIST_FILE%" del /f /q "%TARGET_LIST_FILE%" >nul 2>nul
        pause
        exit /b 1
    ) else (
        echo [OK] Copied to: "%%L"
    )
)
echo.

echo [3/3] Verifying SHA256...
for /f "usebackq delims=" %%L in ("%TARGET_LIST_FILE%") do (
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

echo [SUCCESS] Deploy completed.
pause
exit /b 0
