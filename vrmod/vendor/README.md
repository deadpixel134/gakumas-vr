# Local vendor dependencies

배포 패키지를 만들 때 필요한 외부 바이너리는 이 폴더 아래에 로컬로 준비하지만 Git에는 커밋하지 않습니다.

- `staging/bepinex-6.0.0-be.785/`: Doorstop의 `winhttp.dll`과 .NET 6 런타임 파일
- `openxr-loader-1.1.59/`: Khronos OpenXR loader 1.1.59
- `downloads/`: 내려받은 원본 패키지 캐시

이 파일들은 각각의 공식 배포처에서 취득해야 하며 라이선스 고지는 [`../THIRD_PARTY_NOTICES.txt`](../THIRD_PARTY_NOTICES.txt)를 따릅니다. 게임 파일과 Localify 파일을 이 폴더에 복사하거나 커밋하지 마십시오.
