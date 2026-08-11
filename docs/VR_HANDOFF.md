# Gakumas VR 개발 인수인계

작성 기준: 2026-08-11  
코드/설치 기준: `GakumasVR.RuntimeBootstrap` v0.154.0  
목적: 새 Codex 세션이 과거 대화 없이 현재 코드, 검증 수준, 안전 경계를 그대로 이어받기 위한 운영 문서

## 0. 먼저 읽을 결론

- **[확정: 소스/설치]** 현재 소스와 설치 DLL은 v0.154.0이며 SHA `7C4A29EA3FB7E6FE96AFC12B3231592987B4F9A4FDC2F6C0171DF7B095D428E7`로 일치한다. Core SHA는 `B4B5C63CB507101D2B5F56EE784860628A9DBC27E87495987002A3DA2567C13B`이다. 코어 테스트 13/13, OpenXR x64 ABI와 loader action export 15개, Release 빌드 경고 0/오류 0을 확인했다.
- **[사용자 실기/M6 달성]** fresh stereo 부재 시 최종 backbuffer를 view-space 정면 1.6m에 자동 표시하고, stereo가 준비되면 정면 패널을 제거해 왼손 Grip/FOV 보조 패널로 전환한다. 오른손 ray UV, 원형 cursor, A/trigger click·drag, pre-press latch, B back과 stick scroll, 그립 토글, 종횡비와 자동 복귀가 정상 판정됐다.
- **[확정: v0.154 손 패널]** 왼손 controller local +Y 0.10m의 상단 끝에 패널 중심을 직접 두고, view-space에서 수직·viewer-facing으로 갱신한다. 표시 조건은 tracking과 손 HMD FOV이며 PID 31424에 visible/hidden 전환이 기록됐다. viewer-facing ON/OFF는 M7 GUI로 미룬다.
- **[확정: M5 topology]** v0.142 PID 32908에서 홈·상점·프로듀스 준비·스토리 선택·세로/가로 ADV를 순회해 world RT/RawImage와 별도 `UICamera` 조작 Canvas를 확인했다. v0.143 PID 2988은 실제 영상 재생 중 `Campus.Common.CampusVideoPlayer → OnDemandVideoPlayerImage` 경로를 확인했고 진단/runtime failure는 0건이었다.
- **[사용자 승인: M5]** 라이브 외 게임 진행 UI는 모든 상황에서 지속 갱신해 표시하고, 실시간 3D만 stereo 후보, custom/사전 렌더 영상은 비율 보존 2D surface, 불명확·실패 시 최종 backbuffer SafePanel로 폴백한다. v0.142 종류별 화면 순회와 v0.143 실제 영상 재생 성공으로 M5를 2026-08-10 달성했다.
- **[확정: M4 성능]** v0.141 PID 36804의 실제 3D 구간 100.5초에서 Present 59.47fps, stereo 59.14 pair/s, OpenXR submit 평균 114.79fps·중앙값 117.60fps, pair age 8.38ms, source→clone 3.01ms를 기록했다. buffer block, eye 누락과 런타임 오류는 0건이었다.
- **[사용자 승인]** 게임 자체가 60fps를 고정하지 못하므로 현재 game-rate stereo와 near-120Hz OpenXR 결과가 충분하고 추가 차이는 오차 범위라고 판정했다. 정확한 90/120 stereo pair/s는 비차단 선택 목표다.
- **[정책: 안정성 테스트 생략]** 사용자는 향후 모든 안정성 전용 테스트를 생략하도록 지시했다. 지속 시간, 반복 횟수, 장시간 자원 수명과 누수 추세는 완료 조건이 아니며 **비차단·미검증**으로 남긴다. 실제 관측된 크래시는 계속 결함으로 취급한다.
- **[확정: 최종 M3 시각]** clone 전용 VLBloom intensity 140%, 정수 diffusion 최소 1단계, VLDOF/VLTextureBlur OFF, OpenXR eye 최종 blit +0.2 EV가 사용자 실기에서 허용 가능한 밝기·가독성으로 판정됐다. PC/source 카메라와 원본 Volume은 변경하지 않는다.
- **[확정: v0.131 UI]** PID 39884에서 hidden → shown → capture-ready가 3회 반복되고 failure/fallback은 0건이었다. 사용자가 UI 표시·숨김·재표시를 정상 판정했다.
- **[실기 무효: v0.106]** PID 27020은 live와 연속 publish를 수행했지만 `VolumeComponent.set_active/1` MissingMethodException으로 v0.104 전체 OFF에 안전 폴백했다. 따라서 VLBloom 단독 A/B로 판정하지 않는다.
- **[실기 무효: v0.107]** PID 41140은 metadata에 존재하는 `VL.Rendering.VLBloom`을 잘못 가정한 image에서 찾지 못해 v0.104 전체 OFF에 안전 폴백했다.
- **[미검증: v0.108]** 정확한 namespace/type을 generation 구성 시 loaded image에서 일회 탐색하도록 수정·설치했다. 짧은 live에서 `stereo-visual-effect-override-ready`를 확인해야 한다.
- **[사용자 실기: v0.103]** clone depth 강제로 캐릭터의 광원 가림이 약간 개선됐지만 빛에 파묻히는 현상이 일부 남았다. PID 35804의 두 live에서 source false/Auto, clone true/On과 failure 0을 확인했다.
- **[사용자 실기: v0.104]** clone post-processing OFF에서 캐릭터 파묻힘은 사실상 사라지고 잔여는 제작 의도로 허용 가능한 수준이었으나 화면이 다소 밋밋했다. 좌우는 문제 없음으로 판정했다.
- **[실패/철회: v0.105]** depth/후처리를 유지하고 clone HDR만 false로 바꿨으나 해결 이전의 파묻힘이 그대로 재발했다. 가설을 기각하고 v0.104로 소스·설치를 복귀했다.
- **[확정: v0.102 실패/철회]** PID 35696의 첫 live는 607 pair와 UI capture까지 정상 동작했지만 이탈의 세로 전환 직후 `generation-retired` 전에 `coreclr.dll` `0xc0000005`로 종료됐다. 이탈 콜백의 명시적 `il2cpp_gchandle_free` 경로를 폐기하고 사용자 지시로 소스·설치·진단 스크립트를 세션 시작 시점 v0.101에 정확히 복구했다.
- **[확정: 로컬 재검증]** 2026-08-09에 코어 테스트 9개와 Release 빌드가 다시 통과했다. 경고 0, 오류 0이다.
- **[주의: 기준선]** 같은 날 `Verify-Baseline.ps1`은 `gakumas.exe`, `GameAssembly.dll`, `global-metadata.dat` 변경으로 실패했다. 사용자가 현재 설치본에서 임시 호환성 테스트 진행을 명시적으로 승인했으므로 테스트는 계속하되, 승인된 제품 기준선 결과와 구분한다.
- **[확정: v0.96 실기/로그]** 첫 라이브는 1,668 eye pair까지 정상 게시됐지만 이탈 후 clone camera가 Unity camera 목록에서 사라졌다. 런타임은 stale clone 포인터를 보관했고 두 번째 라이브 source 준비 직후 `coreclr.dll` access violation(`0xc0000005`)로 게임이 종료됐다.
- **[확정: v0.97 실기/로그]** stale camera 크래시는 해결됐지만 두 번째 clone을 임시 `Live` 장면에서 너무 일찍 만들어 실제 env 장면 전환 때 다시 제거됐다. 재사용 RT의 이전 영상이 첫 pair로 보인 뒤 NRE로 평면 폴백했다.
- **[확정: v0.98 실기/로그]** 첫 live는 621 pair까지 정상 게시됐지만 두 번째 env에서 이전 eye RT wrapper가 무효화되어 clear 단계 NRE로 평면을 유지했다.
- **[확정: v0.99 실기/로그]** 세 live generation 재진입은 성공했으나 두 번째 이후 이탈에서 PC portrait 레이아웃 파손과 후속 UI capture NRE가 발생했다.
- **[확정: v0.100 실기/로그]** 44.3초/40.3초/38.0초 live 지속으로 재진입·시간 조건은 충족했지만 portrait 파손이 재발했고 수동 resize로 복구됐다.
- **[확정: v0.101 실기/로그]** PID 30636에서 live source/clones-ready/retire 각 3회, 자동 portrait nudge/원복 각 2회, Canvas refresh 12회와 관련 failure 0을 확인했다. 사용자가 수동 리사이즈 없는 정상 복귀를 확인했다.
- **[마일스톤]** M2는 2026-08-09, M3·M4·M5는 2026-08-10, M6는 2026-08-11 달성했다. 현재 M7 입력·설정·설치 제품화 단계다. 정확한 90/120 stereo 생산은 번호가 있는 마일스톤이 아닌 비차단 선택 목표다.
- **[다음 사용자 결정: M7]** 기본 오른손 ray/A/trigger/B/stick 입력은 두 패널이 같은 UV 경로를 쓴다. 설정 GUI에서 패널 손·포인터 손, 버튼, 패널 위치·크기·회전과 viewer-facing ON/OFF를 교환할 수 있어야 한다.
- **[역사적 주의: v0.148]** v0.148은 홈 keying 노이즈와 비-live clone 화면비 손상을 겨냥한 이전 정책의 실험본이다. v0.150 이후 keyed overlay를 폐기하고 최종 백버퍼 패널로 대체해 현재 제품 경로에는 사용하지 않는다.
- **[주의]** 이 작업공간 또는 상위 경로에 `.git` 디렉터리가 없다. 따라서 현재 작업 트리의 clean/dirty, 추적 파일, 원래 diff를 Git으로 판정할 수 없다.
- 기능 작업을 재개하기 전에 `AGENTS.md`, `VR_MILESTONES.md`, 이 문서, `GAKUMAS_VR_STATUS.md`, `GAKUMAS_VR_DESIGN.md`, `vrmod/CHANGELOG.md` 순서로 읽는다.

