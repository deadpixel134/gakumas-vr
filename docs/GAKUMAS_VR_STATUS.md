# 학원 아이돌마스터 VR 모드 — 현재 상태와 인수인계

최종 갱신: 2026-08-15
현재 소스 버전: v0.175.6 (현재 공개 정식 릴리스)
현재 설치 버전: v0.175.6
실기 기준 버전: v0.173.0(6DoF 이동·월드축 회전·roll 분리), v0.154.0(M6 표시·입력), v0.143.0(M5 topology), v0.141.0(M4 성능), v0.131.0(최종 시각), v0.90.0(UI 회귀)
테스트 상태: v0.175.6 코어 43/43·관리 7/7·Release 빌드·199개 패키지 manifest·클린 설치·Localify 공존/제거·실제 설치 해시 일치. 공간/크기 GUI 재확인과 사용자 VR 실기는 미수행; v0.173 사용자 VR 실기 성공 및 “완벽” 판정
현재 마일스톤: **M7 달성 — 2026-08-13**, M8 런타임 호환성·유지보수 진행
현재 변경: live/non-live별 캐릭터/월드 지각 크기와 자동·수동 눈 간격/머리 이동/스틱 이동 배율 후보를 추가했다. 기존 live 독립 6DoF, 기본 왼손 월드축 30° 스냅, 오른손 완전 3D 시야 기준 이동 1.95m/s, physical HMD roll 보존은 유지한다.

이 문서는 다음 작업자가 가장 먼저 읽어야 하는 현재 상태의 기준 문서다. 목표 구조는 `GAKUMAS_VR_DESIGN.md`, 변경 내역은 `../vrmod/CHANGELOG.md`를 참고한다.

## 1. 목표 환경

| 항목 | 현재 기준 |
|---|---|
| 게임 | 학원 아이돌마스터 DMM PC판 |
| 엔진 | Unity 6000.0.77f1 |
| 스크립팅 | IL2CPP, metadata 31.1 |
| 그래픽 | Direct3D 11, URP |
| HMD | Meta Quest 2 실기 |
| 주 런타임 | Virtual Desktop OpenXR (Bundled) |
| 예비 런타임 | SteamVR OpenXR, Meta Quest Link OpenXR — 아직 회귀 테스트 전 |
| 게임 표시 | 창모드, 세로/가로 전환 빈번 |
| 공존 대상 | Gakumas Localify 한글 패치 |

2026-08-02 게임 업데이트에서 Unity 2022.3.57f1이 6000.0.77f1로, IL2CPP metadata가 31.1로 변경됐다. 구형 BepInEx/Cpp2IL interop은 metadata 또는 code/metadata registration 탐색에 실패하므로 현재 제품 경로에서 사용하지 않는다.

## 2. 현재 설치 상태

- 설치 런타임: `vrmod/runtime/GakumasVR.RuntimeBootstrap.dll` v0.175.5
- v0.175.5 빌드/패키지/설치 SHA-256: `AA13B13F4BFB5A3A988D5B801D088F4FF8C28ED1BD77A40BD373AB0CD5B06E3B`
- 배포 ZIP SHA-256: `A9E1BBFC2129A05807270F0B4035369576D4BDB595FBC0AF7B99F3D09B4ABDD2`
- 현재 중첩 롤백: `vrmod/rollback/product-install-0.175.5-20260815-160227/`; v0.174, v0.173과 이전 제품 설치본도 버전별 보관
- 설치기: 배포 루트 `GakumasVR.Installer.exe`, PowerShell 인터페이스 `vrmod/installer/Install-GakumasVR.ps1`
- 설치기는 전체 payload를 쓰기 전에 검증하고 `gakumas.exe` 실행 중 교체를 거부한다. 제거는 manifest가 소유한 동일 해시 파일만 대상으로 하며 사용자 설정·Localify·수정된 파일을 보존한다.

### v0.175.5 공간/크기 후보

- `spatial.live`와 `spatial.nonLive`는 각각 캐릭터/월드 지각 크기와 눈 간격·머리 이동·스틱 이동 보정 프로필을 가진다. 지원 몰입형 장면에만 적용되며 평면 정면/손 패널에는 적용하지 않는다.
- 기본 100%와 자동 모드는 v0.174의 배율과 같아야 한다. 자동 보정은 크기의 역수이며, 수동 모드는 각 항목을 독립 조절한다. 전역 eye offset 기준값은 기존 `render.worldEyeOffsetScale`이다.
- 이전 실패 실험의 `render.worldScale`은 무시된다. 기존 사용자 `settings.json`을 설치기가 덮어쓰지 않으므로, 남아 있는 legacy 항목은 안전하지만 새 공간 프로필을 쓰려면 설정 프로그램에서 저장해야 한다.
- 설정 GUI의 공간/크기 탭과 표준 탭의 행 배치는 소스에서 수정·패키지화했으나, 실제 화면 배치와 live/non-live VR 효과는 아직 사용자 실기로 확인하지 않았다. 따라서 M8 및 기능 검증 완료로 판정하지 않는다.

### v0.173 승인 조작 기준

- 기본 왼손 스틱은 world yaw/pitch 회전, 오른손 스틱은 최종 시야 방향의 완전한 3D 이동이다. 설정의 이동 손을 바꾸면 역할이 자동 교체된다.
- 기본 회전은 15° 스냅이며 15°/30°/45°/60°와 부드러운 회전을 선택할 수 있다. 우세한 한 축만 처리하고 중앙 복귀 전에는 스냅을 반복하지 않는다.
- 게임 카메라의 roll과 VR 진입 당시 HMD 기울기는 제거한다. 현재 HMD absolute pose의 yaw/pitch/roll을 원점과 각각 비교하여 실제 physical roll 변화량만 마지막에 적용하므로 스틱 회전은 roll을 생성하지 않는다.
- 위·아래를 보며 이동하면 최종 시야에 따라 상승·하강한다. 라이브 6DoF는 옵션이며 활성화하면 연출 카메라 경로와 독립된 진입 anchor를 사용한다.
- 구현 계약과 다른 게임 이식 기준은 [`ko/VR_INTERACTION_SPEC.md`](ko/VR_INTERACTION_SPEC.md)에 정리했다.

### v0.174 기본 프로필 변경

- 신규 설정과 설정 GUI의 기본값 복원은 live 독립 6DoF를 ON으로 시작한다.
- 기본 스냅 턴은 30°이며 15°/30°/45°/60°와 smooth 선택은 유지한다.
- 기본 이동 속도는 1.50m/s의 1.3배인 1.95m/s다.
- 업데이트는 기존 `settings.json`을 보존한다. 현재 실사용 설정은 live 6DoF ON과 속도 1.95m/s로 맞췄고, 누락된 스냅 값은 v0.174 schema 기본 30°로 해석된다.
- 이 변경은 자동 테스트·패키지·실제 설치까지 완료했으나 v0.174 별도 사용자 VR 실기는 아직 진행하지 않았다. 조작 수학과 roll 격리의 실기 기준은 v0.173이다.

v0.100 두 번째 실기 PID 25368에서 live generation 3/4/5가 약 44.3초/40.3초/38.0초 지속되어 M2의 재진입·30초 조건을 충족했다. v0.101 PID 30636에서는 서로 다른 live source 3회와 자동 portrait 1픽셀 nudge/원복 2회가 failure 없이 기록됐고, 사용자가 수동 리사이즈 없는 정상 복귀를 확인했다. 따라서 M2를 2026-08-09 달성했다.

M3는 사용자가 장시간·자원 수명 계측을 완료 조건에서 제외하고 시각·UI 기준으로 축소한 뒤 2026-08-10 달성했다. M4는 v0.141에서 게임 속도 stereo와 near-120Hz OpenXR submit을 사용자 승인 기준으로 확정해 같은 날 달성했다. M5는 v0.142/v0.143의 홈·메뉴·커뮤니케이션·custom video topology 실기와 당시 표시 정책 확정으로 같은 날 달성했다. M6는 v0.150~v0.154에서 자동 정면 패널, stereo 왼손 Grip 보조 패널, 오른손 범용 입력과 전환을 구현하고 사용자 VR 실기 및 v0.154 로그로 확인해 2026-08-11 달성했다. M7은 v0.155~v0.173의 다국어 설정·설치·자동 업데이트와 6DoF 조작을 v0.173 사용자 실기로 최종 승인해 2026-08-13 달성했다. 사용자는 직접 터치와 live 좌상단 시계 회귀, 모든 안정성 전용 테스트를 완료 조건에서 제외했으며, 생략된 항목은 검증 완료가 아니라 비차단·미검증으로 남는다. 현재 단계는 M8 런타임 호환성·유지보수다.

