[CmdletBinding()]
param(
    [string]$GameRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
)

$ErrorActionPreference = "Stop"
$vrmodRoot = Join-Path $GameRoot "vrmod"
$env:DOTNET_CLI_HOME = Join-Path $vrmodRoot ".dotnet-home"
$env:NUGET_PACKAGES = Join-Path $vrmodRoot ".nuget-packages"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

$testProject = Join-Path $vrmodRoot "tests\GakumasVR.Core.Tests\GakumasVR.Core.Tests.csproj"
$managementTestProject = Join-Path $vrmodRoot "tests\GakumasVR.Management.Tests\GakumasVR.Management.Tests.csproj"
$runtimeProject = Join-Path $vrmodRoot "src\GakumasVR.RuntimeBootstrap\GakumasVR.RuntimeBootstrap.csproj"
$configuratorProject = Join-Path $vrmodRoot "src\GakumasVR.Configurator\GakumasVR.Configurator.csproj"
$installerProject = Join-Path $vrmodRoot "src\GakumasVR.Installer\GakumasVR.Installer.csproj"

dotnet restore $testProject --ignore-failed-sources
if ($LASTEXITCODE -ne 0) { throw "Core test restore failed." }
dotnet run --project $testProject -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Core tests failed." }

dotnet restore $managementTestProject --ignore-failed-sources
if ($LASTEXITCODE -ne 0) { throw "Management test restore failed." }
dotnet run --project $managementTestProject -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Management tests failed." }

dotnet restore $runtimeProject --ignore-failed-sources
if ($LASTEXITCODE -ne 0) { throw "Runtime bootstrap restore failed." }
dotnet build $runtimeProject -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Runtime bootstrap build failed." }

dotnet build $configuratorProject -c Release
if ($LASTEXITCODE -ne 0) { throw "Configurator build failed." }
dotnet build $installerProject -c Release
if ($LASTEXITCODE -ne 0) { throw "Installer build failed." }

Write-Host "CHECK: core/management tests and runtime/configurator/installer builds passed"