표기 규칙:

- **[확정]** 현재 소스, 파일 해시, 로그 또는 사용자 실기로 직접 확인한 사실
- **[미검증]** 구현은 존재하지만 해당 버전의 PC/VR 실기가 끝나지 않은 항목
- **[추정]** 코드나 증상으로 가능한 원인을 좁힌 상태이며 아직 A/B 또는 로그로 확정하지 않은 항목

## 1. 최종 목표

학원 아이돌마스터 DMM판을 Meta Quest에서 다음 방식으로 안전하게 VR화한다.

1. Virtual Desktop OpenXR를 주 경로로 사용한다. SteamVR OpenXR와 Meta Quest Link OpenXR는 예비 경로다.
2. fresh stereo가 없는 완전 평면 VR 환경에서는 최종 게임 화면 전체를 시야 정면에 자동 표시하고, stereo 환경에서는 기본 왼손 컨트롤러의 Grip/FOV 보조 패널로 제공한다.
3. 라이브와 확인된 비-live 실시간 3D 세계는 좌·우 눈별 카메라와 OpenXR Projection Layer로 깊이 있게 표시한다.
4. UI, 영상, 시계와 메뉴 조작은 장면별 keyed alpha 추출 대신 Localify까지 포함한 공통 전체 화면 panel source를 사용한다.
5. 창모드와 빈번한 1080x1920 ↔ 1920x1080 전환 중에도 OpenXR 세션은 유지한다.
6. Localify 한글 패치의 번역, 폰트, 텍스처와 설정을 보존한다.
7. VR 초기화나 immersive 경로가 실패해도 게임은 창모드로 계속 실행되고 VR만 평면 패널 또는 비활성 상태로 폴백한다.
8. 확정된 렌더 기준을 바탕으로 world eye offset, eye render scale, 패널/포인터 손 역할, 손 기준 패널 배치, 후처리와 OpenXR 런타임을 조정하는 별도 데스크톱 GUI 설정 도구를 만든다.
9. 현재 제품 기준은 game-rate stereo와 near-120Hz OpenXR submit이다. 정확한 90/120 stereo 생산은 선택 목표이며, 장면/카메라/UI 전환은 이벤트 기반 fast path로 1~10ms 반응을 목표로 한다.

컨트롤러 ray/pointer와 게임 UI 좌표 주입, 설치/제거 패키지, 설정 GUI까지 완료돼야 제품 목표를 달성한 것으로 본다.

## 2. 현재 설치 및 저장소 상태

| 항목 | 현재 값 | 판정 |
|---|---|---|
| 런타임 소스 버전 | `Entrypoint.cs`와 csproj 모두 0.154.0 | [확정] |
| 설치 DLL | `vrmod/runtime/GakumasVR.RuntimeBootstrap.dll` | [확정] |
| 빌드/설치 DLL | v0.154 SHA `7C4A29EA...D428E7`, 서로 일치 | [확정/실기 성공] |
| 설치 Core DLL SHA-256 | `B4B5C63C...7C13B` | [확정] |
| 직전 rollback | v0.153 `runtime-bootstrap-v0.153.0-20260811-204412/`; v0.141과 이전 A/B 버전도 별도 보관 | [확정] |
| Doorstop 설정 | `doorstop_config.ini`, target은 `vrmod/runtime`의 v0.154 DLL | [확정] |
| Localify proxy | 루트 `version.dll`, 기준선과 일치 | [확정] |
| Doorstop proxy | 루트 `winhttp.dll` | [확정] |
| 게임 프로세스 | v0.154 최신 실기 PID 31424 종료 | [확정] |
| 최신 런타임 로그 | v0.154 PID 31424, pointer ready, Grip ON/OFF, 손 FOV visible/hidden, 정면↔stereo 전환, 관련 failure 0 | [확정] |
| render scale | `vrmod/config/render-resolution-scale.txt` = `0.65`; 범위 0.50~1.00, 안전 기본값 0.75 | [확정] |
| v0.100 최신 로그 | PID 25368, ready generation 5개; 마지막 3개 44.3초/40.3초/38.0초 | [확정] |
| v0.101 최신 로그 | PID 30636, live generation 3개; portrait nudge/restore 각 2회, 관련 failure 0 | [확정] |
| Git metadata | 작업공간과 상위 경로에 `.git` 없음 | [확정] |

### 기준선 차단 상태

2026-08-09 재검사 결과:

| 파일 | 현재 값 | manifest 값 | 결과 |
|---|---|---|---|
| `gakumas.exe` | 길이 641,160, SHA `B311A720...D24CB` | 길이 동일, SHA `6E3579F0...F1C72` | Changed |
| `GameAssembly.dll` | 길이 155,684,864, SHA `8BDA021D...67B25` | 길이 155,680,768, SHA `3642CAAC...193D9` | Changed |
| `global-metadata.dat` | 길이 44,472,904, SHA `B6120AF6...D61DA` | 길이 44,472,824, SHA `911F412E...CE0CE` | Changed |
| `UnityPlayer.dll` | SHA `C19CB7B1...B8E5` | 동일 | Match |
| `version.dll` | SHA `594F9EB9...9098` | 동일 | Match |
| Localify 설정/버전 파일 | manifest와 동일 | 동일 | Match |

`gakumas.exe`는 여전히 Unity `6000.0.77f1`을 보고하지만 수정 시각이 2026-08-05이고, `GameAssembly.dll`과 metadata도 같은 날 바뀌었다. v0.53~v0.95 로그가 이 변경 뒤 생성됐으므로 공개 IL2CPP API 기반 런타임이 현재 바이너리에서 실제로 동작한 증거는 있다. 그러나 manifest가 승인되지 않은 상태라는 안전 차단은 별개다. 원본 파일을 수정하거나 과거 파일로 덮어쓰지 말고 업데이트 출처와 정상 설치 여부를 확인한 뒤 새 baseline을 명시적으로 승인해야 한다.

## 3. 구현 완료 기능과 검증 수준

### 로더와 공존

- **[확정]** BepInEx interop/Cpp2IL 없이 Doorstop + CoreCLR + 공개 IL2CPP export로 동작한다.
- **[확정]** Localify는 `version.dll`, Doorstop은 `winhttp.dll`을 사용하므로 현재 설치에서는 두 proxy가 공존한다.
- **[확정]** `GameAssembly.dll`, `UnityPlayer.dll`, 원본 에셋을 VR 모드가 패치하지 않는다.
- **[확정]** 설치 스크립트는 게임 실행 중 DLL 교체를 거부하고 기존 런타임을 `vrmod/rollback/`에 복사한다.
- **[확정]** OpenXR 초기화 예외는 `openxr-probe-failure`로 기록하고 bootstrap worker 안에서 삼켜 게임 프로세스의 일반 실행을 유지한다.

### 장면·방향·카메라 진단

- **[확정]** Unity 메인 스레드에서 scene, 실제 render width/height, `Screen.orientation`, 카메라와 UI 계층을 수집한다.
- **[확정]** 실제 게임은 가로 전환 뒤에도 `Screen.orientation == 1`을 유지할 수 있으므로 width/height가 방향 판정의 권위 신호다.
- **[사용자 실기 확인]** 세로 → 가로 → 세로 평면 패널 전환은 정상이다.
- **[주의]** `OrientationStabilizer`와 `SceneClassifier` 결과는 현재 진단 로그용이다. 이 분류기가 OpenXR 레이어를 직접 제어하지 않는다. 실제 패널 swapchain은 backbuffer 크기 변화로 재생성되고, 실제 immersive 전환은 fresh stereo texture 유무로 결정된다.

### 평면 패널

- **[사용자 실기 확인/현행 코드]** Virtual Desktop에서 최종 게임 backbuffer를 head-fixed OpenXR quad로 표시하는 기존 경로는 검증됐다. v0.150은 이를 완전 평면 문맥의 자동 정면 패널로 복원하고 stereo 문맥에서는 손 부착형 보조 quad로 전환한다.
- **[사용자 실기 확인]** 세로/가로 비율, 상하 반전, 방향, 색감/명부 날림이 사용할 수 있는 수준으로 보정됐다.
- **[확정: 코드]** `PreferCompositedGameBackBuffer = true`이므로 패널은 Localify 텍스트와 게임 UI 상태를 포함한 최종 합성 backbuffer를 우선한다.
- **[확정: 코드]** source 크기/format이 바뀌면 OpenXR panel swapchain을 재생성한다.

### 라이브 스테레오

- **[확정: 코드]** `Game3DManager` 카메라를 live source로 사용하고 좌·우 clone 카메라, 여섯 개의 RenderTexture(세 쌍), GPU event query를 만든다.
- **[확정: 코드]** OpenXR eye pose를 source transform의 로컬 offset으로 변환하고 물리 eye offset의 27.5%를 게임 월드에 적용한다.
- **[확정: 코드]** 눈별 비대칭 FOV를 `Matrix4x4.Frustum` projection에 적용한다. 좌우 원래 mapping을 유지한다.
- **[확정: 코드]** Quest 권장 2688x2880에 설정 scale을 적용한다. 현재 `0.65`의 1744x1872 eye RT를 사용하고 잘못된 설정은 `0.75`로 폴백한다.
- **[확정: 코드]** clone은 원본 `Camera.CopyFrom`, URP renderer index와 여러 `UniversalAdditionalCameraData` 속성을 복사하며 post-processing을 강제로 `true`로 둔다. 현재 AA는 원본 값을 복사한다.
- **[사용자 실기 확인: v0.86]** 경량 main-thread pump에서 약 20 eye pair/s, 체감 프레임 정상이다.
- **[사용자 실기 확인: v0.91 기준]** 후처리를 켠 영상은 PC와 거의 비슷하지만 특정 영역/전체 흐림과 빛 번짐이 남았다.
- **[사용자 실기 확인: v0.94]** 한 라이브는 2분 이상 유지됐다.
- **[사용자 실기/로그 확인: v0.96]** 첫 라이브의 27.5% 스테레오 생산은 1,668 pair까지 지속됐다. 두 번째 live 진입은 stale clone pointer로 크래시했다.
- **[확정: v0.99 코드]** source가 사라질 때 camera/eye RT/render request generation과 UI scene cache를 폐기하고 GPU query를 Release한다. 다음 concrete env source에서 모두 다시 생성한다.

