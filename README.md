# TMP Font Pipeline

**English** | [한국어](README.ko.md)

Extract unique characters from localization JSON, then warm up runtime TMP fonts so Dynamic atlas growth and first-draw spikes happen outside gameplay.

**Includes**

- **Character extract** — `String*.json` → `unique_chars_*.txt` (language buckets, Ui/Dialogue split, sanitize)
- **Static atlas Apply** — Generated txt → Editor Window + `FontAtlasApplyProfile` for TMP Static SDF bake
- **Runtime warmup** — `FontWarmupService` + `IFontWarmupTarget` (per-role sample, one font per frame)
- **Demo** (optional) — `Assets/Demo` playground: `SampleScene`, language switch, string key picker

Copy only **`Assets/TmpFontPipeline`** into a game. `Assets/Demo` is reference-only.

![SampleScene — EN (Ui + Dialogue, language buttons, key picker)](docs/images/scene-en.png)

## Install

Copy `unity-tmp-font/Assets/TmpFontPipeline` into your project `Assets/` (keep Runtime/Editor asmdefs).

This repo’s Unity project already has `Assets/TmpFontPipeline` and a demo playground at `Assets/Demo`. **Demo is not part of the install.**

After opening the project in Unity, import **TMP Essentials** once (Window → TextMeshPro → Import TMP Essential Resources). Default extract uses Unity `JsonUtility`. `com.unity.nuget.newtonsoft-json` is an optional Editor dependency for Newtonsoft parser mode.

## Invariants

- Charset SSOT = extracted Static atlas (`unique_chars_*.txt` → TMP Character Table), not warmup sample text.
- Warmup controls **when** glyphs first draw. It does not guarantee every glyph.
- Warmup **sample text is per role** — Ui uses UI-bucket strings (e.g. `Confirm`), Dialogue uses dialogue-bucket strings (e.g. `dlg_intro`). Pick samples from the **same extract bucket** as the target Static atlas.
- Sanitize before extract removes TMP tags, placeholders, and tokens — do not feed raw JSON into the atlas. **Runtime display** (Demo `DemoStringTable`) reads JSON fields as-is — keep on-screen sentences complete in JSON (no runtime `[token]` substitution).
- Split UI and Dialogue buckets so dialogue-only CJK does not inflate the UI font atlas.
- **Font usage role:** `Ui` = in-game shared (Demo Medium), `Dialogue` = dialogue (Regular). Not a weight enum — it maps which extract bucket goes into which Static asset.
- European languages are fully split (`English`, `French`, `German`, `Italian`, `Spanish`) — Ui and Dialogue each use their own `unique_chars_<Language>.txt` / `unique_chars_<Language>_StringDialogue.txt`.
- Simplified Chinese field name is **`SimplifiedChinese`** (correct spelling).

Sanitize removes: TMP style/color/bold/italic/size tags, `<sprite…>`, `{0}` placeholders, `[token]`, control/layout code points.

`Assets/Demo/Fonts/Dynamic/` is **not** an extract target. Use Dynamic only for unpredictable strings (e.g. leaderboard nicknames).

## Out of scope

- A full shipping localization / Font Asset template (`Assets/Demo` is a playground)
- Treating Dynamic atlas as the Static extract pipeline
- Runtime `[token]` substitution or a string-table system (display copy lives complete in JSON)

## Editor — character extract

![Extract tab — JSON folder, bucket preview, Extract Unique Characters](docs/images/editor-window-extract.png)

**Tmp Font Pipeline → Open Window** → **Extract** tab.

1. Set JSON/output folders and parser (`JsonUtility` default; use `Newtonsoft` for irregular JSON).
2. **Extract Unique Characters**. After editing `String*.json`, **run Extract again** so it matches `Assets/Demo/Generated/`.
   - Default input: `Assets/Demo/SampleData` (`String*.json`)
   - Default output: `Assets/Demo/Generated` (`unique_chars_*.txt`)

| Output | Source |
|------|------|
| `unique_chars_Korean.txt`, etc. | UI / non-dialogue `String*.json` |
| `unique_chars_*_StringDialogue.txt` | `StringDialogue*` files |
| `unique_chars_English.txt` | `English` field (`String*.json`) |
| `unique_chars_French.txt` | `French` field (`String*.json`) |
| `unique_chars_German.txt` | `German` field (`String*.json`) |
| `unique_chars_Italian.txt` | `Italian` field (`String*.json`) |
| `unique_chars_Spanish.txt` | `Spanish` field (`String*.json`) |

## Editor — Static atlas Apply

![Apply tab — Font Atlas Apply Profile, Enabled entries](docs/images/editor-window-apply.png)

Same window → **Apply** tab (Demo seed from **Help**).

1. Assign a **Font Atlas Apply Profile** (or **Use Demo Profile**), toggle entry **Enabled** (or **Enable All** / **Disable All**).
2. **Apply Generated Characters**.
   - **Ping** highlights the profile asset in the Project window.
   - Apply sets target atlas size to `2048x2048`, then bakes.
3. Confirm the target Font Asset Atlas Population Mode = **Static**.

**Help** tab — Demo seed:

![Help tab — Create Demo Assets / Resync Demo Assets](docs/images/editor-window-help.png)

