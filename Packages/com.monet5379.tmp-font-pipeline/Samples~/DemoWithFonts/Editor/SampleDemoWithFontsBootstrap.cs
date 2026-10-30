using TmpFontPipeline.Editor;
using UnityEditor;
using UnityEngine;

namespace TmpFontPipeline.Sample.DemoWithFonts.Editor
{
    public static class SampleDemoWithFontsBootstrap
    {
        private const string ProfileFileName = "FontAtlasApplyProfile.asset";
        private const string RoleCatalogFileName = "FontRoleCatalog.asset";
        private const string SampleFolderName = "DemoWithFonts";

        private static readonly CjkFontBinding[] CjkBindings =
        {
            new(FontAtlasBucket.Korean, "KO", "KO/NotoSansKR-Medium SDF.asset", "KO/NotoSansKR-Regular SDF.asset"),
            new(FontAtlasBucket.Japanese, "JP", "JP/NotoSansJP-Medium SDF.asset", "JP/NotoSansJP-Regular SDF.asset"),
            new(FontAtlasBucket.SimplifiedChinese, "SC", "SC/NotoSansSC-Medium SDF.asset", "SC/NotoSansSC-Regular SDF.asset"),
            new(FontAtlasBucket.TraditionalChinese, "TC", "TC/NotoSansTC-Medium SDF.asset", "TC/NotoSansTC-Regular SDF.asset"),
        };

        private static readonly string[] EuropeanLanguageIds = { "EN", "FR", "DE", "IT", "ES" };

        private const string EuropeanUiFontRelativePath = "EN/NotoSans-Medium SDF.asset";
        private const string EuropeanDialogueFontRelativePath = "EN/NotoSans-Regular SDF.asset";

        [MenuItem("TMP 폰트 파이프라인/샘플/Noto 데모 경로 연결", priority = 125)]
        public static void LinkImportedSamplePaths()
        {
            if (!TryFindSampleRoot(out string sampleRoot, out string profilePath))
            {
                Debug.LogError(
                    $"{EditorKoDisplayNames.LogPrefix}: Import된 **Demo (Noto + Baked Atlas)** 샘플을 찾지 못했습니다. " +
                    "Package Manager에서 해당 샘플을 Import한 뒤 다시 실행하세요.");
                return;
            }

            string generatedFolder = $"{sampleRoot}/Generated";
            string fontsRoot = $"{sampleRoot}/Font";
            string roleCatalogPath = $"{sampleRoot}/Settings/{RoleCatalogFileName}";

            EnsureFolder(sampleRoot, "Settings");

            FontAtlasApplyProfile applyProfile = CreateOrLoadApplyProfile(profilePath, generatedFolder, fontsRoot);
            _ = CreateOrLoadRoleCatalog(roleCatalogPath, fontsRoot);

            FontAtlasApplySettings.ActiveProfilePath = profilePath;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = applyProfile;
            EditorGUIUtility.PingObject(applyProfile);
            Debug.Log(
                $"{EditorKoDisplayNames.LogPrefix}: Noto 데모 샘플 경로 연결 — Profile={profilePath}, Catalog={roleCatalogPath}, Generated={generatedFolder}");
        }

        private static bool TryFindSampleRoot(out string sampleRoot, out string profilePath)
        {
            sampleRoot = null;
            profilePath = null;

            string[] profileGuids = AssetDatabase.FindAssets($"t:{nameof(FontAtlasApplyProfile)}");
            for (int i = 0; i < profileGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(profileGuids[i]);
                if (TryParseSampleRoot(path, out sampleRoot))
                {
                    profilePath = $"{sampleRoot}/Settings/{ProfileFileName}";
                    return true;
                }
            }

            string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            for (int i = 0; i < fontGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(fontGuids[i]);
                if (!TryParseSampleRoot(path, out sampleRoot))
                {
                    continue;
                }

                profilePath = $"{sampleRoot}/Settings/{ProfileFileName}";
                return true;
            }

            return false;
        }

