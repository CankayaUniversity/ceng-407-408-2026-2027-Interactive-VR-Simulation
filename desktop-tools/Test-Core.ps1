param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
& dotnet run --project (Join-Path $ProjectRoot 'desktop-tests\DesktopCoreTests.csproj') -- $ProjectRoot
if ($LASTEXITCODE -ne 0) { throw 'Masaüstü çekirdek testleri başarısız.' }