### 라이브 UI

- **[확정: 코드]** 원본 `UICamera` target을 투명 RT로 두 번의 정상 Present 동안만 바꾸고 `3DTexture`, 알려진 Background/BlackTint Graphic을 잠시 cull한 뒤 원상 복구한다.
- **[확정: 코드]** `/UICanvasGroup/LiveOverlayContent/MusicTimeRoot/MusicTime`의 자식 `CanvasRenderer` 활성/cull/inherited alpha를 우선 감시한다.
- **[확정: 코드]** UI가 숨겨지면 registry를 즉시 clear하고, 표시되면 터치 이펙트가 가라앉도록 500ms 기다린 뒤 one-shot 재캡처한다.
- **[확정: 코드]** UI 전용 pixel shader는 RGB peak가 `3/255` 이하인 검정만 alpha 0으로 만든다. 영상/스테레오에는 black-key를 적용하지 않는다.
- **[사용자 실기 확인: v0.90]** 투명 배경, UI ON/OFF/재표시, 터치 이펙트 잔상 제거가 정상이다.
- **[주의]** one-shot이므로 UI가 켜진 동안 타이머나 애니메이션 같은 동적 내용은 고정될 수 있다.

### M5 topology와 개정된 M6 표시 invariant

- **[확정: 홈]** `Game3DManager`의 `_VLTargetTexture_2160x3840`을 활성 `.../3dTargetImage`가 표시하고, 조작 UI는 별도 `UICamera` Canvas다. `HomeMonitor`는 768x768 RT/Canvas로 world 안의 평면 영상 표면을 구성한다.
- **[확정: ADV]** 가로 `_VLTargetTexture_3840x2160`, 세로 `_VLTargetTexture_2160x3840` world를 `ADVEngine/.../Main Layer/Render Target`이 표시한다. Choices/Content/Player Control/UI Canvas는 `UICamera`에 분리돼 있다.
- **[확정: 영상]** Unity `VideoPlayer`는 0개이며 실제 경로는 `Campus.Common.CampusVideoPlayer`와 `OnDemandVideoPlayerImage`의 Canvas/RawImage/custom material 합성이다. URL·인증 정보는 수집하지 않는다.
- **[M6 world invariant]** camera 또는 scene 이름만으로 immersive를 허용하지 않는다. 활성 world-presenting RawImage와 source RT가 함께 유효해야 하며 custom/사전 렌더 영상에 가짜 stereo를 만들지 않는다.
- **[M6 panel invariant]** live 여부와 무관하게 최종 백버퍼 전체를 하나의 지속 갱신 panel source로 사용한다. fresh stereo가 없으면 view-space 정면에 자동 표시하고, stereo가 있으면 기본 왼손 보조 패널로 전환한다.
- **[M6 toggle invariant]** Grip 토글은 stereo 문맥의 손 패널에만 적용한다. 시작 OFF이고 왼손 Grip의 깊은 press/완전 release hysteresis로 한 번씩 토글한다. 패널 중심은 controller tip에 있고 view-space 수직·viewer-facing이며, 손이 HMD FOV 밖이면 숨긴다. OFF에서는 panel swapchain을 파괴하지 않되 백버퍼 복사, acquire/write, quad 제출과 pointer hit-test를 하지 않는다. 정면 주 콘텐츠 패널은 토글과 무관하게 자동 표시한다.
- **[M6 fallback]** world source 상실 시 이전 stereo generation을 즉시 clear하고 같은 최종 백버퍼 정면 패널로 전환한다. 이전 stereo 또는 평면 프레임을 잔상으로 남기지 않으며 PC 게임은 계속 실행한다.
- **[M7 input invariant/실기 완료]** 기본 포인터 손은 오른손이며 aim ray를 panel UV와 foreground game client 좌표에 매핑한다. A click/drag, trigger pre-press latch click/drag, B back, stick scroll과 원형 cursor는 v0.151 사용자 실기를 통과했다. 직접 터치와 패널 손·포인터 손·버튼·viewer-facing 설정은 남아 있다.

## 4. 현재 미완성 기능과 버그

우선순위와 무관한 전체 목록이다.

- **[주의/확정] 게임 기준선 불일치:** 위 세 핵심 파일이 manifest와 다르다. 사용자 승인으로 임시 호환성 실기는 진행하지만 baseline manifest는 별도 확인 없이 갱신하지 않는다.
- **[확정] v0.101 M2 수정:** v0.100에서 수동 창 리사이즈로 복구된 portrait 파손을 자동화하기 위해, 두 번째 이후 live 이탈마다 canonical portrait 높이에서 1픽셀 nudge 후 100ms 뒤 원복한다. 사용자 실기와 PID 30636 로그에서 성공했다.
- **[확정: v0.96 결함] stale clone pointer:** 첫 live 이탈 때 clone이 `Camera.allCameras`에서 제거됐는데 `_stereoCloneSetupReady`와 포인터가 남았다. 두 번째 source-ready 직후 coreclr AV가 발생했다.
- **[확정: v0.102 회귀] managed wrapper handle 수명:** 이탈 콜백에서 generation wrapper handle을 명시적으로 해제한 v0.102는 첫 live 이탈에서 coreclr access violation을 일으켰다. 해당 코드는 완전히 제거했다. M3에서는 우선 v0.101의 기존 generation 이벤트와 외부 process memory로 무변경 기준선을 수집하며, 동일한 해제 접근은 재사용하지 않는다.
- **[확정: 코드 제한] setup failure 복구 범위:** 정상 source 이탈은 v0.97에서 새 generation으로 복구한다. 그러나 `_stereoContinuousFailed`에 도달하는 실제 생성/렌더 실패는 여전히 process lifetime 동안 immersive를 중단할 수 있다.
- **[사용자 실기 확인] 후처리 잔여 문제:** v0.91에서 PC와 거의 같아졌으나 일부 blur/빛 번짐이 남았다. v0.92에서 AA만 꺼도 개선되지 않아 AA 단독 원인은 기각됐다.
- **[추정/미진단] 그림자·블룸 일부 누락:** 그림자는 post-processing과 별개일 수 있고, bloom/DoF/motion blur/volume update와 clone camera context를 분리 측정하지 않았다. 원본 Volume을 직접 변경하지 않는다.
- **[확정: 구조 제한] UI는 정지 one-shot:** 라이브 타이머, 애니메이션, 동적 강조를 지속 갱신하지 않는다.
- **[대체 예정] 라이브 alpha UI:** one-shot/black-key UI는 새 제품 목표가 아니다. 최종 백버퍼 손 패널에서 좌상단 시계를 포함한 전체 화면이 지속 갱신되는지 구현 후 확인한다.
- **[해결: v0.146/v0.147 결함]** 홈·커뮤니케이션의 keyed 플랫 overlay 중첩/노이즈와 가로 화면비 파손은 당시 정책의 실패다. v0.150 이후 overlay를 제출하지 않고 정면/손 최종 백버퍼 패널로 대체했으며 사용자가 평면 표시와 종횡비를 정상 판정했다.
- **[미검증: v0.148]** Present 경계 동기 UI 합성과 비-live clone 렌더 순서 수정은 빌드·설치됐지만 사용자 실기가 없고 개정된 손 패널 정책도 구현하지 않았다.
- **[확정: 절충] black-key 손실 가능:** 순수 검정 글자/아이콘 세부까지 투명해질 수 있다.
- **[보류] 스킬 최상위 2D 이미지:** 플레이 중 스킬 이미지/애니메이션 누락은 알려져 있으나 사용자가 해당 디버깅을 다시 요청할 때만 착수한다.
- **[부분 완료] Quest 컨트롤러 입력:** ray, A/trigger click·drag, B back, stick scroll과 좌표 변환은 사용자 실기를 통과했다. 직접 터치와 GUI 기반 역할·버튼 교환은 M7에 남아 있다.
- **[미검증] 예비 런타임:** SteamVR OpenXR와 Meta Quest Link OpenXR 회귀 테스트가 없다.
- **[미구현] 제품화:** 설정 파일/GUI, 설치·제거 패키지, 업데이트 호환성 게이트와 안전한 장시간 자원 계측이 없다.
- **[확정: 도구 복귀]** v0.102의 generation 자원 검사 스크립트는 제거했고 기존 세 진단 스크립트 기본값도 원래 `0.52.0`으로 복귀했다. v0.101 검사 시 `-ExpectedVersion 0.101.0`을 명시한다.

## 5. 실제 런타임 구조

```text
DMM launch
  ├─ version.dll -> Localify (보존)
  └─ winhttp.dll -> Doorstop/CoreCLR
       └─ Doorstop.Entrypoint.Start()
            ├─ D3D11/DXGI/Present hook 설치
            └─ RuntimeProbe worker
                 ├─ GameAssembly 공개 IL2CPP export 로드
                 ├─ Time.get_frameCount icall hook -> Unity main thread sampler
                 │    ├─ scene/orientation/camera/UI 관측
                 │    ├─ live source camera 탐색
                 │    ├─ 좌·우 clone의 정상 Unity render arm/finalize
                 │    └─ world/UI/stereo D3D11 texture registry 게시
                 └─ OpenXrProbe.Collect() 전용 frame loop
                      ├─ active runtime/HMD/session/view 조회
                      ├─ 기본 game-backbuffer quad
                      ├─ fresh stereo가 있으면 projection layer로 대체
                      └─ fresh UI가 있으면 alpha quad 추가
```

