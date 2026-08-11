[한국어](README.md) | [English](README.en.md) | [日本語](README.ja.md)

# Gakumas VR

학원 아이돌마스터 DMM판을 위한 비공식 Meta Quest/OpenXR VR 모드입니다. 3D 장면은 양안 VR로, 그 밖의 화면은 비율을 보존한 평면 패널로 표시하며 VR 컨트롤러로 게임 UI를 조작할 수 있습니다.

현재 공개 버전은 **v0.163.0 프리릴리스**입니다. Meta Quest 2 + Virtual Desktop OpenXR에서 개발·검증했습니다. SteamVR OpenXR와 Meta Quest Link는 규격상 사용 가능한 예비 지원 대상으로, 아직 이 프로젝트에서 실기 검증하지 않았습니다.

## 문서

- [설치 방법](docs/ko/INSTALLATION.md)
- [사용 방법과 조작법](docs/ko/USAGE.md)
- [프로그램 구조와 동작 방식](docs/ko/ARCHITECTURE.md)
- [개발자 안내](vrmod/README.md)
- [현재 개발 상태](docs/GAKUMAS_VR_STATUS.md) · [설계 기록](docs/GAKUMAS_VR_DESIGN.md) · [마일스톤](docs/VR_MILESTONES.md) · [변경 기록](vrmod/CHANGELOG.md)

## 핵심 기능

- 라이브·홈·커뮤니케이션 등 지원되는 3D 환경의 OpenXR 스테레오 출력
- 2D 화면의 시야 정면 자동 패널과 3D 화면의 왼손 보조 패널
- 오른손 레이 포인터, A/트리거 클릭, B 뒤로 가기, 스틱 스크롤
- 한국어·영어·일본어 설치기 및 설정 프로그램
- 렌더 배율, 입체감, 패널 위치·손 역할·버튼, VFX 세부 설정
- 기존 Localify 한글 패치와 설정을 보존하는 설치·제거 절차

## 주의 사항

- Windows 11 x64, DMM판, Unity 6000.0.77f1 기준으로 개발 중인 프리릴리스입니다.
- v0.163.0부터 필요한 Dobby를 배포 ZIP에 포함합니다. 기존 Localify/BepInEx가 같은 경로의 파일을 제공하면 설치기가 이를 덮어쓰지 않고 보존합니다.
- 문제가 생기면 게임을 종료한 뒤 설치기로 제거하거나 이전 버전으로 롤백하십시오. 이슈에 로그를 첨부하기 전 계정 식별자나 인증 정보가 없는지 확인하십시오.

이 저장소에는 게임 원본 파일, Localify 에셋, 사용자 설정, 로그, 롤백 데이터와 빌드 산출물을 포함하지 않습니다. 프로젝트 소스는 [MIT License](LICENSE)로 배포되며 외부 구성 요소는 각자의 라이선스를 따릅니다. 자세한 내용은 [크레딧 및 외부 라이선스](CREDITS.md)를 참고하십시오.

> Gakumas VR은 비공식 팬 프로젝트이며 게임 개발사·배급사와 관련이 없습니다. 게임과 관련 상표·저작물의 권리는 각 권리자에게 있습니다. 사용하려면 정식으로 설치한 게임이 필요합니다.
