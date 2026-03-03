@echo off
setlocal

REM Script directory and project root
set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
cd /d "%SCRIPT_DIR%"

REM ===== Edit this if your game path is different =====
set "BANNERLORD_ROOT=F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord"
set "CONFIG=Debug"

echo [Voxforge] One-click build started...
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

echo [1/1] Building project...
dotnet build "%PROJECT_ROOT%\Voxforge.csproj" -c %CONFIG% /p:BannerlordRoot="%BANNERLORD_ROOT%"
set "ERR=%ERRORLEVEL%"

echo.
if not "%ERR%"=="0" (
    echo [FAILED] Build failed. ExitCode=%ERR%
    pause
    exit /b %ERR%
)

echo [SUCCESS] Build completed.
pause
exit /b 0
