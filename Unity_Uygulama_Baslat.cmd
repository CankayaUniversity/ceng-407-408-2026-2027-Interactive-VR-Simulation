@echo off
setlocal
set "STUDIO_EXE=%~dp0Builds\WorkplaceStudio-Windows\WorkplaceStudio.exe"
if not exist "%STUDIO_EXE%" (
  echo Windows uygulamasi henuz derlenmemis.
  echo Once Unity 6000.6.7f1 kurulumunu tamamlayin.
  echo Unity menusu: Workplace Studio / Build Windows Application
  echo Alternatif: powershell -File desktop-tools\Build-Windows.ps1
  pause
  exit /b 1
)
start "Workplace Studio" "%STUDIO_EXE%"
