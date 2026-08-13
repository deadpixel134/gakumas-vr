# Gakumas VR 변경 기록

코드 변경과 실기 결과를 분리해 기록한다. 작업 문서는 마일스톤 완료 시점 또는 사용자의 명시적 문서 요청에서 함께 갱신한다.

## v0.173.0 — 2026-08-13

상태: 코어 39/39·관리 7/7·Release 빌드·199개 패키지 manifest·clean install·Localify 공존 검증 및 사용자 VR 실기 성공 — **M7 달성**

- OpenXR absolute HMD orientation을 원점 대비 yaw/pitch/roll 성분으로 분리한다. scene base와 artificial stick 회전에서는 roll을 제거하고, 현재 HMD의 실제 physical roll 변화량만 마지막에 합성한다.
- raw relative HMD quaternion을 navigation 회전에 통째로 곱을 때 기울어진 origin의 physical yaw가 roll로 새던 경로를 폐기했다. 최종 순서는 `Yaw × Pitch × physical Roll`이며 스틱 회전은 roll을 만들지 않는다.
- 사용자는 최종 VR 실기 결과를 “완벽했다”고 판정했다. 런타임 SHA-256은 `0851E03A8A30B3FB3822C4626120CFB7463CC8C81405067D8A6659D36234FD15`, 배포 ZIP SHA-256은 `CFC89565B303C631E0A4694774D40D4F74D19C8FC6B80E7F557E772FF121C9B0`다.

## v0.172.0 — 2026-08-13

- HMD 위치·컨트롤러 이동·스틱 회전을 하나의 world-space navigation 합성으로 정리하고 scene camera roll을 제거했다.
- 사용자 실기에서 고개를 돌린 상태의 회전 뒤 수평이 기울어지는 현상이 남아, relative quaternion의 roll leakage를 v0.173에서 성분 분리로 수정했다.

## v0.171.0 — 2026-08-13

- 반대 손 스틱에 world-axis yaw/pitch 시야 회전을 구현했다. 대각 입력은 절댓값이 큰 한 축만 선택하고 기본 15° 스냅, 선택 15°/30°/45°/60°와 smooth 모드를 설정에 추가했다.
- 스냅 활성 임계값 0.65와 deadzone 0.20 재무장으로 한 번 기울일 때 한 번만 회전한다.

## v0.170.0 — 2026-08-13

- VR 스틱 스크롤을 비활성화하고 기본 왼손을 시야 회전, 오른손을 이동으로 지정했다. `locomotionHand`를 바꾸면 역할이 자동 교환된다.
- 이동은 최종 HMD 시야의 pitch를 포함한 forward/right basis를 매 프레임 사용해 위·아래를 보며 전진할 때 상승·하강한다.

## v0.169.0 — 2026-08-13

- 컨트롤러 스틱 이동을 현재 시야 중심 기준으로 추가했다. 이후 실기 피드백에 따라 수평면 제한과 패널/스크롤 역할 모델을 v0.170의 완전 3D 이동·양손 navigation으로 교체했다.

## v0.168.0 — 2026-08-13

- live 장면에서도 설정으로 6DoF를 활성화할 수 있게 했다. 활성화 시 게임 연출 카메라의 경로·각도와 독립된 진입 anchor를 사용하며 기본값은 OFF다.

## v0.167.0 — 2026-08-13

- 승인된 비-live 3D 장면에 positional 6DoF를 추가했다. scene/source generation이 바뀌면 origin을 다시 잡고 stale pose는 적용하지 않는다.

## v0.166.0 — 2026-08-13

- 설정 프로그램이 GitHub의 최신 stable Release를 확인해 새 버전의 ZIP과 `.sha256`을 내려받고, 크기·해시·archive 구조를 검증한 뒤 게임 종료 상태에서 설치기로 넘기는 자동 업데이트를 추가했다.
- 캐시된 scene/source 신호를 이용해 3D 전환 fast path를 추가로 단축했다. 이 버전은 이후 6DoF 브랜치의 `main` 기준점이다.

## v0.165.0 — 2026-08-13

- 3D 장면 감지 후 VR 전환 지연의 주 원인이 저주기 topology 확인과 stable-frame gate임을 분리하고, 안전 조건을 유지한 event/cache fast path로 전환 반응을 줄였다.
- 사용자 VR 실기 완료 판정을 받았다.

## v0.164.0 — 2026-08-12

- 패키지를 게임 폴더에 쓰기 전에 199개 payload manifest, 필수 clean-install 구성, Dobby/OpenXR 네이티브 로딩과 보존 정책을 모두 검증한다.
- manifest 소유 파일 중 설치 당시 해시가 같은 파일만 제거해 Localify·사용자 설정·수정된 파일을 보존한다.

## v0.163.0 — 2026-08-12

- Apache-2.0 Dobby 바이너리와 라이선스를 배포 ZIP에 포함해 clean install에서 별도 한글 패치/BepInEx 설치에 의존하지 않게 했다.

## v0.162.0 — 2026-08-11

- eye render scale 허용 범위를 `0.50~2.00`으로 확장하고 설정 GUI에서 1.00 초과 시 성능·VRAM 경고를 표시한다.

## v0.161.0 — 2026-08-11

- 설정 GUI에 자동 VFX preset과 post-processing, VL bloom intensity/diffusion, depth of field, texture blur, star streak, flare의 수동 설정을 추가했다.

## v0.155.0~v0.160.0 — 2026-08-11~2026-08-12

- 버전 있는 JSON 설정 schema와 한국어 기본의 한국어·영어·일본어 설정 GUI를 도입하고, 가져오기/내보내기/기본값 복원을 추가했다.
- 설정과 설치 프로그램을 독립 EXE로 패키징하고 창 크기를 조정했다. manifest 기반 설치·제거·rollback은 Localify와 사용자 파일을 소유 대상으로 간주하지 않는다.
- 직접 터치와 live 좌상단 시계 회귀는 사용자 결정으로 제품 완료 조건에서 제외했다. ray/A/trigger/B 입력과 Grip 패널 토글을 유지했다.

## v0.154.0 — 2026-08-11

상태: 코어 테스트/Release 빌드/설치/해시 검증 및 사용자 PC/VR 실기 성공 — **M6 달성**

- v0.153 손 패널의 controller tip 위 추가 상향 거리(패널 반높이와 여유)를 제거하고 패널 중심을 왼손 controller local +Y 0.10m의 상단 끝에 직접 배치했다. view-space 수직·viewer-facing과 손 HMD FOV gate는 유지했다.
- PID 31424에 `controller-pointer-input-ready`, Grip ON/OFF, 손 시야 진입/이탈에 따른 panel visible/hidden, 정면 패널↔live/non-live stereo 전환이 기록됐다. 사용자는 최종 위치를 포함한 구현을 정상 승인했다.
- 코어 테스트 13/13, OpenXR x64 ABI, loader action export 15개, Release 빌드 경고 0/오류 0을 확인했다. Bootstrap 빌드/설치 SHA-256은 `7C4A29EA3FB7E6FE96AFC12B3231592987B4F9A4FDC2F6C0171DF7B095D428E7`, Core SHA-256은 `B4B5C63CB507101D2B5F56EE784860628A9DBC27E87495987002A3DA2567C13B`이다. 직전 v0.153 rollback은 `rollback/runtime-bootstrap-v0.153.0-20260811-204412/`이다.

## v0.153.0 — 2026-08-11

- v0.152에서 panel center/FOV gate가 계속 실패한 로그를 근거로, controller tip 위치만 손 pose에서 계산하고 quad는 view-space에서 수직·viewer-facing으로 갱신하도록 바꿨다.
- 표시 조건을 합의된 `PanelEnabled && tracked && handInView`로 단순화하고 패널 중심·앞면 gate를 제거했다. 손이 시야 밖이면 100ms hysteresis 뒤 숨긴다.
- 사용자 실기에서 패널 표시는 복구됐으나 controller tip에서 지나치게 높다고 판정해 v0.154에서 상향 거리를 제거했다. Bootstrap SHA-256은 `25A5C2875D3614202D81F18AEC98CF121B2C20EBCBF85CF2E2101924127AA555`다.

## v0.152.0 — 2026-08-11

- 화면 높이를 반영해 패널 하단을 controller tip 위에 두는 동적 손 로컬 Y offset을 적용했다.
- 사용자 실기와 로그에서 Grip 토글·tracking·views는 정상이지만 `inViewAndFacing=False`가 계속되어 패널이 표시되지 않았다. 큰 패널 반높이를 손 로컬 축에 더한 배치가 FOV gate를 실패시킨 것으로 확정하고 v0.153에서 폐기했다. Bootstrap SHA-256은 `4EECABEAB7B5CE683AD058B2C20695474F66821E9390ED3A6C1F306E7DEB0D8B`다.

## v0.151.0 — 2026-08-11

- OpenXR 규격과 다른 `XR_TYPE_ACTION_STATE_GET_INFO=30`을 58로 수정했다. `-1/XR_ERROR_VALIDATION_FAILURE`를 발생시키던 첫 squeeze 조회가 전체 controller frame을 중단하던 원인이었다.
- squeeze, trigger, primary, secondary와 thumbstick 상태 조회를 개별 실패 격리하고 10초 rate-limit 로그를 추가해 한 action 실패가 나머지 조작을 중단하지 않게 했다.
- 사용자 실기에서 정면 패널, 종횡비, 오른손 ray cursor/A/trigger/B/stick, 왼손 Grip ON/OFF, stereo 이탈 뒤 정면 패널 자동 복귀가 모두 정상 판정됐다. Bootstrap SHA-256은 `CCCF107BDAF6EF03E917DD2BA71ACE9E044696F2F780A35F4858BAB15CED6F80`다.

