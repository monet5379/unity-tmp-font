using TmpFontPipeline;
using UnityEditor;
using UnityEngine;

namespace TmpFontPipeline.Editor
{
    public static class CharacterExtractionMenu
    {
        private const string MenuExtract = "Tmp Font Pipeline/Extract Unique Characters (JSON)";
        private const string MenuApply = "Tmp Font Pipeline/Apply Generated Characters to Font Assets";
        private const string MenuSelectProfile = "Tmp Font Pipeline/Font Atlas Apply Profile/Select Active Profile...";
        private const string MenuUseDemoProfile = "Tmp Font Pipeline/Font Atlas Apply Profile/Use Demo Profile";
        private const string MenuJsonUtility = "Tmp Font Pipeline/JSON Parser/JsonUtility";
        private const string MenuNewtonsoft = "Tmp Font Pipeline/JSON Parser/Newtonsoft";

        [MenuItem(MenuExtract, priority = 50)]
        public static void ExtractUniqueCharactersFromJson()
        {
            StringJsonCharacterExtractor.Extract();
        }

        [MenuItem(MenuApply, priority = 51)]
        public static void ApplyGeneratedCharactersToFontAssets()
        {
            FontAtlasApplyProfile profile = FontAtlasApplySettings.LoadActiveProfile();
            if (profile == null)
            {
                Debug.LogError(
                    "Font atlas apply profile not found. Use Tmp Font Pipeline/Font Atlas Apply Profile/Create Demo Assets " +
                    "or Select Active Profile.");
                return;
            }

            FontAtlasApplier.Apply(profile);
        }

        [MenuItem(MenuSelectProfile, priority = 110)]
        public static void SelectActiveApplyProfile()
        {
            string absolutePath = EditorUtility.OpenFilePanel(
                "Select Font Atlas Apply Profile",
                Application.dataPath,
                "asset");
            if (string.IsNullOrEmpty(absolutePath))
            {
                return;
            }

            string assetPath = ToAssetPath(absolutePath);
            if (AssetDatabase.LoadAssetAtPath<FontAtlasApplyProfile>(assetPath) == null)
            {
                Debug.LogError($"Not a FontAtlasApplyProfile: {assetPath}");
                return;
            }

            FontAtlasApplySettings.ActiveProfilePath = assetPath;
            Debug.Log($"Active font atlas apply profile: {assetPath}");
        }

        [MenuItem(MenuUseDemoProfile, priority = 111)]
        public static void UseDemoApplyProfile()
        {
            FontAtlasApplySettings.ActiveProfilePath = FontAtlasApplySettings.DemoProfilePath;
            Debug.Log($"Active font atlas apply profile: {FontAtlasApplySettings.DemoProfilePath}");
        }

        [MenuItem(MenuJsonUtility, priority = 100)]
        public static void SelectJsonUtilityParser()
        {
            JsonParserSettings.Mode = JsonParserMode.JsonUtility;
        }

        [MenuItem(MenuJsonUtility, true)]
        public static bool ValidateSelectJsonUtilityParser()
        {
            Menu.SetChecked(MenuJsonUtility, JsonParserSettings.Mode == JsonParserMode.JsonUtility);
            return true;
        }

        [MenuItem(MenuNewtonsoft, priority = 101)]
        public static void SelectNewtonsoftParser()
        {
            JsonParserSettings.Mode = JsonParserMode.Newtonsoft;
        }

        [MenuItem(MenuNewtonsoft, true)]
        public static bool ValidateSelectNewtonsoftParser()
        {
            Menu.SetChecked(MenuNewtonsoft, JsonParserSettings.Mode == JsonParserMode.Newtonsoft);
            return true;
        }

        private static string ToAssetPath(string absolutePath)
        {
            string normalized = absolutePath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (normalized.StartsWith(dataPath, System.StringComparison.OrdinalIgnoreCase))
            {
                return "Assets" + normalized.Substring(dataPath.Length);
            }

            return normalized;
        }
    }
}
