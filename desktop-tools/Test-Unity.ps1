param([string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw 'Unity Editor 6000.6.7f1 kurulu değil. -UnityEditor ile farklı bir Unity.exe yolu belirtebilirsiniz.' }
$results = Join-Path $repoRoot 'Builds\DesktopTests\editmode-results.xml'
$log = Join-Path $repoRoot 'Logs\desktop-tests.log'
New-Item -ItemType Directory -Path (Split-Path -Parent $results), (Split-Path -Parent $log) -Force | Out-Null
# UI focus regression tests need a real panel and graphics device.
$testArguments = @('-batchmode', '-projectPath', ('"' + $repoRoot + '"'), '-runTests', '-testPlatform', 'EditMode', '-testFilter', 'WorkplaceStudio.Tests', '-testResults', ('"' + $results + '"'), '-logFile', ('"' + $log + '"'))
$testProcess = Start-Process -FilePath $UnityEditor -ArgumentList $testArguments -WindowStyle Hidden -Wait -PassThru
if ($testProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $results)) { throw 'Unity testleri başarılı tamamlanmadı. Logs/desktop-tests.log dosyasını inceleyin.' }
[xml]$report = Get-Content -LiteralPath $results -Raw
if ($report.'test-run'.result -ne 'Passed') { throw 'Unity test raporunda başarısız test var.' }
Write-Output "Unity test raporu: $results"