### 스레드와 동기화

- `Entrypoint.Start()`는 먼저 graphics hook을 설치하고 background worker 하나를 시작한다.
- worker는 IL2CPP domain/assembly 준비를 기다린 뒤 `UnityEngine.Time::get_frameCount()` icall을 Dobby로 후킹한다.
- hook callback `OnFrameCount()`는 실제 Unity main thread에서 실행된다. 매 새 게임 frame마다 `TryPumpStereo()`, 약 100ms마다 `TryCapture()`를 실행한다.
- OpenXR frame loop는 background worker에서 계속 `xrWaitFrame/xrBeginFrame/xrEndFrame`을 실행한다.
- Unity→OpenXR texture 전달은 `UnityRenderSourceRegistry`의 COM AddRef/Release lease와 freshness timestamp를 사용한다.
- OpenXR→Unity eye pose 전달은 `OpenXrStereoStateRegistry` snapshot을 사용한다.
- Unity eye pair는 좌·우 clone actual completion을 확인한 뒤 카메라를 끄고 필요한 GPU event query를 거쳐 완성된 pair만 registry에 게시한다.

### OpenXR 구성

- 64-bit `HKLM\SOFTWARE\Khronos\OpenXR\1\ActiveRuntime` manifest를 읽는다.
- loader 우선순위는 `vrmod/runtime/openxr_loader.dll`, 그다음 SteamVR 설치 경로다. 현재 mod runtime에는 loader가 없으므로 SteamVR의 `openxr_loader.dll` 파일을 사용하지만 SteamVR 프로세스를 실행할 필요는 없다. 실제 active runtime은 Virtual Desktop이다.
- OpenXR session은 게임의 Present swapchain과 연결된 D3D11 device로 한 번 생성한다.
- READY 이벤트를 최대 10초 기다린다. 초기화 실패 뒤 같은 프로세스에서 자동 재시도하는 구조는 아니다. 따라서 VD 연결과 active OpenXR runtime 설정을 게임 실행 전에 끝낸다.
- frame loop에는 더 이상 90초/120초/12,000 frame 상한이 없다. `STOPPING`, `LOSS_PENDING`, `EXITING` 또는 frame 오류까지 계속된다.
- OpenXR session이 살아 있어도 fresh stereo texture가 없으면 기본 game backbuffer quad만 제출한다. 이것이 live 이탈/실패 시 VR 평면 폴백이다.

### 카메라와 레이어

| 데이터 | 생산자 | freshness | OpenXR 표현 |
|---|---|---:|---|
| 최종 게임 backbuffer | DXGI swapchain | 정면 자동 패널 또는 visible 손 패널 frame에서 직접 획득 | v0.154는 flat-only view-space 정면 quad, stereo에서는 controller tip 위치의 view-space upright/viewer-facing 보조 quad |
| live world RT | `Game3DManager.targetTexture` | 1,500ms | 현재 `PreferCompositedGameBackBuffer=true`라 기본 panel source로 사용하지 않음; 진단/카메라 식별용 |
| stereo eye pair | 좌·우 clone 정상 Unity render | 750ms | `XrCompositionLayerProjection` |
| live UI RT | 원본 `UICamera` one-shot | 1,500ms touch 갱신 | alpha quad, z=-1.58m |

정면 패널 폭/높이는 source 종횡비를 유지하며 최대 1.8m x 1.3m, 손 패널은 최대 0.42m다. stereo가 준비되면 projection layer가 첫 레이어가 되고 Grip-enabled 손 패널과 cursor가 추가된다. stereo가 없으면 정면 패널과 cursor만 제출한다.

## 6. 라이브 진입·이탈·source camera 전환 흐름

### 최초 진입

1. `TryCapture()`가 scene과 카메라를 약 1초 간격으로 다시 열거한다.
2. scene이 `Live` 또는 `env_3d_live_*`이고 이름이 정확히 `Game3DManager`인 camera에 유효한 target texture가 있으면 `_lastLiveCamera`와 live world registry를 갱신한다.
3. 활성 scene이 구체적인 `env_3d_live_*`이고 `width > height && _lastLiveCamera != 0`이면 `_stereoPumpEligible=true`가 되고 `stereo-live-source-ready`를 기록한다. 중간 `Live` 장면은 source가 있어도 평면을 유지한다.
4. 최초 generation에서 `TryEnsureStereoCloneResources()`가 네 eye RT, 두 clone camera, render request 객체, GPU query를 만든다. clone에 `DontDestroyOnLoad`를 요청해도 실제 scene 전환에서 제거될 수 있으므로 process-lifetime 자원으로 간주하지 않는다.
5. setup 후 3초 기다린다.
6. `TryPumpStereo()`가 33ms 목표로 fresh OpenXR view를 읽고, alternate buffer pair를 고른 뒤 새 source camera의 transform/Camera 속성/눈별 projection을 적용한다.
7. 두 clone camera를 enable해 두 번의 정상 Unity Present 동안 렌더한다.
8. `FinalizeNaturalStereoRender()`가 camera를 disable하고 GPU 완료를 기다린다. 최초 pair는 visible-pixel 검사 후 양쪽을 원자적인 한 쌍으로 registry에 게시한다.
9. OpenXR thread는 750ms 이내의 eye pair와 valid views가 있으면 panel quad 대신 projection layer를 제출한다.

### UI 표시 전환

1. live stereo가 한 번 검증된 뒤 실제 자식 CanvasRenderer 표시 상태를 본다.
2. UI OFF면 UI registry를 즉시 clear한다.
3. UI ON이면 500ms 기다린 뒤 UICamera를 UI RT로 두 Present 동안 redirect한다.
4. 3D RawImage와 알려진 불투명 배경을 잠시 cull하고 반드시 원상 복구한다.
5. 결과가 visible이면 registry에 게시하고 OpenXR thread가 black-key를 적용해 alpha quad로 제출한다.

### 이탈

1. 카메라 재탐색에서 유효한 `Game3DManager`를 못 찾으면 `_lastLiveCamera=0`, live world registry clear.
2. stereo eligibility가 false로 바뀌면 `RetireStereoCameraGeneration()`이 armed clone/UI capture를 복구하고 stereo/UI registry를 즉시 clear한다.
3. clone camera/object 포인터, setup/publish 상태와 scene-bound UI camera/renderer/canvas cache를 모두 폐기한다. 비영 Unity pointer만으로 native object 생존을 판단하지 않는다.
4. eye RenderTexture와 render request wrapper도 scene-bound로 간주해 포인터를 폐기한다. D3D11 GPU query는 Release한다.
5. OpenXR session/frame loop는 종료하지 않는다. eye pair가 사라진 다음 frame부터 최종 game backbuffer quad가 폴백으로 남는다.
6. **[확정: v0.101]** 두 번째 이후 generation 이탈이면 저장된 canonical portrait 크기에 대해 windowed `Screen.SetResolution(width, height-1, false)`를 호출하고 약 100ms 뒤 원래 크기로 복구한 다음 Canvas layout을 갱신한다. Win32/DXGI resize나 창 위치 변경은 하지 않는다.

### 다음 라이브

1. 새 `Game3DManager`를 찾으면 같은 OpenXR session에서 eligibility를 다시 true로 만든다.
2. **[확정: 현재 코드]** 좌·우 clone camera, eye RT 여섯 개(triple pair), render request와 GPU query를 실제 env scene에서 모두 새로 만든다. 새 eye RT는 검정 clear한다.
3. **[확정: v0.99 코드]** 새 source의 `Camera.CopyFrom`과 `CopyUniversalAdditionalCameraData()`를 새 clone에 적용하므로 곡별 renderer/post-process/AA 상태를 이전 generation에서 가져오지 않는다.
4. 3초 warm-up 뒤 새 pair를 visible-pixel 검증하고 registry에 게시한다.
5. **[확정: v0.99 실기]** 세 generation 모두 `ready → clones-ready → stereo-natural-output-validated → publish-rate`에 도달했고 clone/render failure는 없었다. 남은 M2 결함은 portrait 복귀 레이아웃과 후속 UI capture다.

## 7. 핵심 파일과 역할

