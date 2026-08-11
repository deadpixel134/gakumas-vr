# Local vendor dependencies

배포 패키지를 만들 때 필요한 외부 바이너리는 이 폴더 아래에 로컬로 준비하지만 Git에는 커밋하지 않습니다.

- `staging/bepinex-6.0.0-be.785/`: Unity Doorstop 4.5.0의 `winhttp.dll`, .NET Runtime 6.0.7과 Dobby 파일
- `openxr-loader-1.1.59/`: Khronos OpenXR Loader 1.1.59
- `downloads/`: 공식 배포처에서 내려받은 원본 패키지 캐시

v0.163.0 패키지는 staging의 Dobby를 `BepInEx/core/dobby.dll`에 포함하고 Apache-2.0 전문을 함께 배포합니다. 설치기는 기존 Localify/BepInEx가 같은 경로의 파일을 제공하면 이를 보존하며, 없는 경우에만 패키지 파일을 설치합니다.

외부 파일은 각각의 공식 배포처에서 취득하고 [`../THIRD_PARTY_NOTICES.txt`](../THIRD_PARTY_NOTICES.txt)의 라이선스 고지를 따라야 합니다. 게임 파일, Localify 파일, 사용자 설정이나 로그를 이 폴더에 복사하거나 커밋하지 마십시오.
