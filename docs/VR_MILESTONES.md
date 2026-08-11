# Gakumas VR 개발 마일스톤

최종 갱신: 2026-08-11  
현재 마일스톤: **M7 — 입력·설정·설치 제품화**  
현재 소스/설치본 v0.154.0, M6 자동 정면 패널·stereo 왼손 Grip 패널·오른손 레이 입력 사용자 실기 완료

이 문서는 기능 목록이 아니라 다음 단계로 넘어가기 위한 실기 완료 조건을 정의한다. 빌드 성공이나 로그만으로 사용자 실기 조건을 대신하지 않는다.

## 알림 규칙

- 마일스톤의 모든 완료 조건이 사용자 VR 실기와 필요한 로그에서 확인되면, 같은 응답에서 사용자에게 **“마일스톤 Mx 달성”**이라고 명시적으로 알린다.
- 달성 즉시 이 문서의 상태·근거·날짜와 `GAKUMAS_VR_STATUS.md`를 갱신한다. 런타임 변경이 동반되면 `CHANGELOG.md`와 버전도 함께 갱신한다.
- 일부 조건만 성공했거나 추정인 경우 달성으로 표시하지 않는다. 현재 막힌 조건과 다음 검증만 알린다.
- 이미 달성한 마일스톤의 회귀가 발견되면 달성 기록은 보존하되 `회귀 발생`으로 표시하고 복구를 우선한다.

## 안정성 테스트 정책 변경

- 사용자는 2026-08-10부터 향후 지속 시간, 반복 횟수, 장시간 자원 수명과 누수 추세를 포함한 모든 안정성 전용 테스트를 생략하도록 지시했다.
- 생략한 안정성 항목은 달성 또는 검증 완료로 표시하지 않고 **비차단·미검증**으로 남긴다. 실제 작업 중 관측된 크래시나 회귀는 계속 결함으로 취급한다.
- 성능 단계는 짧은 사용자 VR 실기, 런타임 timing 로그와 사용자가 승인한 체감 기준으로 판정한다.

## 마일스톤 표

| ID | 상태 | 목표 | 완료 조건 |
|---|---|---|---|
| M0 | **달성** | 안전한 부트스트랩과 평면 패널 | Localify 공존, VD OpenXR 패널, 세로↔가로↔세로, 색/방향 정상, VR 실패 시 게임 생존 |
| M1 | **달성** | 단일 라이브 몰입형 vertical slice | 한 live에서 스테레오 깊이·연속 영상·후처리 기준·UI ON/OFF/재표시가 동작하고 평면으로 정상 이탈 |
| M2 | **달성 — 2026-08-09** | 다중 라이브 수명주기 | 같은 프로세스에서 live A → 평면 → 서로 다른 live B, 두 곡 모두 30초 이상 stereo publish, 크래시·검정·영구 평면 폴백·이전 곡 잔상·PC 창 레이아웃 파손 없음 |
| M3 | **달성 — 2026-08-10** | 시각 완성도와 UI 회귀 | 캐릭터 가독성·그림자 허용 기준, 블룸·blur 기준, UI 표시/숨김/재표시를 사용자 VR 실기로 확정 |
| M4 | **달성 — 2026-08-10** | 60Hz 성능 단계 | 게임 속도 stereo 생산, near-120Hz OpenXR submit, frame pacing/조작/PC mirror 회귀 없음, 시작 시 검증되는 render-scale 품질 폴백 정의 |
| M5 | **달성 — 2026-08-10** | 비-live 화면 VR 적용성 조사 | 메인 홈, 진행 중 메뉴/선택, 커뮤니케이션, 영상 재생의 실제 Camera/Canvas/RenderTexture/custom video 경로를 확인하고 장면별 immersive/panel/UI 정책과 안전 폴백을 확정 |
| M6 | **달성 — 2026-08-11** | 전체 VR 환경 표시 통합 | fresh stereo가 없는 완전 평면 문맥은 최종 백버퍼를 시야 정면에 자동 표시하고, 확인된 실시간 3D는 stereo를 유지하면서 같은 백버퍼를 기본 왼손 Grip 토글·시야 조건부 보조 패널로 제공하며, 전환과 PC 화면을 회귀시키지 않음 |
| M7 | **진행 중 — 입력 기반 완료** | 입력·설정·설치 제품화 | 사용자 실기 완료된 오른손 ray cursor, A/trigger click·drag, B back, stick scroll을 유지하고, 직접 터치 및 패널 손/포인터 손·버튼·viewer-facing 교환 가능한 설정 schema·GUI, 설치·제거·rollback 패키지를 완성 |
| M8 | 대기 | 릴리스 후보 | VD 주 경로와 SteamVR/Quest Link 예비 경로, 업데이트 호환성 gate, 로그 개인정보 점검, 회귀표 전 항목 통과 |

