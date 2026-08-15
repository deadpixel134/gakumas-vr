# Gakumas VR 세션 인수인계

최종 갱신: 2026-08-15
코드/설치 기준: `GakumasVR.RuntimeBootstrap` v0.175.5 로컬 후보 (정식 배포 기준 v0.174.0)
마일스톤: **M7 달성**, M8 런타임 호환성·유지보수 진행

이 문서는 새 개발 세션이 현재 사실과 안전 경계를 빠르게 복구하기 위한 기준이다. 현재 판정은 [`GAKUMAS_VR_STATUS.md`](GAKUMAS_VR_STATUS.md), 구조는 [`GAKUMAS_VR_DESIGN.md`](GAKUMAS_VR_DESIGN.md), 세부 조작 수학과 타 게임 이식 계약은 [`ko/VR_INTERACTION_SPEC.md`](ko/VR_INTERACTION_SPEC.md), 버전 이력은 [`../vrmod/CHANGELOG.md`](../vrmod/CHANGELOG.md)를 따른다.

## 0. 먼저 읽을 결론

- v0.175.5 로컬 후보는 코어 43/43, 관리 7/7, Release 빌드, 199개 패키지 manifest, 네이티브 의존성, clean install, Localify 공존·제거와 실제 설치 검사를 통과했다.
- 런타임 빌드·패키지·설치 SHA-256은 `AA13B13F4BFB5A3A988D5B801D088F4FF8C28ED1BD77A40BD373AB0CD5B06E3B`다.
- `vrmod/dist/GakumasVR-v0.175.5.zip` SHA-256은 `A9E1BBFC2129A05807270F0B4035369576D4BDB595FBC0AF7B99F3D09B4ABDD2`다.
- 사용자는 v0.173 VR 실기 결과를 “완벽했다”고 최종 승인했다. 이 승인으로 M7을 2026-08-13 달성했다.
- 기본 조작은 왼손 스틱 world-axis 시야 회전, 오른손 스틱 final-view 완전 3D 이동이다. 이동 손 설정을 바꾸면 역할이 교환된다.
- v0.174 기본 회전은 30° 스냅이며 15°/30°/45°/60°와 smooth를 선택할 수 있다. 기본 이동 속도는 1.95m/s다. 스틱 회전은 roll을 만들지 않고 실제 HMD roll 변화만 보존한다.
- 비-live 3D와 live 독립 6DoF가 기본 적용된다. live 6DoF는 연출 카메라와 독립된 진입 anchor를 쓰며 설정에서 끌 수 있다.
- v0.175.5 자동·설치 검증은 완료됐으나 공간/크기 GUI 재확인과 별도 사용자 VR 실기는 아직 없다. 조작 수학과 roll 격리의 사용자 실기 기준은 v0.173이다.
- 직접 터치와 live 좌상단 시계 회귀는 사용자 결정으로 생략했다. 구현·검증 완료로 오해하지 말고 다시 요청받기 전에는 완료 조건으로 요구하지 않는다.
- 지속 시간·반복 횟수·장시간 자원 수명 검사는 사용자 결정으로 비차단·미검증이다. 실제 관측되는 크래시와 회귀는 계속 결함이다.
- SteamVR OpenXR와 Meta Quest Link/Air Link는 예비 지원이며 이 프로젝트에서 아직 실기하지 않았다. 현재 검증 경로는 Meta Quest 2 + Virtual Desktop OpenXR다.

## 1. 저장소와 배포 기준

| 항목 | 현재 값 |
|---|---|
| 저장소 | `https://github.com/deadpixel134/gakumas-vr` |
| 런타임 소스 기준 커밋 | `cf213ef`는 v0.174 정식 기준. v0.175.5는 이 작업 트리의 로컬 후보이며 새 tag/release/공개 커밋은 아직 없음 |
| 구현 브랜치 | `feature/6dof`에서 개발 후 `main`에 반영; 현재 작업 트리 변경은 별도 확인 필요 |
| 정식 기준 | tag `v0.174.0`, stable Release, pre-release 아님. v0.175.5 ZIP은 로컬 검증 산출물 |
| 런타임 DLL | `vrmod/runtime/GakumasVR.RuntimeBootstrap.dll` |
| 배포 ZIP | `vrmod/dist/GakumasVR-v0.175.5.zip` |
| ZIP 해시 파일 | `vrmod/dist/GakumasVR-v0.175.5.zip.sha256` |
| 현재 rollback | `vrmod/rollback/product-install-0.175.5-20260815-160227/` |
| 사용자 로컬 변경 | `vrmod/config/settings.json`, `settings.json.bak` — 커밋 금지 |

