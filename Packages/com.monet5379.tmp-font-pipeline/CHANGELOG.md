# Changelog

이 파일은 [Semantic Versioning](https://semver.org/) 형식을 따른다.

## [Unreleased]

## [0.1.0] - 2026-10-30

### Added

- UPM 패키지 `com.monet5379.tmp-font-pipeline` — `Runtime/` · `Editor/` · `Samples~/Demo` (JSON 예시 · `IFontWarmupTarget` 스크립트).
- Git URL·임베디드 설치. 개발 repo는 `Packages/manifest.json`의 `file:com.monet5379.tmp-font-pipeline`.

### Changed

- **Breaking (레이아웃):** `Assets/TmpFontPipeline` 복사 설치 대신 Package Manager 설치. Git 루트 = Unity 프로젝트 (`project/` 분리 없음).
- 패키지에서 Demo 기본 경로·**Create Demo Assets** UI 제거. Extract JSON/출력은 `EditorPrefs`; Apply Active Profile은 지정 경로만 (Demo 폴백 없음).
- `FontAtlasApplyProfile._generatedFolder` 기본값 빈 문자열.
- Demo 부트스트랩(`DemoFontProfileBootstrap`)은 이 개발 repo의 `Assets/Demo/Editor/` 전용.

### Notes

- Git URL pin: `https://github.com/monet5379/unity-tmp-font.git?path=/Packages/com.monet5379.tmp-font-pipeline#v0.1.0`
- 구조·불변조건: [레포 README](https://github.com/monet5379/unity-tmp-font) · [Architecture](https://github.com/monet5379/unity-tmp-font/blob/main/docs/architecture/Architecture_TmpFontPipeline.md)