| 파일 | 역할 |
|---|---|
| `AGENTS.md` | 영구 안전/문서 동기화 규칙 |
| `docs/VR_HANDOFF.md` | 세션 간 현재 구현·검증·우선순위의 운영 기준 |
| `docs/VR_MILESTONES.md` | 단계별 완료 조건, M2~M6 달성 근거, 현재 M7 계획, 사용자 알림 규칙 |
| `docs/GAKUMAS_VR_STATUS.md` | 버전별 설치/실기 상태와 이슈 목록 |
| `docs/GAKUMAS_VR_DESIGN.md` | 목표 아키텍처, 기술 결정, 폴백 설계 |
| `vrmod/CHANGELOG.md` | 모든 runtime 버전 변경과 실기 결과 |
| `doorstop_config.ini` | Doorstop enable, target assembly, CoreCLR 경로 |
| `vrmod/src/GakumasVR.RuntimeBootstrap/Entrypoint.cs` | 진입점, bootstrap version, IL2CPP API binding, JSONL event schema |
| `.../D3D11DeviceCapture.cs` | D3D11/DXGI 생성 및 Present hook, device/context/swapchain/serial 포착 |
| `.../D3D11Interop.cs` | COM texture copy/readback/BMP, RTV, event query, GPU fence helper |
| `.../D3D11VerticalBlitter.cs` | 상하 반전 blit, UI black-key pixel shader |
| `.../MainThreadSampler.cs` | scene/camera/UI 탐색, stereo clone 생산, natural UI capture |
| `.../OpenXrProbe.cs` | runtime/HMD/session, swapchain, quad/projection/UI layer frame loop |
| `.../OpenXrStereoStateRegistry.cs` | OpenXR eye pose/FOV/recommended size를 Unity main thread에 전달 |
| `.../UnityRenderSourceRegistry.cs` | world/UI/stereo D3D11 texture의 COM-safe cross-thread lease |
| `vrmod/src/GakumasVR.Core/OrientationStabilizer.cs` | width/height 기반 5-frame 방향 안정화 진단 |
| `vrmod/src/GakumasVR.Core/SceneClassifier.cs` | scene presentation 분류 모델; 현재 runtime 제어가 아닌 진단 |
| `vrmod/tests/GakumasVR.Core.Tests/Program.cs` | scene/orientation 상태 머신 9개 독립 테스트 |
| `vrmod/scripts/Build-VRMod.ps1` | restore, core tests, Release runtime build |
| `vrmod/scripts/Install-Bootstrap.ps1` | 실행 중 교체 방지, rollback, runtime 3파일 설치 |
| `vrmod/scripts/Verify-Baseline.ps1` | 게임/Localify 핵심 파일 hash gate |
| `vrmod/scripts/Test-RuntimeBootstrap.ps1` | 최신 IL2CPP runtime-ready 확인 |
| `vrmod/scripts/Test-SceneDiagnostics.ps1` | 지정 버전 render snapshot 검사 |
| `vrmod/scripts/Test-PresentationState.ps1` | 지정 버전 portrait/landscape/round-trip 로그 검사 |
| `vrmod/scripts/Test-OpenXrRuntime.ps1` | 지정 버전 HMD/session/panel 결과 검사 |
| `vrmod/logs/runtime-bootstrap.jsonl` | 누적 runtime 사실의 원본; 약 76MB이므로 version/PID로 범위를 좁혀 읽기 |
| `vrmod/baseline/installation-manifest.json` | 현재는 오래된 지원 기준선; 무단 갱신 금지 |
| `vrmod/rollback/` | 설치 직전 runtime과 폐기한 loader 경로 보관 |

`vrmod/src/GakumasVR.Bootstrap`과 `GakumasVR.Diagnostic`은 초기 BepInEx 경로의 보존 소스다. 현재 제품 runtime으로 착각하지 않는다. `Test-Coexistence.ps1`도 interop 생성 전제의 과거 진단이므로 현재 Doorstop 경로의 필수 테스트가 아니다.

## 8. 중요한 클래스·메서드·훅

### 진입/IL2CPP

- `Doorstop.Entrypoint.Start()` — D3D hook 설치와 worker 시작. 중복 시작 방지.
- `RuntimeProbe.Run()` — GameAssembly/domain/assembly 준비, sampler 설치, D3D 확인, OpenXR frame loop 실행.
- `Il2CppApi` — `il2cpp_domain_get`, assembly/class/method/field 탐색, `il2cpp_runtime_invoke`, GC handle 등 공개 export wrapper.
- `MainThreadSampler.Install()` — `UnityEngine.Time::get_frameCount()` icall을 Dobby로 hook.
- `MainThreadSampler.OnFrameCount()` — 게임 frame당 stereo pump, 100ms 진단 sampler의 메인 진입점.

### D3D11/DXGI

- `D3D11DeviceCapture.Install()` — `D3D11CreateDevice`, `D3D11CreateDeviceAndSwapChain`, DXGI factory의 네 swapchain 생성 메서드 hook.
- `CapturePresentDevice()` — 실제 presentation swapchain의 D3D11 device/context를 QueryInterface로 확보.
- `TryInstallPresentHook()` / `OnPresent()` — swapchain vtable Present(index 8) hook, `PresentSerial` 증가.
- `D3D11Interop.WaitForGpu()` — event query fence. 완료 전 eye/UI texture를 OpenXR에 게시하면 안 된다.
- `D3D11VerticalBlitter.BlitFlipped()` — offscreen Unity RT의 상하 반전과 UI에만 transparent-black 적용.

### camera/stereo/UI

- `TryCapture()` — scene/orientation/카메라/UI 상태 및 stereo eligibility 갱신.
- `CaptureCameras()` — `Game3DManager` + target texture를 유효 live source로 고정.
- `TryEnsureStereoCloneResources()` — 각 concrete live generation마다 eye RT/request/query와 clone/URP data를 모두 새로 생성.
- `RetireStereoCameraGeneration()` — source 상실 때 armed 상태 복구, registry clear, scene-bound camera/UI 포인터와 generation 상태 폐기.
- `ApplyWindowedResolutionNudgeStep()` — v0.101의 제한된 portrait 복구 경로. Unity `Screen.SetResolution`으로 1픽셀 nudge/원복하며 성공·실패 event를 기록한다.
- `TryPumpStereo()` — 매 게임 frame에서 33ms target과 two-Present state machine 구동.
- `TrySubmitStereoRenderDiagnostic()` — 이름은 legacy지만 현재 연속 stereo arm 경로.
- `FinalizeNaturalStereoRender()` — disable, actual completion/fence 검증, triple-buffer pair 게시.
- `CopyUniversalAdditionalCameraData()` — renderer index와 URP 속성 복사, clone post-processing true.
- `TryGetLiveUiVisibilityState()` — 자식 CanvasRenderer 우선, CanvasGroup fallback.
- `TryCaptureNaturalUiLayer()` / `RestoreNaturalUiCapture()` — UICamera one-shot redirect와 반드시 필요한 복원.
- `UnityRenderSourceRegistry.ClearStereoTextures()` — source 이탈 시 과거 eye 즉시 제거.

### OpenXR

- `OpenXrProbe.Collect()` / `ProbeInstance()` — active runtime, extension, system, D3D11 요구사항.
- `CreateAndProbeSession()` — 실제 game Present device로 session 생성.
- `RunTestPatternFrameLoop()` — legacy 이름이지만 현재 지속적인 제품 frame loop.
- `OpenXrStereoStateRegistry.UpdateViews()` — predicted eye pose/FOV를 main thread로 전달.
- `CopyStereoEyeToSwapchain()` — eye RT를 OpenXR eye swapchain으로 flip/copy/fence.
- `CreatePanelSwapchainResources()` — 평면 및 alpha UI quad의 크기/pose 생성.

## 9. 런타임 로그로 확인된 사실

- 로그는 한 줄당 하나의 JSON object이며 시간은 `timestampUtc`라는 이름과 달리 현재 `+09:00` offset으로 직렬화된 기록도 있다. 문자열 비교보다 JSON datetime으로 처리한다.
- 최신 v0.97 PID 29232는 첫 live를 164 pair까지 약 18.4~21 pair/s로 게시하고 generation retirement에 성공했다.
- 같은 session의 두 번째 live는 임시 `Live`에서 clone-ready 후 실제 `env_3d_live_all001-00-noon`으로 바뀌었다. 재사용 RT의 첫 pair가 validated됐지만 87ms 뒤 `stereo-natural-render-failure`가 `NullReferenceException`을 기록했고 이후 평면 폴백했다. 프로세스 크래시는 없었다.
- 두 번째 env snapshot에는 새 clone이 `Camera.allCameras`에 없었다. 이전 곡 픽셀이 남은 RT가 첫 visible 검사에 통과해 잠깐 VR이 표시된 것으로 코드/로그가 일치한다.
- v0.98 PID 38100 첫 live는 621 pair까지 약 18.4~20.7 pair/s로 게시됐다. 두 번째 concrete env/source-ready 감지는 성공했지만 `stereo-camera-clones-failure`가 `clear-reused-eye-render-targets` NRE를 기록했다.
- v0.99 PID 18472는 세 live generation을 모두 ready/validated/publish했다. 지속은 약 8.6초/49.2초/16.1초, 최대 pair는 97/882/249이며 clone/render failure는 0이다. 후속 live UI capture failure는 21건/1건이다.
- v0.99 두 번째 이탈 뒤 `Screen` 높이가 1920에서 1788/1780/1785로 변한 뒤 1920으로 돌아왔고 PC portrait UI는 왼쪽으로 압축됐다. 당시 모드 코드에는 window/Screen/DXGI resize 호출이 없었다.
- v0.100 최신 PID 25368은 ready generation 5개를 기록했다. generation 3/4/5의 지속은 약 44.3초/40.3초/38.0초이고 최대 pair는 782/737/634이며 clone/render failure는 없었다. 따라서 M2의 재진입과 두 live 30초 이상 조건은 충족했다.
- 같은 v0.100 실기에서 portrait 레이아웃 파손이 다시 나타났고 사용자가 창을 수동 리사이즈하자 즉시 정상화됐다. 이는 resize에 의한 presentation/Canvas rebind가 효과적이라는 확정된 증상이며, 정확한 내부 원인이 Unity Canvas인지 swapchain인지는 아직 추정이다.
- v0.101 PID 30636은 `ssmk001`, `all006`, `all001` source를 순서대로 찾아 clones-ready와 generation retire를 각 3회 기록했다. 첫 두 generation은 normal output validation까지 도달했다.
- 두 번째와 세 번째 이탈에서 `1080x1919` nudge 뒤 `1080x1920` restore가 각각 기록됐다. `portrait-resolution-nudge-failure`, clone/render/UI capture/sampler failure는 0이고 사용자가 수동 개입 없는 정상 화면을 확인했다.
- v0.100 지속 기록과 v0.101 복귀 기록은 서로 다른 session이지만 각각 M2의 시간 조건과 복귀 조건을 직접 검증한다. 두 근거를 결합해 M2를 달성 처리했다.
- v0.96 PID 37120 첫 live는 1,668 pair까지 약 20 pair/s로 게시됐고 UI capture/show/hide도 정상 기록됐다.
- 첫 live 이탈 snapshot에서는 clone을 포함한 camera 4개였으나 다음 `Live`/`OutGame` snapshot에서는 원본 camera 2개만 남았다. 두 번째 source의 `stereo-live-source-ready` 직후 managed failure event 없이 프로세스가 종료됐다.
- Windows `.NET Runtime` event 1023과 Application Error event 1000은 같은 PID에서 `coreclr.dll`, exit code `c0000005` access violation을 기록했다. 코드 상태와 함께 보면 사라진 clone의 stale wrapper 사용이 원인으로 확정된다.
- v0.95 네 session 모두 `runtime-ready`, D3D11 ready, OpenXR stereo view sample, stereo clone ready, 첫 eye pair validated까지 도달했다.
- v0.95에는 `sampler-failure`, `stereo-natural-render-failure`, `openxr-probe-failure`가 없다.
- v0.95 단일 라이브 publish-rate 평균은 session별 약 19.93, 19.90, 20.79, 19.62 pair/s였다.
- v0.95 장시간 session은 source 이탈까지 약 2분 27초 또는 2분 30초 동안 publish-rate 기록을 유지했다.
- v0.95 PID 34560과 38492에서는 이탈 후 `stereo-live-source-ready`가 다시 찍혔지만 그 뒤 새 pair validation/rate 기록이 없다. 이것만으로 재진입 성공을 선언할 수 없다.
- v0.95 PID 30368은 `env_3d_live_all006-00-night`, PID 17684는 `env_3d_live_ssmk001-00-noon`을 한 번씩 포함한다. 서로 다른 곡을 각각 다른 프로세스에서 실행한 증거이지 같은 프로세스 재진입 증거가 아니다.
- v0.94는 143개의 publish-rate와 2,903 eye pair까지 기록했다. 이탈 뒤 `OutGame`, 다시 `Live 1920x1080 cameraCount=2`까지 갔지만 새 env 3D source가 나타나기 전에 로그가 끝났다. 사용자 실기에서 이 시점 VR이 꺼졌다.
- Virtual Desktop 성공 로그의 runtime은 `VirtualDesktopXR (Bundled)`, reported runtime은 `VirtualDesktopXR`, HMD system은 `Oculus Quest2`였다.
- 권장 eye size는 2688x2880, 실측 IPD는 약 0.06146~0.06154m, view state flags는 15였다.
- v0.90 로그와 사용자 실기에서 UI hidden → shown → 500ms settle → capture ready, 다시 hidden clear가 정상이다.
- `openxr-runtime-ready` event는 `OpenXrProbe.Collect()`가 반환한 뒤에야 기록된다. frame loop가 정상 동작 중인 동안 이 event가 없는 것은 실패 증거가 아니다. `openxr-stereo-view-sample`, stereo events와 실제 HMD 출력으로 active session을 판단한다.
- 누적 로그 전체에는 초기 개발 버전의 bootstrap/OpenXR failure가 있으므로 전체 파일의 마지막 failure만 보고 현재 버전을 실패 처리하지 않는다. 반드시 마지막 해당 version의 `bootstrap-start` PID로 session을 자른다.
- 로그 폴더에서 viewer ID/token/authorization 이름 패턴은 검출되지 않았다. 그래도 raw 로그를 공유하기 전에는 다시 검사한다.

