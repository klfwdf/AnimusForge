@echo off
setlocal
set "SCRIPT=%~dp0install_gccz_test_artifacts.ps1"
if not exist "%SCRIPT%" (
  echo Missing install script: %SCRIPT%
  pause
  exit /b 1
)
echo Launching elevated PowerShell installer...
echo Script: %SCRIPT%
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath powershell.exe -ArgumentList '-NoExit -NoProfile -ExecutionPolicy Bypass -File %SCRIPT%' -Verb RunAs -Wait"
if errorlevel 1 (
  echo Installer launch failed or was cancelled.
  pause
  exit /b 1
)
echo Installer process closed.
pause
