using UnityEditor;

namespace TmpFontPipeline.Editor
{
    public static class EuropeanExtractSettings
    {
        public const string PrefsKey = "TmpFontPipeline.EuropeanExtract";

        public static EuropeanExtractMode Mode
        {
            get
            {
                string raw = EditorPrefs.GetString(PrefsKey, nameof(EuropeanExtractMode.PerLanguage));
                return System.Enum.TryParse(raw, out EuropeanExtractMode mode)
                    ? mode
                    : EuropeanExtractMode.PerLanguage;
            }
            set => EditorPrefs.SetString(PrefsKey, value.ToString());
        }
    }
}