2026-08-09 인수인계 재검사에서 `Verify-Baseline.ps1`은 `gakumas.exe`, `GameAssembly.dll`, `global-metadata.dat` 변경으로 실패했다. `UnityPlayer.dll`, Localify `version.dll`과 설정 파일은 일치한다. 이는 새 기준선 승인을 대신하지 않는다. 사용자가 현재 설치본에서 임시 호환성 테스트 진행을 명시적으로 승인했으므로 v0.96 이후 실기는 진행하되, 결과를 승인된 제품 기준선 검증과 구분해 기록한다. 상세 현재 해시와 인수인계 순서는 `VR_HANDOFF.md`를 따른다.

v0.86 로그의 `stereo-publish-rate`는 약 19.4~20.7 pair/s, 마지막 publish 간격 32~63ms, Present delta 2~3을 기록했다. 목표 30fps에는 미달하지만 사용자는 프레임 출력이 정상적으로 보인다고 판정했다. v0.85의 약 2fps 병목은 해소됐다.

## 3. 로더와 런타임 구조

현재 모드는 BepInEx interop을 사용하지 않는다.

```text
Doorstop Entrypoint (.NET 6)
  ├─ 공개 IL2CPP export로 타입/메서드 탐색
  ├─ Dobby로 UnityEngine.Time.get_frameCount icall 후킹
  │    └─ Unity 메인 스레드에서 장면/카메라/Canvas 조작
  ├─ D3D11 device/context/swapchain/Present 후킹
  │    └─ GPU fence, 백버퍼 및 RenderTexture 복사
  └─ 별도 OpenXR 프레임 루프
       ├─ 평면: XrCompositionLayerQuad
       └─ 스테레오: XrCompositionLayerProjection
```

핵심 파일:

| 파일 | 역할 |
|---|---|
| `vrmod/src/GakumasVR.RuntimeBootstrap/Entrypoint.cs` | Doorstop 시작, IL2CPP API, 로그 모델, 버전 |
| `MainThreadSampler.cs` | 장면 탐색, 카메라/RT/UI 상태 머신과 Unity 호출 |
| `D3D11DeviceCapture.cs` | D3D11 장치와 Present 후킹, Present serial |
| `D3D11Interop.cs` | 텍스처 복사/검사/저장, GPU event query |
| `OpenXrProbe.cs` | VD OpenXR 세션, quad/projection layer 제출 |
| `UnityRenderSourceRegistry.cs` | Unity 생산 텍스처와 OpenXR 소비 스레드 사이 lease |

## 4. 검증 완료 기능

### 부트스트랩과 공존

- 게임과 Localify를 유지한 채 독립 Doorstop 런타임이 로드된다.
- Unity 메인 스레드에서 IL2CPP API 호출이 가능하다.
- VR/OpenXR 실패가 게임 실행 실패로 이어지지 않는다.
- 게임 업데이트 후 구형 BepInEx가 metadata 31을 처리하지 못한 원인과 복구 경로를 확인했다.

### 장면과 방향

- `Splash`, `Title`, `OutGame`, `Produce`, `Live`, 여러 `env_3d_*` 장면을 실측했다.
- 실제 렌더 크기는 세로 `1080x1920`, 라이브 가로 `1920x1080`으로 전환된다.
- `Screen.orientation`은 전환 뒤에도 값 1을 유지할 수 있으므로 실제 너비/높이 비율을 주 신호로 사용한다.
- 세로 → 가로 → 세로 전환은 Quest 패널에서 정상 동작했다.

### VRSCENE-001 — 비-live 화면 VR 적용 확대

상태: **v0.154 사용자 실기 완료, M6 달성**

- v0.142 PID 32908에서 홈/상점/프로듀스 준비/스토리 선택/세로·가로 ADV를 조사했다. 홈 world는 `Game3DManager → _VLTargetTexture_2160x3840 → .../3dTargetImage`이며 UI는 별도 `UICamera` Canvas다.
- 가로 ADV는 `_VLTargetTexture_3840x2160`, 세로 ADV는 `_VLTargetTexture_2160x3840`을 `.../Main Layer/Render Target` RawImage가 표시한다. choices/content/player-control Canvas는 `UICamera`, background Canvas는 `Game3DManager`에 결합된다.
- UI-only 메뉴에서도 `Game3DManager`가 남을 수 있으므로 scene/camera 이름이 아니라 활성 world-presenting RawImage, RT identity·크기와 screen context를 함께 전환 신호로 사용한다.
- v0.143 PID 2988은 최대 Canvas 101/RawImage 195를 failure 0으로 기록했다. Unity `VideoPlayer` 대신 `Campus.Common.CampusVideoPlayer`가 홈 모니터와 가샤 배경의 `OnDemandVideoPlayerImage`에서 활성화되는 actual custom video path를 확인했다.
- 2026-08-11 사용자 결정으로 M6 정책을 다시 구체화했다. fresh stereo가 없는 완전 평면 문맥에서는 Localify·UI·영상·시계를 포함한 최종 백버퍼를 시야 정면 1.6m에 자동 표시한다. Grip OFF나 손 추적에 의존하지 않아 검은 공간만 남지 않는다.
- fresh stereo가 있는 3D 문맥에서는 정면 패널을 제거하고 stereo world를 유지한다. 기본 왼손 보조 패널은 시작 OFF이고 Grip press edge로 토글하며, tracking·HMD 손 FOV와 100ms hysteresis를 적용한다. v0.154 패널 중심은 controller local +Y 0.10m의 상단 끝에 있고 view-space에서 수직으로 플레이어를 향한다. OFF에서는 관련 GPU copy/acquire/write/submit과 pointer hit-test를 생략한다.
- v0.150은 기본 오른손 aim ray를 정면/손 패널 UV와 foreground game client 좌표에 연결했다. v0.151에서 잘못된 `XR_TYPE_ACTION_STATE_GET_INFO` 값을 58로 고치고 액션별 실패를 격리한 뒤, 별도 원형 cursor quad, A click/drag, pre-press 좌표 latch를 적용한 trigger click/drag, B→Escape, thumbstick Y→wheel을 사용자 실기로 확인했다. 게임 창이 foreground가 아니면 입력하지 않는다.
- v0.154 PID 31424에는 `controller-pointer-input-ready`, `hand-panel-enabled/disabled`, 손 FOV에 따른 `hand-panel-visible/hidden`, `front-panel-mode-entered/exited`가 모두 기록됐다. 사용자는 평면 표시·종횡비·모든 입력·그립 토글·3D 이탈 자동 복귀와 최종 패널 위치를 정상 판정했다.
- 직접 터치는 사용자 결정으로 현재 제품 범위에서 제외했다. 패널 손·포인터 손·버튼과 패널 배치·viewer-facing은 설정 GUI에서 변경할 수 있다. keyed UI-only 합성과 live one-shot alpha UI는 제품 기본 경로가 아니며 stereo 추출이 불명확하면 정면 패널로 자동 폴백한다.

### Virtual Desktop 평면 패널

- SteamVR을 실행하지 않고 Virtual Desktop의 Quest 데스크톱에서 게임을 실행한다.
- VD 게임 탭이 아니라 VD 데스크톱에서 실행하는 것이 현재 검증 경로다.
- D3D11 게임 백버퍼를 OpenXR quad layer로 제출한다.
- 세로/가로 패널 방향, 상하 반전, 색공간/명부 날림 보정을 실기 완료했다.
- Localify 번역 UI와 UI ON/OFF를 포함한 최종 합성 화면은 평면 패널에서 정상이다.

### OpenXR 양안 데이터

- 런타임: `VirtualDesktopXR (Bundled)`
- view state flags: 15
- 실측 물리 IPD: 약 0.061 m
- 좌/우 eye 위치: 약 -0.0305 m / +0.0305 m
- 눈별 비대칭 FOV를 `Matrix4x4.Frustum`에 반영한다.
- 게임 월드 카메라의 눈 간격은 v0.96부터 물리 eye offset의 27.5%를 사용한다. 과거 25%는 실기에서 양호했고 22.5%가 v0.71~v0.95 기준이었다. 27.5%는 깊이감이 강해지는 대신 융합 피로 가능성이 있어 아직 실기 판정 전이다.
- 좌우 이미지 순서는 원래 매핑이 맞다. v0.69의 eye swap은 정렬을 크게 악화시켰다.

