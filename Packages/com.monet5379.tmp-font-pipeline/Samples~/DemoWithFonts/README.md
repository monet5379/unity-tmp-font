# Demo (Noto + Baked Atlas) sample

Package Manager에서 **Import**한 뒤, 한 번 **TMP 폰트 파이프라인 → 샘플 → Noto 데모 경로 연결**을 실행하세요. Apply Active Profile·프로필 Generated 폴더·`FontRoleCatalog` 참조가 Import 경로에 맞춰집니다.

## 포함

- `SampleData/` — `StringUI.json`, `StringDialogue.json` (슬림 **Demo** 샘플과 동일)
- `Generated/` — `SampleData` 기준 추출 txt (유럽어 **MergedEuropean** → `unique_chars_European*.txt` 포함)
- `Font/` — Noto Static SDF + TTF (OFL, `Font/*/TTF/` 및 각 `.meta` 참고)
- `Settings/` — `FontAtlasApplyProfile`, `FontRoleCatalog` (경로 연결 메뉴로 Generated·폰트 참조 갱신)
- `SampleFontWarmupTarget` — `Settings/FontRoleCatalog` 할당 후 Warmup에 사용

씬·UI 데모는 포함하지 않습니다. 슬림 예시만 필요하면 **Demo (JSON + Warmup)** 샘플만 Import하세요.

## Import 후 (권장 순서)

1. **Noto 데모 경로 연결** (위 메뉴)
2. Extract 창에서 JSON 폴더 = `…/DemoWithFonts/SampleData`, 출력 = `…/DemoWithFonts/Generated` (이미 채워져 있으면 재추출은 JSON 변경 시만)
3. Apply 탭에서 Active Profile = `…/Settings/FontAtlasApplyProfile` — charset을 다시 bake할 때만 **Generated 문자 적용**

Extract 유럽어 모드는 **MergedEuropean** 권장 (`unique_chars_European*.txt` ↔ Apply `FontAtlasBucket.European`).
