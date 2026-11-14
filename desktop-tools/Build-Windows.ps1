param([string]$UnityEditor)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot 'Prepare-DesktopContent.ps1') -ProjectRoot $repoRoot
if (-not $UnityEditor) {
    $UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe'
}
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw 'Unity Editor 6000.6.7f1 bulunamadı. Unity Hub üzerinden kurun veya -UnityEditor ile Unity.exe yolunu belirtin.' }
$logDirectory = Join-Path $repoRoot 'Logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logFile = Join-Path $logDirectory 'desktop-build.log'
$buildArguments = @('-batchmode', '-quit', '-projectPath', ('"' + $repoRoot + '"'), '-buildTarget', 'Win64', '-executeMethod', 'WorkplaceStudio.Desktop.Editor.DesktopBuild.BuildWindows', '-logFile', ('"' + $logFile + '"'))
$buildProcess = Start-Process -FilePath $UnityEditor -ArgumentList $buildArguments -WindowStyle Hidden -Wait -PassThru
if ($buildProcess.ExitCode -ne 0) { throw 'Unity derlemesi başarısız. Logs/desktop-build.log dosyasını inceleyin.' }
$executable = Join-Path $repoRoot 'Builds\WorkplaceStudio-Windows\WorkplaceStudio.exe'
if (-not (Test-Path -LiteralPath $executable)) { throw 'Unity başarılı bir Windows executable üretmedi.' }
Write-Output "Uygulama: $executable"
