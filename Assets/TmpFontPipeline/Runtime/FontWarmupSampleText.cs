using System;

namespace TmpFontPipeline
{
    // Static atlas에 넣을 짧은 sample. 전 glyph 보장이 아님 — 언어별 atlas에 있는 글자만 사용.
    public static class FontWarmupSampleText
    {
        // Demo StringUI "Confirm"과 동일한 문구 — unique_chars_* 추출 결과에 포함됨.
        public const string Korean = "확인";
        public const string Japanese = "確定";
        public const string SimplifiedChinese = "确认";
        public const string TraditionalChinese = "確認";
        public const string English = "Confirm";
        public const string French = "CONFIRMER";
        public const string German = "BESTÄTIGEN";
        public const string Italian = "CONFERMA";
        public const string Spanish = "CONFIRMAR";

        // Demo StringDialogue "dlg_intro"와 동일 — unique_chars_*_StringDialogue 추출 결과에 포함됨.
        public const string DialogueKorean = "어서 오세요, 모험가.";
        public const string DialogueJapanese = "ようこそ、冒険者。";
        public const string DialogueSimplifiedChinese = "欢迎，冒险者。";
        public const string DialogueTraditionalChinese = "歡迎，冒險者。";
        public const string DialogueEnglish = "Welcome, adventurer.";
        public const string DialogueFrench = "Bienvenue, aventurier.";
        public const string DialogueGerman = "Willkommen, Abenteurer.";
        public const string DialogueItalian = "Benvenuto, avventuriero.";
        public const string DialogueSpanish = "Bienvenido, aventurero.";

        // languageId(Catalog 단축형 또는 필드명)와 역할에 맞는 warmup sample을 반환합니다.
        public static string GetForLanguage(string languageId, FontUsageRole role)
        {
            if (string.IsNullOrEmpty(languageId))
            {
                return role == FontUsageRole.Dialogue ? DialogueEnglish : English;
            }

            if (EqualsId(languageId, "KO", nameof(FontAtlasBucket.Korean)))
            {
                return role == FontUsageRole.Dialogue ? DialogueKorean : Korean;
            }

            if (EqualsId(languageId, "JP", nameof(FontAtlasBucket.Japanese)))
            {
                return role == FontUsageRole.Dialogue ? DialogueJapanese : Japanese;
            }

            if (EqualsId(languageId, "SC", nameof(FontAtlasBucket.SimplifiedChinese)))
            {
                return role == FontUsageRole.Dialogue ? DialogueSimplifiedChinese : SimplifiedChinese;
            }

            if (EqualsId(languageId, "TC", nameof(FontAtlasBucket.TraditionalChinese)))
            {
                return role == FontUsageRole.Dialogue ? DialogueTraditionalChinese : TraditionalChinese;
            }

            if (EqualsId(languageId, "EN", nameof(FontAtlasBucket.English)))
            {
                return role == FontUsageRole.Dialogue ? DialogueEnglish : English;
            }

            if (EqualsId(languageId, "FR", nameof(FontAtlasBucket.French)))
            {
                return role == FontUsageRole.Dialogue ? DialogueFrench : French;
            }

            if (EqualsId(languageId, "DE", nameof(FontAtlasBucket.German)))
            {
                return role == FontUsageRole.Dialogue ? DialogueGerman : German;
            }

            if (EqualsId(languageId, "IT", nameof(FontAtlasBucket.Italian)))
            {
                return role == FontUsageRole.Dialogue ? DialogueItalian : Italian;
            }

            if (EqualsId(languageId, "ES", nameof(FontAtlasBucket.Spanish)))
            {
                return role == FontUsageRole.Dialogue ? DialogueSpanish : Spanish;
            }

            return role == FontUsageRole.Dialogue ? DialogueEnglish : English;
        }

        private static bool EqualsId(string languageId, string shortId, string fieldName)
        {
            return languageId.Equals(shortId, StringComparison.OrdinalIgnoreCase)
                || languageId.Equals(fieldName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
