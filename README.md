# TMP Font Pipeline

Editor character extraction from localization JSON and runtime TMP font warmup. Moves Dynamic atlas growth and first-draw spikes out of gameplay.

**Phase 2a (done):** Editor extract — `String*.json` → `unique_chars_*.txt`.  
**Phase 2d (done):** `FontAtlasApplyProfile` + Apply menu — Generated txt → Static SDF atlas.  
**Phase 2b/2c (next):** Runtime warmup and Demo scene wiring.

## Install

Copy `unity-tmp-font/Assets/TmpFontPipeline` into your project's `Assets/` (keep Runtime/Editor asmdefs).

This repo's Unity project already has it under `Assets/TmpFontPipeline` and a demo playground under `Assets/Demo`.

After opening the project in Unity, import **TMP Essentials** once (Window → TextMeshPro → Import TMP Essential Resources). Default extract uses Unity `JsonUtility`. `com.unity.nuget.newtonsoft-json` is an Editor dependency for the optional Newtonsoft parser mode only.

## Invariants

- Character set SSOT = extracted Static atlas (`unique_chars_*.txt` → TMP Character Table), not warmup sample text.
- Warmup moves **when** fonts are first drawn; it does not guarantee every glyph.
- Sanitize strips TMP tags, placeholders, and tokens before extraction — raw JSON is not copied verbatim into the atlas.
- UI and Dialogue string buckets stay separate so dialogue-only CJK does not inflate UI font atlases.
- **Font usage role:** `Ui` = in-game common (Medium weight in Demo); `Dialogue` = dialogue (Regular). Role is not TMP weight — it is which Static asset receives which extract bucket.
- European is fully split by language (`English`, `French`, `German`, `Italian`, `Spanish`) for both Ui and Dialogue buckets.
- Language field for Simplified Chinese is **`SimplifiedChinese`** (correct spelling).

## Usage

### Editor — extract (Phase 2a)

Menu: **Tmp Font Pipeline → Extract Unique Characters (JSON)**

JSON parser (explicit, no auto-fallback):

- **Tmp Font Pipeline → JSON Parser → JsonUtility** (default) — fixed language columns; Demo `SampleData` works as-is
- **Tmp Font Pipeline → JSON Parser → Newtonsoft** — irregular / sheet-exported JSON

1. Input: `String*.json` under `Assets/Demo/SampleData`
2. Output: `unique_chars_*.txt` under `Assets/Demo/Generated`

### Editor — apply to Static atlas (Phase 2d)

1. Create a **Font Atlas Apply Profile** (`Assets → Create → Tmp Font Pipeline → Font Atlas Apply Profile`) mapping each **Bucket × Role** to a Static `TMP_FontAsset`, or run **Tmp Font Pipeline → Font Atlas Apply Profile → Create Demo Assets** in this repo.
   - If you already had old profiles (before European split), run this menu again to resync entries.
2. (Optional) **Tmp Font Pipeline → Font Atlas Apply Profile → Select Active Profile...** — defaults to `Assets/Demo/FontAtlasApplyProfile.asset` when present.
3. Run **Extract Unique Characters (JSON)**, then **Apply Generated Characters to Font Assets**.
   - Applier sets target atlas size to `2048x2048` before baking.
4. Confirm Atlas Population Mode = **Static** on target fonts.

Runtime font lookup (Demo / your game): **Font Role Catalog** (`Assets → Create → Tmp Font Pipeline → Font Role Catalog`) — `LanguageId` + `Ui` / `Dialogue` → `TMP_FontAsset`.

Demo mapping (after Create Demo Assets):

| Bucket | Role | Demo Static asset |
|--------|------|-------------------|
| Korean (etc.) | Ui | `*/NotoSans*-Medium SDF` |
| Korean (etc.) | Dialogue | `*/NotoSans*-Regular SDF` |
| English/French/German/Italian/Spanish | Ui | `EN/FR/DE/IT/ES/*-Medium SDF` |
| English/French/German/Italian/Spanish | Dialogue | `EN/FR/DE/IT/ES/*-Regular SDF` |

Buckets (extract output):

| Output | Source |
|--------|--------|
| `unique_chars_Korean.txt` (etc.) | UI / non-dialogue `String*.json` |
| `unique_chars_*_StringDialogue.txt` | `StringDialogue*` files |
| `unique_chars_English.txt` | `English` field from `String*.json` |
| `unique_chars_French.txt` | `French` field from `String*.json` |
| `unique_chars_German.txt` | `German` field from `String*.json` |
| `unique_chars_Italian.txt` | `Italian` field from `String*.json` |
| `unique_chars_Spanish.txt` | `Spanish` field from `String*.json` |

Sanitize removes: TMP style/color/bold/italic/size tags, `<sprite…>`, `{0}` placeholders, `[token]` data tokens, and control/layout code points.

`Assets/Demo/Fonts/Dynamic/` is **not** fed by this extract — use Dynamic only for unpredictable text (e.g. leaderboard names).

