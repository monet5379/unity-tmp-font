# TMP Font Pipeline

로컬라이즈 JSON에서 고유 글자를 추출하고, Static TMP atlas에 Apply한 뒤, 런타임 `FontWarmupService`로 Dynamic atlas 성장·첫 draw 스파이크를 플레이 밖으로 옮기는 UPM 패키지이다.

## 요구 사항

- Unity **6000.3** 이상 (`package.json`)
- **TextMesh Pro:** Unity 6에서는 `com.unity.ugui`에 포함. 소비 프로젝트에서 **Window → TextMeshPro → Import TMP Essential Resources**를 한 번 실행한다.
- 패키지 의존: `com.unity.ugui` · `com.unity.nuget.newtonsoft-json` (Editor의 선택적 Newtonsoft JSON 파서)

## 설치

### Git URL (권장)

1. **Window → Package Manager → + → Install package from git URL**
2. 아래 URL 입력 (태그는 릴리스 버전에 맞게 변경):

```text
https://github.com/monet5379/unity-tmp-font.git?path=/Packages/com.monet5379.tmp-font-pipeline#v0.2.0
```

### 로컬 임베디드

`Packages/com.monet5379.tmp-font-pipeline` 폴더를 Unity 프로젝트 `Packages/` 아래에 두면 Package Manager **In Project**에 표시된다.

## Samples

Package Manager에서 **TMP Font Pipeline** → **Samples**에서 Import한다.

| 샘플 | Import 경로 | 내용 |
| --- | --- | --- |
| **Demo (JSON + Warmup)** | `Assets/Samples/com.monet5379.tmp-font-pipeline/<version>/Demo/` | `SampleData` · `SampleFontWarmupTarget` · asmdef `TmpFontPipeline.Sample.Demo` |
| **Demo (Noto + Baked Atlas)** | `…/DemoWithFonts/` | 위 JSON + Noto SDF/TTF · `Generated` txt · Apply 프로필 · `FontRoleCatalog` · Import 후 **TMP 폰트 파이프라인 → 샘플 → Noto 데모 경로 연결** |

**Demo (JSON + Warmup)** — Extract 창 **JSON 폴더** 예시와 `IFontWarmupTarget` 최소 스크립트만. Static 폰트·Generated·Apply 프로필은 호스트가 준비한다.

**Demo (Noto + Baked Atlas)** — End-to-end 검증용. 씬·UI는 없다. OFL Noto는 `Font/*/TTF/` 참고. Extract 유럽어 **MergedEuropean** ↔ Apply `FontAtlasBucket.European` (`unique_chars_European*.txt`).

개발용 [GitHub 레포](https://github.com/monet5379/unity-tmp-font)는 `Assets/Project` · `Assets/Scenes` · `Assets/Editor` Demo 놀이터만 쓰고 UPM 샘플 **Import는 하지 않는다** (루트 README NOTE).

## Editor — Extract · Apply

메뉴 **TMP 폰트 파이프라인 → 창 열기**.

| 탭 | 역할 |
| --- | --- |
| **추출** | **JSON 폴더** · **출력 폴더** (값은 `EditorPrefs`에 저장). 비어 있으면 창에서 직접 지정. `String*.json` → `unique_chars_*.txt` |
| **적용** | **Apply 프로필** (`FontAtlasApplySettings` Active 경로). 엔트리 **켜** 후 **Generated 문자 적용** (atlas 2048 bake) |
| **도움말** | 단계 요약 (개발 repo용 **데모 에셋 생성** 버튼 없음; Noto 샘플은 **TMP 폰트 파이프라인 → 샘플 → Noto 데모 경로 연결**) |

패키지는 `Assets/Demo/...` 같은 고정 Demo 경로를 넣지 않는다. JSON·Generated·프로필 경로는 **소비 프로젝트**가 정한다.

파서: 기본 `JsonUtility`. 불규칙 JSON은 **추출** 탭에서 **Newtonsoft.Json** 모드.

## Runtime

- **Font Role Catalog** — `Assets → Create → TMP 폰트 파이프라인 → 폰트 역할 카탈로그` (`LanguageId` + `Ui` / `Dialogue` → `TMP_FontAsset`). **Demo (Noto + Baked Atlas)** Import 시 `Settings/FontRoleCatalog` + **Noto 데모 경로 연결** 메뉴.
- **`IFontWarmupTarget`** — 게임 코드에서 구현. `FontWarmupService.RequestWarmup` (Ui → Dialogue, font 1개당 1프레임)
- **`FontWarmupSampleText.GetForLanguage`** — 편의용 짧은 문구 (Demo JSON 키와 맞춘 **기본값**). 게임 문자열이 다르면 `GetSampleText`를 override하되, sample은 해당 role의 **추출 charset** 안에 둔다.

## 레포 문서

불변조건·파이프라인 Architecture는 [GitHub 레포 README](https://github.com/monet5379/unity-tmp-font) 및 [`docs/architecture/Architecture_TmpFontPipeline.md`](https://github.com/monet5379/unity-tmp-font/blob/main/docs/architecture/Architecture_TmpFontPipeline.md)를 참고한다.

## License

[MIT](https://github.com/monet5379/unity-tmp-font/blob/main/LICENSE)