## M2 달성 근거

1. v0.100 PID 25368에서 세 live generation이 재진입했고 generation 3/4/5가 약 44.3초/40.3초/38.0초 지속됐다. clone/render failure는 없었다.
2. 같은 실기의 portrait 파손은 수동 창 리사이즈로 복구되어 resize rebind 필요성을 확인했다.
3. v0.101 PID 30636에서 서로 다른 live source 3회, generation retire 3회, `portrait-resolution-nudge-applied → portrait-resolution-restored` 2회, Canvas layout refresh 12회가 기록됐다.
4. v0.101 최신 세션에 portrait nudge, clone/render, UI capture 또는 sampler failure가 없었다.
5. 사용자가 두 번째 이후 이탈 후 수동 개입 없는 정상 portrait 복귀를 확인했다.

v0.99은 세 generation 재진입 자체에는 성공했지만 첫·세 번째 live가 30초 미만이었고 이탈 뒤 PC 창 레이아웃 파손이 있어 M2 달성으로 처리하지 않았다.

위 지속 기록과 v0.101 자동 복구 실기를 결합해 M2의 모든 완료 조건을 충족했다.

## M3 달성 근거와 완료 조건 변경

1. v0.101 PID 30340에서 서로 다른 3개 live, generation ready/retire 각 3회와 failure 0, 총 약 125.5초의 부분 안정성 기준선을 확보했다.
2. v0.103의 clone depth 강제로 광원 가림이 일부 개선됐고, v0.104 전체 후처리 OFF에서 캐릭터 파묻힘의 주원인이 후처리임을 확정했다.
3. v0.116~v0.120의 실제 `VLPostProcessPass` 메서드 격리 결과 `VLBloom` 본체가 과도한 광막과 캐릭터 밝기를 함께 만든다는 것을 확인했다.
4. 최종 v0.131은 clone 전용 `VLBloom` intensity 140%, diffusion 최소 1단계, `VLDOF`/`VLTextureBlur` OFF와 OpenXR eye 최종 출력 `+0.2 EV`를 사용한다. 사용자가 캐릭터 가독성, 밝기, 잔여 그림자를 허용 가능한 최종 영상으로 판정했다.
5. v0.131 PID 39884에서 UI hidden → shown → capture-ready가 3회 반복되고 관련 failure/fallback이 0건이었으며, 사용자가 표시·숨김·재표시를 정상 판정했다.
6. 사용자는 2026-08-10에 서로 다른 3개 live/총 30분과 clone/RT/handle 장시간 수명 계측을 제품 완료 조건에서 제외하도록 명시했다. 이후 모든 안정성 전용 테스트도 생략하도록 범위를 확대했으며, 해당 항목은 **검증 완료가 아니라 비차단·미검증**으로 남는다.

위에서 개정한 시각·UI 완료 조건이 사용자 VR 실기와 로그로 충족되어 M3를 2026-08-10 달성했다. 다음 단계는 M4 60Hz 성능 경로다.

## M4 달성 근거와 완료 조건 변경

