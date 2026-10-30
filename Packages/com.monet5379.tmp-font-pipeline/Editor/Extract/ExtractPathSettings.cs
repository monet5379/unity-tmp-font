using UnityEditor;

namespace TmpFontPipeline.Editor
{
    public static class ExtractPathSettings
    {
        public const string JsonSearchPathPrefsKey = "TmpFontPipeline.JsonSearchPath";
        public const string OutputPathPrefsKey = "TmpFontPipeline.OutputPath";

        public static string JsonSearchPath
        {
            get => EditorPrefs.GetString(JsonSearchPathPrefsKey, string.Empty);
            set => EditorPrefs.SetString(JsonSearchPathPrefsKey, value ?? string.Empty);
        }

        public static string OutputPath
        {
            get => EditorPrefs.GetString(OutputPathPrefsKey, string.Empty);
            set => EditorPrefs.SetString(OutputPathPrefsKey, value ?? string.Empty);
        }
    }
}