배포 ZIP은 Git에 커밋하지 않고 Release asset으로만 게시한다. 런타임 구현 기준은 `cf213ef`, 문서를 포함한 공개 소스 기준은 `v0.174.0` tag로 찾는다.

## 1.1 v0.175.5 공간/크기 인수인계

- `spatial.live`/`spatial.nonLive`은 새 stereo generation에서 명시적 source 종류에 따라 고정된다. 기본 100% 자동은 v0.174 동작과 같고, 자동 배율은 크기의 역수다.
- 각 프로필은 눈 간격·머리 이동·스틱 이동을 0.00~4.00 수동 multiplier로 독립 override할 수 있다. 전역 물리 IPD 기준 `render.worldEyeOffsetScale`은 그대로 유지한다.
- 기존 사용자 `settings.json`은 보존했다. 여기의 legacy `render.worldScale`은 런타임에서 무시된다. `spatial`이 없는 설정은 기본 100% 자동으로 해석되며, 설정 프로그램에서 저장하면 새 구조가 기록된다.
- 다음 사용자 검증은 설정 GUI의 공간/크기 탭 행 배치, 100% 자동 회귀, live/non-live 서로 다른 프로필, 세 수동 override, 평면 패널/UI 입력/장면 전환/VR 폴백이다. 이 항목들은 아직 완료로 기록하지 않는다.

## 2. 안전·공존 불변식

- `GameAssembly.dll`, `UnityPlayer.dll`, 원본 `version.dll`, 게임 에셋을 수정하지 않는다.
- Localify의 번역, 폰트, 텍스처, 설정과 사용자 파일을 보존한다.
- 게임 실행 중 runtime DLL이나 설치 파일을 교체하지 않는다.
- 설치 전 기존 소유 파일을 `vrmod/rollback/`의 버전·timestamp 디렉터리에 보관한다.
- 제거는 설치 manifest가 소유하고 설치 당시 해시와 같은 파일만 삭제한다. 수정됐거나 알 수 없는 파일은 남긴다.
- VR/OpenXR가 실패해도 게임은 창모드로 계속 실행되어야 한다. VR은 정면 패널 또는 비활성으로 폴백한다.
- 로그에 계정 식별자, viewer ID, token, URL query 또는 실행 인증 정보를 남기지 않는다.
- 게임 업데이트로 핵심 파일 기준선이 바뀌면 자동 승인하지 않는다.

## 3. 사용자에게 보이는 현재 동작

### 표시

- fresh stereo가 없는 UI-only, 영상, 로딩, 폴백 문맥은 최종 게임 backbuffer 전체를 HMD 정면 1.6m quad로 자동 표시한다.
- 승인된 3D source가 fresh stereo를 만들면 정면 패널을 제거하고 projection world를 표시한다.
- 3D 안의 전체 게임 UI는 패널 손 Grip으로 켜고 끄는 손 패널을 사용한다. 기본 패널 손은 왼손이고 시작은 OFF다.
- 손 패널은 tracking과 손 HMD FOV가 유효할 때만 보이며 100ms 이탈 hysteresis를 쓴다. OFF에서는 관련 copy/acquire/write/submit과 pointer hit-test를 생략한다.
- 패널 위치·크기·회전·viewer-facing, 패널 손·포인터 손과 버튼은 설정 GUI에서 변경할 수 있다.
- stereo source를 잃으면 이전 eye texture를 즉시 버리고 정면 패널로 자동 복귀한다.

### UI 조작

- 기본 오른손 aim ray를 표시 중인 패널 plane과 교차시켜 UV를 foreground 게임 client 좌표로 변환한다.
- 원형 VR cursor를 표시한다. Primary face는 click/drag, trigger는 pre-press 좌표 latch 뒤 click/drag, secondary face는 back/Escape다.
- trigger 당김의 미세한 aim 흔들림을 줄이기 위해 trigger가 깊어지기 전 좌표를 먼저 고정한다. 흔들림이 싫으면 primary face button을 쓴다.
- foreground가 게임 창이 아니면 입력을 주입하지 않으며 button-up을 보장한다.
- VR 환경에서 thumbstick scroll은 쓰지 않는다. 두 스틱은 이동과 시야 회전에 사용한다.
- 직접 터치는 지원하지 않는다.

### 6DoF 이동과 회전

