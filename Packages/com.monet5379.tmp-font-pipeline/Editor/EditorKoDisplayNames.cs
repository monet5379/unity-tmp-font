using TmpFontPipeline;

namespace TmpFontPipeline.Editor
{
    public static class EditorKoDisplayNames
    {
        internal const string LogPrefix = "TMP 폰트 파이프라인";

        internal static string Bucket(FontAtlasBucket bucket) =>
            bucket switch
            {
                FontAtlasBucket.Korean => "한국어",
                FontAtlasBucket.SimplifiedChinese => "간체 중국어",
                FontAtlasBucket.TraditionalChinese => "번체 중국어",
                FontAtlasBucket.Japanese => "일본어",
                FontAtlasBucket.English => "영어",
                FontAtlasBucket.French => "프랑스어",
                FontAtlasBucket.German => "독일어",
                FontAtlasBucket.Italian => "이탈리아어",
                FontAtlasBucket.Spanish => "스페인어",
                FontAtlasBucket.European => "유럽어 (통합)",
                _ => bucket.ToString(),
            };

        internal static string Role(FontUsageRole role) =>
            role switch
            {
                FontUsageRole.Ui => "UI",
                FontUsageRole.Dialogue => "대화",
                _ => role.ToString(),
            };

        internal static string LanguageField(string fieldName) =>
            fieldName switch
            {
                nameof(FontAtlasBucket.Korean) => "한국어",
                nameof(FontAtlasBucket.SimplifiedChinese) => "간체 중국어",
                nameof(FontAtlasBucket.TraditionalChinese) => "번체 중국어",
                nameof(FontAtlasBucket.Japanese) => "일본어",
                nameof(FontAtlasBucket.English) => "영어",
                nameof(FontAtlasBucket.French) => "프랑스어",
                nameof(FontAtlasBucket.German) => "독일어",
                nameof(FontAtlasBucket.Italian) => "이탈리아어",
                nameof(FontAtlasBucket.Spanish) => "스페인어",
                _ => fieldName,
            };

        internal static string EntryLabel(FontAtlasApplyEntry entry) =>
            $"{Bucket(entry.Bucket)}/{Role(entry.Role)}";

        internal static string EntryContext(FontAtlasApplyEntry entry) =>
            $"{Bucket(entry.Bucket)}/{Role(entry.Role)} ({entry.FontAsset?.name ?? "없음"})";

        internal static string ParserMode(JsonParserMode mode) =>
            mode switch
            {
                JsonParserMode.JsonUtility => "Unity JsonUtility",
                JsonParserMode.Newtonsoft => "Newtonsoft.Json",
                _ => mode.ToString(),
            };

        internal static string EuropeanMode(EuropeanExtractMode mode) =>
            mode switch
            {
                EuropeanExtractMode.PerLanguage => "언어별",
                EuropeanExtractMode.MergedEuropean => "유럽어 통합",
                _ => mode.ToString(),
            };
    }
}
