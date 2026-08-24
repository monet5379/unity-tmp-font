using UnityEditor;

namespace TmpFontPipeline.Editor
{
    public static class JsonParserSettings
    {
        public const string PrefsKey = "TmpFontPipeline.JsonParser";

        public static JsonParserMode Mode
        {
            get
            {
                string raw = EditorPrefs.GetString(PrefsKey, nameof(JsonParserMode.JsonUtility));
                return System.Enum.TryParse(raw, out JsonParserMode mode)
                    ? mode
                    : JsonParserMode.JsonUtility;
            }
            set => EditorPrefs.SetString(PrefsKey, value.ToString());
        }
    }
}