## 10. 폐기하거나 실패한 접근

| 접근 | 결과 | 결정 |
|---|---|---|
| BepInEx 6 pre.2 + Cpp2IL | metadata 31 미지원 | 폐기 |
| BepInEx be.785 + 최신 Cpp2IL | metadata 31.1 파싱 후 code/metadata registration 탐색 실패 | interop 비의존 Doorstop 경로 유지 |
| Window subclass / `GetMessage` 계열 main-thread callback | access denied 또는 callback 없음 | `Time.get_frameCount` icall hook 사용 |
| game world RT만 평면 panel source | UI/Localify 누락 | 최종 game backbuffer를 기본 panel source로 사용 |
| eye 강제 좌우 swap | 위치/방향 정렬이 크게 악화 | 원래 OpenXR eye index mapping 유지 |
| 너무 이른 clone/manual render | 검은 eye RT | live source + XR view + 정상 render timing 대기 |
| URP data 없는 독립 clone camera | 검은 출력 | `UniversalAdditionalCameraData`를 명시적으로 추가/복사 |
| `JsonUtility.FromJsonOverwrite`로 URP 복사 | metadata에 필요한 메서드 없음 | renderer index/속성 개별 복사 |
| 반복 `Camera.SubmitRenderRequestsInternal` | 조명·후처리가 프레임마다 생겼다 사라짐 | 정상 Unity render loop에서 camera enable 후 두 Present 대기 |
| single eye buffer를 생산/소비 동시 사용 | 잠재적 tearing/눈 불일치 | double buffer + GPU event query |
| 완성 eye pair 고정 | 안정적이나 정지화면 | 진단에만 사용, 연속 이중 버퍼로 전환 |
| 단순 30fps interval 변경 | 약 2fps로 저하 | 100ms sampler에서 pump를 분리해 매 frame 경량 실행 |
| 반복 URP UI render request | PC 화면 플리커링 | 기본 비활성, 제품 경로 금지 |
| CanvasRenderer mesh replay | 58 draw이나 결과 투명/빈 RT | 비활성/폐기 |
| UICamera 초기 one-shot | 최초 UI hidden 또는 sparse visibility 검사로 빈 결과 | 실제 UI 표시 감지와 retry 추가 |
| 배경 Graphic cull만 사용 | alpha 255 검은 배경 지속 | UI 전용 black-key 사용 |
| 상위 UICanvasGroup만 토글 신호로 사용 | UI OFF를 못 따라감 | 실제 자식 CanvasRenderer 우선 |
| UI 표시 직후 캡처 | touch 이펙트가 정지 잔상으로 남음 | 500ms settle 유지 |
| clone AA None | blur/빛 번짐이 유지 | AA 단독 원인 가설 기각, 원본 AA 복사로 복귀 |
| 30초 stereo / 90초 OpenXR 제한 | 정지 또는 VR 종료 | 임의 상한 제거, session event 기반 수명 |

## 11. 반드시 유지할 invariant와 주의사항

1. 원본 `GameAssembly.dll`, `UnityPlayer.dll`, `version.dll`, 게임 에셋을 수정하거나 과거 버전으로 되돌리지 않는다.
2. Localify 번역/폰트/텍스처/설정을 보존한다. 최종 backbuffer panel은 Localify 호환의 회귀 기준이다.
3. 게임 실행 중 runtime DLL을 교체하지 않는다. 설치 전 rollback을 만든다.
4. baseline mismatch가 있으면 자동으로 manifest를 덮어쓰지 않는다. 업데이트가 정상인지 확인하고 명시적으로 새 기준선을 승인한다.
5. Unity object/Camera/Canvas 변경은 main thread에서만 한다. OpenXR worker에서 `il2cpp_runtime_invoke`를 호출하지 않는다.
6. 원본 camera, UICamera target, CanvasRenderer cull을 바꿨다면 모든 성공/실패 경로에서 원상 복구한다.
7. black eye/UI texture를 registry에 게시하지 않는다. 최초 visible validation과 GPU fence를 유지한다.
8. 좌·우를 독립 시점에 교체하지 않는다. 완성된 pair만 원자적으로 게시하고 OpenXR가 읽는 동안 COM lease를 유지한다.
9. source camera가 없으면 old stereo를 즉시 clear하고 game backbuffer panel로 폴백한다. 오래된 곡의 eye를 touch해 살려두지 않는다.
10. eye mapping을 임의로 swap하지 않는다. 물리 eye pose와 동일 index를 유지한다.
11. post-processing flicker를 만든 반복 manual render request를 되살리지 않는다. 정상 Unity Present 동기화 경로를 유지한다.
12. 원본 Volume/lighting asset을 전역 변경하지 않는다. 시각 A/B는 clone 전용이어야 한다.
13. OpenXR 실패가 게임 종료로 전파되지 않게 한다. 새 예외 경로도 worker 경계에서 안전하게 기록/폴백한다.
14. `openxr-runtime-ready` 부재만으로 active session 실패를 선언하지 않는다.
15. 빌드, 설치, PC 실행, VR 실기는 서로 다른 상태다. 사용자 확인 없이 “검증 완료”로 올리지 않는다.
16. 로그에 viewer ID, token, 실행 인자 또는 인증 정보를 추가하지 않는다.
17. v0.131 영상, v0.90 UI, v0.141 성능을 회귀 기준으로 유지한다.

## 12. 빌드·설치·테스트·재현

### 안전 사전 검사

```powershell
Get-Process -Name gakumas -ErrorAction SilentlyContinue
.\vrmod\scripts\Verify-Baseline.ps1
```

현재 두 번째 명령은 실패하는 것이 재현된다. 원칙적으로 새 기준선 승인 전에는 설치/실기를 중단한다. 사용자가 현재 설치본의 임시 호환성 테스트를 명시적으로 승인한 기존 예외 범위에서만 후속 검증을 진행하며, 제품 기준선 검증 완료와 구분한다.

### 빌드

```powershell
.\vrmod\scripts\Build-VRMod.ps1
```

이 명령은 core tests를 먼저 실행하고 runtime을 Release로 빌드한다. 로컬 NuGet/cache는 `vrmod/.dotnet-home`, `vrmod/.nuget-packages`를 사용한다.

### 설치

게임이 완전히 종료됐고 baseline이 승인된 뒤에만:

```powershell
.\vrmod\scripts\Install-Bootstrap.ps1
```

스크립트가 기존 DLL 세 개를 timestamped rollback에 저장하고 `vrmod/runtime/`으로 복사한다. 현재 v0.154 설치 SHA-256은 위 표와 같고, 직전 v0.153은 `runtime-bootstrap-v0.153.0-20260811-204412`에 보관됐다.

### v0.103 / M3 라이팅 가림 A/B — 부분 개선

