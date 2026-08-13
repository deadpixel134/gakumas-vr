[한국어](VR_INTERACTION_SPEC.md) | [English](../en/VR_INTERACTION_SPEC.md) | [日本語](../ja/VR_INTERACTION_SPEC.md)

# 재사용 가능한 VR 조작·포즈 합성 명세

[프로젝트 홈](../../README.md) · [사용 방법](USAGE.md) · [프로그램 구조](ARCHITECTURE.md)

이 문서는 Gakumas VR v0.173.0에서 사용자 실기로 승인되고 v0.174.0에서 기본값을 조정한 조작감과 안전 구조를 다른 Unity 게임의 VR 모드에서도 재현하기 위한 구현 계약이다. 게임별 카메라 탐색 코드는 달라질 수 있지만, 아래 좌표·입력·표시 불변식은 그대로 유지해야 같은 동작을 기대할 수 있다.

## 1. 사용자에게 보이는 계약

- 기본 역할은 **왼손 스틱 시야 회전, 오른손 스틱 이동**이다. 설정에서 이동 손을 바꾸면 두 역할이 함께 교체된다.
- 이동은 현재 최종 시야의 완전한 3차원 전방·오른쪽 벡터를 따른다. 위를 보며 전진하면 상승하고 아래를 보며 전진하면 하강한다.
- 시야 회전은 월드 기준 yaw와 pitch만 바꾼다. 스틱 조작으로 roll을 만들지 않는다.
- 기본 회전은 30° 스냅이며 15°/30°/45°/60°를 선택할 수 있다. 부드러운 연속 회전도 선택할 수 있다.
- 라이브 독립 6DoF는 기본 활성이고 이동 속도 기본값은 1.95m/s다.
- 실제로 HMD를 옆으로 기울인 변화량은 roll로 보존한다. VR 진입 당시의 목 기울기, 장면 카메라 roll, 스틱 회전에서 파생된 roll은 보존하지 않는다.
- VR 안에서 스틱 스크롤은 사용하지 않는다.
- 3D가 없으면 최종 게임 화면을 시야 정면 패널로 표시한다. 3D에서는 같은 화면을 손 패널로 제공하고 반대 손의 ray와 버튼으로 조작한다.
- 실패 시 게임 창은 계속 실행하고 VR만 평면 패널 또는 비활성 상태로 폴백한다.

## 2. 좌표계와 원점

OpenXR tracking pose를 Unity 좌표로 바꿀 때 현재 구현은 다음 변환을 사용한다.

```text
positionUnity   = ( x,  y, -z)
rotationUnity   = (-x, -y,  z, w)
```

스테레오 generation을 시작할 때 좌·우 눈의 중앙 위치와 정규화한 평균 orientation을 원점으로 한 번 캡처한다. 이후 위치는 `inverse(origin) × (current - origin)`으로 상대화한다. 장면이 바뀌거나 generation이 폐기되면 pose mapper, 이동 offset, 인공 회전, 입력 latch를 함께 초기화한다.

라이브 독립 6DoF에서는 진입 당시 게임 카메라의 월드 위치와 방향을 anchor로 한 번 캡처한다. 이후 게임 카메라 경로와 각도 변화는 VR 시점을 끌고 가지 않는다. 비-live 3D에서는 현재 유효한 source 카메라의 위치와 바라보는 방향을 기준으로 삼는다.

## 3. 위치와 이동

물리 머리 이동과 양쪽 눈 offset은 OpenXR 원점에 상대화한 뒤 Unity 좌표로 변환한다. 게임 월드 눈 간격은 설정된 `worldEyeOffsetScale`을 적용한다.

스틱 이동은 deadzone을 적용한 `(strafe, forward)`를 최종 시야 quaternion으로 회전한다.

```text
worldDelta = finalViewRotation × (strafe, 0, forward)
offset    += normalize-if-needed(worldDelta) × speed × dt
```

