[CmdletBinding()]
param(
    [string]$GameRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
)

$ErrorActionPreference = "Stop"
$running = Get-Process -Name "gakumas" -ErrorAction SilentlyContinue
if ($running) {
    throw "Refusing to replace the runtime while gakumas is running (PID: $($running.Id -join ', '))."
}

$sourceDirectory = Join-Path $GameRoot "vrmod\src\GakumasVR.RuntimeBootstrap\bin\Release\net6.0"
$destinationDirectory = Join-Path $GameRoot "vrmod\runtime"
$files = @(
    "GakumasVR.RuntimeBootstrap.dll",
    "GakumasVR.RuntimeBootstrap.deps.json",
    "GakumasVR.Core.dll"
)

foreach ($file in $files) {
    $source = Join-Path $sourceDirectory $file
    if (-not (Test-Path -LiteralPath $source)) {
        throw "Runtime build output is missing: $source"
    }
}

$installedBootstrap = Join-Path $destinationDirectory "GakumasVR.RuntimeBootstrap.dll"
if (Test-Path -LiteralPath $installedBootstrap) {
    $version = (Get-Item -LiteralPath $installedBootstrap).VersionInfo.ProductVersion
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $rollbackDirectory = Join-Path $GameRoot "vrmod\rollback\runtime-bootstrap-v$version-$stamp"
    New-Item -ItemType Directory -Path $rollbackDirectory -Force | Out-Null
    foreach ($file in $files) {
        $installed = Join-Path $destinationDirectory $file
        if (Test-Path -LiteralPath $installed) {
            Copy-Item -LiteralPath $installed -Destination (Join-Path $rollbackDirectory $file) -Force
        }
    }
    Write-Host "ROLLBACK: $rollbackDirectory"
}

New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
foreach ($file in $files) {
    Copy-Item -LiteralPath (Join-Path $sourceDirectory $file) -Destination (Join-Path $destinationDirectory $file) -Force
}

$installedVersion = (Get-Item -LiteralPath $installedBootstrap).VersionInfo.ProductVersion
$installedHash = (Get-FileHash -LiteralPath $installedBootstrap -Algorithm SHA256).Hash
Write-Host "INSTALLED: runtime bootstrap v$installedVersion"
Write-Host "SHA256: $installedHash"