### 연속 스테레오 — v0.82 실기 성공

현재 안정 경로는 수동 `Camera.SubmitRenderRequestsInternal`이 아니다.

1. 원본 `Game3DManager` 카메라 속성과 명시적인 `UniversalAdditionalCameraData` 속성을 좌/우 clone 카메라에 복사한다.
2. 눈별 위치와 비대칭 projection을 설정한다.
3. clone 카메라를 Unity의 정상 카메라 렌더 루프에 넣는다.
4. 실제 게임 Present 두 번을 기다린다.
5. 양쪽 GPU 작업 완료 후 두 눈을 한 쌍으로 검증한다.
6. 표시 중인 eye texture와 다음 렌더 대상을 이중 버퍼로 분리한다.
7. 완성된 쌍만 OpenXR Projection Layer에 교체한다.

v0.82 사용자 실기 결과:

- 화면이 연속으로 움직임
- 스테레오 깊이 정상
- 조명/이펙트가 생겼다 사라지는 현상 해결
- 좌우 동기화 양호
- 별도 크래시나 조작 지연 보고 없음

기본 eye RenderTexture는 Quest 권장 2688x2880의 65%인 1744x1872로 생성된다. v0.162부터 설정 schema와 GUI는 `eyeRenderScale`의 `0.50~2.00` 범위를 허용하고 1.00 초과에서 성능·VRAM 경고를 표시하며, 누락·손상·범위 밖 값은 `0.75`로 폴백한다. v0.93부터 30초 생산 제한은 제거됐고 v0.100에서 같은 프로세스의 여러 라이브 재진입을 확인했다. 장시간 자원 수명과 누수 계측은 사용자 지시에 따라 비차단·미검증으로 남는다.

## 5. 확정된 실패 경로와 이유

| 경로 | 결과 | 결론 |
|---|---|---|
| BepInEx pre.2 | metadata 31 미지원 | 사용 중단 |
| BepInEx be.785 + Cpp2IL | metadata 31.1 파싱 후 registration 탐색 실패 | interop 생성에 의존하지 않음 |
| 반복 URP UI render request | PC 플리커링 | 제품 경로 금지 |
| 원본/clone 카메라 수동 `SubmitRenderRequestsInternal` 반복 | 이펙트/후처리가 프레임마다 생겼다 사라짐 | 정상 Unity 렌더 구간 밖의 전역 렌더 상태가 불완전함 |
| 좌우 eye texture를 쓰면서 동시에 OpenXR가 읽음 | 잠재적 좌우 불일치 | 이중 버퍼 + GPU fence 필요 |
| 독립 clone Camera만 생성 | 검은 출력 | URP 추가 카메라 데이터가 필요 |
| `JsonUtility.FromJsonOverwrite`로 URP 데이터 복사 | 게임 metadata에 메서드 없음 | 명시적 속성 복사 사용 |
| CanvasRenderer mesh replay | 58 draw 명령은 기록되나 결과 RT가 투명/빈 화면 | 커스텀 UI 셰이더·마스크·렌더 컨텍스트 재현 실패, 사용 중단 |
| 좌우 이미지 강제 swap | 위치/방향 크게 불일치 | 원래 eye mapping 유지 |

v0.80에서 한 쌍을 30초 고정했을 때 화면은 완전히 안정적이었다. 따라서 과거 이펙트 점멸은 OpenXR Projection Layer와 평면 폴백이 번갈아 나온 문제가 아니라 반복 수동 렌더 내용 자체의 문제였다.

v0.81에서 clone 카메라를 Unity 정상 렌더 루프에 0.3초 참여시키고 한 쌍을 고정했을 때 정상 조명·이펙트가 포함됐다. 이 결과가 v0.82 경로의 근거다.

## 6. 현재 미해결 문제

### VRUI-001 — 몰입형 스테레오에서 2D UI 미표시

상태: v0.90 VR UI 실기 성공

- v0.83은 CanvasRenderer 58개 요소를 투명 RT에 동적으로 재생하고 OpenXR의 두 번째 alpha quad layer로 제출했다.
- 로그에는 draw 성공이 기록됐지만 저장된 `vrmod/logs/v0.60-ui-element-replay.bmp`는 완전히 빈 화면이었다.
- 사용자 실기에서도 UI가 표시되지 않았다.
- v0.84는 원본 `UICamera`를 투명 RT로 두 번의 정상 Present 동안 리디렉션하고 `LiveFullScreenRoot/VisbleRoot/3DTexture` RawImage만 cull한 뒤 원상 복구하도록 구현했다.
- v0.84 실기에서는 UI가 표시되지 않았다. `19:23:57.508`에 `ui-natural-capture-armed`가 기록됐고 약 109ms 뒤 `The normal-render-loop UI texture contained no visible pixels.`로 실패했다.
- 실패 시 `UpdateLiveUiTexture`가 호출되지 않으므로 OpenXR UI quad는 생성·제출되지 않는다. 따라서 이번 결과는 헤드셋에 제출된 UI가 FOV 밖에 놓인 현상이 아니다.
- UI quad가 제출될 경우 1920x1080 소스는 정면 1.58m에 약 1.8x1.0125m로 배치되어 시야각이 약 59.3°x35.5°다. 같은 실행에서 측정한 눈별 OpenXR FOV는 약 94°x98°이므로 정면을 보는 조건에서는 화면 가장자리까지 FOV 안에 들어간다.
- 다만 UI가 가장자리에만 있는 점은 다른 방식으로 영향을 줄 수 있다. 현재 `HasVisiblePixels`는 1920x1080 전체를 16x16의 256개 점으로만 검사하며 첫 샘플은 대략 (60, 33)이다. 얇은 가장자리 UI를 샘플이 모두 비껴가면 실제 내용이 있어도 빈 RT로 오판한다.
- 라이브 다시보기는 최초 진입 시 UI 숨김 상태로 시작한다. one-shot 캡처가 UI ON 전에 실행됐을 가능성도 높으며, 현재 실패 후 `_uiCaptureFrameCount=-1`이 되어 재시도하지 않는다.

다음 UI 진단은 캡처 RT 전체 저장과 alpha/RGB 점유율 측정을 먼저 추가하고, UI ON 이후 재캡처 및 빈 결과 재시도를 허용해야 한다. 이 진단은 30fps 변경과 결과가 섞이지 않도록 별도 버전으로 진행한다.

v0.86 실기 결과:

- UI 내용 자체는 정상적으로 보였지만 1920x1080 전체가 불투명 검정으로 제출되어 화면 중앙을 가렸다.
- 저장된 32-bit 진단 BMP의 32x32 alpha 표본 1,024개가 모두 255였다.
- `ui-natural-capture-ready`가 기록됐고, 이후 같은 one-shot texture를 계속 `TouchLiveUiTexture`했기 때문에 게임에서 UI를 꺼도 VR에서는 남았다.
- 검정 영역에는 `LiveFullScreenRoot/Background`와 `OverlayRoot/FullScreen/FadeRoot/BlackTint`가 포함될 수 있으며, 기존 코드는 3DTexture만 cull했다.

v0.87 구현:

- 캡처 중 3DTexture, `LiveFullScreenRoot/Background`, `FadeRoot/BlackTint`의 원래 cull 상태를 저장하고 함께 제외한 뒤 복구한다.
- `UICanvasGroup`의 GameObject 활성 상태와 alpha를 100ms 진단 주기에서 감시한다. alpha 0이면 UI texture를 registry에서 해제하고, alpha 1이면 새 one-shot 캡처를 요청한다.
- 새 진단 이미지는 `v0.87.0-ui-natural-capture.bmp` 형식으로 저장한다.
- 아직 one-shot이므로 UI가 켜진 동안 시간 표시 등 프레임 단위 내용은 고정될 수 있다. 이번 검증은 투명 배경과 ON/OFF 수명에 한정한다.

v0.87 실기 결과와 v0.88 수정:

- `ui-natural-visibility-shown`까지는 정상이며 스테레오는 약 19~21 pair/s를 유지했다.
- UI 캡처 arm 전에 `Unexpected Unity object array length: 1021.`로 실패했다. 배경 Graphic 두 개를 찾기 위해 base `Graphic` 전체를 기본 한도 512로 열거한 것이 원인이다.
- v0.88은 base Graphic 대신 `UnityEngine.UI.Image`만 최대 2,048개 탐색한다.
- 캡처 실패 시 UI registry를 비우고 `_uiCaptureFrameCount=0`을 유지해 2초 후 재시도한다. 일시적 실패가 영구 미표시 상태로 굳지 않는다.

v0.88 실기 결과:

- 첫 캡처는 가시 픽셀 없음으로 실패했지만, 2초 재시도에서 3개 배경 Graphic을 억제하고 `ui-natural-capture-ready`까지 도달했다.
- VR에서는 여전히 불투명 검은 배경이 UI와 함께 화면을 가렸고, 게임 UI를 OFF해도 해당 레이어가 남았다.
- `v0.88.0-ui-natural-capture.bmp`의 32x32 표본은 alpha 1,024개가 모두 255였고, 1,024개 중 999개가 거의 검은 불투명 픽셀이었다. UICamera/URP 출력이 배경 Graphic cull 뒤에도 alpha를 불투명하게 기록하므로 특정 Graphic 추가 제외만으로는 해결할 수 없다.
- `ui-natural-visibility-hidden` 이벤트가 없었다. 상위 `UICanvasGroup` 자체의 alpha/활성 상태는 실제 라이브 오버레이 토글 신호가 아니다.

v0.89 구현:

- UI 전용 D3D11 blit pixel shader에서 RGB 최대값이 `3/255` 이하인 픽셀의 alpha만 0으로 만든다. UI가 아닌 스테레오/평면 blit에는 적용하지 않는다.
- 진단 BMP는 투명화 전 원본 RT가 아니라 OpenXR UI swapchain에 blit된 결과를 저장해 실제 제출 alpha를 검사할 수 있게 했다.
- `/UICanvasGroup/LiveOverlayContent/MusicTimeRoot/MusicTime` Graphic을 우선 찾고, 그 자식 `CanvasRenderer`의 GameObject 활성 상태, `cull`, `GetInheritedAlpha()`로 UI 표시 상태를 판정한다. 정확한 경로가 없으면 `LiveOverlayContent` 아래 첫 Graphic을 사용하며, 해당 API를 쓸 수 없으면 기존 상위 CanvasGroup 검사로 폴백한다.
- black-key 방식은 순수 검정 UI 픽셀도 투명하게 만들 수 있는 진단 단계의 절충안이다. 배경 제거와 글자/아이콘 손실 여부를 실기로 함께 확인해야 한다.

v0.89 실기 결과와 v0.90 수정:

- VR UI가 검은 배경 없이 정상적으로 보였고, UI OFF 시 사라지며 다시 ON 하면 재캡처되어 나타났다. 자식 CanvasRenderer 표시 감지와 layer clear 수명 연동은 성공이다.
- 로그상 UI 표시 감지와 캡처 arm이 `11:03:48.685 UTC`에 같은 sampler 호출에서 발생했고, 약 100ms 뒤 캡처가 ready가 됐다.
- UI를 켠 터치 입력의 이펙트가 이 one-shot에 일부 포함되어 계속 남아 거슬렸다. 동적 이펙트가 계속 재생된 것이 아니라 너무 이른 캡처 프레임이 고정된 문제다.
- v0.90은 UI 표시 감지 후 500ms 동안 캡처를 시작하지 않는다. 터치 애니메이션이 가라앉은 뒤 기존 두 Present 캡처를 수행하므로 UI 표시는 약 0.5~0.7초 늦어질 수 있다.
- v0.90 사용자 실기에서 터치 이펙트 잔상이 제거됐고, UI 표시·OFF·재표시도 모두 정상으로 확인됐다.

### VRFX-001 — 그림자·블룸·가독성

상태: **v0.131 사용자 실기 완료, M3 최종 시각 기준 확정**

최종 clone 전용 기준은 `VLBloom.intensity` 원본의 140%, 정수 `diffusion` 최소 1단계, `VLDOF`/`VLTextureBlur` 비활성이다. `threshold` 상향은 실제 `0.45 → 0.95`가 적용됐지만 사용자 체감 차이가 없어 채택하지 않았다. PC 카메라와 원본 Volume profile은 변경하지 않는다. OpenXR eye 복사 셰이더는 sRGB를 선형으로 변환해 `+0.2 EV`를 적용한 뒤 다시 sRGB로 인코딩하며 UI·평면 패널·PC backbuffer에는 적용하지 않는다.

v0.131 PID 39884에서 UI hidden/shown/capture-ready가 3회 반복되고 failure/fallback 0건이었으며 사용자가 UI 표시·숨김·재표시를 정상 판정했다. 장시간·clone/RT/handle 수명 계측은 사용자가 M3 완료 조건에서 제외했으므로 검증 완료로 표시하지 않는다.

사용자 관찰:

- 현재 VR 스테레오 화면에서 조명에 의한 그림자와 블룸 효과 일부가 원본 PC 화면보다 생략된 것으로 보인다.
- 광원보다 카메라 가까이에 있는 물체, 특히 캐릭터가 광원을 가려야 하는 구도에서도 빛이 물체를 무시하고 통과해 보인다는 추가 관찰이 있다.

현재 코드와의 관련성:

- v0.79~v0.90은 clone 카메라의 `UniversalAdditionalCameraData.renderPostProcessing`을 명시적으로 `false`로 강제했다.
- 따라서 URP Volume 기반 블룸이 빠지는 것은 해당 버전 설정상 예상 가능한 결과다.
- 그림자는 일반적으로 후처리가 아니므로 별도 원인이 있을 수 있다. `renderShadows`, shadow distance/cascade, renderer feature, light culling, 추가 pass와 카메라별 shadow atlas 사용을 확인해야 한다.
- v0.82의 정상 렌더 루프 전환으로 과거 후처리 점멸 원인은 제거됐으므로, 테스트 재개 후 `renderPostProcessing=true` A/B를 다시 시도할 가치가 있다.

과거 권장 진단 순서:

1. 같은 라이브/같은 타임라인에서 PC 백버퍼와 좌/우 eye RT를 동시에 저장한다.
2. clone의 `renderPostProcessing`, `renderShadows`, `requiresDepthTexture`, renderer index를 로그로 남긴다.
3. 정상 렌더 루프를 유지한 채 post-processing만 true/false A/B한다.
4. 블룸이 복구되고 점멸이 없으면 기본값을 true로 되돌린다.
5. 그림자가 계속 없으면 광원 shadow 설정, shadow caster culling mask, URP shadow pass를 별도 비교한다.
6. 좌우 한쪽에만 블룸/그림자가 보이는 결과는 승인하지 않는다.

v0.91 구현:

- 정상 Unity 렌더 루프, Present 2회 동기화, 이중 버퍼, eye 해상도와 UI 경로는 그대로 유지한다.
- clone 카메라의 `renderPostProcessing`만 `true`로 강제해 v0.90과 한 변수 A/B가 가능하다.
- `stereo-camera-clones-ready`에 `stereoSourceRenderShadows`, `stereoSourcePostProcessing`, `stereoClonePostProcessing`을 기록한다.
- 그림자 설정은 강제 변경하지 않고 원본 `renderShadows`를 계속 복사한다. 이번 버전에서 그림자가 여전히 빠지면 shadow pass/culling/renderer feature를 다음 원인으로 좁힌다.
- 실기 승인 조건은 블룸 개선, 좌우 일치, 후처리 점멸 없음, 체감 프레임과 UI의 비회귀다.

v0.91 실기 결과와 v0.92 수정:

- 로그에서 원본 `renderPostProcessing=true`, `renderShadows=true`, clone `renderPostProcessing=true`를 확인했다.
- 사용자는 VR 화면이 PC와 거의 비슷해졌다고 판정해 전체 후처리 누락 가설을 확인했다.
- 다만 특정 영역 또는 화면 전체가 흐려지고 빛이 과하게 번져 보이는 현상이 조금 남았다.
- v0.92는 블룸을 포함한 전체 후처리와 원본 그림자 설정은 유지하고, clone의 URP `antialiasing`만 `None(0)`으로 강제한다.
- 원본과 clone 안티앨리어싱 enum을 `stereoSourceAntialiasing`과 `stereoCloneAntialiasing`으로 기록한다. 흐림이 줄면 시간/공간 AA와 75% eye 해상도 확대 조합이 원인이다.
- 흐림이 그대로면 다음 A/B 대상은 Volume의 Depth of Field, Motion Blur, Bloom 순서다. 원본 게임 Volume을 직접 수정하지 않고 clone 전용 override를 설계해야 한다.

