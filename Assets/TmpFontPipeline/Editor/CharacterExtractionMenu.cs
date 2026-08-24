using UnityEditor;

namespace TmpFontPipeline.Editor
{
    public static class CharacterExtractionMenu
    {
        private const string MenuExtract = "Tmp Font Pipeline/Extract Unique Characters (JSON)";
        private const string MenuJsonUtility = "Tmp Font Pipeline/JSON Parser/JsonUtility";
        private const string MenuNewtonsoft = "Tmp Font Pipeline/JSON Parser/Newtonsoft";

        [MenuItem(MenuExtract, priority = 50)]
        public static void ExtractUniqueCharactersFromJson()
        {
            StringJsonCharacterExtractor.Extract();
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
    }
}