1. v0.132~v0.134 timing 계측과 actual clone completion 추적으로 기존 two-Present 대기의 원인을 분리했고, 두 eye가 같은 Present에서 항상 완료되며 별도 eye 누락이 없음을 확인했다.
2. v0.135에서 불필요한 Present boundary를 제거하고, v0.136에서 양안 OpenXR 복사와 GPU fence를 하나로 묶었다. v0.137은 triple eye buffer와 lease-aware 재사용으로 첫 pair 이후 main-thread GPU fence를 제거했다.
3. v0.138~v0.139는 시작 시 검증되는 `vrmod/config/render-resolution-scale.txt`를 추가하고 현재 값을 `0.65`로 확정했다. 허용 범위는 `0.50~1.00`, 누락·손상·범위 밖 값은 안전 기본값 `0.75`로 폴백하며 향후 별도 GUI가 같은 설정을 편집한다.
4. v0.141 PID 36804의 실제 3D 구간 100.5초에서 Present 59.47fps, stereo publish 59.14 pair/s, OpenXR submit 평균 114.79fps·중앙값 117.60fps, pair age 8.38ms, source→clone 3.01ms를 기록했다. buffer reuse block, eye 누락과 런타임 오류는 0건이었고 live 이탈과 generation retire도 정상 기록됐다.
5. 사용자는 게임 자체도 60fps를 고정 유지하지 못하는 조건에서 이 결과가 충분하고 추가 차이는 오차 범위라고 판정했다. 따라서 60.00/120.00 고정 수치를 요구하지 않고 **게임 속도 stereo + near-120Hz OpenXR submit**을 현재 제품 성능 기준으로 승인했다.
6. v0.140의 외부 GPU 부하 동시 실행에서는 NVIDIA TDR/LiveKernelEvent 141이 발생했으나, 통제 재실행과 재부팅 뒤 v0.141에서는 추가 TDR이 없었다. 이는 장시간 안정성 검증 근거로 사용하지 않으며 관측 이력으로만 유지한다.
7. 사용자는 향후 모든 안정성 테스트를 완료 조건에서 제외했다. 기존 10분 유지 조건은 비차단·미검증으로 남으며, 관측된 성능과 회귀 기준 충족으로 M4를 2026-08-10 달성 처리한다.

## M5 달성 및 M6 비-live 화면 범위

### M5 달성 근거

1. v0.142 PID 32908에서 홈, 상점, 프로듀스 준비, 스토리 선택, 세로/가로 ADV를 순회했다. 홈의 `Game3DManager`는 `_VLTargetTexture_2160x3840`을 만들고 활성 `3dTargetImage`가 이를 표시했다. UI-only 메뉴에서도 카메라가 남을 수 있으므로 camera 존재만으로 immersive를 허용하지 않는다는 전환 규칙을 확정했다.
2. 가로 커뮤니케이션은 `_VLTargetTexture_3840x2160`, 세로 커뮤니케이션은 `_VLTargetTexture_2160x3840`을 `ADVEngine/.../Main Layer/Render Target` RawImage로 표시했다. `Choices Canvas`, `Content Canvas`, `Player Control Canvas`와 대화 UI는 별도 `UICamera`에 결합돼 world/UI 분리가 가능하다.
3. 홈과 메뉴의 조작 UI도 대부분 `UICamera` 기반 ScreenSpaceCamera Canvas이며, world RT를 표시하는 RawImage와 독립된 수명을 가진다. 사용자는 라이브 외 게임 진행 중 UI 표시를 필수로 하고, 후속 Quest 입력을 화면별 예외 없는 범용 pointer/click/drag/scroll 경로로 만들도록 확정했다.
4. v0.142의 누적 비활성 RawImage가 기존 진단 상한 512를 넘어 8회 inventory failure를 일으켰다. v0.143은 상한을 2,048로 조정했고 PID 2988에서 최대 Canvas 101/RawImage 195를 포함한 73초 실기 동안 진단·runtime failure 0건을 확인했다.
5. 두 실기 모두 Unity `VideoPlayer` 인스턴스는 0개였다. v0.143은 게임의 실제 custom media 타입을 식별해 `Campus.Common.CampusVideoPlayer`가 홈 모니터와 가샤 배경의 `OnDemandVideoPlayerImage`에 활성화되는 경로를 확인했다. URL·인증 정보는 수집하지 않았다.
6. 정책은 활성 world-presenting RawImage와 source RT가 함께 있을 때만 실시간 3D를 stereo 후보로 하고, 동적 조작 UI는 독립 layer로 유지한다. custom/사전 렌더 영상은 비율 보존 2D panel 또는 기존 UI layer로 표시하며 가짜 stereo를 만들지 않는다. topology가 불명확하거나 source/UI/media 추출이 실패하면 최종 backbuffer SafePanel로 즉시 폴백한다.
7. 사용자는 v0.142에서 종류별 화면을 최대한 순회하고 v0.143에서 실제 영상 재생까지 완료했다. v0.143 빌드/설치 SHA-256은 `8BF8D82AEA5A5DE8D4D72C73C1952096B493397301CC854257EFBE9B40F9865F`로 일치한다.