v0.92 실기 결과와 폴백 기준:

- clone AA를 꺼도 흐림/빛 번짐 문제가 유지되어 안티앨리어싱은 주원인이 아니다.
- 추가 효과 분리가 최종적으로 어렵거나 안정성을 해치면 전체 후처리를 처음 활성화한 v0.91 영상 설정으로 복구한다.
- v0.93은 이 폴백을 즉시 적용해 원본 카메라의 AA를 다시 clone에 복사한다. 후처리와 그림자도 v0.91과 동일하다.

v0.103 깊이 가림 A/B:

- 원본과 clone의 renderer index, render type, `requiresDepthOption`, `requiresDepthTexture`를 `stereo-camera-clones-ready`에 기록한다.
- 원본 카메라가 `Auto` 또는 false를 반환하더라도 오프스크린 좌·우 clone만 `requiresDepthTexture=true`로 명시 설정한다. 설정 유지가 확인되지 않으면 clone setup을 중단하고 평면으로 폴백한다.
- 원본 Volume·조명·에셋과 bloom 강도는 변경하지 않는다. 이 버전의 단일 변경 변수는 clone depth texture 요구다.
- PID 35804에서 source depth `Auto(2)`/false와 clone depth `On(1)`/true가 두 generation에 동일하게 기록됐다. 서로 다른 두 live, UI capture, 이탈과 portrait restore가 failure 0으로 동작했고 약 20 pair/s를 유지했다.
- 사용자는 광원 가림이 약간 개선됐지만 캐릭터가 빛에 파묻혀 보이는 현상이 일부 남는다고 판정했다. depth 입력은 원인의 일부로 확인했지만 해결 완료는 아니다.

v0.104 후처리 분리 A/B:

- v0.103의 clone depth 강제는 유지하고 clone `renderPostProcessing`만 false로 변경했다.
- 원본 카메라·Volume·조명·에셋은 바꾸지 않는다. 캐릭터 가독성이 회복되는지와 블룸·색보정·DoF 손실을 함께 관찰한다.
- 잔여 파묻힘이 사라지면 후처리 계열이 원인임을 확정하고 clone 전용으로 Bloom/Lens Flare/노출을 세분한다. 그대로면 renderer feature 또는 실제 조명/shadow pass 쪽으로 이동한다.
- 사용자 실기에서 캐릭터 파묻힘은 사실상 사라졌고 잔여 광량은 제작 의도로 허용 가능한 수준이었다. 좌우 차이는 문제 없음으로 판정했다.
- 전체 화면은 다소 밋밋해 최종 화질안으로는 채택하지 않는다. 이 결과로 잔여 파묻힘의 원인이 후처리 계열임을 확정한다.

v0.105 후처리+LDR 절충 A/B:

- clone post-processing을 true로 복원하되 clone `Camera.allowHDR=false`를 매 `CopyFrom` 뒤 재적용한다. clone depth `On`도 유지한다.
- 목표는 색보정과 후처리 질감을 복구하면서 Bloom/Lens Flare가 캐릭터를 덮는 고휘도만 제한하는 것이다.
- source/clone HDR 상태를 clone-ready 로그에 기록한다. 원본 Camera/Volume/조명/에셋은 변경하지 않는다.
- 사용자 실기에서 해결 이전과 동일하게 캐릭터 파묻힘이 재발했다. clone HDR OFF는 효과가 없어 가설을 기각하고 v0.105를 철회했다.
- 소스·설치를 v0.104로 되돌렸고 재빌드/기존 rollback/설치 DLL SHA-256 `0BB78747...964C3E`가 일치한다.

v0.106 개별 효과 A/B와 v0.107 수정:

- 설정 파일 `vrmod/config/visual-effect-mode.txt`는 `all-off`, `all-on`, `vlbloom-off`, `bloom-off`를 지원한다. 현재는 `vlbloom-off`다.
- clone post-processing과 depth를 켠 채 clone 소유 scripted VolumeStack에서 게임 전용 `VLBloom`만 inactive로 한다. Color Adjustments, Tonemapping, DoF 등 나머지는 유지한다.
- 원본 Volume profile과 source Camera는 수정하지 않는다. clone API나 component가 없으면 자동으로 v0.104 전체 후처리 OFF로 폴백한다.
- 코어 테스트 9/9, Release 빌드 경고 0/오류 0, 설치 성공. PID 27020 실기에서는 `VolumeComponent.set_active/1` MissingMethodException으로 v0.104 전체 OFF에 안전 폴백했다. live 연속 publish와 generation retire는 동작했지만 VLBloom 단독 A/B는 무효다.
- v0.107은 실제 Unity 정의에 맞춰 기반 `VolumeComponent.active` bool field를 `il2cpp_field_set_value`로 false로 설정했다. PID 41140에서는 effect image를 잘못 가정해 `VL.Rendering.VLBloom`을 찾지 못하고 다시 전체 OFF로 폴백했다.
- v0.108은 정확한 namespace/type을 loaded image에서 일회 탐색하도록 수정했다. 코어 테스트/Release 빌드/설치가 성공했으며 VR 실기는 아직 하지 않았다.

### VRPERF-002 — 스테레오 게시가 약 2fps로 제한됨

상태: v0.85 원인 확인, v0.86 수정 설치/미실기

- 사용자 관찰: v0.85 VR 스테레오가 거의 초당 2프레임 수준이다.
- 구조적 제한: 진단 샘플러 100ms throttle과 arm/finalize 두 호출 때문에 최대 약 5 pair/s다.
- 추가 부하: clone 카메라 enabled 상태가 3개/5개 카메라 signature를 번갈아 만들며 v0.85 한 실행에서 `render-snapshot` 290개를 기록했다. 각 snapshot은 Camera/Canvas/RawImage/UI Graphic 전체를 열거하고 큰 JSON을 기록한다.
- 해당 실행 뒤 로그는 약 61.7MiB였다. 고해상도 2016x2160 eye 렌더 두 장의 GPU 비용까지 더해져 체감 갱신률이 구조적 상한보다 낮아질 수 있다.
- 다음 수정은 매 프레임 실행되는 경량 stereo pump와 100ms 진단 수집을 분리해야 한다. pair 게시 시간, Present delta와 실제 fps를 계측하고, clone enabled 토글은 snapshot signature에서 제외하거나 snapshot 수집을 저주기로 제한한다.
- v0.86 실기: `TryPumpStereo`는 약 19.4~20.7 pair/s를 기록했고 사용자는 프레임 출력이 정상적으로 보인다고 판정했다.
- v0.86 구현: render snapshot signature에서 clone에 의해 3↔5로 바뀌는 camera count를 제거하고 카메라 재탐색 주기를 0.5초에서 1초로 낮췄다. 전체 UI snapshot은 상태 변경 또는 10초 heartbeat에서만 기록한다.

### VRUI-002 — 스킬 최상위 이미지 누락

상태: 사용자 요청 시에만 디버깅

플레이 중 스킬 사용 시 최상위 레이어에 등장하는 2D 이미지/애니메이션이 VR 출력에서 보이지 않은 적이 있다. 사용자가 추후 요청하면 이벤트 전후 Canvas 생성/파괴, sorting order, overrideSorting, 카메라 stack과 최종 backbuffer 합성 순서를 수집한다.

### VRPERF-001 — 제품 성능과 수명