### Runtime — warmup (Phase 2b)

Implement `IFontWarmupTarget` in **your** game or Demo code. Call `FontWarmupService.RequestWarmup` on boot and language change.

```csharp
using TmpFontPipeline;

public sealed class MyFontWarmupTarget : IFontWarmupTarget
{
    public IReadOnlyList<TMP_FontAsset> GetFontsForWarmup(string languageId) { /* ... */ }
    public string GetSampleText(string languageId) => FontWarmupSampleText.CjkSample;
    public void PreloadSpriteAssets(string languageId) { }
}
```

`Assets/Demo` is a playground shell only — not a localization or font-asset template.

Demo fonts (`Assets/Demo/Fonts/`): Static SDF per language for localized strings; `Dynamic/` for unpredictable text (e.g. leaderboard names). Licenses: [LICENSE-AND-CREDITS.md](Assets/Demo/Fonts/LICENSE-AND-CREDITS.md).

Write-up:

- [TMP Static font atlas](https://monet5379.github.io/notes/tmp-static-font-atlas/)
- [TMP font warmup](https://monet5379.github.io/notes/tmp-font-warmup/)

## Current design

- **Phase 1:** folder layout, asmdefs, API stubs, sample `String*.json`, Demo script shells.
- **Phase 2a (done):** `StringTextSanitizer` + `StringJsonCharacterExtractor` — language buckets (CJK + EN/FR/DE/IT/ES), Dialogue split. JSON parser selectable: default **JsonUtility**, optional **Newtonsoft** (EditorPrefs, no auto-fallback).
- **Phase 2d (done):** `FontAtlasApplyProfile`, `FontRoleCatalog`, `FontAtlasApplier` — Bucket×Role → Static SDF apply menu; `FontAtlasFileNames` SSOT shared with extractor.
- **Phase 2b (next):** `FontWarmupService` — hidden canvas, one font per frame, supersede, optional sprite preload hook.
- **Phase 2c (next):** Demo scene wiring, `FontRoleCatalog`, README screenshots under `docs/images/`.

## License

[MIT](LICENSE)

---

# TMP Font Pipeline

로컬라이즈 JSON에서 고유 글자를 추출하고, 런타임 TMP 폰트 워밍업으로 Dynamic atlas 성장·첫 draw 스파이크를 플레이 밖으로 옮기는 파이프라인입니다.

**Phase 2a (완료):** Editor 추출 — `String*.json` → `unique_chars_*.txt`.  
**Phase 2d (완료):** `FontAtlasApplyProfile` + Apply 메뉴 — Generated txt → Static SDF atlas.  
**Phase 2b/2c (다음):** Runtime 워밍업·Demo 씬 연결.

## 설치

`unity-tmp-font/Assets/TmpFontPipeline`를 프로젝트 `Assets/`로 통째 복사합니다 (Runtime/Editor asmdef 유지).

이 저장소 Unity 프로젝트에는 이미 `Assets/TmpFontPipeline`와 데모 놀이터 `Assets/Demo`가 있습니다.

Unity에서 프로젝트를 연 뒤 **TMP Essentials**를 한 번 import합니다 (Window → TextMeshPro → Import TMP Essential Resources). 기본 추출은 Unity `JsonUtility`입니다. `com.unity.nuget.newtonsoft-json`은 선택적 Newtonsoft 파서 모드용 Editor 의존성입니다.

## 불변조건

- 문자셋 SSOT = 추출된 Static atlas (`unique_chars_*.txt` → TMP Character Table). 워밍업 sample text가 아닙니다.
- Warmup은 **언제** 처음 그리는지를 담당합니다. 전 glyph 보장은 하지 않습니다.
- 추출 전 sanitize로 TMP 태그·placeholder·토큰을 제거합니다 — JSON 원문을 그대로 아틀라스에 넣지 않습니다.
- UI·Dialogue 버킷을 분리해, 대화 전용 CJK가 UI 폰트 atlas를 불필요하게 키우지 않습니다.
- **Font usage role:** `Ui` = 인게임 공용(Demo Medium), `Dialogue` = 대화(Regular). weight enum이 아니라 어떤 Static asset에 어떤 extract 버킷을 넣을지 구분합니다.
- 유럽어는 언어별로 완전 분리합니다 (`English`, `French`, `German`, `Italian`, `Spanish`) — Ui/Dialogue 모두 각자 `unique_chars_<Language>.txt`/`unique_chars_<Language>_StringDialogue.txt`를 사용합니다.
- 간체 중국어 필드명은 **`SimplifiedChinese`** (정식 철자)입니다.

## 사용

### Editor — 추출 (Phase 2a)

메뉴: **Tmp Font Pipeline → Extract Unique Characters (JSON)**

JSON 파서 (명시 선택, 자동 fallback 없음):

- **Tmp Font Pipeline → JSON Parser → JsonUtility** (기본) — 고정 언어 컬럼; Demo `SampleData` 그대로 사용
- **Tmp Font Pipeline → JSON Parser → Newtonsoft** — 불규칙·시트 export JSON

1. 입력: `Assets/Demo/SampleData` 아래 `String*.json`
2. 출력: `Assets/Demo/Generated` 아래 `unique_chars_*.txt`

### Editor — Static atlas 적용 (Phase 2d)

1. **Font Atlas Apply Profile** 생성 (`Assets → Create → Tmp Font Pipeline → Font Atlas Apply Profile`) — Bucket×Role별 Static `TMP_FontAsset` 매핑. 이 레포에서는 **Tmp Font Pipeline → Font Atlas Apply Profile → Create Demo Assets** 실행.
   - 유럽 분리 이전 프로필이 있으면 이 메뉴를 다시 실행해 엔트리를 동기화합니다.
2. (선택) **Select Active Profile...** — 미설정 시 `Assets/Demo/FontAtlasApplyProfile.asset` 사용.
3. **Extract Unique Characters (JSON)** 후 **Apply Generated Characters to Font Assets** 실행.
   - Apply 시 대상 atlas 크기를 `2048x2048`로 맞춘 뒤 bake합니다.
4. 대상 Font Asset의 Atlas Population Mode = **Static** 확인.

런타임 폰트 조회(Demo·게임): **Font Role Catalog** — `LanguageId` + `Ui` / `Dialogue` → `TMP_FontAsset`.

버킷(추출 출력):

| 출력 | 소스 |
|------|------|
| `unique_chars_Korean.txt` 등 | UI / non-dialogue `String*.json` |
| `unique_chars_*_StringDialogue.txt` | `StringDialogue*` 파일 |
| `unique_chars_English.txt` | `English` 필드 (`String*.json`) |
| `unique_chars_French.txt` | `French` 필드 (`String*.json`) |
| `unique_chars_German.txt` | `German` 필드 (`String*.json`) |
| `unique_chars_Italian.txt` | `Italian` 필드 (`String*.json`) |
| `unique_chars_Spanish.txt` | `Spanish` 필드 (`String*.json`) |

Sanitize 제거 대상: TMP style/color/bold/italic/size 태그, `<sprite…>`, `{0}` placeholder, `[token]`, 제어·레이아웃 코드포인트.

`Assets/Demo/Fonts/Dynamic/`은 이 추출의 대상이 **아닙니다**. 예측 불가 문자열(리더보드 닉네임 등)에만 Dynamic을 씁니다.

### Runtime — 워밍업 (Phase 2b)

`IFontWarmupTarget`을 **게임·Demo 코드**에서 구현합니다. 부팅·언어 변경 시 `FontWarmupService.RequestWarmup`을 호출합니다.

```csharp
using TmpFontPipeline;

public sealed class MyFontWarmupTarget : IFontWarmupTarget
{
    public IReadOnlyList<TMP_FontAsset> GetFontsForWarmup(string languageId) { /* ... */ }
    public string GetSampleText(string languageId) => FontWarmupSampleText.CjkSample;
    public void PreloadSpriteAssets(string languageId) { }
}
```

`Assets/Demo`는 놀이터 껍데기일 뿐입니다. 로컬라이즈·Font Asset 템플릿이 아닙니다.

데모 폰트 (`Assets/Demo/Fonts/`): 로컬라이즈 문자열용 언어별 Static SDF, 예측 불가 문자열(리더보드 닉네임 등)용 `Dynamic/`. 라이선스: [LICENSE-AND-CREDITS.md](Assets/Demo/Fonts/LICENSE-AND-CREDITS.md).

글:

- [TMP Static 폰트 아틀라스](https://monet5379.github.io/notes/tmp-static-font-atlas/)
- [TMP 폰트 워밍업](https://monet5379.github.io/notes/tmp-font-warmup/)

## 현재 설계

- **Phase 1:** 폴더·asmdef·API 스텁·샘플 `String*.json`·Demo 스크립트 껍데기.
- **Phase 2a (완료):** `StringTextSanitizer` + `StringJsonCharacterExtractor` — 언어 버킷(CJK + EN/FR/DE/IT/ES), Dialogue 분리. JSON 파서 선택: 기본 **JsonUtility**, 선택 **Newtonsoft** (EditorPrefs, 자동 fallback 없음).
- **Phase 2d (완료):** `FontAtlasApplyProfile`, `FontRoleCatalog`, `FontAtlasApplier` — Bucket×Role → Static SDF Apply 메뉴; `FontAtlasFileNames` SSOT를 extractor와 공유.
- **Phase 2b (다음):** `FontWarmupService` — 숨김 캔버스, font 1개당 1프레임, supersede, sprite preload 훅.
- **Phase 2c (다음):** Demo 씬 연결, `FontRoleCatalog`, `docs/images/` README 스크린샷.

## 라이선스

[MIT](LICENSE)