- pitch를 제거하거나 XZ 평면에 투영하면 안 된다.
- 프레임 일시 정지 뒤 과도하게 순간 이동하지 않도록 적분 `dt`를 최대 0.1초로 제한한다.
- deadzone 기본값은 0.20, 이동 속도 기본값은 1.95m/s다.
- 이동 offset, 물리 머리 위치, 눈 offset은 모두 동일한 월드 navigation basis에서 계산해야 한다.

## 4. 스틱 회전 상태기

스틱의 미세한 대각 오차가 yaw와 pitch를 동시에 바꾸지 않도록 매 샘플에서 절댓값이 큰 한 축만 선택한다.

```text
abs(x) >= abs(y) -> yaw 입력만 사용
abs(y) >  abs(x) -> pitch 입력만 사용
```

스냅 모드:

- 활성 임계값은 0.65다.
- 한 번 임계값을 넘으면 선택한 축으로 설정 각도만큼 정확히 한 단계 회전한다.
- 스틱이 deadzone 0.20 안으로 돌아와야 다시 arm된다.
- 유지 중 자동 반복하지 않는다.

부드러운 모드:

- 선택된 한 축에 radial deadzone 재매핑을 적용한다.
- `degreesPerSecond × min(dt, 0.1)`로 적분한다.
- 인공 pitch는 극점 특이점을 피하도록 약 ±89.1°로 제한한다.

인공 yaw와 pitch는 별도 scalar로 누적하고 매 프레임 quaternion을 새로 만든다. 이전 최종 quaternion에 증분 quaternion을 계속 곱하는 방식은 축 drift와 roll 혼입 때문에 사용하지 않는다.

## 5. roll 분리와 최종 회전

v0.173.0의 핵심 규칙은 **HMD의 실제 roll 변화량만 마지막에 다시 넣는 것**이다.

각 OpenXR eye orientation을 Unity 좌표로 변환한 뒤 월드축 성분을 다음과 같이 구한다.

```text
forward = rotation × (0,0,1)
right   = rotation × (1,0,0)
up      = rotation × (0,1,0)

yaw   = atan2(forward.x, forward.z)
pitch = atan2(-forward.y, length(forward.xz))
roll  = atan2(right.y, up.y)
```

VR 원점의 yaw/pitch/roll을 각각 저장하고 현재 값과의 차이를 계산한다. yaw와 roll 차이는 `[-π, π]`로 wrap한다.

장면 카메라에서는 forward만 사용해 base yaw/pitch를 구하고 base roll은 폐기한다. 최종 회전은 다음 scalar를 합쳐 매 프레임 재구성한다.

```text
finalYaw   = baseYaw   + artificialYaw   + physicalYawDelta
finalPitch = clamp(basePitch + artificialPitch + physicalPitchDelta)
finalRoll  = physicalRollDelta

finalRotation = Yaw(finalYaw) × Pitch(finalPitch) × Roll(finalRoll)
```

따라서:

- 스틱을 몇 번 돌려도 roll 항은 변하지 않는다.
- VR 진입 당시 HMD가 기울어져 있어도 그 기울기는 원점에서 상쇄된다.
- 같은 목 기울기를 유지한 채 고개만 좌우로 돌리면 새로운 roll이 생기지 않는다.
- VR 진입 후 사용자가 실제로 목을 더 기울인 변화량만 화면 roll로 나타난다.

쿼터니언 상대 회전 `inverse(origin) × current`를 통째로 `artificial × relativeHmd`에 곱하면 안 된다. 기울어진 원점에서 물리 yaw가 상대 quaternion의 roll로 표현되어 스틱 회전 뒤 수평선이 기울 수 있다.

## 6. 패널과 UI 조작

