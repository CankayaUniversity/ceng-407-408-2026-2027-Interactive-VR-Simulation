param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$cataloguePath = Join-Path $ProjectRoot 'Assets\_Project\Desktop\Resources\DesktopCatalogue.json'
$catalogue = Get-Content -LiteralPath $cataloguePath -Raw | ConvertFrom-Json
$outputRoot = Join-Path $ProjectRoot 'Assets\StreamingAssets\WorkplaceStudio'
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
function Copy-VerifiedAsset([string]$Source, [string]$RelativeOutput, [string]$ExpectedSha) {
    $destination = [System.IO.Path]::GetFullPath((Join-Path $outputRoot $RelativeOutput))
    if (-not $destination.StartsWith($outputRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Generated output escaped the intended content directory' }
    if (-not (Test-Path -LiteralPath $Source)) { throw "Model eksik: $Source. Önce git lfs pull çalıştırın." }
    $sourceSha = (Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($sourceSha -ne $ExpectedSha) { throw "Model bütünlüğü doğrulanamadı: $Source. Katalog ve Git LFS sürümünü kontrol edin." }
    if (Test-Path -LiteralPath $destination) {
        if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant() -ne $ExpectedSha) { throw "Üretilmiş hedef dosyası farklı; üzerine yazılmadı: $destination" }
    } else {
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $Source -Destination $destination
    }
    Write-Output "Hazır: $RelativeOutput"
}
foreach ($environment in $catalogue.environments) {
    $relativeSource = $environment.editor.modelUrl.TrimStart('/').Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    Copy-VerifiedAsset (Join-Path $ProjectRoot ('web-editor\public\' + $relativeSource)) $environment.modelFile $environment.modelSha256
}
Copy-VerifiedAsset (Join-Path $ProjectRoot 'web-editor\public\guides\piko\piko.glb') 'guides\piko.glb' 'fd2338c12af77d767f769fdf0c06beba1b3279df78ceb6c36dbda2c5f82383ac'
Write-Output 'Masaüstü içerikleri hazır. Unity sahnesi: Assets/_Project/Desktop/Scenes/WorkplaceDesktop.unity'