- **M4 달성 기준:** v0.141 PID 36804에서 Present 59.47fps, stereo 59.14 pair/s, OpenXR submit 평균 114.79fps·중앙값 117.60fps, pair age 8.38ms와 source→clone 3.01ms를 기록했다. 사용자는 게임 자체가 60fps 고정이 아닌 점을 고려해 현재 결과를 충분한 제품 성능으로 승인했다.
- 현재 제품 기준은 게임 속도 stereo 생산과 near-120Hz OpenXR submit이다. 정확한 90/120 stereo pair/s는 게임이 그 프레임률을 제공하는 환경의 선택적 확장 목표이며 제품 진행을 막지 않는다.
- 장면/카메라/UI 전환 반응은 1~10ms 이내를 목표로 한다. 전체 Unity 객체 열거를 1ms마다 반복하지 않고, 이벤트/캐시 기반 fast path와 저주기 상세 진단을 분리한다.
- v0.86~v0.97 실측 약 20 pair/s는 역사적 프로토타입 기준이다. v0.132~v0.141에서 실제 render completion, 단일 양안 fence, triple buffer와 OpenXR low-latency polling으로 교체됐다.
- OpenXR worker는 `Normal` 우선순위를 사용한다. OpenXR swapchain GPU 완료 poll만 최초 최대 1ms spin하고 이후 yield하며, main-thread 일반 GPU wait는 기존 동작을 유지한다.
- v0.140 PID 35100에서 외부 GPU 작업과 함께 NVIDIA TDR/LiveKernelEvent 141이 발생했다. 통제 재실행과 재부팅 뒤 v0.141에서는 재현되지 않았지만 안정성 통과 근거로 사용하지 않는다.
- 사용자는 향후 모든 안정성 전용 테스트를 생략하도록 지시했다. 지속 시간, 반복 횟수, 장시간 자원 수명과 누수 추세는 비차단·미검증으로 기록하며 실제 관측된 크래시는 계속 결함으로 취급한다.
- v0.82~v0.92는 기능 검증용 30초 캡처 창이었다. 제한 이후 마지막 스테레오 쌍의 갱신이 멈췄다.
- v0.93은 30초 제한을 제거하고 Live 가로 장면이 유효한 동안 33ms 목표의 양안 생산을 계속한다.
- OpenXR view snapshot이 일시적으로 1.5초 이상 갱신되지 않으면 영구 실패로 고정하지 않고 대기한 뒤 자동 재시도한다. 이 상태는 `stereo-view-state-waiting`으로 최대 10초에 한 번 기록한다.
- Live 장면을 벗어날 때 한 쌍이 렌더 중이면 양쪽 clone 카메라를 즉시 비활성화하고 arm 상태를 해제한다. 마지막 texture는 즉시 clear되어 평면 폴백한다.
- v0.93 실기 로그에서 양안은 약 55초 동안 계속 게시됐고 별도 stereo failure는 없었다. 그러나 OpenXR 프레임 루프가 부트스트랩 후 정확히 약 90초에 정상 반환했으며, 직후 `stereo-view-state-waiting`이 기록되고 VR 출력이 꺼졌다.
- 원인은 `testDurationMilliseconds=90_000`, 전체 loop 120,000ms, 최대 12,000프레임이라는 상위 진단 상한이다. 30초 스테레오 제한과 별개였다.
- v0.94는 세 상한을 모두 제거한다. 활성 루프에서 세션 이벤트를 계속 poll하고 `STOPPING`이면 `xrEndSession`, `LOSS_PENDING`/`EXITING`이면 루프 종료하며, 그 외에는 무기한 frame submit을 계속한다.
- v0.94 실기에서 첫 라이브 `env_3d_live_ssmk001-00-noon`은 약 20 pair/s로 2분 이상 유지되어 90초 경계 제거는 성공했다. OpenXR failure/exit 이벤트도 없었다.
- 첫 라이브 이탈 뒤 다른 라이브 진입 시 `Live`가 먼저 1920x1080으로 바뀌었지만 cameraCount=2이고 실제 env 3D source camera는 아직 없었다. 기존 `_stereoPumpEligible`은 장면명과 가로 비율만 사용해 이 준비 구간을 잘못 활성화했다.
- v0.95는 `IsLiveScene && landscape && _lastLiveCamera != 0`을 모두 만족해야 생산을 시작·재개한다. 불충족으로 전환되면 registry의 이전 좌우 eye texture COM 참조를 즉시 clear해 오래된 곡이 고정되지 않게 한다.
- `stereo-live-source-ready`/`stereo-live-source-unavailable` 이벤트로 장면, 크기와 clone 준비 상태를 기록한다.
- v0.96 실기에서 첫 라이브는 1,668 eye pair까지 정상 게시됐다. 이탈 후 `Camera.allCameras`에서 clone 두 개가 사라졌지만 런타임의 `_stereoCloneSetupReady`와 clone 포인터는 남았다. 두 번째 라이브의 `stereo-live-source-ready` 직후 `coreclr.dll`이 `0xc0000005` access violation으로 종료됐다. 이는 scene 수명이 끝난 Unity camera wrapper를 비영 포인터 검사만으로 재사용한 결함과 일치한다.
- v0.97은 source 상실 시 `RetireStereoCameraGeneration()`으로 armed render와 UI capture를 복구하고 stereo/UI registry, clone camera 포인터, scene-bound UI cache 및 generation 상태를 폐기한다. 다음 live source에서 clone과 `UniversalAdditionalCameraData`를 새로 복사한다. eye RT 네 개, render request 네 개와 GPU query는 재사용해 대형 GPU 자원의 반복 할당을 피한다.
- v0.98 실기에서 위 eye RT/request 재사용 가정이 틀렸음이 확인됐다. rooted RenderTexture wrapper도 scene 전환 뒤 NRE를 냈다.
- v0.99부터 camera, eye RT, request를 모두 scene-bound generation으로 폐기하고 GPU query도 Release한 뒤 다음 concrete env에서 전부 재생성한다.
- v0.102 PID 35696은 첫 live에서 607 pair와 UI capture까지 정상 동작했지만, 이탈의 세로 전환 직후 `generation-retired` 전에 `coreclr.dll` `0xc0000005`로 종료됐다. Windows 충돌 이벤트와 로그 종단을 근거로 이탈 콜백 안의 명시적 `il2cpp_gchandle_free` 경로를 회귀 원인으로 판정했다.
- v0.102의 handle 추적·해제·경계 계측은 모두 소스와 설치본에서 제거했고 v0.101로 정확 복귀했다. M3 계측은 먼저 기존 로그와 외부 process memory처럼 런타임 수명을 바꾸지 않는 관측부터 설계한다. 장면 이탈 훅에서 GC handle을 해제하는 접근은 재사용하지 않는다.
- 이 항목의 과거 2016x2160×4 이중 버퍼 비용은 v0.137 triple buffer와 v0.139 1744x1872 설정으로 대체됐다. 현재 기준은 VRPERF-001의 v0.141 실측값이다.

### VRRUNTIME-001 — 예비 OpenXR 런타임

SteamVR OpenXR와 Meta Quest Link OpenXR는 아직 회귀 테스트하지 않았다.

## 7. 버전별 핵심 결과

