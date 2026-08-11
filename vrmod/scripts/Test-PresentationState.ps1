[CmdletBinding()]
param(
    [string]$GameRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$ExpectedVersion = "0.52.0",
    [switch]$RequireLandscapeTransition,
    [switch]$RequireRoundTrip
)

$ErrorActionPreference = "Stop"
$logPath = Join-Path $GameRoot "vrmod\logs\runtime-bootstrap.jsonl"
if (-not (Test-Path -LiteralPath $logPath)) {
    Write-Warning "WAITING: runtime bootstrap log does not exist."
    exit 2
}

$events = Get-Content -LiteralPath $logPath |
    ForEach-Object { $_ | ConvertFrom-Json } |
    Where-Object bootstrapVersion -eq $ExpectedVersion
$sessionStart = $events | Where-Object event -eq "bootstrap-start" | Select-Object -Last 1
if (-not $sessionStart) {
    Write-Warning "WAITING: no v$ExpectedVersion session has been recorded."
    exit 2
}

$session = $events | Where-Object processId -eq $sessionStart.processId
$failure = $session | Where-Object { $_.event -in @("bootstrap-failure", "sampler-failure") } | Select-Object -Last 1
if ($failure) {
    $failure | Format-List timestampUtc,event,errorType,error
    throw "Runtime presentation diagnostics failed."
}

$decisions = $session | Where-Object event -eq "presentation-decision"
$stablePortrait = $decisions | Where-Object transitionState -eq "StablePortrait" | Select-Object -Last 1
if (-not $stablePortrait) {
    Write-Warning "WAITING: no stable portrait decision has been recorded."
    exit 2
}

$stablePortrait | Select-Object timestampUtc,scene,width,height,orientation,transitionState,presentationContext,presentationMode | Format-List

if ($RequireLandscapeTransition) {
    $request = $decisions | Where-Object {
        $_.orientationKind -eq "Landscape" -and $_.requestRebind -eq $true
    } | Select-Object -Last 1
    $stableLandscape = $decisions | Where-Object transitionState -eq "StableLandscape" | Select-Object -Last 1
    if (-not $request -or -not $stableLandscape) {
        Write-Warning "WAITING: a complete portrait-to-landscape transition has not been recorded."
        exit 2
    }
    $request, $stableLandscape | Select-Object timestampUtc,scene,width,height,orientation,transitionState,orientationKind,freezeFrame,blockPointerInput,requestRebind | Format-Table -AutoSize
}

if ($RequireRoundTrip) {
    $lastLandscape = $decisions | Where-Object transitionState -eq "StableLandscape" | Select-Object -Last 1
    $portraitAfterLandscape = $decisions | Where-Object {
        $_.transitionState -eq "StablePortrait" -and
        $lastLandscape -and
        $_.timestampUtc -gt $lastLandscape.timestampUtc
    } | Select-Object -Last 1
    if (-not $lastLandscape -or -not $portraitAfterLandscape) {
        Write-Warning "WAITING: a complete landscape-to-portrait return has not been recorded."
        exit 2
    }
    $portraitAfterLandscape | Select-Object timestampUtc,scene,width,height,orientation,transitionState,orientationKind | Format-List
}

Write-Host "CHECK: aspect-ratio presentation state diagnostics passed."