1. PC와 VR에서 광원과 카메라 사이를 캐릭터 또는 가까운 물체가 가로막는 같은 구도를 확인한다.
2. 빛이 전경 물체 위로 통과해 보이던 정도가 줄었는지, 좌우 눈 결과가 같은지 확인한다.
3. UI ON/OFF/재표시와 라이브 이탈 후 portrait 복귀가 유지되는지 확인한다.
4. `stereo-camera-clones-ready`의 `stereoCloneRequiresDepthTexture=true`, depth option, render type, renderer index와 failure 유무를 확인한다.

사용자는 약간 개선됐지만 캐릭터 파묻힘이 일부 남는다고 판정했다. PID 35804는 서로 다른 두 live에서 clone depth true/On과 failure 0을 확인했다.

### v0.104 / M3 후처리 OFF A/B — 원인 분리 성공

1. v0.103과 같은 유형의 강한 역광/가림 구도를 확인한다.
2. 캐릭터 윤곽과 표정이 더 잘 보이는지, 빛이 캐릭터 위를 덮는 현상이 사라지는지 확인한다.
3. 동시에 블룸·색감·DoF가 빠져 화면이 얼마나 평평해졌는지 확인한다.
4. 좌우 눈 일치와 UI, 라이브 이탈만 짧게 회귀 확인한다. 장시간 실행은 필요 없다.

사용자 실기에서 캐릭터 파묻힘은 사실상 사라지고 좌우는 문제 없음으로 판정했다. 다만 영상이 다소 밋밋하므로 최종 화질로 채택하지 않는다.

### v0.105 / M3 후처리+LDR 절충 A/B — 실패/철회

1. v0.103/v0.104와 같은 강한 역광 구도에서 캐릭터 가독성을 확인한다.
2. v0.104보다 색감과 입체감이 돌아왔는지 확인한다.
3. v0.103처럼 빛이 다시 캐릭터를 덮는지 확인한다.
4. 좌우 눈은 명확한 불일치가 있는지만 보고, 짧은 live 한 번으로 판정한다.

사용자 실기에서 해결 이전의 캐릭터 파묻힘이 그대로 재발했다. HDR OFF 가설을 기각하고 소스·설치를 v0.104로 정확 복귀했다.

### v0.106 / M3 VLBloom 단독 OFF A/B — 폴백으로 무효

1. v0.104/v0.105와 같은 강한 역광 구도에서 캐릭터 가독성을 확인한다.
2. v0.104보다 색감과 입체감이 돌아오면서도 캐릭터 파묻힘이 재발하지 않는지 확인한다.
3. 짧은 live 한 번으로 좌우 눈, UI, 이탈 후 portrait 복귀만 회귀 확인한다. 이 효과 판별에 30분 실행은 필요 없다.
4. 판정 전 로그에서 `stereo-visual-effect-override-ready`를 확인한다. `stereo-visual-effect-override-fallback`이면 VLBloom 단독 OFF가 아니라 v0.104 전체 OFF 폴백을 본 것이므로 A/B 판정을 무효로 한다.

PID 27020은 `VolumeComponent.set_active/1` MissingMethodException으로 실제 폴백했다. live 연속 publish와 generation retire는 동작했지만 화면은 v0.104 전체 OFF 기준이므로 VLBloom 단독 결과가 아니다. v0.107에서 기반 `VolumeComponent.active` bool field 쓰기로 수정·설치했으며 같은 절차를 반복한다.

### Virtual Desktop 실행 순서

1. Quest에서 Virtual Desktop로 PC에 먼저 연결한다.
2. PC의 active OpenXR runtime이 Virtual Desktop인지 확인한다.
3. SteamVR 프로세스는 실행하지 않는다. 단, 현재 loader fallback 때문에 SteamVR 설치의 `openxr_loader.dll` 파일은 필요하다.
4. Virtual Desktop의 Games 탭이 아니라 Quest에서 보이는 PC 데스크톱에서 DMM launcher의 실행 버튼을 누른다.
5. title까지만 도달하면 bootstrap/패널 기본 실행은 확인할 수 있다. stereo/UI/re-entry 판정에는 반드시 live 진입이 필요하다.

### v0.101 / M2 portrait 자동 복귀 시나리오 — 통과

사용자가 승인한 임시 호환성 테스트로 한 게임 프로세스에서 짧게 수행한다.

1. 첫 번째 라이브에 진입해 stereo가 시작될 때까지만 기다린 뒤 평면 panel로 이탈한다.
2. 첫 이탈의 portrait 화면이 정상인지 확인한다.
3. 게임을 끄지 않고 다른 두 번째 라이브에 진입해 새 stereo가 표시되는지 확인한 뒤 이탈한다.
4. 두 번째 이탈 때 1픽셀 크기 변경이 잠깐 보일 수 있다. 별도 수동 리사이즈 없이 원래 portrait 크기와 전체 너비 UI로 자동 복귀하는지 확인한다.
5. 최신 v0.101 PID에서 `portrait-resolution-nudge-applied`와 `portrait-resolution-restored`가 순서대로 있고 `portrait-resolution-nudge-failure`가 없는지 확인한다.

v0.100에서 두 live 30초 이상 조건을 충족했고 v0.101에서 자동 복귀도 통과했다. 이 절차는 M2 회귀 시 재사용한다.

PC 화면과 VR 화면은 따로 기록한다. “VR이 꺼짐”, “평면 폴백”, “projection stereo는 살아 있으나 검정”을 같은 증상으로 묶지 않는다.

### 로그 검사

세 버전 인식 스크립트의 기본값은 과거 `0.52.0`이므로 현재 v0.154를 명시한다.

```powershell
.\vrmod\scripts\Test-RuntimeBootstrap.ps1
.\vrmod\scripts\Test-SceneDiagnostics.ps1 -ExpectedVersion 0.154.0
.\vrmod\scripts\Test-PresentationState.ps1 -ExpectedVersion 0.154.0 -RequireLandscapeTransition -RequireRoundTrip
.\vrmod\scripts\Test-OpenXrRuntime.ps1 -ExpectedVersion 0.154.0 -RequireHmd -RequireSession -RequireGamePanel
```

지속 frame loop는 프로세스 종료 시 `openxr-runtime-ready`를 남기기 전에 background thread가 끝날 수 있다. 그러므로 마지막 OpenXR 스크립트는 active-session 실기와 stereo event 분석을 대체하지 못한다.

## 13. 현재 테스트 상태

| 범주 | 상태 | 근거 |
|---|---|---|
| 코어 테스트 | PASS | 2026-08-11, 13/13 |
| Release 빌드 | PASS | 2026-08-11, warning 0/error 0 |
| 빌드↔설치 DLL | MATCH | v0.154 SHA `7C4A29EA...D428E7` |
| v0.154 PC/VD/OpenXR | **성공/M6 기준** | controller-tip upright/viewer-facing 손 패널, 손 FOV visible/hidden, Grip 토글과 정면↔stereo 전환 로그 및 사용자 최종 승인 |
| v0.151 PC/VD/OpenXR | **성공** | flat-only 정면 패널, 종횡비, stereo 자동 전환/복귀, right ray cursor/A·trigger/B/stick, Grip ON/OFF 사용자 확인 |
| v0.150 PC/VD/OpenXR | **부분 성공/입력 실패** | flat 화면·자동 복귀·종횡비 정상. 잘못된 action state 구조체 type으로 모든 controller input 실패, v0.151에서 해결 |
| v0.149 PC/VD/OpenXR | **미실기** | Grip toggle/hand pose/FOV gate/final-backbuffer panel 구현·설치, OpenXR ABI/export 정적 검증 완료 |
| v0.148 PC/VD/OpenXR | **미실기/대상 설계 개정** | 이전 keyed UI·clone 순서 수정 실험본. 손 부착형 전체 화면 패널은 미구현 |
| v0.147 PC/VD/OpenXR | **실패/현행 경로에서 해결** | 홈 keying 노이즈와 화면비 파손. v0.150 이후 keyed overlay 폐기 및 최종 백버퍼 패널 실기로 대체 |
| v0.143 PC/VD/OpenXR | **성공/M5 기준** | 실제 custom video 재생, 홈·ADV·OutGame topology, 최대 Canvas 101/RawImage 195, failure 0, 사용자 영상 재생 O |
| v0.142 PC/VD/OpenXR | **조사 완료/진단 상한 결함** | 종류별 비-live 화면 순회와 world/UI topology 확인; RawImage 배열 한도 failure 8건은 v0.143에서 해결 |
| v0.141 PC/VD/OpenXR | **성공/M4 기준** | 59.14 pair/s, submit 평균 114.79·중앙값 117.60fps, live 이탈/retire와 오류 0, 사용자 성능 승인 |
| 안정성 전용 테스트 | **비차단·미검증** | 사용자 지시로 향후 지속/반복/장시간 자원 수명 테스트 생략 |
| 기준선 검증 | **FAIL/주의** | 게임 핵심 3파일 mismatch; 사용자 승인 임시 테스트와 분리 |
| v0.102 PC/VD/OpenXR | **실패/철회** | 첫 live 607 pair/UI 성공 후 이탈에서 coreclr `0xc0000005`; v0.101로 정확 복귀 |
| v0.105 PC/VD/OpenXR | **실패/철회** | HDR OFF에도 해결 이전 파묻힘 재발; v0.104로 정확 복귀 |
| v0.131 PC/VD/OpenXR | **성공** | 최종 시각 설정 사용자 승인, UI 표시/숨김/재표시 3회와 failure/fallback 0 |
| v0.121~v0.130 시각 A/B | **완료** | VLBloom 본체 원인 확정, bloom/DOF/texture blur/threshold/diffusion/tonemapping 단계별 분리 |
| v0.108 PC/VD/OpenXR | 역사적 진단 | exact type loaded-image 탐색 이후 실제 VL pass 후킹 경로로 전환 |
| v0.107 PC/VD/OpenXR | **A/B 무효** | `VL.Rendering.VLBloom` image 탐색 실패로 v0.104 전체 OFF 폴백 |
| v0.106 PC/VD/OpenXR | **A/B 무효** | live/publish/retire 동작, `set_active/1` 실패로 v0.104 전체 OFF 폴백 |
| v0.104 PC/VD/OpenXR | 원인 분리 성공 | 파묻힘 사실상 제거, 좌우 문제 없음; 영상은 다소 밋밋 |
| v0.103 PC/VD/OpenXR | 부분 개선 | 두 live, 약 20 pair/s, UI/이탈, failure 0; 가림은 개선됐지만 파묻힘 일부 잔류 |
| v0.101 PC/VD/OpenXR | 성공 | live generation 3개, 자동 nudge/restore 각 2회, Canvas refresh 12회, 관련 failure 0; 사용자 정상 확인 |
| v0.100 PC/VD/OpenXR | 부분 성공 | 세 live 44.3초/40.3초/38.0초, clone/render failure 0; portrait 파손은 수동 resize로 복구 |
| v0.99 세 live 재진입 | 부분 성공 | 세 generation 성공, 이탈 후 PC layout 파손/UI capture failure |
| v0.98 첫 live | 성공 | 621 pair, 약 18.4~20.7 pair/s |
| v0.98 두 번째 live | 실패/평면 | 이전 eye RT wrapper NRE로 clone setup 실패 |
| v0.97 첫 live | 성공 | 164 pair, 약 18.4~21 pair/s |
| v0.97 두 번째 live | **평면 폴백** | 첫 pair 뒤 clone NRE; 프로세스 생존 |
| v0.96 첫 live | 성공 | 1,668 pair, 약 20 pair/s |
| v0.96 두 번째 live | **크래시** | stale clone pointer, coreclr `0xc0000005` |
| v0.96 27.5% eye offset | 부분 실기 | 첫 live 생산 확인; 편안함 최종 판정 전 |
| v0.95 단일 live 지속 | 로그상 성공 | 여러 session 약 2~2.5분, ~20 pair/s, failure 없음 |
| v0.95 같은-process 다른 live 재진입 | 미검증 | ready 재감지는 있으나 새 pair publish 증거 없음 |
| v0.94 단일 live >2분 | 사용자/로그 확인 | 2,903 pair, 143 rate records |
| v0.94 다른 live 재진입 | 실패 | 준비 `Live` 구간에서 VR 종료 |
| 영상 회귀 | v0.91 기준 | PC와 거의 유사, blur/빛 번짐 잔여 |
| UI 회귀 | v0.90 기준 | 투명/ON/OFF/재표시/터치 잔상 성공 |
| 성능 회귀 | v0.86 기준 | 약 20 pair/s, 체감 정상 |
| SteamVR OpenXR | 미실기 | 예비 경로 |
| Quest Link OpenXR | 미실기 | 예비 경로 |
| Quest controller input | **ray 입력 실기 성공/부분 완료** | right ray cursor, A/trigger click·drag, B back, stick scroll 성공; 직접 터치와 설정 GUI 미구현 |

