using TmpFontPipeline;
using UnityEditor;
using UnityEngine;

namespace TmpFontPipeline.Editor
{
    public static class DemoFontProfileBootstrap
    {
        private const string DemoFolder = "Assets/Demo";
        private const string DemoFontsRoot = "Assets/Demo/Fonts";
        private const string ApplyProfilePath = FontAtlasApplySettings.DemoProfilePath;
        private const string RoleCatalogPath = "Assets/Demo/FontRoleCatalog.asset";

        // Demo Static font 경로 SSOT — Apply Profile·Role Catalog 시드 공통.
        private static readonly DemoFontBinding[] DemoFontBindings =
        {
            new DemoFontBinding(
                FontAtlasBucket.Korean,
                "KO",
                $"{DemoFontsRoot}/KO/NotoSansKR-Medium SDF.asset",
                $"{DemoFontsRoot}/KO/NotoSansKR-Regular SDF.asset"),
            new DemoFontBinding(
                FontAtlasBucket.Japanese,
                "JP",
                $"{DemoFontsRoot}/JP/NotoSansJP-Medium SDF.asset",
                $"{DemoFontsRoot}/JP/NotoSansJP-Regular SDF.asset"),
            new DemoFontBinding(
                FontAtlasBucket.SimplifiedChinese,
                "SC",
                $"{DemoFontsRoot}/SC/NotoSansSC-Medium SDF.asset",
                $"{DemoFontsRoot}/SC/NotoSansSC-Regular SDF.asset"),
            new DemoFontBinding(
                FontAtlasBucket.TraditionalChinese,
                "TC",
                $"{DemoFontsRoot}/TC/NotoSansTC-Medium SDF.asset",
                $"{DemoFontsRoot}/TC/NotoSansTC-Regular SDF.asset"),
            new DemoFontBinding(
                FontAtlasBucket.English,
                "EN",
                $"{DemoFontsRoot}/EN/NotoSans-Medium SDF.asset",
                $"{DemoFontsRoot}/EN/NotoSans-Regular SDF.asset"),
            new DemoFontBinding(
                FontAtlasBucket.French,
                "FR",
                $"{DemoFontsRoot}/FR/NotoSansFR-Medium SDF.asset",
                $"{DemoFontsRoot}/FR/NotoSansFR-Regular SDF.asset"),
            new DemoFontBinding(
                FontAtlasBucket.German,
                "DE",
                $"{DemoFontsRoot}/DE/NotoSansDE-Medium SDF.asset",
                $"{DemoFontsRoot}/DE/NotoSansDE-Regular SDF.asset"),
            new DemoFontBinding(
                FontAtlasBucket.Italian,
                "IT",
                $"{DemoFontsRoot}/IT/NotoSansIT-Medium SDF.asset",
                $"{DemoFontsRoot}/IT/NotoSansIT-Regular SDF.asset"),
            new DemoFontBinding(
                FontAtlasBucket.Spanish,
                "ES",
                $"{DemoFontsRoot}/ES/NotoSansES-Medium SDF.asset",
                $"{DemoFontsRoot}/ES/NotoSansES-Regular SDF.asset"),
        };

        [MenuItem("Tmp Font Pipeline/Font Atlas Apply Profile/Create Demo Assets", priority = 120)]
        public static void CreateDemoAssets()
        {
            EnsureDemoFolder();

            FontAtlasApplyProfile applyProfile = CreateOrLoadApplyProfile();
            _ = CreateOrLoadRoleCatalog();

            FontAtlasApplySettings.ActiveProfilePath = ApplyProfilePath;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = applyProfile;
            EditorGUIUtility.PingObject(applyProfile);
            Debug.Log($"Tmp Font Pipeline: demo assets ready at {ApplyProfilePath} and {RoleCatalogPath}");
        }

        private static void EnsureDemoFolder()
        {
            if (!AssetDatabase.IsValidFolder(DemoFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Demo");
            }
        }

        private static FontAtlasApplyProfile CreateOrLoadApplyProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<FontAtlasApplyProfile>(ApplyProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<FontAtlasApplyProfile>();
                AssetDatabase.CreateAsset(profile, ApplyProfilePath);
            }

            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("_generatedFolder").stringValue = StringJsonCharacterExtractor.DefaultOutputPath;
            serialized.FindProperty("_entries").arraySize = DemoFontBindings.Length * 2;

            int entryIndex = 0;
            for (int i = 0; i < DemoFontBindings.Length; i++)
            {
                DemoFontBinding binding = DemoFontBindings[i];
                SetApplyEntry(serialized, entryIndex++, binding.Bucket, FontUsageRole.Ui, binding.UiFontAssetPath);
                SetApplyEntry(serialized, entryIndex++, binding.Bucket, FontUsageRole.Dialogue, binding.DialogueFontAssetPath);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static void SetApplyEntry(
            SerializedObject profile,
            int index,
            FontAtlasBucket bucket,
            FontUsageRole role,
            string fontAssetPath)
        {
            SerializedProperty entry = profile.FindProperty("_entries").GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("Bucket").enumValueIndex = (int)bucket;
            entry.FindPropertyRelative("Role").enumValueIndex = (int)role;
            entry.FindPropertyRelative("FontAsset").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(fontAssetPath);
            entry.FindPropertyRelative("Enabled").boolValue = true;
        }

        private static FontRoleCatalog CreateOrLoadRoleCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<FontRoleCatalog>(RoleCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FontRoleCatalog>();
                AssetDatabase.CreateAsset(catalog, RoleCatalogPath);
            }

            SerializedObject serialized = new SerializedObject(catalog);
            serialized.FindProperty("_languages").arraySize = DemoFontBindings.Length;

            for (int i = 0; i < DemoFontBindings.Length; i++)
            {
                DemoFontBinding binding = DemoFontBindings[i];
                SetRoleGroup(serialized, i, binding.LanguageId, binding.UiFontAssetPath, binding.DialogueFontAssetPath);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private static void SetRoleGroup(
            SerializedObject catalog,
            int index,
            string languageId,
            string uiFontPath,
            string dialogueFontPath)
        {
            SerializedProperty group = catalog.FindProperty("_languages").GetArrayElementAtIndex(index);
            group.FindPropertyRelative("LanguageId").stringValue = languageId;
            group.FindPropertyRelative("UiFont").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(uiFontPath);
            group.FindPropertyRelative("DialogueFont").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(dialogueFontPath);
        }

        private readonly struct DemoFontBinding
        {
            public DemoFontBinding(
                FontAtlasBucket bucket,
                string languageId,
                string uiFontAssetPath,
                string dialogueFontAssetPath)
            {
                Bucket = bucket;
                LanguageId = languageId;
                UiFontAssetPath = uiFontAssetPath;
                DialogueFontAssetPath = dialogueFontAssetPath;
            }

            public FontAtlasBucket Bucket { get; }
            public string LanguageId { get; }
            public string UiFontAssetPath { get; }
            public string DialogueFontAssetPath { get; }
        }
    }
}