| 버전 | 핵심 변경/결과 |
|---|---|
| 초기 진단군 | Doorstop, IL2CPP main-thread hook, 장면/방향/카메라 탐색 |
| v0.8~초기 OpenXR군 | VD OpenXR 세션, 검은 화면과 테스트 패턴으로 layer 경로 검증 |
| 패널 안정화군 | 체크보드 패널, 상하 반전, 색공간 보정, 세로/가로 전환 성공 |
| v0.53 | Present 동기화 백버퍼/월드 RT 캡처 근거 수집 |
| v0.57~v0.59 | CanvasRenderer UI 재생 58 draw 진단; 실제 결과는 빈 RT로 최종 실패 판정 |
| v0.60 | 최종 게임 백버퍼 평면 패널로 UI/Localify/ON-OFF 및 플리커링 해결 |
| v0.61 | OpenXR eye pose/IPD/FOV 실측 |
| v0.62 | 좌우 clone camera/RT 기반 생성 |
| v0.63~v0.65 | 너무 이른 렌더 요청으로 검은 eye RT |
| v0.66 | 라이브 안정 후 원본 Game3DManager 수동 렌더 성공 |
| v0.67 | 눈별 위치/FOV 스테레오 이미지 저장 성공 |
| v0.68 | 정적 OpenXR Projection Layer, 깊이 확인; 눈 피로와 정렬 문제 |
| v0.69 | eye swap 실험 악화 — 폐기 |
| v0.70 | 원래 eye mapping + 월드 눈 간격 25%, 사용자 평가 “딱 됐다” |
| v0.71 | 눈 간격 22.5%, 정적 표시 시간 확대 |
| v0.72 | 원본 카메라 연속 5fps; 이펙트 프레임 간헐적 |
| v0.73 | 독립 clone camera가 URP 데이터 없이 검은 출력 |
| v0.74~v0.75 | 검은 출력 차단 성공; JsonUtility 복사 API 부재로 평면 폴백 |
| v0.76 | URP 속성 명시 복사로 clone 출력 성공; 이펙트 점멸 지속 |
| v0.77 | 이중 버퍼/GPU fence; 수동 렌더 점멸 지속 |
| v0.78 | 75% 해상도/명목 15fps; 점멸 지속 |
| v0.79 | post-processing 강제 off; 점멸 지속 — 후처리 토글 자체가 원인 아님 |
| v0.80 | 완성 한 쌍 30초 고정; 완전 안정 — OpenXR layer 전환 문제 배제 |
| v0.81 | Unity 정상 렌더 루프 정적 캡처; 정상 조명/이펙트 포함 |
| v0.82 | 정상 렌더 루프 + Present 2회 + 이중 버퍼 연속 갱신; 사용자 실기 성공 |
| v0.83 | 투명 UI mesh replay + 두 번째 OpenXR layer; UI 미표시, RT 빈 화면 |
| v0.84 | 원본 UICamera 정상 렌더 UI-only one-shot; 실기 UI 미표시. sparse 가시성 검사에서 빈 RT 판정 후 레이어 미제출 |
| v0.85 | 목표 간격 67ms→33ms; 실기 약 2fps로 실패. 100ms sampler/2단계 publish와 과다 snapshot 로그가 병목 |
| v0.86 | 경량 매 프레임 pump로 약 20 pair/s; 체감 정상. UI는 불투명 검은 배경/one-shot 고정 |
| v0.87 | UI 배경 cull/ON·OFF 연동; Graphic 배열 1,021개가 한도 512를 넘어 캡처 전 실패 |
| v0.88 | Image 한정 탐색 + 2초 재시도로 캡처 성공; 검은 불투명 배경과 UI OFF 뒤 잔류 지속 |
| v0.89 | UI black-key alpha + 실제 자식 CanvasRenderer 감지; 투명 배경과 OFF/ON 성공, 터치 이펙트가 one-shot에 고정 |
| v0.90 | UI 표시 후 500ms 안정화 지연; 터치 잔상 제거와 UI 표시·OFF·재표시 실기 성공 |
| v0.91 | clone 후처리 true; PC와 거의 유사해졌으나 특정 영역/전체 흐림과 빛 번짐 일부 발생 |
| v0.92 | 후처리 유지 + clone AA None; 흐림/빛 번짐 지속으로 AA 가설 기각 |
| v0.93 | 스테레오 30초 제한 제거 성공; 상위 OpenXR 90초 제한으로 VR 종료 |
| v0.94 | 첫 라이브 2분 이상 지속 성공; 다른 라이브 재진입 준비 구간에서 VR 꺼짐 |
| v0.95 | source camera gating + eye clear; 로그상 단일 live 약 2.5분 지속, 같은-process 재진입 미검증 |
| v0.96 | 월드 eye offset 27.5%; 첫 live 1,668 pair 정상, 이탈 때 사라진 clone 포인터를 보관해 두 번째 live 진입 직후 coreclr AV 크래시 |
| v0.97 | stale camera 크래시 해결; 두 번째 generation 첫 pair 뒤 실제 env 장면 전환에서 clone 제거, 이전 eye RT가 거짓 검증된 뒤 NRE·평면 폴백 |
| v0.98 | concrete env gating 성공, 첫 live 621 pair; 두 번째 env에서 이전 eye RT clear NRE로 clone setup 실패·평면 유지 |
| v0.99 | camera/eye RT/request/query generation 전체 재생성; 빌드·설치 완료, M2 VR 미실기 |
| v0.99 실기 | 서로 다른 3개 live 재진입·검증 성공, clone/render failure 0; 이탈 후 PC portrait 레이아웃 파손과 후속 UI capture NRE |
| v0.100 1차 실기 | stereo 진입 3회·UI·각 portrait 복귀 성공, failure 0; 짧은 실행이라 30초 조건 전 |
| v0.100 2차 실기 | 44.3초/40.3초/38.0초 지속 성공; portrait 파손 재발, 수동 resize로 복구 |
| v0.101 | canonical portrait 1px nudge/100ms 원복 자동화; PID 30636에서 적용/원복 2회와 사용자 실기 성공, M2 달성 |
| v0.102 | 첫 live 607 pair/UI 성공 후 이탈에서 coreclr `0xc0000005`; 명시적 GC handle 해제 회귀로 철회, v0.101 복귀 |
| v0.103 | clone depth texture 명시 요구와 depth/renderer 진단 로그; 빌드·설치 성공, 라이팅 가림 VR 실기 전 |
| v0.104 | v0.103 depth 유지 + clone 후처리 OFF 진단; 빌드·설치 성공, VR 실기 전 |
| v0.105 | clone HDR OFF에도 해결 이전 파묻힘 재발; 실패/철회 후 v0.104 정확 복귀 |
| v0.106 | 설정 기반 clone VolumeStack 개별 A/B; PID 27020에서 setter 조회 실패로 전체 OFF 폴백, A/B 무효 |
| v0.107 | `VolumeComponent.active` field 쓰기 수정; effect image 오판으로 전체 OFF 폴백, A/B 무효 |
| v0.108 | `VL.Rendering.VLBloom` loaded-image 일회 탐색; 이후 VolumeStack component 비활성 방식은 최종 출력에 반영되지 않음을 확인 |
| v0.116~v0.120 | 실제 `VLPostProcessPass.Render` 문맥과 선택 메서드를 후킹해 VLBloom 본체가 광막과 밝기의 공통 원인임을 확정 |
| v0.121~v0.129 | 블룸 50%에서 시작해 DOF/texture blur 격리, threshold·diffusion A/B를 거쳐 intensity 140%와 diffusion 정수 최소 1단계 확정 |
| v0.130~v0.131 | VLTonemapping 필드 진단 후 톤 곡선은 유지하고 OpenXR eye 최종 blit에만 선형 +0.2 EV 적용; 사용자 밝기 정상 판정 |
| v0.132~v0.134 | stereo timing 계측, actual clone completion mask와 arm/wait 원인으로 고정 two-Present 병목 확정 |
| v0.135~v0.137 | 중복 Present boundary 제거, 양안 단일 fence, triple eye buffer/lease로 약 20→56 pair/s 및 main-thread fence 제거 |
| v0.138~v0.139 | GUI 호환 render-scale 설정 추가, 0.70/0.65 A/B 후 0.65·1744x1872 확정 |
| v0.140 | worker AboveNormal A/B; 성능 이득 없음. 외부 GPU 부하 동시 세션에서 NVIDIA TDR, 통제 재실행은 정상 |
| v0.141 | worker Normal 복원 + OpenXR 1ms bounded spin; 59.14 pair/s, submit 평균 114.79/중앙값 117.60fps, 사용자 승인으로 M4 달성 |
| v0.142 | M5 저주기 Camera/Canvas/RT/RawImage/Unity VideoPlayer topology 계측; 홈·메뉴·선택·세로/가로 ADV 실기. RawImage 상한 failure 8건 확인 |
| v0.143 | RawImage 상한 2,048, custom media class/surface 계측; 실제 `CampusVideoPlayer → OnDemandVideoPlayerImage` 영상 재생과 failure 0 확인, 사용자 승인으로 M5 달성 |
| v0.146 | 비-live VR/UI 실험 실기. 홈·커뮤니케이션에서 상하 반전된 플랫 화면이 stereo 위에 겹쳤고 가로 커뮤니케이션의 PC 화면비가 파손됨. UI-only 메뉴와 가샤 영상 플랫 화면은 정상 |
| v0.147 | 홈에서 움직이는 플랫/VR 중첩 영역에 keying 노이즈가 발생했고 가로 커뮤니케이션의 PC/VR 화면비 파손 및 이후 라이브 PC 화면비 파손이 재현됨 |
| v0.148 | Present 경계 동기 UI 합성과 비-live clone 렌더 순서 수정, 코어 9/9·빌드·설치·해시 일치. VR 실기 전이며 이후 결정된 손 부착형 전체 화면 패널 정책은 아직 구현하지 않음 |
| v0.149 | OpenXR core Touch action set, 좌·우 grip/aim 및 squeeze/trigger/A·B/X·Y 상태 기반, 시작 OFF 왼손 Grip 토글, 손/패널 FOV·front-facing gate와 100ms hysteresis, 최종 백버퍼 hand-attached quad 구현. OFF 시 panel acquire/copy/submit을 생략하고 기존 keyed/natural UI capture를 비활성화. 코어 11/11·ABI/export·빌드·설치·해시 일치, 사용자 실기 전 |
| v0.150 | fresh stereo 부재 시 전체 백버퍼를 head-fixed 정면 1.6m에 자동 표시하고, stereo 복귀 시 정면 패널을 제거해 왼손 Grip 보조 패널로 전환. right thumbstick action, ray→panel UV→foreground client 좌표, 원형 cursor alpha quad, A/trigger click·drag, trigger pre-press latch, B back, stick scroll 구현. 코어 13/13·ABI/export 15개·빌드·설치·해시 일치, 사용자 실기 전 |
| v0.151 | `XR_TYPE_ACTION_STATE_GET_INFO` 30→58 수정과 액션별 실패 격리. 사용자 실기에서 정면 패널, ray cursor, A/trigger/B/stick, Grip 토글, 3D 이탈 자동 복귀와 종횡비 정상 확인 |
| v0.152 | 손 패널 하단을 controller tip 위에 두는 offset 실험. 중심이 손 로컬 Y축으로 너무 멀어져 FOV gate를 통과하지 못해 패널 미표시 |
| v0.153 | 손만 FOV gate로 사용하고 controller tip 위치에 view-space upright/viewer-facing 패널을 구성해 표시 복구. 사용자 실기에서 위치가 지나치게 높다고 판정 |
| v0.154 | 패널 반높이와 추가 간격을 제거해 중심을 controller tip에 직접 배치. PID 31424의 입력·Grip·FOV·정면/stereo 전환 로그와 사용자 최종 승인으로 M6 달성 |
| v0.155~v0.160 | 버전 있는 JSON 설정 schema, 한국어 기본의 한·영·일 설정 GUI, manifest 기반 설치·제거·rollback과 독립 EXE 설치기를 제품 흐름으로 통합 |
| v0.161 | 자동/수동 VFX 설정과 수동 bloom·DoF·texture blur·star streak·flare 제어 추가 |
| v0.162 | eye render scale 상한을 2.00으로 확장하고 1.00 초과 GUI 경고 추가 |
| v0.163~v0.164 | Dobby를 배포본에 포함하고 payload 전체 사전 검증, clean install, Localify 공존·보존 검사를 강화 |
| v0.165 | 3D source 발견 fast path를 단축해 몰입형 전환 지연 감소. 사용자 실기 완료 |
| v0.166 | GitHub stable Release 기반 서명 없는 안전 자동 업데이트와 추가 3D 진입 단축. `main` 기준점 |
| v0.167~v0.168 | 비-live positional 6DoF와 설정 선택형 live 독립 6DoF 추가 |
| v0.169~v0.170 | 최종 시야 기준 완전 3D 이동, 좌·우 역할 교환과 VR 스틱 스크롤 비활성화 |
| v0.171~v0.172 | 월드축 우세축 스냅/부드러운 회전과 world-space navigation 합성. 실기에서 스틱 회전 roll 누적이 남아 v0.173으로 이관 |
| v0.173 | HMD yaw/pitch/roll을 원점 대비 성분별로 분리하고 실제 HMD roll 변화만 최종 적용. 사용자 VR 실기에서 “완벽” 판정, M7 달성 |
| v0.174 | live 독립 6DoF 기본 ON, 스냅 기본 30°, 이동 기본 1.95m/s. 코어 39/39·관리 7/7·199 manifest·clean/Localify 패키지와 실제 설치 성공, 별도 VR 실기 전 |

