[한국어](../README.md) | [English](../README.en.md) | [日本語](../README.ja.md)

# Gakumas VR 개발자 안내

학원 아이돌마스터 DMM판을 위한 독립 Doorstop + IL2CPP + OpenXR VR 런타임입니다. 사용자 문서는 [설치](../docs/ko/INSTALLATION.md), [사용 방법](../docs/ko/USAGE.md), [프로그램 구조](../docs/ko/ARCHITECTURE.md)에서 확인할 수 있습니다.

현재 런타임 버전은 **v0.174.0**, 진행 단계는 **M8 런타임 호환성·유지보수**입니다. M2~M7의 완료 근거와 미검증 항목은 [마일스톤](../docs/VR_MILESTONES.md)과 [현재 상태](../docs/GAKUMAS_VR_STATUS.md)를 기준으로 판정합니다. 빌드·설치 성공과 PC·VR 실기 성공은 서로 다른 상태입니다. 범용 6DoF 조작과 roll 처리의 권위 명세는 [VR 조작·구조 이식 명세](../docs/ko/VR_INTERACTION_SPEC.md)입니다.

## 저장소 구조

- `src/GakumasVR.RuntimeBootstrap/`: IL2CPP 공개 API, Unity main-thread/D3D11 hook, OpenXR 렌더·입력
- `src/GakumasVR.Core/`: Unity와 분리된 설정 schema, 검증, 입력·표시 상태 로직
- `src/GakumasVR.Configurator/`: 한국어·영어·일본어 데스크톱 설정 GUI
- `src/GakumasVR.Installer/`: 단일 EXE 설치 GUI
- `src/GakumasVR.Management/`: manifest 기반 설치·제거·롤백 엔진
- `installer/`: 배포 패키지 작성과 PowerShell 설치 인터페이스
- `tests/`: Core·Management 회귀 테스트
- `config/`: 런타임 설정
- `baseline/`: 지원 게임·Localify 기준선
- `scripts/`: 빌드, 설치, 기준선과 런타임 진단
- `vendor/`: 로컬 외부 바이너리 staging; Git에는 바이너리를 커밋하지 않음
- `CHANGELOG.md`: 버전별 변경과 실기 결과

상세 렌더·입력 설계는 [설계 문서](../docs/GAKUMAS_VR_DESIGN.md), 다음 세션 인수인계는 [VR_HANDOFF](../docs/VR_HANDOFF.md)를 따릅니다.

## 전제 조건

- Windows 11 x64
- .NET 6 SDK
- PowerShell 7 권장
- Unity 6000.0.77f1 IL2CPP/D3D11 기반 대상 설치본
- 외부 staging 파일:
  - `vendor/staging/bepinex-6.0.0-be.785/`의 Unity Doorstop `winhttp.dll`과 .NET 6 runtime
  - `vendor/openxr-loader-1.1.59/openxr_loader.dll`
  - 실행 대상의 `BepInEx/core/dobby.dll`

외부 파일의 출처와 라이선스는 [vendor 안내](vendor/README.md)와 [THIRD_PARTY_NOTICES](THIRD_PARTY_NOTICES.txt)를 확인하십시오.

## 빌드와 테스트

저장소 루트에서 실행합니다.

```powershell
.\vrmod\scripts\Build-VRMod.ps1
dotnet run --project .\vrmod\tests\GakumasVR.Core.Tests\GakumasVR.Core.Tests.csproj -c Release
dotnet run --project .\vrmod\tests\GakumasVR.Management.Tests\GakumasVR.Management.Tests.csproj -c Release
```

`Build-Package.ps1`은 ZIP 생성 전에 전체 manifest, Dobby/OpenXR 네이티브 로딩, 무한글패치 클린 설치·제거와 Localify 공존·보존을 자동 검사합니다. v0.174.0 결과는 Core 39/39, 실제 배포 패키지 포함 Management 7/7, manifest 199/199입니다. 실제 결과가 문서보다 우선합니다.

배포 패키지는 다음과 같이 만듭니다.

```powershell
.\vrmod\installer\Build-Package.ps1 -Version 0.174.0
```

출력은 `vrmod/dist/` 아래로 제한됩니다. 스크립트는 런타임을 빌드하고 설정기·설치기를 self-contained 단일 EXE로 publish한 뒤 payload SHA-256 manifest와 ZIP을 만듭니다.

## 로컬 설치와 진단

```powershell
.\vrmod\scripts\Verify-Baseline.ps1
.\vrmod\scripts\Install-Bootstrap.ps1
.\vrmod\scripts\Test-Coexistence.ps1
.\vrmod\scripts\Test-RuntimeBootstrap.ps1
.\vrmod\scripts\Test-SceneDiagnostics.ps1
.\vrmod\scripts\Test-PresentationState.ps1 -RequireLandscapeTransition -RequireRoundTrip
.\vrmod\scripts\Test-OpenXrRuntime.ps1
```

게임 실행 중 런타임 DLL을 교체하지 마십시오. 기준선 불일치, `.git` 부재, 빌드 성공만으로 clean 또는 실기 검증 완료를 선언하지 않습니다.

## 버전과 문서 규칙

- 런타임 동작을 바꾸면 `Entrypoint.cs`와 관련 프로젝트 버전을 함께 올립니다.
- 진행 중 마일스톤의 중간 코드 변경마다 상태 문서를 갱신하지 않습니다.
- 모든 완료 조건이 사용자 VR 실기와 필요한 로그로 확인된 시점에 마일스톤 달성을 선언하고 상태·설계·변경 기록·마일스톤·인수인계를 일괄 동기화합니다.
- 문서 전용 감사에서는 런타임을 바꾸지 않고 관측된 제한을 기록할 수 있으나 미검증 항목을 해결 처리하지 않습니다.

## 안전 원칙

- `GameAssembly.dll`, `UnityPlayer.dll`, 원본 `version.dll`, 게임 에셋을 수정하지 않습니다.
- Localify 번역·폰트·텍스처·설정을 보존합니다.
- 설치 전 기존 런타임 파일을 `vrmod/rollback/`에 백업합니다.
- OpenXR 실패가 게임 실행 실패로 번지지 않게 하며 VR만 평면 패널로 폴백합니다.
- 로그에 계정 식별자, viewer ID, token 또는 실행 인증 정보를 기록하지 않습니다.
- 사용자 소유의 설정, 로그, 롤백, 게임 파일과 vendor binary를 커밋하지 않습니다.

## 라이선스

프로젝트 소스는 저장소 루트의 [MIT License](../LICENSE)를 따릅니다. OpenXR Loader, .NET Runtime, Unity Doorstop, BepInEx와 Dobby는 각자의 라이선스를 따르며 [크레딧](../CREDITS.md)과 배포 고지에 기록합니다. 게임 및 Localify 파일은 이 프로젝트의 라이선스 범위에 포함되지 않습니다.
