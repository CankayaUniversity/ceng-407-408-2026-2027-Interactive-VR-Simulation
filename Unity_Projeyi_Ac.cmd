@echo off
setlocal
set "WORKPLACE_UNITY_EDITOR=C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe"
if not "%~1"=="" set "WORKPLACE_UNITY_EDITOR=%~1"
if not exist "%WORKPLACE_UNITY_EDITOR%" (
  echo Unity Editor 6000.6.7f1 bulunamadi.
  echo Once Unity Hub kurulumu ve Editor kurulumunu tamamlayin.
  echo Belgeler: docs\DESKTOP_APP.md
  pause
  exit /b 1
)
start "Unity - Workplace Studio" "%WORKPLACE_UNITY_EDITOR%" -projectPath "%~dp0."