## v0.150.0 — 2026-08-11

상태: 코어 테스트/Release 빌드/설치/해시 검증 성공, 사용자 PC/VR 실기 전 — **M6 미달성**

- fresh stereo texture가 없으면 최종 게임 backbuffer 전체를 `XR_VIEW_REFERENCE_SPACE` 기준 Z=-1.6m, 최대 1.8m x 1.3m의 정면 quad로 자동 표시한다. 완전 평면 문맥에서는 왼손 Grip OFF나 손 추적 상태와 무관하게 주 콘텐츠를 유지한다.
- fresh stereo가 준비되면 정면 패널을 제거하고 projection world를 제출한다. 보조 패널은 v0.149의 시작 OFF 왼손 Grip/FOV/front-facing 경로를 유지하며, stereo가 사라지면 이전 프레임을 남기지 않고 정면 패널로 자동 복귀한다.
- Oculus Touch action set에 좌·우 thumbstick vector2f action을 추가했다. 기본 오른손 aim ray를 현재 정면/손 패널 plane과 교차시켜 UV를 foreground 게임 client 좌표로 변환하고, 별도 흰색/검정 원형 alpha quad cursor를 표시한다.
- A 버튼은 click/hold drag, B 버튼은 Escape back, 오른쪽 thumbstick Y는 wheel scroll로 주입한다. trigger는 0.15에서 조준 좌표를 미리 latch하고 0.72에서 click하며, 120ms 유지와 3.5% content 이동 뒤에만 drag로 전환해 눌림 동작의 미세 조준 틀어짐을 줄인다. 게임 창이 foreground가 아니면 입력을 주입하지 않으며 button-up을 보장한다.
- 직접 터치, 좌우 패널/포인터 손 역할과 버튼 mapping 설정 GUI는 아직 구현하지 않았다. visual cursor 생성이 실패해도 panel과 좌표 입력은 계속되고, controller action 실패 시 정면 패널/stereo와 PC 게임은 계속된다.
- runtime version을 0.150.0으로 올렸다. 코어 테스트 13/13, OpenXR x64 ABI, 현재 loader action export 15개, Release 빌드 경고 0/오류 0을 확인했다. Bootstrap 빌드/설치 SHA-256은 `34EA5DE32453E04368C22D07E9A395313F71C53263760BA9FBF3C8E053F5978D`, Core SHA-256은 `B4B5C63CB507101D2B5F56EE784860628A9DBC27E87495987002A3DA2567C13B`이다. 직전 v0.149 rollback은 `rollback/runtime-bootstrap-v0.149.0-20260811-194356/`이다.

## v0.149.0 — 2026-08-11

상태: 코어 테스트/Release 빌드/설치/해시 검증 성공, 사용자 PC/VR 실기 전 — **M6 미달성**

- OpenXR core Oculus Touch interaction profile에 좌·우 grip/aim pose, squeeze/trigger float와 A·B/X·Y boolean action을 등록하고 session action set/action space를 구성했다. 오른손 상태는 M7 범용 입력 기반으로만 읽으며 아직 게임 입력을 주입하지 않는다.
- 패널은 시작 시 OFF이고 왼손 Grip `0.72` press/`0.25` release edge와 250ms debounce로 한 번씩 토글한다. 이 상태기는 Core로 분리해 hold 중복 방지, 완전 release, shallow/debounce 거부 테스트를 추가했다.
- 최종 게임 backbuffer 전체를 왼손 grip action space의 최대 0.42m quad로 표시한다. 왼손/패널이 양안 FOV 안에 있고 grip pose의 palm-facing 조건을 만족할 때만 보이며 이탈에는 100ms hysteresis를 적용한다.
- OFF 또는 visibility gate 실패 시 panel swapchain 자원은 보존하되 acquire/write, 전체 백버퍼 GPU copy와 quad 제출을 생략한다. stereo world는 독립적으로 계속 제출한다.
- v0.148의 Present 동기 keyed UI producer 호출과 기존 live natural one-shot UI capture를 비활성화했다. UI·영상·시계는 Localify까지 포함한 최종 backbuffer 손 패널을 공통 source로 사용한다.
- 코어 테스트 11/11, OpenXR x64 구조체 ABI, 현재 loader의 action export 14개, Release 빌드 경고 0/오류 0을 확인했다. Bootstrap 빌드/설치 SHA-256은 `016811F34D40FF0BC5246A7783FE8840B41D730A72331B7A59D119A4C3DD62C5`, Core SHA-256은 `0CE0FDC3C7400E249999B881835884CB2F809535D5B72A9368EB80C8AF999F30`이다. v0.148 rollback은 `rollback/runtime-bootstrap-v0.148.0-20260811-175143/`이다.

## v0.143.0 — 2026-08-10

상태: 코어 테스트/Release 빌드/설치/VR 비-live·실제 영상 실기 성공, **M5 달성**

- v0.142에서 누적 비활성 `RawImage`가 진단 배열 한도 512를 넘어 발생한 inventory failure 8건을 반영해 상한을 2,048로 올렸다. PID 2988의 약 73초 실기에서 최대 Canvas 101/RawImage 195를 기록했고 진단 및 runtime failure는 0건이었다.
- Unity `VideoPlayer` 인스턴스가 없는 실제 게임 구성을 위해 `CampusTimelineCriVideoPlayer`, `ABLoopVideoPlayer`, `CampusVideoPlayer`, `CampusVideoPlayerUnityMaskable`과 Vuplex WebView surface를 시작 시 한 번 식별하고, 활성·준비·재생·texture 상태만 1초 변경 기반으로 기록하는 M5 custom-media probe를 추가했다.
- 실제 영상 실기에서 `Campus.Common.CampusVideoPlayer`가 홈 모니터와 가샤 배경의 `OnDemandVideoPlayerImage`에서 활성화되는 Canvas/RawImage/custom material 경로를 확인했다. URL, 계정 또는 인증 정보는 수집하지 않는다.
- 사용자 확인은 v0.142 종류별 화면 순회와 v0.143 실제 영상 재생 성공을 포함한다. 확인된 topology를 근거로 실시간 world, 지속 갱신 UI, 비율 보존 video panel과 최종 backbuffer SafePanel 정책을 확정해 M5를 달성했다.
- 빌드/설치 SHA-256은 `8BF8D82AEA5A5DE8D4D72C73C1952096B493397301CC854257EFBE9B40F9865F`다. v0.142 rollback은 `rollback/runtime-bootstrap-v0.142.0-20260810-171946/`이다.

## v0.142.0 — 2026-08-10

상태: 코어 테스트/Release 빌드/설치 성공, M5 topology 실기 완료 — 진단 상한 failure 8건은 v0.143에서 수정

- Camera path/active/target RT format, URP renderer/render type/stack, Canvas와 RawImage inventory, Unity `VideoPlayer` inventory를 추가했다. non-live 상세 topology는 1초 저주기·변경 기반으로만 기록하며 1ms fast path에서 전체 객체를 열거하지 않는다.
- PID 32908에서 홈, 상점, 프로듀스 준비, 스토리 선택, 세로/가로 ADV를 순회했다. 홈 `Game3DManager → _VLTargetTexture_2160x3840 → 3dTargetImage`, 가로 ADV `_VLTargetTexture_3840x2160`, 세로 ADV `_VLTargetTexture_2160x3840`과 별도 `UICamera` 조작 Canvas를 확인했다.
- UI-only 메뉴에서도 world camera가 잔존할 수 있음을 확인해 camera/scene 이름만으로 immersive를 허용하지 않고 활성 world-presenting RawImage와 source RT를 함께 요구하도록 정책을 정했다.
- 빌드/설치 SHA-256은 `E486CBFE2579EC12D060DE81D31455F7C743BFAB8AB6295465ED165B538FD0AC`다. v0.141 rollback은 `rollback/runtime-bootstrap-v0.141.0-20260810-170625/`이다.

## v0.141.0 — 2026-08-10

상태: 코어 테스트/Release 빌드/설치/VR 성능 실기 성공, **M4 달성**

- OpenXR worker 우선순위를 `Normal`로 복원했다.
- 일반/main-thread GPU wait는 유지하고 OpenXR swapchain 복사 완료 대기에만 최초 최대 1ms bounded spin 후 `Thread.Yield()`로 폴백하는 low-latency polling을 적용했다.
- PID 36804의 100.5초 실제 3D 구간에서 Present 59.47fps, stereo 59.14 pair/s, OpenXR submit 평균 114.79fps·중앙값 117.60fps, pair age 8.38ms, source→clone 3.01ms를 기록했다. buffer block, eye 누락과 런타임 오류는 0건이었다.
- 사용자는 게임 자체가 60fps 고정이 아닌 점을 고려해 현재 성능이 충분하며 추가 개선은 오차 범위라고 승인했다. 향후 안정성 전용 테스트는 모두 생략하고 비차단·미검증으로 기록한다.
- 빌드/설치 SHA-256은 `C1A3990E837AA32551FEB0CD9F237D63DF82A59FCC8680E461B110D0D1870AAE`다. v0.140 rollback은 `rollback/runtime-bootstrap-v0.140.0-20260810-162006/`이다.

## v0.140.0 — 2026-08-10