위 topology, 전환 신호, 표시 정책과 실패 폴백이 사용자 실기와 로그로 확정되어 M5를 2026-08-10 달성했다. 다음 단계는 승인된 범위를 구현하는 M6다.

M5는 구현 전에 다음 네 문맥을 실제 코드와 런타임 로그로 구분했다.

1. 메인 홈 화면: 실시간 3D 배경·캐릭터와 2D 홈 UI의 camera stack 및 render target을 확인한다.
2. 게임 진행 중 메뉴/선택: 3D 배경이 유지되는 선택 화면과 완전한 UI-only 화면을 구분하고, UI 입력 좌표와 표시 layer 정책을 정한다.
3. 게임 진행 중 커뮤니케이션: 실시간 3D 커뮤와 사전 렌더/2D 연출을 구분하고 실제 world source camera와 대화 UI 수명을 확인한다.
4. 영상 재생: `VideoPlayer`, movie texture, 최종 backbuffer 합성 경로와 종횡비를 확인한다. 사전 렌더 영상에 가짜 stereo를 만들지 않고 VR panel 배치 대상으로 취급한다.

M5 완료 조건은 각 문맥의 실제 source/UI/video topology, 전환 신호, 표시 정책과 실패 시 SafePanel 폴백을 사용자와 확정하는 것이었고 v0.143 실기로 충족됐다. 전체 객체를 1ms 주기로 열거하거나 아직 검증되지 않은 camera를 immersive source로 승인하지 않는 원칙은 M6에서도 유지한다.

### 2026-08-11 M6 표시·입력 정책 개정

사용자는 M6 구현 실험 결과를 검토한 뒤 장면별 UI 추출 정책을 다음 공통 모델로 대체했다.

1. 라이브를 포함한 모든 VR 환경에서 최종 게임 화면 전체를 평면 패널 source로 제공한다.
2. fresh stereo가 없는 UI-only·영상·로딩·폴백 문맥에서는 기존 VR 프레임을 남기지 않고 최종 백버퍼를 `XR_VIEW_REFERENCE_SPACE` 기준 시야 정면 1.6m에 자동 표시한다. 이 주 콘텐츠 패널은 Grip 상태와 무관하게 켜져 있어 검은 공간만 남지 않게 한다.
3. fresh stereo가 있는 3D 문맥에서는 정면 패널을 제거하고 stereo world를 유지한다. 보조 패널은 기본 왼손 controller 상단 끝에 배치하며, 왼손 Grip press edge로 토글하고 tracking·HMD 손 FOV gate와 hysteresis를 적용한다. v0.154에서는 view-space 수직·viewer-facing이며 OFF에서는 보조 패널 copy/acquire/write/submit/pointer hit-test를 생략한다.
4. 조작은 기본 오른손 aim ray가 현재 표시된 정면 또는 손 패널과 만나는 UV를 게임 client 좌표로 변환해 수행한다. 흰색/검정 원형 cursor를 별도 alpha quad로 표시하고, A는 click/drag, trigger는 0.15 pre-press 좌표 latch 뒤 0.72에서 click하며 120ms와 이동 임계값 뒤 drag로 전환한다. B는 Escape back, thumbstick Y는 wheel scroll이다. 게임 창이 foreground가 아니면 입력을 주입하지 않는다.
5. 패널 손과 포인터 손은 역할로 추상화하고 M7 설정 GUI에서 좌우와 버튼 매핑을 교환할 수 있어야 한다.
6. 검증된 실시간 3D source의 stereo world는 유지하지만, UI·영상·시계·메뉴는 keyed UI-only layer가 아니라 Localify까지 포함한 최종 백버퍼 전체 패널을 공통 경로로 사용한다.
7. v0.146/v0.147에서 관측된 플랫 오버랩 노이즈와 가로 커뮤니케이션/라이브 PC 화면비 파손은 당시 정책의 실패로 보존한다. v0.150 이후 keyed overlay를 제거하고 최종 백버퍼 정면/손 패널로 대체했으며 사용자는 평면 표시와 종횡비를 정상 판정했다.

