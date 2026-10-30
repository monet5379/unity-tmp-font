using TmpFontPipeline.Editor;
using UnityEditor;
using UnityEngine;

namespace TmpFontPipeline.Demo.Editor
{
    public static class DemoFontProfileBootstrap
    {
        private const string FontsRoot = "Assets/Addressables/Font";
        private const string GeneratedFolder = "Assets/Project/Font/Generated";
        private const string ApplyProfilePath = "Assets/Project/Font/FontAtlasApplyProfile.asset";
        private const string RoleCatalogPath = "Assets/Addressables/Scriptable/FontRoleCatalog.asset";

        private const string EuropeanUiFontPath = "Assets/Addressables/Font/EN/NotoSans-Medium SDF.asset";
        private const string EuropeanDialogueFontPath = "Assets/Addressables/Font/EN/NotoSans-Regular SDF.asset";

        private static readonly CjkFontBinding[] CjkBindings =
        {
            new(FontAtlasBucket.Korean, "KO", $"{FontsRoot}/KO/NotoSansKR-Medium SDF.asset", $"{FontsRoot}/KO/NotoSansKR-Regular SDF.asset"),
            new(FontAtlasBucket.Japanese, "JP", $"{FontsRoot}/JP/NotoSansJP-Medium SDF.asset", $"{FontsRoot}/JP/NotoSansJP-Regular SDF.asset"),
            new(FontAtlasBucket.SimplifiedChinese, "SC", $"{FontsRoot}/SC/NotoSansSC-Medium SDF.asset", $"{FontsRoot}/SC/NotoSansSC-Regular SDF.asset"),
            new(FontAtlasBucket.TraditionalChinese, "TC", $"{FontsRoot}/TC/NotoSansTC-Medium SDF.asset", $"{FontsRoot}/TC/NotoSansTC-Regular SDF.asset"),
        };

        private static readonly string[] EuropeanLanguageIds = { "EN", "FR", "DE", "IT", "ES" };

        [MenuItem("TMP 폰트 파이프라인/폰트 아틀라스 Apply 프로필/데모 에셋 생성", priority = 120)]
        public static void CreateDemoAssets()
        {
            EnsureAssetFolders();

            FontAtlasApplyProfile applyProfile = CreateOrLoadApplyProfile();
            _ = CreateOrLoadRoleCatalog();

            FontAtlasApplySettings.ActiveProfilePath = ApplyProfilePath;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = applyProfile;
            EditorGUIUtility.PingObject(applyProfile);
            Debug.Log(
                $"데모 에셋 준비 완료 — {ApplyProfilePath}, {RoleCatalogPath} " +
                "(유럽어 EN~ES → FontAtlasBucket.European + 공용 EN SDF, Extract는 MergedEuropean 권장)");
        }

        private static void EnsureAssetFolders()
        {
            EnsureFolder("Assets", "Project");
            EnsureFolder("Assets/Project", "Font");
            EnsureFolder("Assets/Project/Font", "Generated");
            EnsureFolder("Assets/Addressables", "Scriptable");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                _ = AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static FontAtlasApplyProfile CreateOrLoadApplyProfile()
        {
            FontAtlasApplyProfile profile = AssetDatabase.LoadAssetAtPath<FontAtlasApplyProfile>(ApplyProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<FontAtlasApplyProfile>();
                AssetDatabase.CreateAsset(profile, ApplyProfilePath);
            }

            const int entryCount = 10;
            SerializedObject serialized = new(profile);
            serialized.FindProperty("_generatedFolder").stringValue = GeneratedFolder;
            serialized.FindProperty("_entries").arraySize = entryCount;

            int entryIndex = 0;
            for (int i = 0; i < CjkBindings.Length; i++)
            {
                CjkFontBinding binding = CjkBindings[i];
                SetApplyEntry(serialized, entryIndex++, binding.Bucket, FontUsageRole.Ui, binding.UiFontAssetPath);
                SetApplyEntry(serialized, entryIndex++, binding.Bucket, FontUsageRole.Dialogue, binding.DialogueFontAssetPath);
            }

            SetApplyEntry(serialized, entryIndex++, FontAtlasBucket.European, FontUsageRole.Ui, EuropeanUiFontPath);
            SetApplyEntry(serialized, entryIndex, FontAtlasBucket.European, FontUsageRole.Dialogue, EuropeanDialogueFontPath);

            _ = serialized.ApplyModifiedPropertiesWithoutUndo();
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
            FontRoleCatalog catalog = AssetDatabase.LoadAssetAtPath<FontRoleCatalog>(RoleCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FontRoleCatalog>();
                AssetDatabase.CreateAsset(catalog, RoleCatalogPath);
            }

            int languageCount = CjkBindings.Length + EuropeanLanguageIds.Length;
            SerializedObject serialized = new(catalog);
            serialized.FindProperty("_languages").arraySize = languageCount;

            int groupIndex = 0;
            for (int i = 0; i < CjkBindings.Length; i++)
            {
                CjkFontBinding binding = CjkBindings[i];
                SetRoleGroup(serialized, groupIndex++, binding.LanguageId, binding.UiFontAssetPath, binding.DialogueFontAssetPath);
            }

            for (int i = 0; i < EuropeanLanguageIds.Length; i++)
            {
                SetRoleGroup(serialized, groupIndex++, EuropeanLanguageIds[i], EuropeanUiFontPath, EuropeanDialogueFontPath);
            }

            _ = serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private readonly struct CjkFontBinding
        {
            public CjkFontBinding(
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
