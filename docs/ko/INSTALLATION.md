[한국어](INSTALLATION.md) | [English](../en/INSTALLATION.md) | [日本語](../ja/INSTALLATION.md)

# 설치 방법

[프로젝트 홈](../../README.md) · [사용 방법](USAGE.md) · [프로그램 구조](ARCHITECTURE.md)

## 준비물

- Windows 11 x64
- DMM판 학원 아이돌마스터의 정상 설치본
- OpenXR를 사용할 수 있는 PC VR 환경
- v0.174.0 배포 ZIP에 포함된 Dobby 런타임 의존성

> 설치기는 Localify가 있으면 번역·폰트·텍스처·설정과 기존 `BepInEx/core/dobby.dll`을 보존합니다. Localify가 없으면 필요한 Dobby만 설치하고 Localify 파일은 새로 만들지 않습니다. 게임 폴더를 변경하기 전에 전체 패키지 해시와 필수 의존성을 검사합니다. 이 무한글패치 클린 설치 경로는 PowerShell·GUI 관리 엔진의 설치·제거 자동 검증을 통과했으며 VR 실기 검증은 아직 남아 있습니다.

## 설치

1. GitHub의 [Releases 페이지](https://github.com/deadpixel134/gakumas-vr/releases)에서 최신 정식 릴리스 ZIP을 내려받습니다.
2. 게임과 DMM 런처에서 실행 중인 `gakumas.exe`를 완전히 종료합니다.
3. ZIP을 게임 폴더가 아닌 임시 폴더에 풉니다.
4. `GakumasVR.Installer.exe`를 실행합니다.
5. `gakumas.exe`, `GameAssembly.dll`, `UnityPlayer.dll`이 있는 게임 폴더를 선택합니다.
6. 설치를 누르고 완료 메시지를 확인합니다. 설치기는 기존 파일을 `게임 폴더\vrmod\rollback\`에 백업합니다.
7. 설치기에서 **설정 열기**를 누르거나 `게임 폴더\vrmod\tools\GakumasVR.Configurator.exe`를 실행해 설정을 확인합니다.
8. 사용하는 PC VR 소프트웨어를 활성 OpenXR 런타임으로 지정한 뒤 게임을 실행합니다.

설정은 게임을 완전히 종료한 상태에서 저장해야 하며 다음 실행부터 적용됩니다.

## OpenXR 런타임

### Virtual Desktop — 실기 확인됨

1. Quest에서 Virtual Desktop으로 PC에 연결합니다.
2. Virtual Desktop Streamer에서 VDXR/Virtual Desktop OpenXR를 활성 런타임으로 지정합니다.
3. Quest 안의 데스크톱 화면에서 DMM 런처를 실행하고 게임을 시작합니다.

Virtual Desktop의 **Games** 탭이 아닌 데스크톱의 DMM 런처를 사용합니다. 이 구성에서는 SteamVR를 따로 실행할 필요가 없습니다.

### SteamVR — 예비 지원

SteamVR 설정에서 SteamVR를 활성 OpenXR 런타임으로 지정한 뒤 DMM 런처에서 게임을 시작합니다. Windows D3D11 OpenXR 경로는 규격상 호환되지만 이 프로젝트의 실기 검증은 아직 없습니다.

### Meta Quest Link/Air Link — 예비 지원

Quest Link 또는 Air Link로 PC에 연결하고 Meta Quest Link 앱에서 Meta 런타임을 활성 OpenXR 런타임으로 지정한 뒤 게임을 시작합니다. 이 프로젝트의 실기 검증은 아직 없습니다.

## 업데이트

설정 프로그램은 실행할 때와 **업데이트 확인** 버튼을 눌렀을 때 GitHub의 최신 정식 Release 태그를 현재 버전과 비교합니다. 새 버전이면 ZIP과 SHA-256을 내려받아 검증하고, 게임이 종료된 상태에서 설치한 뒤 설정 프로그램을 다시 시작합니다. 게임 실행 중에는 업데이트를 보류합니다. 수동 업데이트도 새 Release의 설치기를 같은 게임 폴더에 실행하면 됩니다. 기존 `vrmod/config/settings.json`은 보존되며 교체되는 파일의 이전 버전은 롤백 폴더에 저장됩니다.

## 제거와 롤백

1. 게임을 완전히 종료합니다.
2. 사용했던 배포 폴더의 `GakumasVR.Installer.exe`를 실행합니다.
3. 게임 폴더를 선택하고 **제거** 또는 제공되는 **롤백**을 누릅니다.

설치기는 자체 manifest에 기록된 파일만 대상으로 합니다. 설치 후 사용자가 수정한 파일은 해시가 달라지므로 임의 삭제하지 않고 경고를 남깁니다. 설치 전에 존재하던 파일은 백업에서 복원하며 `vrmod/config/settings.json`과 Localify의 `version.dll`, `gakumas-local/`은 보존합니다.

## 문제 해결

- VR가 시작되지 않으면 활성 OpenXR 런타임과 설치된 `BepInEx/core/dobby.dll` 존재 여부를 먼저 확인하십시오.
- 설정이 적용되지 않으면 게임을 종료한 상태에서 저장했는지 확인하십시오.
- 화면은 나오지만 조작되지 않으면 게임 창을 포그라운드로 두십시오.
- 실패 시 게임 창은 계속 동작하고 VR는 평면 패널로 폴백하는 것이 기본 정책입니다.
- 이슈에 `vrmod/logs/`의 로그를 첨부할 때 계정 식별자, viewer ID, token, 실행 인증 정보가 없는지 먼저 확인하십시오.