- OpenXR worker를 `AboveNormal`로 시험했으나 통제된 v0.140 PID 7700에서 OpenXR submit 114.09fps로 v0.139보다 유의한 개선이 없었다.
- 외부 GPU 작업이 겹친 PID 35100에서 NVIDIA TDR/LiveKernelEvent 141과 연쇄 프로세스 종료가 발생했다. 통제 재실행에서는 재현되지 않았지만 우선순위 변경은 채택하지 않았다.

## v0.139.0 — 2026-08-10

- eye render scale을 `0.70`에서 `0.65`로 조정해 eye target을 1744x1872로 낮췄다.
- PID 32536에서 Present 57.35fps, stereo 57.04 pair/s, OpenXR submit 115.46fps·중앙값 117.11fps를 기록했다. 추가 해상도 감소의 이득이 작아 `0.65`를 현재 기준으로 확정했다.

## v0.138.0 — 2026-08-10

- 향후 별도 GUI와 호환되는 `vrmod/config/render-resolution-scale.txt`를 추가했다. invariant float `0.50~1.00`만 허용하고 잘못된 값은 `0.75`로 안전 폴백한다.
- 설정값 `0.70`에서 1880x2016 eye target, Present 58.01fps, stereo 57.84 pair/s, OpenXR submit 113.67fps를 기록했다.

## v0.137.0 — 2026-08-10

- eye render buffer를 triple buffering으로 확장하고 registry에 active stereo lease와 안전한 재사용 판정을 추가했다.
- 첫 pair의 동기 GPU 검증은 유지하되 이후 main-thread GPU wait를 제거하고, 보호된 D3D11 command ordering과 OpenXR 최종 fence를 사용했다.
- Present 56.57fps, stereo 56.48 pair/s, main-thread fence 약 0ms를 기록했고 사용자가 체감이 상당히 개선됐다고 판정했다.

## v0.136.0 — 2026-08-10

- 좌·우 OpenXR swapchain blit 뒤 각각 기다리던 GPU fence를 양안 복사 후 한 번만 기다리도록 합쳤다.
- OpenXR submit 113.28fps, copy 1.66ms로 개선됐고 사용자 실기에서 이상이 없었다.

## v0.135.0 — 2026-08-10

- 두 clone의 실제 렌더 완료 뒤에도 남아 있던 중복 Present boundary 조건을 제거했다.
- stereo 약 49.8 pair/s, pair age 10.77ms로 개선됐으나 main-thread GPU fence 약 5.94ms가 다음 병목으로 확인됐다.

## v0.134.0 — 2026-08-10

- arm 시점, source/clone render delta와 stereo wait 원인을 추가 계측했다.
- 모든 arm이 source render 전에 수행되고 양안 clone이 같은 Present에서 완료되며 eye 누락이 없음을 확인했다. 지연의 26%는 별도 Present boundary 조건 때문이었다.

## v0.133.0 — 2026-08-10

- 고정 두-Present 대기 대신 좌·우 clone 실제 렌더 완료 mask와 최소 Present 조건으로 publish하고 즉시 다음 pair를 arm했다.
- stereo 39.71 pair/s, pair age 14.73ms로 개선됐으나 1/2-Present cadence 혼합에 따른 가벼운 stutter가 관측됐다.

## v0.132.0 — 2026-08-10

- `StereoPerformanceTelemetry`를 추가해 Present, stereo 생산, source/clone render, GPU wait, OpenXR copy/submit을 1초 단위로 계측했다. 렌더 동작은 변경하지 않았다.

## v0.131.0 — 2026-08-10

상태: 코어 테스트/Release 빌드/설치/VR 시각·UI 실기 성공, **M3 달성**

- `VLTonemapping`에 독립 exposure가 없음을 확인하고 원본 톤 곡선은 유지했다.
- OpenXR stereo eye blit에만 sRGB decode → 선형 `+0.2 EV` → sRGB encode를 적용했다. PC mirror, UI와 평면 panel은 변경하지 않는다.
- 최종 clone 기준은 VLBloom intensity 140%, 정수 diffusion 최소 1단계, VLDOF/VLTextureBlur OFF다.
- PID 39884에서 UI hidden/shown/capture-ready 3회와 failure/fallback 0건을 확인했고 사용자가 표시·숨김·재표시를 정상 판정했다.
- 사용자는 M3 완료 조건에서 장시간·clone/RT/handle 수명 계측을 제외하도록 결정했다. 해당 항목은 미검증으로 남기고 시각·UI 조건 충족으로 M3를 2026-08-10 달성했다.
- 빌드/설치 SHA-256은 `F740259B45D2AE2FB098D57CAE51D3D477496F033A395DC98A5FBE3D9CB988B0`이다.

## v0.130.0 — 2026-08-10

- 화면 설정은 v0.129와 동일하게 유지하고 `VLTonemapping` metadata를 진단했다.
- 필드는 mode, toe/shoulder 곡선, gamma, LUT, GT contrast/black brightness 계열이며 독립 exposure는 없었다.

## v0.129.0 — 2026-08-10

- clone 전용 VLBloom intensity를 원본의 140%로 조정했다. diffusion 최소 1단계와 VLDOF/VLTextureBlur OFF를 유지했다.

## v0.128.0 — 2026-08-10

- VLBloom diffusion이 float 비율이 아니라 원본 값 6의 정수 단계임을 로그로 확인했다.
- `IntParameter.m_Value`로 읽고 쓰며 최소 활성값 1로 clamp하도록 수정했다. intensity는 float parameter로 별도 처리한다.

## v0.127.0 — 2026-08-10

- clone VLBloom intensity 120%와 diffusion 축소를 함께 시험했다.
- 로그에서 intensity `4.25 → 5.10`은 정상 float였지만 diffusion float 표시는 subnormal 값으로 나타나 타입 재검사가 필요함을 확인했다.

## v0.126.0 — 2026-08-10

- intensity와 threshold는 원본으로 두고 VLBloom diffusion만 축소하는 A/B를 추가했다. 사용자 체감 차이는 작았다.

## v0.125.0 — 2026-08-10

- clone VLBloom intensity를 원본으로 복구하고 threshold를 `+0.5` 조정했다.
- PID 30248에서 threshold `0.45 → 0.95`가 6,188 clone 호출에 failure 0으로 적용됐지만 사용자는 이전과 동일하다고 판정해 채택하지 않았다.

## v0.124.0 — 2026-08-10

- 화면 동작은 유지하고 VLBloom, VLDiffusion, 관련 VL metadata 필드 진단을 추가했다.
- VLBloom의 `intensity`, `threshold`, `diffusion`, `color`와 VLDiffusion의 contrast/blend 계열을 확인했다.

## v0.123.0 — 2026-08-10

- v0.122 설정에 clone 전용 `DoVLTextureBlur` bypass를 추가했다.
- 10,102 clone 렌더에서 모두 차단됐지만 사용자는 눈에 띄는 차이를 거의 느끼지 못해 최종적으로 OFF 유지가 허용된다고 판정했다.

## v0.122.0 — 2026-08-10

- clone 전용 `DoVLDOF`를 bypass하고 VLBloom 50%를 유지했다.
- 사용자는 화면이 더 나아졌다고 판정해 VLDOF OFF를 최종 기준에 유지했다.

## v0.121.0 — 2026-08-10

- `VLBloom.intensity` float parameter를 clone `SetupVLBloom` 호출 동안만 50%로 조정하고 즉시 원복했다.
- 사용자 실기에서 과도한 광막이 해결됐고 50% 적용도 명확히 확인됐다. 이후 밝기 복구를 위한 세부 조정을 진행했다.

## v0.120.0 — 2026-08-10

- VLBloom을 유지하고 clone `DrawStarStreak`만 bypass했다.
- 과도한 빛은 다시 나타나고 밝기는 복구돼 StarStreak 단독 원인 가설을 기각했다.

## v0.119.0 — 2026-08-10

- clone `SetupVLBloom`만 bypass했다. 과도한 광막은 해결됐지만 캐릭터 밝기도 낮아져 VLBloom 본체가 두 현상의 공통 원인임을 확정했다.

## v0.118.0 — 2026-08-10

- VLPostProcessPass의 diffusion, additive, bloom, star streak, paraffin, flare, texture blur, virtual effect 메서드 시그니처를 런타임 metadata로 기록했다.

## v0.117.0 — 2026-08-10

- clone은 게임 전용 `VLPostProcessPass.Render` 대신 기반 URP `PostProcessPass.Render`를 호출하고 source는 원본 VL 경로를 유지했다.
- 사용자 실기에서 광막이 해결됐지만 캐릭터가 어두워져 원인을 VL custom pass 묶음으로 확정했다.

## v0.116.0 — 2026-08-10

- `RenderingData.cameraData.camera`를 offset으로 읽어 source와 좌·우 clone 렌더 문맥을 구분했다.
- clone에서만 DrawFlare를 실제로 차단했지만 사용자 화면은 동일해 VL flare 단독 원인 가설을 기각했다.

## v0.115.0 — 2026-08-10

- DrawFlare 호출과 `Camera.current` 상태를 계측했다. SRP 경로에서 모든 호출의 Camera.current가 null임을 확인했다.

## v0.114.0 — 2026-08-10

- `VLPostProcessPass.DrawFlare` 후킹을 추가했으나 SRP의 Camera.current가 null이어서 clone 판별에 사용할 수 없었다.

## v0.112.0–v0.113.0 — 2026-08-10

- 게임 전용 `VL.Rendering.Internal.VLPostProcessPass`와 `VLPostProcessData`를 진단해 bloom, flare, DOF, star streak, motion blur, texture blur, virtual effect 등 실제 렌더 경로를 확인했다.

