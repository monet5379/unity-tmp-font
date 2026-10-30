using TmpFontPipeline.Editor;
using UnityEditor;

namespace TmpFontPipeline.Project.Editor
{
    [InitializeOnLoad]
    internal static class ProjectExtractPathDefaults
    {
        private const string DefaultJsonSearchPath = "Assets/Addressables/JSON";
        private const string DefaultOutputPath = "Assets/Project/Font/Generated";
        private const string DefaultApplyProfilePath = "Assets/Project/Font/FontAtlasApplyProfile.asset";

        private const string LegacyJsonSearchPath = "Assets/Demo/SampleData";
        private const string LegacyOutputPath = "Assets/Demo/Generated";
        private const string LegacyOutputPathFontRoot = "Assets/Font/Generated";

        static ProjectExtractPathDefaults()
        {
            SeedIfMissing(ExtractPathSettings.JsonSearchPathPrefsKey, DefaultJsonSearchPath);
            SeedIfMissing(ExtractPathSettings.OutputPathPrefsKey, DefaultOutputPath);
            MigrateLegacyPath(ExtractPathSettings.JsonSearchPathPrefsKey, LegacyJsonSearchPath, DefaultJsonSearchPath);
            MigrateLegacyPath(ExtractPathSettings.OutputPathPrefsKey, LegacyOutputPath, DefaultOutputPath);
            MigrateLegacyPath(ExtractPathSettings.OutputPathPrefsKey, LegacyOutputPathFontRoot, DefaultOutputPath);

            if (!EditorPrefs.HasKey(EuropeanExtractSettings.PrefsKey))
            {
                EuropeanExtractSettings.Mode = EuropeanExtractMode.MergedEuropean;
            }

            if (!EditorPrefs.HasKey(FontAtlasApplySettings.PrefsKey))
            {
                FontAtlasApplySettings.ActiveProfilePath = DefaultApplyProfilePath;
            }
        }

        private static void SeedIfMissing(string prefsKey, string defaultPath)
        {
            if (!EditorPrefs.HasKey(prefsKey))
            {
                EditorPrefs.SetString(prefsKey, defaultPath);
            }
        }

        private static void MigrateLegacyPath(string prefsKey, string legacyPath, string currentDefault)
        {
            if (EditorPrefs.GetString(prefsKey, string.Empty) == legacyPath)
            {
                EditorPrefs.SetString(prefsKey, currentDefault);
            }
        }
    }
}
