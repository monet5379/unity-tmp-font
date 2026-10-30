# Changelog

이 파일은 [Semantic Versioning](https://semver.org/) 형식을 따른다.

## [Unreleased]

### Removed

- 개발 repo **데모 에셋 생성** 메뉴·`DemoFontProfileBootstrap` — Apply 프로필·`FontRoleCatalog`는 repo 에셋으로 유지, Extract·Active Profile 시드는 `ProjectExtractPathDefaults`.

### Changed

- Editor `MenuItem` 경로를 **Tools → TMP Font Pipeline** 아래로 통일.
- README·Architecture·Samples README·`package.json` 샘플 설명을 위 메뉴·`ProjectExtractPathDefaults`·버전 관리 Demo 에셋 기준으로 정리.
## [0.2.0] - 2026-10-30

### Added

- UPM Samples **Demo (Noto + Baked Atlas)** (`Samples~/DemoWithFonts`) — Noto SDF/TTF · Generated txt · Apply/Catalog · Import 후 **Noto 데모 경로 연결** 메뉴. 슬림 샘플 displayName을 **Demo (JSON + Warmup)** 으로 구분.
- Extract 유럽어 **MergedEuropean** 모드 — `unique_chars_European*.txt` 출력 · Apply `FontAtlasBucket.European` (기본 추출은 언어별 `PerLanguage`).
- Ui 버킷 Extract·Apply에 필수 `_` — JSON에 없어도 Static charset에 포함 (`FontAtlasMandatoryCharacters`; Dialogue Extract txt에는 주입하지 않음).
- Apply — Enabled 엔트리가 같은 `TMP_FontAsset`을 두 번 이상 참조하면 Console·Apply 탭 경고 (Apply 실행은 계속).

### Changed

- `Runtime/` 소스를 **Atlas** · **Warmup** 폴더로 분리 — asmdef·`TmpFontPipeline` public API는 동일.
- ScriptableObject Create 메뉴 경로 한글화 (`TMP 폰트 파이프라인/...`).

### Notes

- Git URL pin: `https://github.com/monet5379/unity-tmp-font.git?path=/Packages/com.monet5379.tmp-font-pipeline#v0.2.0`

## [0.1.1] - 2026-10-30

### Fixed

- 빈 `FontAtlasApplyProfile` Generated Folder가 Windows에서 드라이브 루트 txt를 가리키던 문제 — 경로 해석·Apply 조기 실패·창 버튼 비활성.
- Extract/Apply 실패 후에도 창이 완료로 표시되던 문제 — `bool` 반환·실패 메시지·JSON 폴더에 `String*.json` 없으면 Extract 비활성.

### Notes

- Git URL pin: `https://github.com/monet5379/unity-tmp-font.git?path=/Packages/com.monet5379.tmp-font-pipeline#v0.1.1`

## [0.1.0] - 2026-10-30

### Added

- UPM 패키지 `com.monet5379.tmp-font-pipeline` — `Runtime/` · `Editor/` · `Samples~/Demo` (JSON 예시 · `IFontWarmupTarget` 스크립트).
- Git URL·임베디드 설치. 개발 repo는 `Packages/manifest.json`의 `file:com.monet5379.tmp-font-pipeline`.

### Changed

- **Breaking (레이아웃):** `Assets/TmpFontPipeline` 복사 설치 대신 Package Manager 설치. Git 루트 = Unity 프로젝트 (`project/` 분리 없음).
- 패키지에서 Demo 기본 경로·**Create Demo Assets** UI 제거. Extract JSON/출력은 `EditorPrefs`; Apply Active Profile은 지정 경로만 (Demo 폴백 없음).
- `FontAtlasApplyProfile._generatedFolder` 기본값 빈 문자열.
- Demo 부트스트랩(`DemoFontProfileBootstrap`)은 이 개발 repo의 `Assets/Editor/` 전용.

### Notes

- Git URL pin: `https://github.com/monet5379/unity-tmp-font.git?path=/Packages/com.monet5379.tmp-font-pipeline#v0.1.0`
- 구조·불변조건: [레포 README](https://github.com/monet5379/unity-tmp-font) · [Architecture](https://github.com/monet5379/unity-tmp-font/blob/main/docs/architecture/Architecture_TmpFontPipeline.md)