## v0.109.0–v0.111.0 — 2026-08-10

- clone-owned VolumeStack에서 VLBloom, 표준 Bloom, VLTonemapping, VLStarStreak, ColorAdjustments와 전체 known component를 개별 또는 일괄 비활성화했다.
- API 호출은 성공했지만 최종 VR 출력은 동일해 이 경로가 게임의 실제 VL custom pass 상태를 제어하지 못함을 확정했다.

## v0.108.0 — 2026-08-09

상태: 코어 테스트/Release 빌드/설치 성공, clone 전용 `VLBloom` OFF VR 실기 전, M3 진행 중

- v0.107 PID 41140은 `VL.Rendering.VLBloom`이 실제 metadata에 존재하지만 잘못 가정한 `vl-unity.Runtime.dll` 이미지에서 찾지 못해 v0.104 전체 후처리 OFF로 안전 폴백했다. 따라서 해당 화면도 VLBloom 단독 A/B 근거로 쓰지 않는다.
- v0.108은 metadata의 정확한 namespace/type `VL.Rendering.VLBloom`을 사용하고, generation 구성 시 로드된 IL2CPP assembly의 각 image에 `ClassFromName`을 한 번씩 호출해 소속 image를 찾는다. 프레임별 class/객체 전체 열거는 추가하지 않는다.
- 표준 `Bloom`은 `UnityEngine.Rendering.Universal.Bloom`을 정확한 namespace로 찾는다. clone-owned VolumeStack, `VolumeComponent.active` field 쓰기, 원본 profile 비변경, 전체 OFF 안전 폴백은 유지한다.
- 코어 테스트 9개 통과, Release 빌드 경고 0개/오류 0개. PID 41140 종료 후 v0.107을 `rollback/runtime-bootstrap-v0.107.0-20260809-231058/`에 보관하고 설치했다. 빌드/설치 SHA-256은 `46CC98B6FBED8D304F52AD26B10D6EB415BA1B1539B55C7980A8D27BBD9448B5`이며 PC/VR 실기는 아직 하지 않았다.

## v0.107.0 — 2026-08-09

상태: 코어 테스트/Release 빌드/설치 성공, 잘못된 effect image 가정으로 전체 OFF 폴백·A/B 무효, M3 진행 중

- v0.106 PID 27020 실기에서 설정 `vlbloom-off`는 읽혔지만 `VolumeComponent.set_active`를 찾지 못해 안전하게 v0.104 전체 후처리 OFF로 폴백했다. `System.MissingMethodException`이 기록됐으므로 해당 화면은 VLBloom 단독 A/B 근거로 쓰지 않는다.
- Unity VolumeComponent의 `active`는 property setter가 아니라 public bool field이므로, clone-owned VolumeStack에서 얻은 대상 component의 기반 `VolumeComponent.active` 필드를 `il2cpp_field_set_value`로 false로 설정하도록 수정했다.
- 원본 Volume profile/source camera는 수정하지 않으며 기존 `ViaScripting` clone stack, 실패 시 전체 OFF 폴백과 설정 mode는 유지한다.
- 코어 테스트 9개 통과, Release 빌드 경고 0개/오류 0개. PID 27020 종료 후 v0.106을 `rollback/runtime-bootstrap-v0.106.0-20260809-230202/`에 보관하고 설치했다. 빌드/설치 SHA-256은 `659FB4AFC7F0C499EFA55BC4FE19B25FD06E4BF0CE7008D91E9CD11F02E8B880`이며 PC/VR 실기는 아직 하지 않았다.

## v0.106.0 — 2026-08-09

상태: 코어 테스트/Release 빌드/설치 성공, VR 실행은 전체 OFF 안전 폴백으로 A/B 무효, M3 진행 중

- `vrmod/config/visual-effect-mode.txt`를 추가했다. 프로세스 시작 시 `all-off`, `all-on`, `vlbloom-off`, `bloom-off` 중 하나를 읽으며 현재 값은 `vlbloom-off`다.
- `vlbloom-off`/`bloom-off`에서는 clone post-processing을 켜고 각 clone 카메라의 Volume Framework를 `ViaScripting`으로 전환한다. 매 stereo render 직전 clone 위치에서 자체 VolumeStack을 갱신하고 선택한 VolumeComponent만 inactive로 둔다.
- 첫 A/B는 게임 전용 `VLBloom`만 끈다. 색보정, tonemapping, DoF 등 나머지 후처리와 clone depth `On`은 유지한다.
- 원본 Volume profile, source Camera와 PC 화면은 변경하지 않는다. clone 전용 API 또는 대상 component가 없으면 두 clone 모두 v0.104의 전체 post-processing OFF로 자동 폴백하고 `stereo-visual-effect-override-fallback`을 기록한다.
- 성공 시 `stereo-visual-effect-override-ready`에 mode와 clone-only 적용 상태를 기록한다. clone-ready 이벤트에도 구성/적용/폴백 상태를 추가했다.
- 새로운 전체 객체 고주기 열거는 없다. type/method는 generation 구성 시 해석하고 render 시에는 clone 두 개의 명시적 VolumeStack만 갱신한다.
- 코어 테스트 9개 통과, Release 빌드 경고 0개/오류 0개. 설치 SHA-256은 `3ADC855BD65A275DB5EBB34BE616A0941A3E8545CFCD8120A8A6274FD4EAC222`다.
- 설치 전 v0.104는 `rollback/runtime-bootstrap-v0.104.0-20260809-220130/`에 보관했다. PID 27020에서 live 생성/연속 publish/이탈은 동작했지만 `set_active/1` MissingMethodException으로 전체 OFF 폴백했으므로 VLBloom 단독 실기는 미검증이다.

## v0.105.0 — 2026-08-09

상태: **VR 실기 실패/철회**, v0.104로 소스·설치 복귀, M3 진행 중

- v0.104 사용자 실기에서 캐릭터 파묻힘은 사실상 사라졌고 남은 일부 빛은 제작 의도로 볼 수 있는 수준이었다. 좌우 차이는 명확하지 않아 문제 없음으로 판정했지만, 전체 화면은 다소 밋밋했다.
- 게임 metadata에는 일반 `ScreenSpaceLensFlare` 타입이 없고 커스텀 `VLBloom`과 SRP data-driven Lens Flare 경로가 포함돼 있어, 존재하지 않는 표준 Volume component를 조작하지 않는다.
- v0.103의 clone depth `On`을 유지하고 clone post-processing을 다시 true로 복원한다. 그 대신 clone `Camera.allowHDR`만 false로 강제해 색보정 등 후처리 경로는 살리면서 Bloom/Lens Flare에 들어가는 고휘도 범위를 제한하는 A/B다.
- `Camera.CopyFrom`이 매 pair clone 속성을 원본 값으로 되돌리므로, 자연 렌더와 예비 수동 렌더 양쪽에서 CopyFrom 직후 clone HDR override를 재적용한다.
- `stereo-camera-clones-ready`에 source/clone `allowHDR`를 기록한다. 원본 Camera/Volume/조명/에셋은 변경하지 않는다.
- 코어 테스트 9개 통과, Release 빌드 경고 0개/오류 0개. 설치 SHA-256은 `7E99266BA98756D9BA7083ABDF0DF807426E6A9B98A0EDA55875209157EB78D3`다.
- 설치 전 v0.104는 `rollback/runtime-bootstrap-v0.104.0-20260809-214252/`에 보관했다.
- 사용자 실기에서 v0.103과 동일하게 해결 이전의 캐릭터 파묻힘이 재발했다. clone HDR OFF는 과도한 후처리 광량을 억제하지 못하므로 가설을 기각한다.
- 소스와 설치본을 사용자 판정 성공 v0.104로 되돌렸다. 재빌드와 기존 v0.104 rollback의 SHA-256은 모두 `0BB78747...964C3E`로 정확히 일치한다.
- 실패 v0.105는 `rollback/runtime-bootstrap-v0.105.0-20260809-215051/`에 보관했다.

## v0.104.0 — 2026-08-09

상태: 코어 테스트/Release 빌드/설치/VR 실기 성공, 원인 분리 완료, 최종 화질안은 아님

- v0.103 사용자 실기에서 전경 캐릭터의 광원 가림이 약간 개선됐지만 캐릭터가 빛에 파묻혀 보이는 현상이 일부 남았다.
- v0.103 PID 35804 로그는 source depth `Auto(2)`/실효 false, clone depth `On(1)`/실효 true, renderer index 2, source/clone render type Base(0), source shadow/post-processing true를 두 generation에서 동일하게 기록했다.
- 같은 세션에서 서로 다른 두 live가 생성·이탈했고 약 20 pair/s, UI capture와 portrait restore를 기록했으며 failure 이벤트는 0개였다.
- 잔여 현상이 후처리 계열인지 분리하기 위해 v0.103의 clone depth 강제를 유지한 채 clone `renderPostProcessing`만 false로 바꿨다. 원본 카메라·Volume·조명·게임 에셋은 변경하지 않았다.
- v0.104는 원인 진단용 A/B이며 최종 영상 설정이 아니다. 캐릭터 가독성과 함께 블룸·색보정·DoF 등 전체 후처리 손실을 예상하고 판정한다.
- 코어 테스트 9개 통과, Release 빌드 경고 0개/오류 0개. 설치 SHA-256은 `0BB78747A6455E6A853F9677B9DB1DFE4DCE533AFA1EA15475E1AF91AD964C3E`다.
- 설치 전 v0.103은 `rollback/runtime-bootstrap-v0.103.0-20260809-213310/`에 보관했다.
- 사용자 실기에서 캐릭터 파묻힘은 사실상 사라졌고 남은 일부는 제작 의도로 볼 수 있는 수준이었다. 좌우 차이는 문제 없음으로 판정했다. 다만 전체 후처리 OFF로 화면이 다소 밋밋해 최종 설정으로 채택하지 않는다.

