[CmdletBinding()]
param(
    [string]$GameRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
)

$logPath = Join-Path $GameRoot "vrmod\logs\runtime-bootstrap.jsonl"
if (-not (Test-Path -LiteralPath $logPath)) {
    Write-Warning "WAITING: runtime bootstrap log does not exist. Launch gakumas through DMM."
    exit 2
}

$events = Get-Content -LiteralPath $logPath | ForEach-Object { $_ | ConvertFrom-Json }
$failure = $events | Where-Object event -eq "bootstrap-failure" | Select-Object -Last 1
$ready = $events | Where-Object event -eq "runtime-ready" | Select-Object -Last 1

if ($failure -and (-not $ready -or $failure.timestampUtc -gt $ready.timestampUtc)) {
    $failure | Format-List timestampUtc,event,errorType,error
    throw "Runtime bootstrap failed."
}

if (-not $ready) {
    Write-Warning "WAITING: bootstrap started but IL2CPP runtime-ready was not recorded yet."
    exit 2
}

$hasCoreModule = @($ready.assemblies) -contains "UnityEngine.CoreModule.dll"
[pscustomobject]@{
    timestampUtc = $ready.timestampUtc
    assemblyCount = $ready.assemblyCount
    unityCoreModuleFound = $hasCoreModule
    status = if ($hasCoreModule) { "PASS" } else { "INCOMPLETE" }
} | Format-List

if (-not $hasCoreModule) {
    throw "IL2CPP assembly enumeration did not include UnityEngine.CoreModule.dll."
}

Write-Host "CHECK: runtime IL2CPP API bootstrap passed without Cpp2IL interop."

