# TMP Font Pipeline — Agent Guide

```text
profile: personal
```

공개 패키지·실험용이다. Cursor는 **이 repo + unity-studio-kit** 멀티 루트로 연다.

| Kit | 경로 |
|-----|------|
| 공통 | `unity-studio-kit/docs/common/` · `.cursor/rules/common/` |
| 글쓰기 | `unity-studio-kit/docs/common/WritingGuide.md` |
| 프로필 | `unity-studio-kit/docs/personal/` · `.cursor/rules/personal/` |

## 프로젝트 개요

- **목적:** 로컬라이즈 JSON → 고유 글자 추출 → Static TMP atlas Apply → 런타임 `FontWarmupService` 워밍업
- **Git 루트:** 이 저장소 최상단 (= Unity 프로젝트)
- **Unity:** `./` (에셋은 repo 루트 `Assets/`)

## 레이아웃

```text
unity-tmp-font/
├── AGENTS.md
├── README.md
├── docs/architecture/Architecture_TmpFontPipeline.md
├── Packages/manifest.json                    ← file: 로컬 패키지
├── Packages/com.monet5379.tmp-font-pipeline/ ← UPM (Runtime · Editor · Samples~/Demo · Samples~/DemoWithFonts)
├── Assets/Project/                           ← Demo 스크립트·Apply 프로필·Generated (비설치)
├── Assets/Editor/                              ← DemoFontProfileBootstrap (비설치)
├── Assets/Scenes/                              ← SampleScene
├── Assets/Addressables/                        ← JSON·Static 폰트
└── Assets/TextMesh Pro/                      ← 개발 프로젝트 TMP Essentials (패키지 아님)
```

## 에이전트가 지킬 것

- Kit **common** + **personal** (README 계약 + `Architecture_*`; Plan/Optimization/ADR는 요청 시만).
- `.meta` 생성·수정·삭제 금지.
- 커밋은 사용자 **명시 요청** 시에만. Kit CommitMessages — `type(scope): 한글 제목` + **본문(why) 필수**
- Demo 놀이터(`Assets/Project` · `Assets/Editor` · `Assets/Scenes`)를 출시 스키마·필수 설치 경로로 취급하지 않음. **이 repo**에서는 Demo만 쓰고 UPM **Samples Import는 하지 않는다** (루트 README NOTE).
- 구조·경계·public API·Extract/Apply/Warmup 흐름 변경 시 `docs/architecture/Architecture_TmpFontPipeline.md` 갱신. 계약 변경 시 README 먼저.
- Shell에서 Unity `-batchmode` · 프로젝트 `scripts/` 래퍼 실행 금지 — [`unity-cli-agent.mdc`](https://github.com/monet5379/unity-studio-kit/blob/main/.cursor/rules/common/unity-cli-agent.mdc).

## 이 repo만의 메모

- 네임스페이스: `TmpFontPipeline` · Demo `TmpFontPipeline.Demo` (놀이터 스크립트)
- asmdef: `TmpFontPipeline.Runtime` / `TmpFontPipeline.Editor` (패키지) · `TmpFontPipeline.Sample.Demo` / `TmpFontPipeline.Sample.DemoWithFonts` (+ `.Editor`) · `TmpFontPipeline.Demo` / `TmpFontPipeline.Demo.Editor`
- 의존: `com.unity.ugui` (Unity 6 TMP) · Editor Newtonsoft (`com.unity.nuget.newtonsoft-json`)
- 패키지 코드는 Demo 전용 `Assets/...` 경로를 가정하지 않음. Extract JSON/출력 폴더는 EditorPrefs · Apply 프로필은 사용자 지정 경로만.

### 커밋 (이 repo)

| 항목 | 값 |
|------|-----|
| 언어 | 한글 |
| scope (정본) | `package`, `demo`, `docs` |