## 14. 다음 세션 우선순위

1. **P0 — M7 설정 schema/GUI:** 별도 데스크톱 GUI에서 패널 손·포인터 손, 버튼, 패널 위치·크기·회전과 viewer-facing ON/OFF를 변경하고 런타임 시작 시 검증한다.
2. **P0 — 직접 터치:** 기존 ray UV→foreground client 경로와 중복 클릭 없이 패널 근접 터치를 연결한다.
3. **P1 — 라이브 UI 회귀:** stereo 문맥의 손 패널에서 좌상단 시계가 지속 갱신되는지 확인하고 one-shot alpha UI 의존을 제거한다.
4. **P1 — M7 제품화:** installer/uninstaller/rollback, 설정 기본값 복원·내보내기·가져오기를 완성한다.
5. **P1 — 회귀 보존:** v0.154 표시·입력, v0.141 성능, v0.131 영상, v0.90 UI와 PC mirror 기준을 유지한다.
6. **비차단 — 추가 성능/안정성:** 정확한 90/120 stereo와 예비 런타임 최적화는 선택 사항이다. 안정성 전용 테스트는 수행하지 않고 미검증으로 남긴다.

스킬 최상위 2D 이미지 누락은 이 목록에 남겨두되 사용자가 명시적으로 요청하기 전에는 착수하지 않는다.

## 15. 알려진 문서/코드 해석 주의

- `GAKUMAS_VR_DESIGN.md`에는 최종 목표용 추상 컴포넌트도 있다. 모두 실제 class로 구현된 것은 아니다. 이 문서의 파일/메서드 목록을 현재 코드 기준으로 삼는다.
- `SceneClassifier`의 `IsProfileApproved=false` 때문에 로그의 권장 mode가 SafePanel이어도 실제 stereo registry가 준비되면 OpenXR projection layer는 제출된다.
- `StereoFrozenDiagnosticDurationMilliseconds=30_000` 상수는 남아 있지만 현재 활성 연속 경로의 종료 조건으로 사용되지 않는다. 이름이나 미사용 상수만 보고 30초 제한이 있다고 판단하지 않는다.
- `TrySubmitStereoRenderDiagnostic`, `RunTestPatternFrameLoop`, 일부 `v0.53`/`v0.82` BMP 파일명은 역사적 이름이다. 현재 동작은 연속 stereo와 game panel이다.
- 현재 OpenXR loader가 mod runtime에 vendored되어 있지 않다. VD runtime을 쓰더라도 SteamVR loader 파일 경로가 없으면 loader 탐색에 실패한다.
- raw JSONL은 누적 153MB 이상이고 초기 실패가 섞여 있다. 전체 `Get-Content`보다 `rg`로 version/PID를 먼저 거른 뒤 JSON으로 파싱한다.

## 16. 이 문서 작성 시 변경 범위

v0.132~v0.141 M4에서는 timing telemetry, actual clone completion, 중복 Present boundary 제거, 양안 단일 fence, triple eye buffer/lease, 외부 render-scale 설정과 OpenXR low-latency GPU polling을 순서대로 적용했다. 최종 v0.141은 worker `Normal`, render scale `0.65`, 1744x1872 eye target을 사용한다. PID 36804의 100.5초 실제 3D 구간에서 59.14 pair/s와 OpenXR submit 평균 114.79fps를 기록했고 사용자가 충분한 성능으로 승인했다. v0.140 외부 GPU 부하 동시 세션의 NVIDIA TDR은 통제 재실행에서 재현되지 않았지만 관측 결함 이력으로 유지한다. 사용자의 지시에 따라 이후 안정성 전용 테스트는 모두 생략하며 미검증으로 남긴다. 이 근거로 M4를 2026-08-10 달성 처리했다.

v0.142는 홈·메뉴·선택·세로/가로 ADV의 Camera/Canvas/RenderTexture/RawImage topology를 저주기 변경 기반으로 수집했다. v0.143은 RawImage 진단 한도를 2,048로 올리고 게임의 custom media class만 한 번 식별했다. PID 2988에서 실제 홈 모니터/가샤 영상의 `CampusVideoPlayer → OnDemandVideoPlayerImage` 경로와 failure 0을 확인했고 사용자가 실제 영상 재생을 성공 판정했다. 이 근거로 M5를 2026-08-10 달성했으며 다음 작업은 확정 정책을 구현하는 M6다.

v0.146/v0.147 M6 실기에서는 홈·커뮤니케이션의 플랫 화면 중첩/상하 반전, 움직임 영역 keying 노이즈와 가로 커뮤니케이션/라이브 PC 화면비 파손이 관측됐다. v0.148은 이전 keyed 합성 정책의 실험본이고 v0.149는 왼손 Grip/FOV 손 패널을 도입했다. v0.150은 flat-only 정면 패널과 stereo 손 패널 전환, 오른손 입력을 구현했으나 action state 구조체 type 오류로 입력이 실패했다. v0.151은 이를 수정해 모든 입력과 전환을 사용자 실기로 통과했다. v0.152의 과도한 손 로컬 Y offset은 패널을 FOV gate 밖으로 밀었고, v0.153에서 view-space upright/viewer-facing으로 복구한 뒤 v0.154에서 추가 상향 거리를 제거했다. PID 31424 로그와 사용자 최종 승인으로 M6를 2026-08-11 달성했다.

v0.102는 첫 live 607 pair/UI capture 뒤 이탈에서 coreclr access violation을 일으켜 철회했고 v0.101로 정확 복귀했다. v0.101 PID 30340에서 서로 다른 3개 live/약 125.5초의 M3 부분 기준선을 수집한 뒤, 사용자 지시에 따라 30분 최종 측정은 남겨 두고 시각 결함으로 진행했다. v0.103 clone depth 강제는 PID 35804 두 live/failure 0과 사용자 부분 개선 판정을 얻었다. v0.104 post-processing OFF는 파묻힘을 사실상 제거했지만 영상이 다소 밋밋했다. v0.105 HDR OFF 절충은 해결 이전 파묻힘을 재현해 철회했다. 소스·설치를 v0.104로 정확 복귀했고 코어 테스트 9/9, Release build warning 0/error 0, 설치 SHA-256 `0BB78747A6455E6A853F9677B9DB1DFE4DCE533AFA1EA15475E1AF91AD964C3E`가 기존 v0.104 rollback과 일치한다. 실패 v0.105는 `rollback/runtime-bootstrap-v0.105.0-20260809-215051/`에 보관했다. 원본 게임 바이너리, Localify, Volume/조명/에셋과 baseline manifest는 수정하지 않았다.

Git metadata가 없으므로 이 작업 이전부터 존재한 다른 로컬 변경을 식별하거나 clean 상태를 보증할 수 없다. 새 세션도 `.git`이 복구되기 전까지 `git status`나 `git diff` 결과를 “깨끗함”으로 보고하지 않는다.