## v0.103.0 — 2026-08-09

상태: 코어 테스트/Release 빌드/설치/VR 실기 부분 개선, M3 진행 중

- 사용자가 광원보다 카메라 가까이에 있는 물체, 특히 캐릭터가 광원을 가리지 못하고 빛이 통과해 보이는 현상을 보고했다. 해결 여부는 아직 실기로 확인하지 않았다.
- 원본 카메라의 URP renderer index, render type, `requiresDepthOption`, `requiresDepthTexture`를 clone 생성 시 기록한다.
- 원본의 `Auto` 판정과 별개로 좌·우 clone에만 `UniversalAdditionalCameraData.requiresDepthTexture=true`를 명시 적용하고, getter 재확인 값이 true가 아니면 clone setup을 실패시켜 평면 폴백한다.
- 원본 Camera, Volume, 조명, 블룸 강도와 게임 에셋은 변경하지 않았다. 이번 A/B의 단일 변수는 clone의 명시적 depth texture 요구다.
- `stereo-camera-clones-ready`에 source/clone depth texture·depth option·render type과 renderer index를 추가했다.
- 코어 테스트 9개 통과, Release 빌드 경고 0개/오류 0개. 설치 SHA-256은 `E725D1383628CE9AB4877925072186376DD05F87B689591FA9B74BBD11E0353C`다.
- 설치 전 v0.101은 `rollback/runtime-bootstrap-v0.101.0-20260809-212324/`에 보관했다.
- 기준선 검사는 기존과 동일하게 `gakumas.exe`, `GameAssembly.dll`, `global-metadata.dat` 불일치로 실패했다. 사용자 승인 임시 호환성 범위와 제품 기준선 검증을 계속 구분한다.
- PID 35804에서 source depth는 `Auto(2)`/false, clone depth는 `On(1)`/true로 두 generation 모두 확인됐다. 서로 다른 두 live, UI capture, 이탈/portrait restore가 동작했고 failure는 0개, 게시율은 약 20 pair/s였다.
- 사용자는 캐릭터의 광원 가림이 약간 개선됐지만 빛에 파묻혀 보이는 현상이 일부 남는다고 판정했다. 따라서 depth 누락은 원인의 일부이며 결함 전체를 해결 처리하지 않는다.

## v0.102.0 — 2026-08-09

상태: **실기 실패/철회**, v0.101로 소스·설치 복귀, M3 진행 중

- `il2cpp_gchandle_free`를 선택적 공개 IL2CPP export로 로드하고, 각 live generation에서 모드가 만든 clone GameObject/Camera/URP data, eye/UI RenderTexture와 render request의 GC handle만 명시적으로 추적한다.
- source 이탈 때 stereo/UI texture registry와 armed capture를 먼저 정리한 뒤 generation 소유 handle을 해제한다. export가 없는 게임 버전에서는 게임 실행을 막지 않고 미해제 누적 수를 로그로 남긴다.
- `stereo-camera-clones-ready`, `stereo-generation-ui-resources-ready`, `stereo-camera-generation-retired`에 generation 번호, 활성 clone/eye·UI RT/request/query/handle 수, 누적 handle 생성·해제·미해제 수와 process private/working-set 메모리를 기록한다. 새 고주기 전체 객체 열거는 추가하지 않았다.
- generation 자원 검사 스크립트와 기존 진단 스크립트 기본 버전 변경은 v0.101 복귀와 함께 제거했다.
- 코어 테스트 9개 통과, Release 빌드 경고 0개/오류 0개. PowerShell 검사 스크립트 구문 검증도 통과했다.
- 설치 SHA-256: `E32F40FA28D6C8FA5483D723882B767781A938DD3F9F2761240B8EAA74721D90`
- v0.101 설치본은 `rollback/runtime-bootstrap-v0.101.0-20260809-205111/`에 보관했다.
- generation 식별 로그를 보강하기 전의 중간 v0.102 빌드는 `rollback/runtime-bootstrap-v0.102.0-20260809-205359/`에 보관했다.
- 기준선 검사는 기존과 동일하게 `gakumas.exe`, `GameAssembly.dll`, `global-metadata.dat` 불일치로 실패했다. 승인된 제품 기준선 검증과 임시 호환성 실기를 계속 구분한다.
- v0.102 PID 35696에서 첫 live `ssmk001`은 607 pair, 약 19~20 pair/s와 UI capture까지 정상 동작했다.
- 2026-08-09 20:58:17 live 이탈의 1920x1080→1080x1920 전환 직후 `generation-retired` 이전에 `coreclr.dll` access violation `0xc0000005`로 프로세스가 종료됐다. Windows Application Error/.NET Runtime/WER와 로그 종단이 일치한다.
- 새 코드와 v0.101의 차이 중 이탈 경계에서 실행된 `il2cpp_gchandle_free` 경로가 직접 회귀 원인으로 판정됐다. 결함을 해결 처리하지 않으며 이 방식은 폐기한다.
- 사용자 지시로 소스와 설치본을 시작 시점 v0.101에 정확히 복구했다. 재빌드 SHA-256도 원래 v0.101 `6192DF0D...751DA`와 일치한다. 실패 v0.102 설치본은 `rollback/runtime-bootstrap-v0.102.0-20260809-210528/`에 보관했다.

## v0.101.0 — 2026-08-09

상태: 코어 테스트/Release 빌드/설치/VR 실기 성공, M2 달성

- 두 번째 v0.100 session PID 25368에서 live generation 3/4/5가 약 44.3초/40.3초/38.0초 유지돼 M2의 30초×두 live 조건을 충족했다.
- 그러나 이탈 후 portrait 화면 파손이 다시 발생했고 사용자가 창을 수동 리사이징하면 즉시 복구됨을 확인했다. `Screen` 높이는 1912/1919/1931 등으로 흔들렸다.
- 최신 v0.101 session PID 30636에서 live source/clones-ready/retire가 각 3회, 자동 portrait nudge/원복이 각 2회, Canvas refresh가 12회 기록됐고 관련 failure는 없었다.
- 사용자가 두 번째 이후 이탈에서 수동 리사이즈 없는 정상 portrait 복귀를 확인했다. v0.100의 30초 이상 지속 기록과 결합해 M2를 2026-08-09 달성했다.
- 첫 live 전 정상 portrait 크기를 canonical size로 저장한다. 두 번째 이후 generation 이탈 후 portrait가 안정되면 `Screen.SetResolution` windowed 3-argument overload로 높이를 1픽셀 줄이고 100ms 뒤 canonical 크기로 원복한다.
- 각 단계는 `portrait-resolution-nudge-applied`, `portrait-resolution-restored`, 실패 시 `portrait-resolution-nudge-failure`로 기록한다. 원본 파일이나 창 위치는 변경하지 않는다.
- 원복 뒤 Canvas layout도 다시 갱신한다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `6192DF0D6A3C3DF64F7B3DF08FE377B83FB6A51A2B3610FD87460C6AF8E751DA`
- v0.100 설치본은 `rollback/runtime-bootstrap-v0.100.0-20260809-203425/`에 보관했다.

## v0.100.0 — 2026-08-09

상태: 코어 테스트/Release 빌드/설치 성공, M2 기능 실기 성공/30초×두 live 지속 조건 전

- v0.99은 같은 프로세스에서 서로 다른 세 live의 generation을 모두 생성·검증했다. clone/render failure는 0건이며 두 번째 live는 약 49.2초, 882 pair까지 유지됐다.
- 두 번째와 세 번째 VR 이탈 뒤 PC 창 내용이 왼쪽 약 56%에 압축되고 오른쪽이 검게 남는 현상을 사용자 화면으로 확인했다.
- 같은 시점 로그에서 Unity `Screen` 높이가 `1920 → 1788 → 1780 → 1785 → 1920`으로 흔들렸다. 모드에는 창/해상도/ResizeBuffers 변경 호출이 없으므로 직접 리사이징이 아니라 외부/게임 resize 뒤 Canvas 레이아웃 미갱신 가능성을 우선 진단한다.
- portrait 크기가 안정되어 orientation rebind가 요청되면 `Canvas.ForceUpdateCanvases()`를 호출하고 성공/실패 이벤트를 기록한다. 게임 창 크기 자체는 변경하지 않는다.
- v0.99 후속 live에서 UI natural capture가 `arm-normal-render` NRE로 반복 실패했다. UI capture RenderTexture/request도 source 이탈 시 폐기해 다음 generation에서 재생성한다.
- M2 재진입 핵심은 확인됐지만 30초×두 live 조건과 이탈 후 PC 레이아웃 무결성이 모두 충족되지 않아 마일스톤은 진행 중으로 유지한다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `D7855DD9E7CFAF5F92F5035A4ADC3E4AE98BE415AFFB7C89E742777FB9DEB579`
- v0.99 설치본은 `rollback/runtime-bootstrap-v0.99.0-20260809-201839/`에 보관했다.
- 사용자 v0.100 실기에서 실제 stereo 진입 3회와 각 이탈 후 정상 portrait 창 복귀를 확인했다.
- PID 35400 로그에는 validated generation 3개, UI capture ready 3개, portrait Canvas refresh 12개가 있으며 clone/render/UI/layout-refresh failure는 0개다.
- validated generation의 지속은 약 7.1초, 10.5초, 5.9초였다. M2의 30초×두 live 지속 조건은 아직 충족되지 않아 마일스톤 달성 선언은 보류한다.

