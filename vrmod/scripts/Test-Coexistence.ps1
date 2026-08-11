[CmdletBinding()]
param(
    [string]$GameRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
)

$healthPath = Join-Path $GameRoot "BepInEx\config\GakumasVR\bootstrap-health.json"
$interopPath = Join-Path $GameRoot "BepInEx\interop\UnityEngine.CoreModule.dll"
$bepInExLogPath = Join-Path $GameRoot "BepInEx\LogOutput.log"
$localifyPath = Join-Path $GameRoot "version.dll"

$result = [ordered]@{
    localifyProxyPresent = Test-Path -LiteralPath $localifyPath
    bootstrapHealthPresent = Test-Path -LiteralPath $healthPath
    interopGenerated = Test-Path -LiteralPath $interopPath
    bepinexLogPresent = Test-Path -LiteralPath $bepInExLogPath
    readyForDiagnosticBuild = (Test-Path -LiteralPath $healthPath) -and (Test-Path -LiteralPath $interopPath)
}

[pscustomobject]$result | Format-List
if (-not $result.readyForDiagnosticBuild) {
    Write-Warning "WAITING: start gakumas from DMM and reach the title screen, then close it normally and run this check again."
    exit 2
}

Write-Host "CHECK: BepInEx bootstrap loaded beside Localify; diagnostic build is unblocked."