- 기본 오른손 스틱은 현재 최종 HMD 시야의 forward/right를 사용한다. pitch를 제거하지 않으므로 위·아래를 보며 전진하면 상승·하강한다.
- 기본 왼손 스틱은 월드 yaw/pitch를 회전한다. 대각 입력은 큰 축 하나만 선택해 의도하지 않은 사선 회전을 막는다.
- snap은 기본 30°이고 중앙 deadzone으로 돌아오기 전에는 반복하지 않는다. smooth는 설정한 deg/s를 frame delta에 적분한다.
- physical HMD 위치, controller locomotion offset, eye offset은 같은 roll-free navigation basis에서 합성한다.
- live 6DoF ON에서는 scene camera cut/path와 무관한 독립 origin을 유지하고 source generation이 바뀌면 안전하게 다시 잡는다.

## 4. roll 처리 — 절대 훼손하지 말 것

OpenXR pose를 Unity 좌표로 바꾼다.

```text
positionUnity = (x, y, -z)
rotationUnity = (-x, -y, z, w)
```

VR 진입 또는 generation 전환 때 HMD absolute orientation의 yaw, pitch, roll을 각각 origin으로 저장한다. 매 프레임 현재 absolute orientation에서 같은 세 성분을 구해 origin 대비 변화량을 계산한다.

```text
physicalYawDelta   = currentYaw   - originYaw
physicalPitchDelta = currentPitch - originPitch
physicalRollDelta  = currentRoll  - originRoll

finalYaw   = baseYaw   + artificialYaw   + physicalYawDelta
finalPitch = clamp(basePitch + artificialPitch + physicalPitchDelta)
finalRoll  = physicalRollDelta
finalRotation = Yaw(finalYaw) * Pitch(finalPitch) * Roll(finalRoll)
```

scene base에서는 yaw/pitch만 추출하고 roll을 버린다. 스틱 artificial rotation도 yaw/pitch scalar로만 저장한다. HMD가 실제로 기울어진 변화량은 `physicalRollDelta`로 보존한다.

raw relative HMD quaternion을 artificial/scene quaternion에 통째로 곱하지 않는다. 기울어진 origin에서 HMD yaw가 relative roll로 표현되어 스틱 회전 뒤 수평선이 기울 수 있다. v0.172까지 남았던 회귀가 바로 이 경로였고 v0.173에서 성분별 분리로 제거됐다.

세부 공식, unwrap/축 추출, 상태기와 이식 계층은 [`ko/VR_INTERACTION_SPEC.md`](ko/VR_INTERACTION_SPEC.md)가 권위 문서다.

## 5. 런타임 구조

```text
Doorstop Entrypoint (.NET 6)
  ├─ IL2CPP public export와 Unity main-thread frame hook
  ├─ scene/source classifier와 generation lifecycle
  ├─ stereo clone cameras + RenderTexture registry
  ├─ D3D11 Present/backbuffer capture
  ├─ OpenXR projection/quad submit worker
  ├─ Touch action set, panel ray input, locomotion/view-turn state
  └─ versioned settings + failure isolation

Configurator.exe
  ├─ 한국어 기본 / 영어 / 일본어
  ├─ 설정 검증·원자 저장·가져오기/내보내기
  └─ GitHub latest stable Release 확인·검증·설치기 handoff

Installer.exe / Management
  ├─ package manifest preflight
  ├─ game-running guard
  ├─ Localify-aware install
  └─ owned-file uninstall + rollback
```

핵심 소스:

| 파일 | 역할 |
|---|---|
| `Entrypoint.cs` | runtime entry/version, IL2CPP와 failure boundary |
| `MainThreadSampler.cs` | scene/source state, stereo camera, 6DoF composition |
| `OpenXrProbe.cs` | OpenXR session, pose와 quad/projection submit |
| `OpenXrControllerActions.cs` | action set, ray/button/stick 상태 |
| `VrViewTurnIntegrator.cs` | deadzone, 우세축, snap/smooth artificial yaw/pitch |
| `VrSettings.cs` | schema/default/range validation |
| `UpdateService.cs` | GitHub stable Release, ZIP/SHA 검증 |
| `InstallationEngine.cs` | manifest install/uninstall/rollback |

## 6. 설정 기준

