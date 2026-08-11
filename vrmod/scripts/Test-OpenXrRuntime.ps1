[CmdletBinding()]
param(
    [string]$GameRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$ExpectedVersion = "0.52.0",
    [switch]$RequireHmd,
    [switch]$RequireSession,
    [switch]$RequireTestPattern,
    [switch]$RequireGamePanel,
    [switch]$RequireLiveWorldPanel
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
$graphics = $session | Where-Object event -in @("d3d11-device-ready", "d3d11-device-unavailable") | Select-Object -Last 1
$failure = $session | Where-Object event -eq "openxr-probe-failure" | Select-Object -Last 1
$ready = $session | Where-Object event -eq "openxr-runtime-ready" | Select-Object -Last 1
if ($failure -and -not $ready) {
    $failure | Format-List timestampUtc,errorType,error
    throw "OpenXR runtime probing failed."
}

if (-not $ready) {
    Write-Warning "WAITING: no OpenXR runtime result has been recorded."
    exit 2
}

if ($graphics) {
    $graphics | Select-Object timestampUtc,d3D11HookInstalled,d3D11PresentDeviceCaptured,d3D11DeviceCaptured,d3D11ContextCaptured,d3D11SwapChainCaptured,error | Format-List
}

$ready | Select-Object timestampUtc,activeRuntimeName,activeRuntimeManifest,openXrLoaderPath,openXrLoaderVersion,openXrExtensionCount,supportsD3D11,openXrInstanceCreated,openXrRuntimeReportedName,openXrRuntimeVersion,openXrHmdSystemResult,openXrHmdSystemAvailable,openXrSystemName,openXrMaxSwapchainWidth,openXrMaxSwapchainHeight,openXrMaxLayerCount,openXrOrientationTracking,openXrPositionTracking,openXrRequiredAdapterLuid,openXrMinD3DFeatureLevel,openXrViewCount,openXrRecommendedViewWidth,openXrRecommendedViewHeight,openXrRecommendedSampleCount,openXrSessionCreateResult,openXrSessionCreated,openXrSessionReadyObserved,openXrEmptyFramesSubmitted,openXrTestPatternFramesSubmitted,openXrTestPatternLayerFramesSubmitted,openXrTestPatternWidth,openXrTestPatternHeight,openXrTestPatternFormat,openXrTestPatternTextureDescription,openXrTestPatternPixelReadback,openXrFrameLoopStage,openXrFrameLoopResult | Format-List
if (-not $ready.supportsD3D11) {
    throw "The active OpenXR runtime does not advertise XR_KHR_D3D11_enable."
}
if (-not $ready.openXrInstanceCreated) {
    throw "The OpenXR runtime instance was not created."
}
if ($RequireHmd -and -not $ready.openXrHmdSystemAvailable) {
    throw "The active OpenXR runtime could not provide an HMD system."
}
if ($RequireSession -and -not $ready.openXrSessionCreated) {
    throw "The Unity D3D11 device could not create an OpenXR session (result: $($ready.openXrSessionCreateResult))."
}
if ($RequireSession -and (-not $graphics -or -not $graphics.d3D11PresentDeviceCaptured)) {
    throw "The D3D11 device associated with the game presentation swapchain was not captured."
}
if (($RequireTestPattern -or $RequireGamePanel -or $RequireLiveWorldPanel) -and (
    $ready.openXrFrameLoopResult -ne 0 -or
    $ready.openXrTestPatternFramesSubmitted -lt 1 -or
    $ready.openXrTestPatternLayerFramesSubmitted -lt 1 -or
    $ready.openXrTestPatternWidth -lt 1 -or
    $ready.openXrTestPatternHeight -lt 1)) {
    throw "The OpenXR panel was not submitted successfully."
}
if ($RequireGamePanel -and $ready.openXrTestPatternPixelReadback -notlike "GAME_BACKBUFFER:*") {
    throw "The OpenXR panel was not sourced from the game backbuffer."
}
if ($RequireLiveWorldPanel -and $ready.openXrTestPatternPixelReadback -notlike "LIVE_WORLD_RT:*") {
    throw "The OpenXR panel was not sourced from the live world RenderTexture."
}

Write-Host "CHECK: active OpenXR runtime and D3D11 support diagnostics passed."