## v0.99.0 — 2026-08-09

상태: 코어 테스트/Release 빌드/설치 성공, M2 다중 live 수명주기 VR 실기 전

- v0.98 첫 live는 621 pair까지 약 18.4~20.7 pair/s로 정상 게시됐다.
- 두 번째 `env_3d_live_all001-00-noon`과 source-ready까지 감지했지만 `clear-reused-eye-render-targets`에서 Unity `NullReferenceException`이 발생해 clone setup이 중단되고 평면을 유지했다.
- scene 전환은 clone Camera뿐 아니라 rooted eye RenderTexture와 render request wrapper도 무효화할 수 있음을 확정했다.
- source 이탈 시 camera, eye RT 네 개, render request 네 개의 모든 Unity pointer/handle 상태를 폐기하고 D3D11 GPU query를 Release한다.
- 다음 concrete env live에서 camera, eye RT, render request와 GPU query를 모두 새로 만든다. 새 eye RT는 검정 clear 후 최초 실제 렌더/visible 검증을 통과해야 게시한다.
- `docs/VR_MILESTONES.md`를 추가했다. 현재 M2 진행 중이며, 완료 조건을 사용자 실기와 로그가 모두 충족하면 즉시 “마일스톤 M2 달성”이라고 알린다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `C4CF453A88961E016C75313D80E951A3CC7DAC0E6B12CD1C2A20A33D3E76CCFE`
- v0.98 설치본은 `rollback/runtime-bootstrap-v0.98.0-20260809-200555/`에 보관했다.

## v0.98.0 — 2026-08-09

상태: 코어 테스트/Release 빌드/설치 성공, 두 번째 live 평면 폴백 수정 VR 실기 전

- v0.97은 크래시 없이 첫 live generation을 폐기하고 두 번째 clone generation까지 생성했다.
- 두 번째 live의 임시 `Live` 장면에서 clone을 생성한 뒤 실제 `env_3d_live_all001-00-noon` 장면으로 바뀌며 clone이 다시 제거됐다. 재사용 eye RT의 이전 영상 때문에 첫 pair가 거짓으로 검증되어 VR이 잠깐 표시됐고, 다음 render에서 `NullReferenceException` 후 평면 폴백했다.
- stereo eligibility와 clone setup을 구체적인 `env_3d_live_*` 활성 장면, 가로 화면 및 유효한 `Game3DManager` source가 모두 있을 때로 제한했다. 중간 `Live` 확인/준비 장면은 평면으로 유지한다.
- 재사용 eye RT 네 개를 새 generation 시작 전에 검정으로 clear한다. 새 clone이 실제로 렌더하지 못하면 이전 곡의 잔상이 최초 visible-pair 검사를 통과하지 않는다.
- 장기 성능 목표를 OpenXR/HMD 120Hz 지원으로 지정했다. 장면/카메라/UI 전환 감지는 전체 객체 열거를 1ms마다 수행하지 않고 이벤트 기반 fast path로 1~10ms 반응 지연을 목표로 한다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `32B9BFBC9AC2E16D874BD4E93292CA9F83F9208AB6E643E5CF6EF6E0484655DE`
- v0.97 설치본은 `rollback/runtime-bootstrap-v0.97.0-20260809-195634/`에 보관했다.

## v0.97.0 — 2026-08-09

상태: 코어 테스트/Release 빌드/설치 성공, 재진입 수정 VR 실기 전

- v0.96 사용자 실기에서 첫 라이브는 1,668 eye pair까지 약 20 pair/s로 유지됐고 source 이탈 및 eye clear도 정상 기록됐다.
- 두 번째 `Live 1920x1080`에서 새 source가 ready가 된 직후 `coreclr.dll`의 `0xc0000005` access violation으로 게임이 크래시했다.
- 로그의 `Camera.allCameras`에서 첫 라이브 clone은 이탈 뒤 사라졌지만 런타임의 `_stereoCloneSetupReady`와 camera pointer가 남아 있었다. 두 번째 source에서 파괴된 clone을 재사용한 것이 원인이다.
- source가 unavailable로 바뀌면 scene-bound stereo camera generation과 UI scene cache를 폐기한다. 이전 eye texture는 즉시 clear한다.
- scene-independent eye RenderTexture 네 개, render request와 GPU query는 유지한다. 다음 source에서 좌우 clone camera와 `UniversalAdditionalCameraData`를 새로 만들고 3초 warm-up 뒤 같은 OpenXR session에서 생산을 재개한다.
- `stereo-camera-generation-retired`와 retire failure 진단 이벤트를 추가했다.
- 기존 clone의 GC handle을 현재 해제하지 못하므로 라이브 반복 횟수에 비례한 작은 managed-wrapper leak 가능성은 남아 있다. 대형 eye RT는 재사용한다.
- clone ready 로그의 잘못된 “AA disabled” 문구를 현재 동작인 원본 AA 복사로 정정했다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `1895755713CD9CD6C7EB8AEE7BED631473816CE5F986D726082AE00CF85679C6`
- v0.96 설치본은 `rollback/runtime-bootstrap-v0.96.0-20260809-185915/`에 보관했다.

## v0.96.0 — 2026-08-09

상태: 코어 테스트/Release 빌드/설치 성공, 첫 live 정상/두 번째 live 진입 coreclr access violation

- `StereoWorldEyeOffsetScale`을 `0.225`에서 `0.275`로 변경했다.
- 물리 eye offset의 게임 월드 반영 비율이 22.5%에서 27.5%로 증가한다. 깊이감과 좌우 융합 피로는 사용자 실기 전이다.
- v0.95의 유효 live source camera gating, eye texture clear, 지속 OpenXR 세션과 v0.90 UI 경로는 유지한다.
- 프로젝트 핵심 기능 완료 후 world scale, render scale, 패널, 후처리와 OpenXR 런타임을 조정하는 별도 GUI 설정 도구를 제작하기로 했다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `E57BD493A8319B71612A71B5E6C82A7440FE622D3DA9206107D1310FB083B5CC`
- v0.95 설치본은 `rollback/runtime-bootstrap-v0.95.0-20260809-181920/`에 보관했다.
- 사용자 실기에서 첫 live는 1,668 pair까지 정상 동작했으나, 이탈 후 두 번째 live source-ready 직후 파괴된 이전 clone camera pointer를 재사용해 게임이 크래시했다.

## v0.95.0 — 2026-08-07

상태: 코어 테스트/Release 빌드/설치 성공, PC 및 VR 실기 전

- v0.94 첫 라이브는 약 20 pair/s로 2분 이상 유지되어 OpenXR 90초 상한 제거에 성공했다.
- 다른 라이브 진입 중 `Live`가 1920x1080으로 먼저 바뀌었지만 cameraCount=2이고 실제 env 3D source camera가 없는 준비 구간에서 VR이 꺼졌다.
- `_stereoPumpEligible`을 `IsLiveScene && landscape && _lastLiveCamera != 0` 조건으로 강화해 실제 3D source camera가 있어야 시작·재개한다.
- 자격을 잃으면 `ClearStereoTextures`로 이전 좌우 eye COM 참조와 timestamp를 즉시 해제한다. 새 source camera가 탐색되면 기존 OpenXR 세션에서 생산을 재개한다.
- `stereo-live-source-ready`와 `stereo-live-source-unavailable` 진단 이벤트를 추가했다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `B9EC24B24FF8410EAFFF55AB5F368FDEBBC64D15E51BE9EC4A0F8980B9429063`
- v0.94 설치본은 `rollback/runtime-bootstrap-v0.94.0-20260807-204203/`에 보관했다.

## v0.94.0 — 2026-08-07

상태: 코어 테스트/Release 빌드/설치 성공, 단일 라이브 지속 성공/다른 라이브 재진입 실패

- v0.93 로그에서 스테레오 생산은 30초를 넘어 약 55초간 정상 지속했고 failure가 없었다.
- 별도 OpenXR 프레임 루프가 부트스트랩 후 약 90초에 반환했고, 직후 view stale 대기와 함께 VR 출력이 종료됐다.
- `testDurationMilliseconds=90_000`, 전체 loop 120,000ms, `maximumFrameCount=12_000` 상한을 모두 제거했다.
- 활성 프레임 루프에서 OpenXR 세션 이벤트를 계속 poll한다. `STOPPING`에서는 `xrEndSession`, `LOSS_PENDING`/`EXITING`에서는 안전하게 루프를 종료한다.
- 런타임 세션 종료 이벤트나 실제 frame API 오류가 없는 동안 frame submit을 무기한 계속한다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `B88CAA43D6C93BC2E5F2DDBECCBB1E3C9BF0FE32E688CEE9FDCDB562899EBCA3`
- v0.93 설치본은 `rollback/runtime-bootstrap-v0.93.0-20260807-203421/`에 보관했다.
- 실기에서 첫 라이브는 2분 이상 정상 지속했지만, 다른 라이브 진입 준비 구간에서 실제 3D source camera 없이 재개해 VR이 꺼졌다. OpenXR 세션 자체의 failure/exit는 없었다.

## v0.93.0 — 2026-08-07