개정된 M6 완료 조건은 완전 평면 문맥에서 정면 패널이 자동으로 나타나 종횡비·방향·실시간 갱신을 보존하고, 지원 3D 장면에서는 stereo world와 왼손 Grip 보조 패널이 안정적으로 전환되며, 보조 패널 OFF 동안 관련 GPU 작업이 중단되고 PC 화면을 손상하지 않는 것이다. v0.150에 먼저 연결한 ray 입력은 같은 실기에서 기본 click/back/scroll 좌표를 확인하되, 직접 터치와 역할·버튼 교환 설정 GUI는 M7 완료 조건으로 남긴다. 각 범주의 기능 실기는 필요하지만 지속 시간·반복 횟수 기반 안정성 테스트는 요구하지 않는다.

정확한 90/120 stereo 생산률과 120Hz 추가 미세 최적화는 번호가 있는 제품 마일스톤에서 제외하고 비차단 선택 작업으로 남긴다. M5/M6 다음 제품 단계는 M7이다.

## M6 달성 근거

1. v0.150 실기에서 fresh stereo가 없는 문맥은 최종 백버퍼 정면 패널로 표시됐고, 상하 반전·플랫 overlay 중첩 없이 종횡비가 정상으로 유지됐다. stereo 이탈 뒤 정면 패널도 자동 복귀했다.
2. v0.151은 `XR_TYPE_ACTION_STATE_GET_INFO`를 규격 값 58로 고쳐 컨트롤러 상태 조회를 복구했고, 액션별 실패를 격리했다. 사용자는 오른손 ray cursor, A/trigger click·drag, B back, stick scroll과 왼손 Grip ON/OFF가 모두 정상이라고 판정했다.
3. v0.152의 과도한 손 로컬 Y offset은 FOV gate 실패로 패널을 숨겼다. v0.153은 controller tip을 view-space로 변환하고 손 FOV만 gate로 사용해 표시를 복구했으며, v0.154는 추가 상향 거리를 제거해 패널 중심을 controller tip에 직접 배치했다.
4. v0.154 PID 31424에는 `controller-pointer-input-ready`, `hand-panel-enabled/disabled`, 손 시야에 따른 `hand-panel-visible/hidden`, `front-panel-mode-entered/exited`, live/non-live stereo generation이 함께 기록됐고 controller/OpenXR failure는 없었다.
5. 사용자는 v0.154 손 패널 배치까지 “구현이 잘됐음”으로 최종 승인했다. 지속 시간·반복 횟수 기반 안정성 전용 시험은 사용자 지시에 따라 비차단·미검증으로 남긴다.

위 표시·입력·전환 조건과 사용자 VR 실기 승인이 충족되어 M6를 2026-08-11 달성했다. 다음 단계는 직접 터치, 역할·버튼·패널 방향 설정 GUI와 설치 제품화를 완성하는 M7이다.
