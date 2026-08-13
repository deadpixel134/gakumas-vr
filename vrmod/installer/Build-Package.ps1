[CmdletBinding()]
param(
    [string]$Version = '0.171.0',
    [string]$OutputRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$vrmodRoot = Join-Path $repositoryRoot 'vrmod'
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $vrmodRoot 'dist'
}
$outputFull = [System.IO.Path]::GetFullPath($OutputRoot)
$allowedRoot = [System.IO.Path]::GetFullPath((Join-Path $vrmodRoot 'dist')).TrimEnd('\') + '\'
if (-not (($outputFull.TrimEnd('\') + '\').StartsWith($allowedRoot, [System.StringComparison]::OrdinalIgnoreCase))) {
    throw '패키지 출력은 vrmod\dist 내부여야 합니다.'
}
$packageRoot = Join-Path $outputFull "GakumasVR-v$Version"
if (Test-Path -LiteralPath $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}
$payload = Join-Path $packageRoot 'payload'
[System.IO.Directory]::CreateDirectory($payload) | Out-Null

dotnet build (Join-Path $vrmodRoot 'src\GakumasVR.RuntimeBootstrap\GakumasVR.RuntimeBootstrap.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw '런타임 빌드 실패' }
$publish = Join-Path $vrmodRoot 'src\GakumasVR.Configurator\bin\Release\publish-win-x64'
dotnet publish (Join-Path $vrmodRoot 'src\GakumasVR.Configurator\GakumasVR.Configurator.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $publish
if ($LASTEXITCODE -ne 0) { throw '설정 GUI publish 실패' }
$installerPublish = Join-Path $vrmodRoot 'src\GakumasVR.Installer\bin\Release\publish-win-x64'
dotnet publish (Join-Path $vrmodRoot 'src\GakumasVR.Installer\GakumasVR.Installer.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o $installerPublish
if ($LASTEXITCODE -ne 0) { throw '설치 GUI publish 실패' }

function Copy-PayloadFile {
    param([string]$Source, [string]$Relative)
    $target = Join-Path $payload $Relative
    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($target)) | Out-Null
    [System.IO.File]::Copy($Source, $target, $true)
}

function Assert-FileHash {
    param([string]$Path, [string]$Expected, [string]$Name)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Name 파일이 없습니다: $Path"
    }
    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actual -ne $Expected.ToUpperInvariant()) {
        throw "$Name SHA-256 불일치: expected=$Expected actual=$actual"
    }
}

$vendorRoot = Join-Path $vrmodRoot 'vendor\staging\bepinex-6.0.0-be.785'
$dobbySource = Join-Path $vendorRoot 'BepInEx\core\dobby.dll'
Assert-FileHash `
    $dobbySource `
    '8015DE7D867245A1095D13947A63763878E4CF5FD3D3089B63CC39200B055DED' `
    'Dobby'
Copy-PayloadFile (Join-Path $vendorRoot 'winhttp.dll') 'winhttp.dll'
Copy-PayloadFile $dobbySource 'BepInEx\core\dobby.dll'
Copy-PayloadFile (Join-Path $repositoryRoot 'doorstop_config.ini') 'doorstop_config.ini'
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $vendorRoot 'dotnet') -File) {
    Copy-PayloadFile $file.FullName (Join-Path 'dotnet' $file.Name)
}
$runtimeOutput = Join-Path $vrmodRoot 'src\GakumasVR.RuntimeBootstrap\bin\Release\net6.0'
Copy-PayloadFile (Join-Path $runtimeOutput 'GakumasVR.RuntimeBootstrap.dll') 'vrmod\runtime\GakumasVR.RuntimeBootstrap.dll'
Copy-PayloadFile (Join-Path $runtimeOutput 'GakumasVR.RuntimeBootstrap.deps.json') 'vrmod\runtime\GakumasVR.RuntimeBootstrap.deps.json'
Copy-PayloadFile (Join-Path $runtimeOutput 'GakumasVR.Core.dll') 'vrmod\runtime\GakumasVR.Core.dll'
Copy-PayloadFile (Join-Path $vrmodRoot 'vendor\openxr-loader-1.1.59\openxr_loader.dll') 'vrmod\runtime\openxr_loader.dll'
Copy-PayloadFile (Join-Path $vrmodRoot 'config\settings.default.json') 'vrmod\config\settings.json'
Copy-PayloadFile (Join-Path $vrmodRoot 'THIRD_PARTY_NOTICES.txt') 'vrmod\THIRD_PARTY_NOTICES.txt'
Copy-PayloadFile (Join-Path $repositoryRoot 'LICENSE') 'vrmod\LICENSE.txt'
Copy-PayloadFile (Join-Path $vrmodRoot 'licenses\Dobby-Apache-2.0.txt') 'vrmod\licenses\Dobby-Apache-2.0.txt'
Copy-PayloadFile (Join-Path $publish 'GakumasVR.Configurator.exe') 'vrmod\tools\GakumasVR.Configurator.exe'

foreach ($name in @('Install-GakumasVR.ps1', 'Uninstall-GakumasVR.ps1', 'GakumasVR.Installation.psm1')) {
    [System.IO.File]::Copy((Join-Path $PSScriptRoot $name), (Join-Path $packageRoot $name), $true)
}
$installerExecutable = Join-Path $packageRoot 'GakumasVR.Installer.exe'
[System.IO.File]::Copy(
    (Join-Path $installerPublish 'GakumasVR.Installer.exe'),
    $installerExecutable,
    $true)

$files = New-Object System.Collections.Generic.List[object]
foreach ($file in Get-ChildItem -LiteralPath $payload -File -Recurse | Sort-Object FullName) {
    $payloadPrefix = $payload.TrimEnd('\') + '\'
    $relative = $file.FullName.Substring($payloadPrefix.Length).Replace('\', '/')
    $files.Add([ordered]@{
        path = $relative
        sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
        preserveExisting = ($relative -eq 'vrmod/config/settings.json' -or $relative -eq 'BepInEx/core/dobby.dll')
        preserveOnUninstall = ($relative -eq 'vrmod/config/settings.json')
    })
}
$manifest = [ordered]@{
    schemaVersion = 1
    version = $Version
    createdUtc = [DateTime]::UtcNow.ToString('o')
    loader = 'winhttp-doorstop'
    localifyPolicy = 'preserve-version-dll-and-gakumas-local'
    installer = [ordered]@{
        path = 'GakumasVR.Installer.exe'
        sha256 = (Get-FileHash -LiteralPath $installerExecutable -Algorithm SHA256).Hash.ToUpperInvariant()
    }
    files = $files
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $packageRoot 'package-manifest.json') -Encoding utf8
& (Join-Path $vrmodRoot 'tests\Test-DistributionPackage.ps1') -PackageRoot $packageRoot
$archivePath = "$packageRoot.zip"
if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $packageRoot,
    $archivePath,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false)
$archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToUpperInvariant()
Set-Content `
    -LiteralPath "$archivePath.sha256" `
    -Value "$archiveHash  $([System.IO.Path]::GetFileName($archivePath))" `
    -Encoding ascii
Write-Host "패키지 생성 완료: $packageRoot"
Write-Host "배포 ZIP 생성 완료: $archivePath"
Write-Host "SHA-256 파일 생성 완료: $archivePath.sha256"
