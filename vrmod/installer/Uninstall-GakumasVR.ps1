[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'GakumasVR.Installation.psm1') -Force

$game = Assert-GakumasGameRoot $GameRoot
Assert-GakumasStopped
$statePath = Join-Path $game 'vrmod\install-state.json'
if (-not (Test-Path -LiteralPath $statePath -PathType Leaf)) {
    throw '설치 상태 파일(vrmod\install-state.json)이 없습니다. 추측으로 파일을 제거하지 않습니다.'
}
$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
if ([int]$state.schemaVersion -ne 1) {
    throw "지원하지 않는 설치 상태 버전입니다: $($state.schemaVersion)"
}
$backupRoot = Resolve-ContainedPath $game ([string]$state.backupRoot)
$warnings = New-Object System.Collections.Generic.List[string]

foreach ($entry in $state.files) {
    if ([string]$entry.action -ne 'installed') {
        continue
    }
    $preserveProperty = $entry.PSObject.Properties['preserveOnUninstall']
    if ($null -ne $preserveProperty -and [bool]$preserveProperty.Value) {
        Write-Verbose "사용자 데이터 보존: $($entry.path)"
        continue
    }
    $relative = [string]$entry.path
    $destination = Resolve-ContainedPath $game $relative
    if (-not (Test-Path -LiteralPath $destination -PathType Leaf)) {
        $warnings.Add("이미 없음: $relative")
        continue
    }
    $currentHash = Get-FileSha256 $destination
    if ($currentHash -ne ([string]$entry.installedHash).ToUpperInvariant()) {
        $warnings.Add("사용자 변경 파일 보존: $relative")
        continue
    }

    if ([bool]$entry.priorFile) {
        $backup = Resolve-ContainedPath $backupRoot ([string]$entry.backupRelative)
        if (-not (Test-Path -LiteralPath $backup -PathType Leaf)) {
            $warnings.Add("백업 누락으로 보존: $relative")
            continue
        }
        [System.IO.File]::Copy($backup, $destination, $true)
        Write-Verbose "복원: $relative"
    }
    else {
        [System.IO.File]::Delete($destination)
        Write-Verbose "제거: $relative"
    }
}

if ($warnings.Count -eq 0) {
    $previousProperty = $state.PSObject.Properties['previousStateBackup']
    $previousStateBackup = if ($null -ne $previousProperty) {
        [string]$previousProperty.Value
    }
    else {
        ''
    }
    if (-not [string]::IsNullOrWhiteSpace($previousStateBackup)) {
        $previousStatePath = Resolve-ContainedPath $backupRoot $previousStateBackup
        if (-not (Test-Path -LiteralPath $previousStatePath -PathType Leaf)) {
            throw '직전 버전 설치 상태 백업이 없어 롤백 상태를 확정할 수 없습니다.'
        }
        [System.IO.File]::Copy($previousStatePath, $statePath, $true)
        Write-Host "Gakumas VR $($state.version) 제거 완료. 직전 설치 버전으로 복귀했습니다."
    }
    else {
        [System.IO.File]::Delete($statePath)
        Write-Host "Gakumas VR $($state.version) 제거 완료. 설정/로그와 한글패치 파일은 보존했습니다."
    }
}
else {
    foreach ($warning in $warnings) {
        Write-Warning $warning
    }
    Write-Warning '일부 파일을 안전상 건드리지 않았으므로 설치 상태 파일을 유지합니다.'
}