상태: 코어 테스트/Release 빌드/설치 성공, VR 지속 실기 실패(OpenXR 90초 상한)

- v0.92에서 clone AA를 꺼도 흐림/빛 번짐이 유지되어 AA 가설을 기각했다.
- 영상 설정을 전체 후처리를 처음 활성화한 v0.91 기준으로 복구했다. 원본 AA, 후처리와 그림자 설정을 clone에 적용한다.
- `StereoContinuousDurationMilliseconds=30_000` 진단 제한과 capture-window 종료 조건을 제거했다. 승인된 Live 가로 장면 동안 양안 쌍을 계속 생산한다.
- OpenXR view snapshot이 일시적으로 stale이면 영구 실패 처리하지 않고 `stereo-view-state-waiting`을 제한적으로 기록하며 자동 재시도한다.
- Live 장면을 벗어날 때 렌더 중인 좌우 clone 카메라를 비활성화하고 arm 상태를 해제한다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `B007957C337F8949CB8FE7DE8959CFCD06C2F51B9639ABEB77921EE807118EDE`
- v0.92 설치본은 `rollback/runtime-bootstrap-v0.92.0-20260807-202701/`에 보관했다.
- 실기에서 양안 게시율은 30초를 넘어 유지됐지만 상위 OpenXR 프레임 루프가 부트스트랩 후 약 90초에 종료되어 VR 출력이 꺼졌다.

## v0.92.0 — 2026-08-07

상태: 코어 테스트/Release 빌드/설치 성공, VR 실기에서 AA 가설 기각

- v0.91 사용자 실기에서 전체 후처리 복원으로 VR 화면이 PC와 거의 비슷해졌다.
- 다만 특정 영역 또는 화면 전체가 흐려지고 빛이 번져 보이는 현상이 조금 남았다.
- 후처리, 블룸과 원본 그림자 설정은 유지하고 clone `UniversalAdditionalCameraData.antialiasing`만 `None(0)`으로 강제한다.
- 원본/clone AA enum을 `stereoSourceAntialiasing`, `stereoCloneAntialiasing`으로 기록한다.
- 흐림이 줄면 AA와 75% eye 해상도 확대 조합을 원인으로 판정한다. 그대로면 Depth of Field, Motion Blur, Bloom을 clone 전용 Volume override로 순차 진단한다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `B97D5C424337D9A424FD41C92A85E72793A3C577CBBDAAD2F550B16D5C68F680`
- v0.91 설치본은 `rollback/runtime-bootstrap-v0.91.0-20260807-202035/`에 보관했다.
- 사용자 실기에서 AA를 꺼도 흐림/빛 번짐이 유지됐다. 최종 분리 실패 시 v0.91 영상 설정으로 복구하기로 결정했다.

## v0.91.0 — 2026-08-07

상태: 코어 테스트/Release 빌드/설치 성공, VR 후처리 부분 성공/흐림 문제 확인

- v0.90의 UI 및 정상 렌더 루프 스테레오 경로는 유지한다.
- v0.79부터 원본 URP 설정 복사 뒤 `renderPostProcessing=false`를 강제하던 코드를 `true`로 바꿔 블룸을 한 변수로 A/B한다.
- 그림자 설정은 원본 `renderShadows` 값을 그대로 복사하며 이번 버전에서는 강제 변경하지 않는다.
- `stereo-camera-clones-ready` 로그에 원본 `renderShadows`, 원본 `renderPostProcessing`, clone 적용 후처리 값을 추가했다.
- 실기에서 블룸, 좌우 일치, 후처리 점멸, 스테레오 성능과 v0.90 UI 회귀를 함께 판정해야 한다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `E24B882C991EA2A7E2056432B6E15C8646A559FB6BEBE156F1A63FF6190721A8`
- v0.90 설치본은 `rollback/runtime-bootstrap-v0.90.0-20260807-201250/`에 보관했다.
- 사용자 실기에서 전체 화면 인상은 PC와 거의 비슷해졌으나 특정 영역 또는 전체 화면이 흐려지고 빛이 번져 보이는 현상이 조금 있었다.

## v0.90.0 — 2026-08-07

상태: 코어 테스트/Release 빌드/설치 성공, VR UI 실기 성공

- v0.89 실기에서 검은 배경 제거, UI OFF 제거와 다시 ON 시 재캡처가 모두 정상 동작했다.
- UI를 켠 터치 입력의 이펙트가 one-shot 캡처에 일부 포함되어 정지 상태로 남았다.
- 로그상 표시 감지와 capture arm이 같은 sampler 호출에서 발생하고 약 100ms 뒤 ready가 됐으므로, UI 표시 후 500ms 안정화 지연을 추가했다.
- UI 숨김 시 registry clear는 지연 없이 유지한다. 다시 표시할 때만 약 0.5~0.7초 늦게 캡처될 수 있다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `2F5F58329DC1EBDFF0F1D8497AE949BCE5F039C87C665F029FC96DB97CA7485F`
- v0.89 설치본은 `rollback/runtime-bootstrap-v0.89.0-20260807-200614/`에 보관했다.
- 사용자 실기에서 터치 이펙트 잔상이 사라졌고 UI 표시, OFF 제거와 다시 ON 시 재표시가 모두 정상 동작했다. 별도 PC 장시간 회귀는 아직 남아 있다.

## v0.89.0 — 2026-08-07

상태: 코어 테스트/Release 빌드/설치 성공, VR UI 기능 성공/잔상 문제 확인

- v0.88 실기에서 UI 캡처는 성공했지만 불투명 검은 배경이 화면을 가렸고 UI OFF 뒤에도 레이어가 남았다.
- v0.88 진단 BMP의 32x32 alpha 표본 1,024개가 모두 255였으며 999개가 거의 검은 불투명 픽셀이었다. UICamera/URP가 배경 cull 뒤에도 opaque alpha를 출력함을 확인했다.
- UI 전용 D3D11 blit shader를 추가해 RGB 최대값이 `3/255` 이하인 검은 픽셀만 alpha 0으로 바꾼다. 스테레오/평면 blit은 변경하지 않았다.
- UI 진단 BMP를 원본 캡처 RT가 아니라 black-key가 적용된 OpenXR UI swapchain 이미지에서 저장한다.
- 상위 `UICanvasGroup` 대신 `LiveOverlayContent/MusicTimeRoot/MusicTime` 자식 `CanvasRenderer`의 활성 상태, cull, inherited alpha를 우선 감시한다. UI OFF면 registry를 clear하고 ON이면 새 one-shot 캡처를 요청한다.
- 경로/API가 없으면 `LiveOverlayContent` 아래 첫 Graphic 및 기존 상위 CanvasGroup 검사로 폴백한다.
- black-key가 순수 검정 UI 픽셀도 지울 수 있으므로 실기에서 가독성 손실을 함께 확인해야 한다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `B2ED1B30AED8B4CE0200C6FF111C63DFEE18C59B7E88C1F799CD38180A29EB96`
- v0.88 설치본은 `rollback/runtime-bootstrap-v0.88.0-20260807-200036/`에 보관했다.
- 실기에서 UI는 검은 배경 없이 정상 표시됐고 OFF 시 사라지며 다시 ON 하면 재표시됐다. 다만 UI를 켠 터치 이펙트가 one-shot에 일부 고정됐다.

## v0.88.0 — 2026-08-07

상태: 코어 테스트/Release 빌드/설치 성공, VR 실기 실패

- v0.87은 `UICanvasGroup` 표시 감지 후 UI 캡처를 시작하기 전에 `Unexpected Unity object array length: 1021.`로 실패했다.
- 배경 요소 탐색을 base `Graphic` 전체에서 `UnityEngine.UI.Image`로 좁히고 최대 배열 길이를 2,048로 올렸다.
- 캡처 실패 시 UI texture를 clear하고 2초 뒤 재시도한다. 실패가 영구 미표시로 고정되지 않는다.
- 스테레오는 v0.87 실기에서도 약 19~21 pair/s를 유지했다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `0D2F5FE468129A5B6A41B09BA418E35C5BC3B4BAE72E4662C3B2FA4842DC9209`
- v0.87 설치본은 `rollback/runtime-bootstrap-v0.87.0-20260807-195306/`에 보관했다.
- 실기에서 2초 재시도로 캡처는 성공했으나 불투명 검은 배경과 UI OFF 뒤 잔류가 계속됐다. `ui-natural-visibility-hidden`도 기록되지 않아 상위 CanvasGroup이 실제 토글 신호가 아님을 확인했다.

## v0.87.0 — 2026-08-07

상태: 코어 테스트/Release 빌드/설치 성공, VR 실기 실패

- v0.86 실기에서 UI는 표시됐지만 불투명 검은 1920x1080 배경이 화면 중앙을 가렸고 UI OFF 뒤에도 남았다.
- 진단 BMP의 32x32 alpha 표본 1,024개가 모두 255였다.
- 자연 UI 캡처 중 3DTexture 외에 `LiveFullScreenRoot/Background`와 `FadeRoot/BlackTint` Graphic도 cull하고 원래 상태로 복구한다.
- `UICanvasGroup`의 활성 상태와 alpha를 감시한다. 숨김 시 `ClearLiveUiTexture`로 OpenXR UI layer source를 해제하고, 표시 시 새 one-shot 캡처를 요청한다.
- 억제한 Graphic 수를 `uiCaptureSuppressedGraphicCount`에 기록한다.
- UI 진단 이미지 이름을 런타임 버전 기반 `v0.87.0-ui-natural-capture.bmp`로 변경했다.
- 스테레오 pump는 v0.86 경로를 그대로 유지한다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `1A057882DF9D49502D8CBA9E5792A3ACFF7B0C9DECAEE2AF1759D7AADDD831E4`
- v0.86 설치본은 `rollback/runtime-bootstrap-v0.86.0-20260807-194747/`에 보관했다.
- 실기에서 UI 표시 감지는 성공했지만 base Graphic 배열 1,021개가 기본 한도 512를 넘어 캡처 arm 전에 실패했다. UI는 표시되지 않았다.