## 8. 다음 개발·테스트 체크리스트

1. **M8 런타임 호환성:** SteamVR OpenXR와 Meta Quest Link/Air Link를 실제 장치에서 확인하기 전까지 예비 지원으로 표시한다.
2. **게임 업데이트 gate:** 핵심 게임 파일 기준선이 바뀌면 자동 승인하지 않고 호환성 진단과 창모드 생존을 먼저 확인한다.
3. **배포 유지보수:** GitHub stable Release의 ZIP과 `.sha256`을 같은 버전으로 게시하고 자동 업데이트가 게임 종료 상태에서만 교체하도록 유지한다.
4. **회귀:** v0.174 기본 프로필과 v0.173 6DoF/roll, v0.154 표시·입력, v0.141 성능, v0.131 영상, v0.90 UI와 PC mirror 기준을 유지한다.
5. 전체 객체 열거를 1ms 주기로 옮기지 않는다. 안정성 전용 테스트는 수행하지 않고 비차단·미검증으로 남긴다.
6. 직접 터치와 live 좌상단 시계 회귀는 사용자 결정으로 생략했다. 다시 요청받기 전에는 완료 조건으로 요구하지 않는다.

### VRCONFIG-001 — 최종 GUI 설정 도구

상태: **v0.174 구현·패키징·실제 설치 완료, v0.174 사용자 VR 실기 전**

- 게임 프로세스에 UI 프레임워크를 주입하지 않는 별도 데스크톱 GUI로 만든다.
- 최소 설정 항목은 월드 eye offset scale(현재 27.5%), eye render scale, 패널 손, 포인터 손, 손 기준 패널 offset/크기/회전, 가시성 여유, 후처리 모드와 OpenXR 런타임 선택이다.
- `eyeRenderScale` 기본값은 `0.65`, 허용 범위는 `0.50~2.00`, 안전 폴백은 `0.75`다. GUI는 1.00 초과에서 성능·VRAM 경고를 표시한다.
- GUI는 버전이 명시된 설정 파일을 원자적으로 저장하고, 런타임은 시작 시 검증한 값만 읽는다. 범위 밖 값이나 손상된 파일은 안전 기본값으로 폴백한다.
- Localify 설정과 파일을 수정하지 않으며, 기본값 복원과 설정 내보내기/가져오기를 제공한다.
- 런타임 하드코딩을 설정 파일로 전환할 때 v0.141 렌더 기준과 안전 폴백을 보존한다.

테스트 보고 형식:

```text
버전:
장면/곡:
PC 화면:
VR 3D/깊이:
VR UI:
그림자:
블룸:
플리커링/지연/크래시:
진입 후 관찰 시간:
```

## 9. 로그와 진단 산출물

- 주 로그: `vrmod/logs/runtime-bootstrap.jsonl`
- stereo BMP: `vrmod/logs/v0.xx-stereo-left.bmp`, `...right.bmp`
- UI replay BMP: `vrmod/logs/v0.60-ui-element-replay.bmp` — 파일명은 과거 고정값이며 v0.83 실행에서도 덮어써졌다.
- Present 비교: `vrmod/logs/v0.53-present-*-backbuffer.bmp`, `...world.bmp`
- 로그의 한 이벤트가 매우 큰 JSON 한 줄일 수 있으므로 PowerShell `Select-String`보다 `rg`로 event/version을 먼저 제한한다.

예시:

```powershell
rg -N '"event":"stereo-[^"]+","bootstrapVersion":"0\.82\.0"' `
  vrmod/logs/runtime-bootstrap.jsonl
```

## 10. 문서 갱신 규칙

진행 중 마일스톤에서는 runtime 변경마다 아래 문서를 중간 갱신하지 않고 코드·설치 해시·로그와 사용자 판정으로 추적한다. 모든 완료 조건이 사용자 VR 실기와 필요한 로그로 확인되는 마일스톤 달성 응답에서 다음 문서를 한 번에 동기화한다. 사용자가 문서화 전용 감사를 명시한 경우만 예외다.

- `GAKUMAS_VR_STATUS.md`: 설치/검증/미해결 상태 갱신
- `GAKUMAS_VR_DESIGN.md`: 구조나 기술 결정이 바뀐 경우 갱신
- `vrmod/CHANGELOG.md`: 버전별 코드 변경과 테스트 결과 추가
- 런타임 버전: `Entrypoint.cs`와 `.csproj`을 함께 갱신
- `VR_MILESTONES.md`, `VR_HANDOFF.md`: 달성 근거와 다음 단계까지 함께 갱신
- 미실기 기능은 “구현됨/설치됨/미실기”로 기록하고 “완료”로 쓰지 않음

이 규칙은 저장소 루트 `AGENTS.md`에도 명시돼 있다.