| Demo profile | Button | Action |
|-------------|------|------|
| None | **Create Demo Assets** | Create and seed `FontAtlasApplyProfile` + `FontRoleCatalog` under `Assets/Demo`, set Active Profile |
| Present | **Resync Demo Assets** | Load the same assets, then **overwrite** with demo bindings (all `Enabled` true; clears manual edits) |

Same as menu **Tmp Font Pipeline → Font Atlas Apply Profile → Create Demo Assets**. Re-run (reseed) if the profile predates the European language split.

Demo mapping (after Create Demo Assets):

| Bucket | Role | Demo Static asset |
|--------|------|-------------------|
| Korean (etc.) | Ui | `*/NotoSans*-Medium SDF` |
| Korean (etc.) | Dialogue | `*/NotoSans*-Regular SDF` |
| English/French/German/Italian/Spanish | Ui | `EN/FR/DE/IT/ES/*-Medium SDF` |
| English/French/German/Italian/Spanish | Dialogue | `EN/FR/DE/IT/ES/*-Regular SDF` |

## Runtime — font lookup

**Font Role Catalog** (`Assets → Create → Tmp Font Pipeline → Font Role Catalog`) — `LanguageId` + `Ui` / `Dialogue` → `TMP_FontAsset`.

The Demo catalog is seeded under `Assets/Demo` by **Create Demo Assets**.

## Runtime — font warmup

Implement `IFontWarmupTarget` in **game or Demo code**. Call `FontWarmupService.RequestWarmup` on boot and language change.

Ui and Dialogue Static atlases use **different extract buckets** (`unique_chars_*.txt` vs `unique_chars_*_StringDialogue.txt`). Pass **per-role** samples so hidden warmup text does not reference glyphs missing from the atlas.

| Role | Demo sample source | Example (KO) |
|------|------------------|---------|
| `Ui` | `StringUI` → `Confirm` | `확인` |
| `Dialogue` | `StringDialogue` → `dlg_intro` | `어서 오세요, 모험가.` |

`FontWarmupSampleText.GetForLanguage(languageId, role)` matches Demo JSON keys. If your game strings differ, override `GetSampleText`, but keep each sample inside that role’s extracted charset.

```csharp
using TmpFontPipeline;

public sealed class MyFontWarmupTarget : IFontWarmupTarget
{
    public IReadOnlyList<TMP_FontAsset> GetFontsForWarmup(string languageId) { /* ... */ }
    public TMP_FontAsset GetFontForWarmup(string languageId, FontUsageRole role) { /* ... */ }
    public string GetSampleText(string languageId, FontUsageRole role) =>
        FontWarmupSampleText.GetForLanguage(languageId, role);
    public void PreloadSpriteAssets(string languageId) { }
}
```

`FontWarmupService` runs **Ui → Dialogue**, one font per frame. On supersede it calls `onSuperseded`. Input blocking during warmup is the caller’s job (see Demo).

## Demo (optional)

Playground only — not a shipping localization or Font Asset template.

1. **Tmp Font Pipeline → Open Window → Help → Create Demo Assets** (once).
2. **Extract**, then **Apply** — confirm Static SDF matches `Assets/Demo/Generated/`.
3. Open **`Assets/Demo/Scenes/SampleScene.unity`** and Play.

| Object | Script | Role |
|----------|----------|------|
| `FontWarmup` | `DemoFontWarmupBootstrap` | Ensures `IFontWarmupTarget` + `FontWarmupService` |
| `FontWarmup` | `DemoLanguageSwitcher` | Boot `EN` → input block → `RequestWarmup` → refresh labels → unblock |
| `UiLabel` / `DialogueLabel` | `DemoLocalizedLabel` | role + string key → font·text after warmup |

Nine flag buttons: `EN`, `KO`, `JP`, `SC`, `TC`, `FR`, `DE`, `IT`, `ES`. Labels use `Confirm` (Ui / Medium) and `dlg_intro` (Dialogue / Regular). Dialogue sample keys: `dlg_intro` (e.g. KO `어서 오세요, 모험가.`), `dlg_boss` (e.g. KO `드래곤이 모험가를 부르고 있다!`) — complete sentences, no data tokens.

**String key picker** (`DemoStringKeyPicker`, auto-added when `_enableKeyPicker` is on): **UI ◀ ▶** / **Dlg ◀ ▶** cycle extracted keys only. Key lists are `DemoStringKeyPicker._uiKeys` / `_dialogueKeys`. Key changes use `SetLabelKey` + `RefreshAllLabels` only (no warmup).

Use the flag buttons to switch languages and check CJK examples.

![SampleScene — KO](docs/images/scene-kr.png)

![SampleScene — JP](docs/images/scene-jp.png)

Demo font licenses: [LICENSE-AND-CREDITS.md](Assets/Demo/Fonts/LICENSE-AND-CREDITS.md).

## Related

- [TMP Static font atlas](https://monet5379.github.io/notes/tmp-static-font-atlas/) — design background (Dragon context; Korean)
- [TMP font warmup](https://monet5379.github.io/notes/tmp-font-warmup/) — warmup vs Static SSOT (Korean)
- [TMP Font Pipeline (site)](https://monet5379.github.io/projects/tmp-font-pipeline/) — portfolio overview (Korean)

## License

[MIT](LICENSE)

English prose may be AI-assisted. If wording conflicts, prefer the [Korean README](README.ko.md) or the code.