## v0.86.0 — 2026-08-07

상태: 코어 테스트/Release 빌드/설치 성공, VR 실기 부분 성공

- `Time.get_frameCount` hook에서 같은 game frame당 한 번만 실행되는 경량 `TryPumpStereo`를 추가했다.
- 전체 장면 진단은 기존 100ms 주기를 유지하면서 pair arm/finalize만 매 프레임 진행한다.
- 두 Present가 끝난 pair를 게시한 직후 33ms 목표가 충족되면 다음 pair를 같은 pump 호출에서 준비한다.
- 약 1초마다 `stereo-publish-rate`를 기록하며 실제 pair/s, 마지막 publish 간격과 Present delta를 남긴다.
- render snapshot signature와 orientation target signature에서 clone camera count 토글을 제거했다.
- 카메라 전체 재탐색 주기를 0.5초에서 1초로 낮췄다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `57251F151795405242C83AFF82F396EB3401CB9395A8BE87F27E8A921989AFCE`
- v0.85 설치본은 `rollback/runtime-bootstrap-v0.85.0-20260807-194000/`에 보관했다.
- UI 캡처 경로는 v0.84와 동일하며 이번 버전의 성능 검증과 분리한다.
- 실기 게시율은 약 19.4~20.7 pair/s였고 사용자는 프레임 출력이 정상적으로 보인다고 판정했다.
- UI 자연 캡처는 성공했지만 불투명 검은 배경과 함께 one-shot으로 고정되어 UI OFF가 반영되지 않았다.

## v0.85.0 — 2026-08-07

상태: 코어 테스트/Release 빌드/설치 성공, VR 실기 성능 실패

- `StereoContinuousIntervalMilliseconds`를 67ms에서 33ms로 줄여 명목 스테레오 갱신률을 약 15fps에서 약 30fps로 올렸다.
- UI 캡처와 OpenXR 합성 경로는 v0.84 그대로 유지해 성능 변경 결과를 분리한다.
- 코어 테스트 9개 통과, 빌드 경고 0개/오류 0개.
- 설치 SHA-256: `B97CA131589CBB37976BE11B598BD681134D246D29D5B560CB1A871F4CAE69DD`
- v0.84 설치본은 `rollback/runtime-bootstrap-v0.84.0-20260807-193012/`에 보관했다.
- 사용자 실기에서 실제 출력은 약 2fps로 보였다.
- `OnFrameCount`가 전체 `TryCapture`를 100ms로 제한하고 pair arm/finalize가 서로 다른 호출을 요구해, 33ms 목표와 무관하게 구조적 상한이 약 5fps임을 확인했다.
- clone enabled 토글 때문에 v0.85 실행에서 전체 `render-snapshot`이 290회 기록됐다. Camera/Canvas/UI 전체 열거와 대형 JSON 기록이 추가 프레임 저하 요인이다.
- 다음 버전은 경량 stereo pump를 진단 sampler에서 분리하고 실제 pair publish fps를 계측해야 한다.

## v0.84.0 — 2026-08-06

상태: 빌드/설치 성공, 2026-08-07 사용자 실기 실패(UI 미표시)

- 실패한 CanvasRenderer mesh replay를 기본 비활성화했다.
- 원본 `UICamera`의 target을 투명 UI RenderTexture로 두 번의 정상 Present 동안 리디렉션한다.
- 캡처 동안 `LiveFullScreenRoot/VisbleRoot/3DTexture`의 CanvasRenderer만 cull한다.
- 캡처 뒤 UICamera target과 cull 상태를 원상복구한다.
- 첫 구현은 one-shot 정지 UI 진단이다.
- 실기 로그에서 `ui-natural-capture-armed` 뒤 캡처 RT에 가시 픽셀이 없다는 검증 실패가 발생했다.
- 실패 때문에 UI texture가 registry에 게시되지 않았고 OpenXR UI quad도 제출되지 않았다. 이번 미표시는 FOV 밖 배치가 아니다.
- 16x16 sparse 검사점이 화면 가장자리 UI를 놓쳤거나, 라이브 최초 UI 숨김 상태에서 one-shot 캡처가 실행됐을 가능성이 남아 있다.

## v0.83.0 — 2026-08-06

상태: 실기 실패

- CanvasRenderer 요소를 투명 RT에 10Hz로 재생했다.
- 3DTexture RawImage를 제외하고 OpenXR alpha quad layer를 Projection Layer 위에 제출했다.
- 로그상 58 elements/58 draw calls였으나 진단 RT가 완전히 비어 있었고 VR UI도 보이지 않았다.
- 이 명령 버퍼 재생 경로는 사용 중단했다.

## v0.82.0 — 2026-08-06

상태: 사용자 실기 성공

- 수동 `SubmitRenderRequestsInternal` 반복 경로를 폐기했다.
- 좌우 clone 카메라를 Unity 정상 렌더 루프에서 실제 Present 두 번 동안 렌더한다.
- 이중 버퍼와 GPU fence 뒤 완성된 양안 쌍만 OpenXR에 게시한다.
- 사용자 확인: 연속 움직임, 깊이, 조명/이펙트 안정.
- 후속 관찰: 원본 PC와 비교해 그림자와 블룸 일부 누락 의심.

## v0.81.0 — 2026-08-06

- clone 카메라를 Unity 정상 렌더 루프에 약 0.3초 넣고 정적 양안 쌍을 저장했다.
- 사용자 확인: 정상 라이브 조명과 이펙트 포함.

## v0.80.0 — 2026-08-06

- 한 번 완성한 양안 쌍을 30초간 갱신하지 않고 유지했다.
- 사용자 확인: 정지화면이 완전히 안정적.
- 결론: 과거 점멸은 OpenXR 레이어 폴백이 아니라 반복 수동 렌더 내용 문제.

## v0.79.0 — 2026-08-06

- clone 카메라의 URP `renderPostProcessing`을 false로 강제했다.
- 이펙트 점멸은 계속돼 후처리 토글만의 문제가 아님을 확인했다.
- 이 설정은 v0.84에도 남아 있으며 블룸 누락의 유력 원인이다.

## v0.78.0 — 2026-08-06

- eye buffer를 권장 크기의 75%(2016x2160)로 낮추고 명목 15fps로 올렸다.
- 점멸 지속. 단순 저주기 샘플링 가설 기각.

## v0.77.0 — 2026-08-06

- 양안 RT를 이중 버퍼링하고 GPU event query로 한 쌍 완료를 동기화했다.
- 점멸 지속. 생산/소비 텍스처 경쟁만의 문제는 아니었다.

## v0.76.0 — 2026-08-06

- `UniversalAdditionalCameraData`를 JSON이 아닌 renderer index와 공개 속성 직접 복사로 전환했다.
- clone 출력은 보였지만 이펙트/후처리 상태가 반복적으로 바뀌었다.

## v0.74.0~v0.75.0 — 2026-08-05~06

- 검은 eye texture를 Projection Layer에 게시하지 않는 visible-pixel guard를 추가했다.
- `JsonUtility.FromJsonOverwrite`가 IL2CPP metadata에 없어 URP 복제에 실패했고 평면 폴백이 유지됐다.

## v0.73.0 — 2026-08-05

- 독립 clone Camera를 처음 연속 렌더했다.
- 사용자 결과: VR 검은 화면. 원인: URP 추가 카메라 데이터 누락.

## v0.68.0~v0.72.0 — 2026-08-05

- OpenXR `XrCompositionLayerProjection`과 눈별 swapchain을 구현했다.
- eye swap은 악화돼 원래 매핑으로 복구했다.
- 월드 눈 간격 25%에서 사용자 평가가 가장 좋았고 이후 22.5%로 미세 조정했다.
- 원본 카메라 연속 수동 렌더는 깊이를 만들었지만 조명/이펙트 프레임이 간헐적이었다.

## v0.61.0~v0.67.0 — 2026-08-05

- VD OpenXR eye pose, IPD 0.061m, 눈별 비대칭 FOV를 실측했다.
- Quest 권장 2688x2880 eye RT와 clone camera 기반을 만들었다.
- 라이브 안정 대기 뒤 Game3DManager 수동 양안 렌더와 BMP 저장에 성공했다.

## v0.53.0~v0.60.0 — 2026-08-05

- Present 시점의 최종 백버퍼와 world RT를 비교했다.
- CanvasRenderer UI 재생 가능성을 조사했으나 실제 UI 전용 출력은 확보하지 못했다.
- 최종 게임 백버퍼를 평면 패널로 사용해 UI/Localify/ON-OFF와 플리커링을 안정화했다.

## 초기 부트스트랩/패널 단계

- BepInEx/Cpp2IL metadata31 경로 실패를 확인하고 독립 Doorstop + IL2CPP API로 전환했다.
- Virtual Desktop OpenXR 세션, D3D11 texture 제출, 체크보드 패턴, 패널 상하 반전과 색공간을 검증했다.
- 세로 → 가로 → 세로 화면 전환을 실기 완료했다.
