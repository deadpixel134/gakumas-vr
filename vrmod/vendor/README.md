# Local vendor dependencies

배포 패키지를 만들 때 필요한 외부 바이너리는 이 폴더 아래에 로컬로 준비하지만 Git에는 커밋하지 않습니다.

- `staging/bepinex-6.0.0-be.785/`: Unity Doorstop 4.5.0의 `winhttp.dll`과 .NET Runtime 6.0.7 파일
- `openxr-loader-1.1.59/`: Khronos OpenXR Loader 1.1.59
- `downloads/`: 공식 배포처에서 내려받은 원본 패키지 캐시

v0.162.0 런타임은 대상 게임의 `BepInEx/core/dobby.dll`도 사용하지만 현재 패키지 작성기는 이를 payload에 복사하지 않습니다. 완전한 클린 설치 지원을 선언하기 전에 Dobby의 정식 배포·라이선스 포함 방식과 실기 검증을 완료해야 합니다.

외부 파일은 각각의 공식 배포처에서 취득하고 [`../THIRD_PARTY_NOTICES.txt`](../THIRD_PARTY_NOTICES.txt)의 라이선스 고지를 따라야 합니다. 게임 파일, Localify 파일, 사용자 설정이나 로그를 이 폴더에 복사하거나 커밋하지 마십시오.
