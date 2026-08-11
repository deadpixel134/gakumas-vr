# Gakumas VR

학원 아이돌마스터 DMM판용 Meta Quest/OpenXR VR 모드 개발 작업공간이다.

현재 단계는 원본 설치본과 Localify를 보존하는 독립 IL2CPP/OpenXR 런타임에서 라이브 연속 스테레오를 검증하고 2D UI 분리를 개발하는 단계다. BepInEx interop 없이 Doorstop + 공개 IL2CPP API로 동작한다.

현재 기준 문서는 [`../docs/GAKUMAS_VR_STATUS.md`](../docs/GAKUMAS_VR_STATUS.md), 단계별 완료 조건은 [`../docs/VR_MILESTONES.md`](../docs/VR_MILESTONES.md)다. v0.100.0의 다중 live 재진입·30초 이상 지속과 v0.101.0의 자동 portrait 1픽셀 nudge/원복 사용자 실기를 근거로 M2를 달성했다. 현재는 M3 장시간 안정성·자원 수명·시각 차이 검증 단계다.

2026-08-09 재검사에서는 게임 핵심 파일 3개의 기준선 불일치가 발견됐다. 사용자가 현재 설치본에서 임시 호환성 테스트를 명시적으로 승인했으므로 테스트는 계속하지만, 승인된 제품 기준선 검증과 구분한다. 상세 해시와 인수인계 순서는 [`../docs/VR_HANDOFF.md`](../docs/VR_HANDOFF.md)를 따른다.

## 현재 체크포인트

- [ ] 현재 게임 업데이트 기준선 재승인 — UnityPlayer/Localify는 일치하나 exe/GameAssembly/metadata 불일치
- [x] BepInEx/Cpp2IL 경로 실기 판정: registration 탐색 실패로 사용 중단
- [x] 런타임 IL2CPP API + Dobby 프레임 훅 기반 진단 부트스트랩
- [x] 실기 장면·화면 크기·방향·카메라 진단 로그 수집
- [x] 실제 종횡비 기반 세로→가로→세로 전환 상태 머신 실기 검증
- [x] Virtual Desktop OpenXR + 실제 게임 백버퍼 기반 Quest 헤드 고정 패널 출력
- [x] 라이브 월드 RT 직접 출력, 상하 반전 및 색공간 보정
- [x] 반복 URP UI 렌더 요청이 PC 플리커링 원인임을 실기 격리하고 해당 경로 비활성화
- [x] OpenXR eye pose/IPD/비대칭 FOV 실측과 정적 Projection Layer
- [x] Unity 정상 렌더 루프 + Present 동기화 + 이중 버퍼 기반 연속 스테레오(v0.82 실기)
- [x] 원본 UICamera 정상 렌더 one-shot 실기(v0.84): UI 미표시, 빈 RT 판정 후 레이어 미제출
- [x] v0.85에서 스테레오 갱신 목표를 15fps(67ms)에서 30fps(33ms)로 상향하고 설치
- [x] v0.85 30fps 실기: 약 2fps로 실패, sampler/publish 병목 확인
- [x] v0.86 경량 매 프레임 stereo pump 분리, 실제 publish fps 계측과 반복 snapshot 억제
- [x] v0.86 VR 실기: 약 20 pair/s, 사용자 체감 프레임 정상
- [x] v0.87 불투명 UI 배경 제외 및 UI ON/OFF layer 수명 연동 구현/설치
- [x] v0.87 실기: Graphic 배열 한도 초과로 UI 캡처 전 실패
- [x] v0.88 Image 한정 탐색·2초 재시도 구현/설치
- [x] v0.88 실기: 캡처 성공, 검은 배경과 UI OFF 뒤 잔류 지속
- [x] v0.89 UI black-key 투명화·자식 CanvasRenderer 표시 감지·제출 결과 BMP 구현/설치
- [x] v0.89 실기: UI 투명 배경·OFF 제거·ON 재캡처 성공, 터치 이펙트 잔상 확인
- [x] v0.90 UI 표시 후 500ms 안정화 지연 구현/설치
- [x] v0.90 실기: 터치 잔상 제거 및 UI 표시·OFF·재표시 정상
- [x] v0.91 clone 후처리 활성화와 원본 그림자/후처리 상태 계측 구현/설치
- [x] v0.91 실기: PC와 거의 유사, 일부/전체 흐림과 빛 번짐 확인
- [x] v0.92 후처리 유지 + clone AA None 구현/설치
- [x] v0.92 실기: 흐림/빛 번짐 지속, AA 가설 기각
- [x] v0.93 v0.91 영상 설정 복구·30초 제한 제거·OpenXR view 자동 재시도 구현/설치
- [x] v0.93 실기: 30초 이후 양안 생산 지속, 상위 OpenXR 90초 제한으로 VR 종료
- [x] v0.94 OpenXR 90초/120초/12,000프레임 상한 제거·세션 이벤트 종료 구현/설치
- [x] v0.94 실기: 첫 라이브 2분 이상 지속, 다른 라이브 재진입 시 VR 꺼짐
- [x] v0.95 live source camera gating·이전 eye texture clear·자동 재개 구현/설치
- [x] v0.96 월드 eye offset scale 27.5% 변경/설치
- [x] v0.96 첫 live 1,668 pair까지 정상; 이탈 뒤 stale clone 포인터로 두 번째 live 진입 시 coreclr access violation 재현
- [x] v0.97 source 상실 시 camera/UI generation 폐기, eye RT/request/query 재사용, 다음 live clone/URP 재생성 구현·설치
- [x] v0.97 실기: 크래시는 해결, 두 번째 임시 Live에서 생성된 clone이 실제 env 전환 때 제거되어 첫 pair 뒤 평면 폴백
- [x] v0.98 concrete env 3D live gating + 재사용 eye RT 사전 clear 구현·설치
- [x] v0.98 실기: 첫 live 621 pair, 두 번째 env에서 무효 eye RT clear NRE로 평면 유지
- [x] v0.99 live generation 전체 재생성 구현·설치
- [x] v0.99 세 live 재진입 성공; 이탈 뒤 PC portrait 레이아웃 파손 발견
- [x] v0.100 portrait Canvas refresh + UI capture generation 재생성 구현·설치
- [x] v0.100 재진입·UI 기능 실기 성공; 첫 실기 이탈은 정상, 장시간 2차 실기에서 portrait 파손 재발
- [x] v0.100 두 live 이상 30초 지속 성공; portrait 파손 재발/수동 resize 복구
- [x] v0.101 portrait 자동 1px resize nudge/원복 구현·설치
- [x] M2/v0.101: 두 번째 이후 이탈에서 수동 개입 없는 정상 portrait 복귀
- [ ] M3: 서로 다른 3개 live/총 30분, UI 회귀, generation별 자원 수명 계측, 그림자·블룸·blur 허용 기준
- [ ] 그림자와 블룸 일부 누락 진단 — 현재 clone 카메라는 post-processing을 켜며, 잔여 차이는 clone 전용으로 분리 진단 필요
- [ ] 장시간 패널 세션의 세로/가로 스왑체인 재생성과 컨트롤러 입력
- [ ] 보류 이슈: 플레이 중 스킬 사용 시 최상위 2D 스킬 이미지/애니메이션의 VR 출력 누락 점검
- [ ] 제한 없는 연속 스테레오의 여러 라이브 재진입·장시간 자원 수명 검증
- [ ] 세로/가로 전환 및 런타임별 회귀 테스트
- [ ] 설치/제거 패키지
- [ ] 핵심 기능 완료 후 별도 GUI 설정 도구와 검증된 설정 파일 스키마

