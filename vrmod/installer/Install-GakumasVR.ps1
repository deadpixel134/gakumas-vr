[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot,

    [string]$PackageRoot = $PSScriptRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'GakumasVR.Installation.psm1') -Force

$game = Assert-GakumasGameRoot $GameRoot
Assert-GakumasStopped
$package = [System.IO.Path]::GetFullPath($PackageRoot)
$manifestPath = Join-Path $package 'package-manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "패키지 manifest를 찾지 못했습니다: $manifestPath"
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ([int]$manifest.schemaVersion -ne 1) {
    throw "지원하지 않는 패키지 manifest 버전입니다: $($manifest.schemaVersion)"
}

$localifyStatus = Get-LocalifyStatus $game
switch ($localifyStatus) {
    'Installed' { Write-Host '한글패치 감지: version.dll과 gakumas-local을 보존한 채 VR을 설치합니다.' }
    'Partial' { Write-Warning '한글패치 흔적이 일부만 발견되었습니다. 관련 파일은 모두 보존하고 VR만 독립 설치합니다.' }
    'Absent' { Write-Host '한글패치 없음: 한글패치 파일을 만들지 않고 VR만 설치합니다.' }
}

$timestamp = [DateTime]::Now.ToString('yyyyMMdd-HHmmss')
$backupRootRelative = "vrmod/rollback/product-install-$($manifest.version)-$timestamp"
$backupRoot = Resolve-ContainedPath $game $backupRootRelative
$statePath = Join-Path $game 'vrmod\install-state.json'
$previousStateBackup = $null
if (Test-Path -LiteralPath $statePath -PathType Leaf) {
    $previousState = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    if ([int]$previousState.schemaVersion -ne 1) {
        throw "기존 설치 상태 버전을 지원하지 않아 안전하게 업그레이드할 수 없습니다: $($previousState.schemaVersion)"
    }
    $previousStateBackup = '_previous-install-state.json'
    [System.IO.Directory]::CreateDirectory($backupRoot) | Out-Null
    [System.IO.File]::Copy(
        $statePath,
        (Join-Path $backupRoot $previousStateBackup),
        $true)
    Write-Host "기존 Gakumas VR $($previousState.version) 설치 상태를 중첩 롤백용으로 보존합니다."
}
$completed = New-Object System.Collections.Generic.List[object]
$stateFiles = New-Object System.Collections.Generic.List[object]

try {
    foreach ($file in $manifest.files) {
        $relative = [string]$file.path
        $source = Resolve-ContainedPath (Join-Path $package 'payload') $relative
        $destination = Resolve-ContainedPath $game $relative
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
            throw "패키지 파일이 없습니다: $relative"
        }
        $sourceHash = Get-FileSha256 $source
        if ($sourceHash -ne ([string]$file.sha256).ToUpperInvariant()) {
            throw "패키지 파일 해시가 manifest와 다릅니다: $relative"
        }

        $preserve = [bool]$file.preserveExisting
        $priorFile = Test-Path -LiteralPath $destination -PathType Leaf
        if ($preserve -and $priorFile) {
            Write-Verbose "보존: $relative"
            $stateFiles.Add([ordered]@{
                path = $relative
                action = 'preserved'
                installedHash = $null
                priorFile = $true
                backupRelative = $null
                preserveOnUninstall = [bool]$file.preserveOnUninstall
            })
            continue
        }

        $backupRelative = $null
        if ($priorFile) {
            $backup = Resolve-ContainedPath $backupRoot $relative
            [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($backup)) | Out-Null
            [System.IO.File]::Copy($destination, $backup, $true)
            $backupRelative = $relative
        }
        [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($destination)) | Out-Null
        [System.IO.File]::Copy($source, $destination, $true)
        $installedHash = Get-FileSha256 $destination
        $entry = [ordered]@{
            path = $relative
            action = 'installed'
            installedHash = $installedHash
            priorFile = [bool]$priorFile
            backupRelative = $backupRelative
            preserveOnUninstall = [bool]$file.preserveOnUninstall
        }
        $completed.Add($entry)
        $stateFiles.Add($entry)
        Write-Verbose "설치: $relative"
    }

    $state = [ordered]@{
        schemaVersion = 1
        version = [string]$manifest.version
        installedUtc = [DateTime]::UtcNow.ToString('o')
        localifyStatus = $localifyStatus
        backupRoot = $backupRootRelative
        previousStateBackup = $previousStateBackup
        files = $stateFiles
    }
    Write-JsonAtomic $statePath $state
    Write-Host "Gakumas VR $($manifest.version) 설치 완료. 한글패치 상태: $localifyStatus"
}
catch {
    Write-Warning '설치 실패. 이번 실행에서 변경한 파일을 되돌립니다.'
    for ($index = $completed.Count - 1; $index -ge 0; $index--) {
        $entry = $completed[$index]
        $destination = Resolve-ContainedPath $game ([string]$entry.path)
        if ([bool]$entry.priorFile) {
            $backup = Resolve-ContainedPath $backupRoot ([string]$entry.backupRelative)
            if (Test-Path -LiteralPath $backup -PathType Leaf) {
                [System.IO.File]::Copy($backup, $destination, $true)
            }
        }
        elseif (Test-Path -LiteralPath $destination -PathType Leaf) {
            [System.IO.File]::Delete($destination)
        }
    }
    throw
}
