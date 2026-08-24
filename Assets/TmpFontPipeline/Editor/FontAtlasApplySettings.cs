using TmpFontPipeline;
using UnityEditor;

namespace TmpFontPipeline.Editor
{
    public static class FontAtlasApplySettings
    {
        public const string PrefsKey = "TmpFontPipeline.FontAtlasApplyProfile";
        public const string DemoProfilePath = "Assets/Demo/FontAtlasApplyProfile.asset";

        public static string ActiveProfilePath
        {
            get => EditorPrefs.GetString(PrefsKey, string.Empty);
            set => EditorPrefs.SetString(PrefsKey, value ?? string.Empty);
        }

        public static FontAtlasApplyProfile LoadActiveProfile()
        {
            string path = ActiveProfilePath;
            if (string.IsNullOrEmpty(path))
            {
                path = DemoProfilePath;
            }

            if (!string.IsNullOrEmpty(path))
            {
                var profile = AssetDatabase.LoadAssetAtPath<FontAtlasApplyProfile>(path);
                if (profile != null)
                {
                    return profile;
                }
            }

            return null;
        }
    }
}