2026-08-02 게임 업데이트로 엔진이 Unity 2022.3.57f1에서 6000.0.77f1로 변경되고 IL2CPP metadata가 v31.1로 바뀌었다. 구형 BepInEx pre.2는 metadata 29까지만 지원했다. be.785와 독립 Cpp2IL pre.21 + StrippedCodeRegSupport 조합은 metadata 파싱에는 성공했지만 code/metadata registration 탐색에 실패했다. 따라서 이 게임에서는 BepInEx interop 경로를 사용하지 않는다. 구형 로더는 `vrmod/rollback/bepinex-6.0.0-pre.2-metadata31-failure`에 보존했다.

현재 구현은 GameAssembly의 공개 IL2CPP 런타임 export로 타입을 탐색하고 `il2cpp_runtime_invoke`로 필요한 Unity API를 호출한다. Unity `Time.get_frameCount` icall을 Dobby로 후킹해 실제 Unity 메인 스레드에서 안전하게 진단한다. 이 방식은 Cpp2IL registration 복원과 생성된 interop DLL에 의존하지 않는다.

2026-08-02 v0.6 실기 로그에서 `Splash`, `Title`, `OutGame`, `Produce`, `Live` 및 다수의 `env_3d_*` 장면을 검출했다. 라이브 진입 시 실제 렌더 크기는 `1080x1920`에서 `1920x1080`으로 바뀌었지만 `Screen.orientation`은 계속 `1`을 반환했다. 따라서 방향 상태는 실제 렌더 크기의 종횡비를 주 신호로 판정하고 `Screen.orientation`은 참고값으로만 사용한다.

## 디렉터리

- `baseline/`: 지원 대상으로 고정한 게임 및 Localify 파일 해시
- `scripts/`: 기준선 검증과 설치 상태 진단 스크립트
- `src/`: 독립 Doorstop 런타임, 코어 상태 머신 및 보존된 초기 BepInEx 진단 소스
- `tests/`: Unity 런타임과 분리 가능한 상태 머신 테스트
- `logs/`: JSONL 런타임 로그와 eye/UI/Present 진단 BMP
- `rollback/`: 설치 전 런타임과 실패한 로더 경로 백업
- `CHANGELOG.md`: 버전별 코드 변경과 실기 결과

현재 상태는 [`../docs/GAKUMAS_VR_STATUS.md`](../docs/GAKUMAS_VR_STATUS.md), 상세 설계는 [`../docs/GAKUMAS_VR_DESIGN.md`](../docs/GAKUMAS_VR_DESIGN.md), 변경 이력은 [`CHANGELOG.md`](CHANGELOG.md)를 참고한다.

## 안전 원칙

- `version.dll`, `GameAssembly.dll`, `UnityPlayer.dll`을 덮어쓰지 않는다.
- 게임 업데이트나 Localify 변경이 감지되면 몰입형 VR을 기본 비활성화한다.
- OpenXR 또는 플러그인 초기화 실패가 게임 실행 실패로 이어지지 않게 한다.
- 로그에 실행 인자, viewer ID, token을 기록하지 않는다.

## 개발 명령

```powershell
.\vrmod\scripts\Build-VRMod.ps1
.\vrmod\scripts\Install-Bootstrap.ps1
.\vrmod\scripts\Test-Coexistence.ps1
.\vrmod\scripts\Test-RuntimeBootstrap.ps1
.\vrmod\scripts\Test-SceneDiagnostics.ps1
.\vrmod\scripts\Test-PresentationState.ps1 -RequireLandscapeTransition -RequireRoundTrip
.\vrmod\scripts\Test-OpenXrRuntime.ps1
```

`Build-VRMod.ps1`은 코어 테스트와 부트스트랩을 항상 빌드한다. `BepInEx/interop`이 아직 없으면 진단 플러그인은 의도적으로 보류한다.