- `settings.default.json`이 배포 기본값이고 사용자 `settings.json`은 설치·업데이트에서 보존한다.
- eye render scale 기본 0.65, 허용 0.50~2.00, invalid fallback 0.75다. 1.00 초과는 성능·VRAM 경고 대상이다.
- world eye offset scale 기본 0.275다.
- live 6DoF 기본 ON, locomotion 기본 ON, 이동 손 right, 속도 1.95m/s다.
- view turn 기본 snap 30°, smooth 기본 속도 90°/s다.
- 패널 손 left, 포인터 손 right, 시작 OFF, Grip toggle, viewer-facing ON이다.
- VFX 기본 preset은 VL bloom 1.40, diffusion 최소, depth of field/texture blur OFF를 기준으로 한다. 사용자는 manual 항목을 개별 변경할 수 있다.

## 7. 빌드·검증·패키징

저장소 루트에서 실행한다.

```powershell
.\vrmod\scripts\Build-VRMod.ps1
dotnet run --project .\vrmod\tests\GakumasVR.Core.Tests\GakumasVR.Core.Tests.csproj -c Release
dotnet run --project .\vrmod\tests\GakumasVR.Management.Tests\GakumasVR.Management.Tests.csproj -c Release
.\vrmod\installer\Build-Package.ps1 -Version 0.174.0
```

판정은 서로 분리한다.

1. 소스 빌드 성공
2. 자동 테스트 성공
3. 패키지 manifest/네이티브/clean install 성공
4. 실제 게임 폴더 설치와 해시 일치
5. PC 실행
6. 사용자 VR 실기

앞 단계가 뒤 단계를 대신하지 않는다. v0.174는 1~4를 통과했고 v0.173은 1~6을 모두 통과했다. v0.174 PC·VR 사용자 실기는 아직 진행하지 않았다. 안정성 전용 장시간·반복 검사는 비차단·미검증이다.

## 8. 자동 업데이트 계약

- 설정기는 GitHub API의 최신 non-draft, non-prerelease Release를 확인한다.
- 현재 버전보다 새 semantic version일 때만 업데이트를 제안/진행한다.
- 예상 이름의 ZIP과 `.sha256` asset을 찾고 최대 크기, HTTP 길이, SHA-256과 archive 구조를 검증한다.
- 게임 실행 중에는 설치하지 않고 종료 후 다시 시도하도록 한다.
- 검증된 archive만 설치기로 넘기며 설치기는 다시 manifest preflight와 rollback을 수행한다.
- 네트워크·API·asset 검증 실패는 기존 설치를 변경하지 않는다.

## 9. 현재 미검증·보류

- SteamVR OpenXR 실제 장치 회귀
- Meta Quest Link/Air Link OpenXR 실제 장치 회귀
- 게임 업데이트 뒤 새 핵심 파일 기준선 승인
- 장시간 HMD 지속, 반복 횟수, GPU/메모리 누수 추세 — 사용자 지시로 비차단·미검증
- 직접 터치 — 사용자 결정으로 생략
- live 좌상단 시계 회귀 — 사용자 결정으로 생략
- 스킬 사용 중 최상위 2D 이미지 누락 — 사용자가 디버깅을 다시 요청할 때만 착수

## 10. 다음 세션 순서

1. 이 문서와 상태·설계·마일스톤·CHANGELOG를 읽는다.
2. `git status --short`, 현재 branch/tag/remote를 확인하고 사용자 설정 변경을 건드리지 않는다.
3. `Entrypoint.cs`, 각 csproj 버전, runtime DLL ProductVersion과 설치/배포 SHA를 대조한다.
4. 게임 핵심 파일 기준선을 읽기 전용으로 확인한다. 불일치는 clean 판정이 아니다.
5. M8 작업은 SteamVR/Quest Link 실기 또는 게임 업데이트 호환성 gate 중 사용자가 지정한 범위만 진행한다.
6. runtime 동작이 바뀌면 버전을 함께 올리고, 사용자 VR 실기 전에는 검증 완료로 선언하지 않는다.

## 11. 현재 릴리스 체크리스트

- [x] v0.174 runtime/source version 일치
- [x] Core 39/39, Management 7/7
- [x] Release build와 199 manifest 검증
- [x] clean install/Localify 공존·제거 검증
- [x] runtime/ZIP SHA-256 확정
- [ ] v0.174 사용자 VR 실기 — 미실시, v0.173 “완벽” 승인을 회귀 기준으로 유지
- [x] 문서 동기화
- [x] `feature/6dof`를 `main`에 반영
- [x] annotated `v0.174.0` tag
- [x] GitHub stable Release에 ZIP과 `.sha256` 게시