- fresh stereo가 없으면 `XR_VIEW_REFERENCE_SPACE`의 정면 quad가 주 콘텐츠다. 검은 공간에 이전 3D 프레임을 남기지 않는다.
- stereo가 있으면 projection world가 주 콘텐츠이고 손 패널은 보조 UI다.
- 손 패널은 controller tip을 중심으로 view-space 수직·viewer-facing으로 배치한다. 설정으로 위치·크기·회전·viewer-facing을 바꿀 수 있다.
- Grip은 손 패널을 토글한다. tracking 또는 손 FOV 조건을 만족하지 않으면 숨기고 GPU copy/acquire/submit과 hit-test도 생략한다.
- 포인터 ray와 패널 plane의 교점을 UV로 바꾸고, 표시된 콘텐츠 영역의 종횡비 letterbox를 제외한 뒤 게임 client 좌표로 변환한다.
- 기본 A/Trigger는 클릭·드래그, B는 뒤로 가기다. Trigger는 눌림 초기 좌표를 먼저 latch해 당길 때의 손 떨림을 줄인다.
- 게임 창이 foreground가 아닐 때 Windows 입력을 주입하지 않으며, 비활성화·전환 시 눌린 버튼을 반드시 release한다.

## 7. 렌더·수명·폴백 구조

- Unity 정상 렌더 루프의 좌·우 clone 카메라가 한 쌍을 생산한다. 두 눈이 완료되기 전에는 게시하지 않는다.
- OpenXR 제출 주기와 게임 렌더 주기를 분리한다. 게임이 약 60fps여도 완성된 최신 쌍을 HMD 주기에 맞춰 재제출할 수 있다.
- camera, eye RenderTexture, render request, GPU query는 scene-bound generation으로 취급한다. 장면 이탈 뒤 stale Unity wrapper를 재사용하지 않는다.
- 전체 객체 탐색은 저주기·변경 기반 진단으로 제한한다. 1~10ms 전환 fast path에서 전체 Unity 객체를 열거하지 않는다.
- source/clone/OpenXR 실패는 projection 제출을 중단하고 최신 최종 backbuffer 패널로 폴백한다. 원본 게임 DLL·에셋은 수정하지 않는다.

## 8. 다른 게임으로 이식할 때의 계층

재사용 가능한 계층:

1. OpenXR session, view, action, swapchain 관리
2. pose 원점·좌표 변환·yaw/pitch/roll 분해
3. 이동 및 스냅/부드러운 회전 적분기
4. 정면/손 패널, ray-plane UV와 입력 latch
5. generation 수명과 안전 폴백
6. 설정 검증, 설치 manifest, rollback

게임별 adapter 계층:

1. 실제 world source 카메라 판별
2. URP/HDRP/Built-in 카메라 복제 방식
3. UI가 포함된 최종 backbuffer 획득
4. 창 방향·종횡비·scene transition 신호
5. 후처리와 게임 전용 VFX override

게임 이름이나 카메라 이름만으로 3D를 승인하지 말고, 실제 활성 render target과 화면에 표시되는 surface의 관계를 확인해야 한다.

## 9. 필수 회귀 테스트

- 기울어진 scene camera에서 좌우 스냅을 반복해도 world right 벡터의 Y가 0에 가까운가.
- HMD 원점에 roll이 있어도 같은 기울기를 유지한 physical yaw가 roll delta 0을 만드는가.
- HMD를 실제로 15° 더 기울이면 최종 roll이 15°에 가까운가.
- 스틱을 유지했을 때 스냅이 한 번만 발생하고 중앙 복귀 뒤 다시 한 번 발생하는가.
- 대각 스틱 오차에서 우세 축 하나만 변하는가.
- 위/아래를 보며 전진할 때 같은 방향으로 상승/하강하는가.
- 손 역할을 교체하면 이동과 회전 action source가 함께 교체되는가.
- 3D 이탈 시 정면 패널이 즉시 복귀하고 이전 3D 프레임이 남지 않는가.
- 설치·업데이트·제거가 사용자 설정과 다른 모드 파일을 보존하는가.

자동 테스트는 수학과 파일 안전 계약을 검증하지만 HMD 런타임·게임 카메라·체감은 사용자 VR 실기로 별도 판정해야 한다.