        private static bool TryParseSampleRoot(string assetPath, out string sampleRoot)
        {
            sampleRoot = null;
            string folderToken = $"/{SampleFolderName}/";
            int markerIndex = assetPath.IndexOf(folderToken, System.StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return false;
            }

            sampleRoot = assetPath.Substring(0, markerIndex + $"/{SampleFolderName}".Length);
            return AssetDatabase.IsValidFolder(sampleRoot);
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                _ = AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static FontAtlasApplyProfile CreateOrLoadApplyProfile(
            string profilePath,
            string generatedFolder,
            string fontsRoot)
        {
            FontAtlasApplyProfile profile = AssetDatabase.LoadAssetAtPath<FontAtlasApplyProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<FontAtlasApplyProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            const int entryCount = 10;
            SerializedObject serialized = new(profile);
            serialized.FindProperty("_generatedFolder").stringValue = generatedFolder;
            serialized.FindProperty("_entries").arraySize = entryCount;

            int entryIndex = 0;
            for (int i = 0; i < CjkBindings.Length; i++)
            {
                CjkFontBinding binding = CjkBindings[i];
                SetApplyEntry(
                    serialized,
                    entryIndex++,
                    binding.Bucket,
                    FontUsageRole.Ui,
                    $"{fontsRoot}/{binding.UiFontRelativePath}");
                SetApplyEntry(
                    serialized,
                    entryIndex++,
                    binding.Bucket,
                    FontUsageRole.Dialogue,
                    $"{fontsRoot}/{binding.DialogueFontRelativePath}");
            }

            SetApplyEntry(
                serialized,
                entryIndex++,
                FontAtlasBucket.European,
                FontUsageRole.Ui,
                $"{fontsRoot}/{EuropeanUiFontRelativePath}");
            SetApplyEntry(
                serialized,
                entryIndex,
                FontAtlasBucket.European,
                FontUsageRole.Dialogue,
                $"{fontsRoot}/{EuropeanDialogueFontRelativePath}");

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

        private static FontRoleCatalog CreateOrLoadRoleCatalog(string roleCatalogPath, string fontsRoot)
        {
            FontRoleCatalog catalog = AssetDatabase.LoadAssetAtPath<FontRoleCatalog>(roleCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FontRoleCatalog>();
                AssetDatabase.CreateAsset(catalog, roleCatalogPath);
            }

            int languageCount = CjkBindings.Length + EuropeanLanguageIds.Length;
            SerializedObject serialized = new(catalog);
            serialized.FindProperty("_languages").arraySize = languageCount;

            int groupIndex = 0;
            for (int i = 0; i < CjkBindings.Length; i++)
            {
                CjkFontBinding binding = CjkBindings[i];
                SetRoleGroup(
                    serialized,
                    groupIndex++,
                    binding.LanguageId,
                    $"{fontsRoot}/{binding.UiFontRelativePath}",
                    $"{fontsRoot}/{binding.DialogueFontRelativePath}");
            }

            for (int i = 0; i < EuropeanLanguageIds.Length; i++)
            {
                SetRoleGroup(
                    serialized,
                    groupIndex++,
                    EuropeanLanguageIds[i],
                    $"{fontsRoot}/{EuropeanUiFontRelativePath}",
                    $"{fontsRoot}/{EuropeanDialogueFontRelativePath}");
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
                string uiFontRelativePath,
                string dialogueFontRelativePath)
            {
                Bucket = bucket;
                LanguageId = languageId;
                UiFontRelativePath = uiFontRelativePath;
                DialogueFontRelativePath = dialogueFontRelativePath;
            }

            public FontAtlasBucket Bucket { get; }
            public string LanguageId { get; }
            public string UiFontRelativePath { get; }
            public string DialogueFontRelativePath { get; }
        }
    }
}
