[CmdletBinding()]
param(
    [string]$GameRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$ExpectedVersion = "0.52.0"
)

$logPath = Join-Path $GameRoot "vrmod\logs\runtime-bootstrap.jsonl"
if (-not (Test-Path -LiteralPath $logPath)) {
    Write-Warning "WAITING: runtime bootstrap log does not exist."
    exit 2
}

$events = Get-Content -LiteralPath $logPath |
    ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object bootstrapVersion -eq $ExpectedVersion
$failure = $events | Where-Object { $_.event -in @("bootstrap-failure", "sampler-failure") } | Select-Object -Last 1
$snapshot = $events | Where-Object event -eq "render-snapshot" | Select-Object -Last 1

if ($failure -and (-not $snapshot -or $failure.timestampUtc -gt $snapshot.timestampUtc)) {
    $failure | Format-List timestampUtc,event,errorType,error
    throw "Runtime scene diagnostics failed."
}

if (-not $snapshot) {
    Write-Warning "WAITING: no v$ExpectedVersion render snapshot has been recorded."
    exit 2
}

$valid = $snapshot.width -gt 0 -and $snapshot.height -gt 0 -and $snapshot.cameraCount -ge 0
$snapshot | Select-Object timestampUtc,bootstrapVersion,scene,width,height,orientation,cameraCount | Format-List
if (-not $valid) {
    throw "Render snapshot contains invalid dimensions or camera count."
}

Write-Host "CHECK: main-thread scene, screen, orientation, and camera diagnostics passed."
