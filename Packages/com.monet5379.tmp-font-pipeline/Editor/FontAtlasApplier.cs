using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TmpFontPipeline.Editor
{
    public static class FontAtlasApplier
    {
        private const int StepReadGeneratedTxt = 1;
        private const int StepLoadSourceFont = 2;
        private const int StepLinkSourceFont = 3;
        private const int StepSetDynamicMode = 4;
        private const int StepClearAtlas = 5;
        private const int StepAddCharacters = 6;
        private const int StepFlushAtlas = 7;
        private const int StepRestoreStaticMode = 8;
        private const int StepVerifyResult = 9;
        private const int TotalApplySteps = StepVerifyResult;
        private const int DefaultAtlasSize = 2048;

        // FontAtlasApplyProfile의 Enabled 항목에 Generated txt를 Static atlas에 반영합니다.
        public static void Apply(FontAtlasApplyProfile profile)
        {
            if (profile == null)
            {
                Debug.LogError("Font atlas apply profile is not set.");
                return;
            }

            IReadOnlyList<FontAtlasApplyEntry> entries = profile.Entries;
            if (entries == null || entries.Count == 0)
            {
                Debug.LogWarning("Font atlas apply profile has no entries.");
                return;
            }

            WarnDuplicateEntries(entries);

            int successCount = 0;
            int skipCount = 0;
            int failCount = 0;
            var summary = new StringBuilder();
            _ = summary.AppendLine("Tmp Font Pipeline: apply generated characters");

            for (int i = 0; i < entries.Count; i++)
            {
                FontAtlasApplyEntry entry = entries[i];
                if (entry == null || !entry.Enabled)
                {
                    skipCount++;
                    continue;
                }

                ApplyEntryResult result = ApplyEntry(profile, entry, summary);
                switch (result)
                {
                    case ApplyEntryResult.Success:
                        successCount++;
                        break;

                    case ApplyEntryResult.Skipped:
                        skipCount++;
                        break;

                    default:
                        failCount++;
                        break;
                }
            }

            if (successCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            _ = summary.AppendLine($"Done: success={successCount}, skipped={skipCount}, failed={failCount}");
            if (failCount > 0)
            {
                Debug.LogWarning(
                    $"Tmp Font Pipeline: apply finished with {failCount} failed entries. See step logs and summary above.");
            }

            Debug.Log(summary.ToString());
        }

        private static void WarnDuplicateEntries(IReadOnlyList<FontAtlasApplyEntry> entries)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                FontAtlasApplyEntry entry = entries[i];
                if (entry == null || !entry.Enabled)
                {
                    continue;
                }

                string key = $"{entry.Bucket}:{entry.Role}";
                if (!seen.Add(key))
                {
                    Debug.LogWarning($"Duplicate apply entry: {key}");
                }
            }
        }

        private static ApplyEntryResult ApplyEntry(
            FontAtlasApplyProfile profile,
            FontAtlasApplyEntry entry,
            StringBuilder summary)
        {
            string context = $"{entry.Bucket}/{entry.Role} ({entry.FontAsset?.name ?? "null"})";

            if (entry.FontAsset == null)
            {
                _ = summary.AppendLine($"  SKIP {entry.Bucket}/{entry.Role}: FontAsset is null");
                return ApplyEntryResult.Skipped;
            }

            if (entry.FontAsset.atlasPopulationMode != AtlasPopulationMode.Static)
            {
                _ = summary.AppendLine(
                    $"  SKIP {entry.Bucket}/{entry.Role}: {entry.FontAsset.name} is not Static");
                return ApplyEntryResult.Skipped;
            }

            string assetPath = profile.GetResolvedPath(entry);
            string absolutePath = AssetPathToAbsolute(assetPath);
            if (!File.Exists(absolutePath))
            {
                LogApplyStep(context, StepReadGeneratedTxt, "read generated txt", false, $"missing {assetPath}");
                _ = summary.AppendLine($"  FAIL {entry.Bucket}/{entry.Role}: missing {assetPath}");
                return ApplyEntryResult.Failed;
            }

            string chars = File.ReadAllText(absolutePath, Encoding.UTF8);
            LogApplyStep(context, StepReadGeneratedTxt, "read generated txt", true, $"{assetPath} ({chars.Length} chars)");
            LogCharacterDiff(entry, entry.FontAsset, chars);

            TMP_FontAsset fontAsset = entry.FontAsset;
            return TryBakeWithDynamicAndRestoreStatic(entry, assetPath, context, fontAsset, chars, summary);
        }

        private static ApplyEntryResult TryBakeWithDynamicAndRestoreStatic(
            FontAtlasApplyEntry entry,
            string assetPath,
            string context,
            TMP_FontAsset fontAsset,
            string chars,
            StringBuilder summary)
        {
            bool restoredStatic = false;

            try
            {
                if (!TryLoadSourceFont(fontAsset, out Font sourceFont, out string sourceFontPath, out string loadFailDetail))
                {
                    LogApplyStep(context, StepLoadSourceFont, "load source TTF from GUID", false, loadFailDetail);
                    _ = summary.AppendLine($"  FAIL {entry.Bucket}/{entry.Role}: {loadFailDetail}");
                    return ApplyEntryResult.Failed;
                }

                LogApplyStep(context, StepLoadSourceFont, "load source TTF from GUID", true, sourceFontPath);

                if (!TryLinkEditorSourceFont(fontAsset, sourceFont, out string linkFailDetail))
                {
                    LogApplyStep(context, StepLinkSourceFont, "link m_SourceFontFile_EditorRef", false, linkFailDetail);
                    _ = summary.AppendLine($"  FAIL {entry.Bucket}/{entry.Role}: {linkFailDetail}");
                    return ApplyEntryResult.Failed;
                }

                LogApplyStep(context, StepLinkSourceFont, "link m_SourceFontFile_EditorRef", true, sourceFont.name);

                if (!TrySetDynamicMode(fontAsset, out string dynamicFailDetail))
                {
                    LogApplyStep(context, StepSetDynamicMode, "switch to Dynamic for bake", false, dynamicFailDetail);
                    _ = summary.AppendLine($"  FAIL {entry.Bucket}/{entry.Role}: {dynamicFailDetail}");
                    return ApplyEntryResult.Failed;
                }

                LogApplyStep(context, StepSetDynamicMode, "switch to Dynamic for bake", true, fontAsset.sourceFontFile.name);

                if (!TrySetAtlasSize(fontAsset, DefaultAtlasSize, out string atlasSizeFailDetail))
                {
                    LogApplyStep(context, StepSetDynamicMode, "set atlas size", false, atlasSizeFailDetail);
                    _ = summary.AppendLine($"  FAIL {entry.Bucket}/{entry.Role}: {atlasSizeFailDetail}");
                    return ApplyEntryResult.Failed;
                }

                LogApplyStep(context, StepSetDynamicMode, "set atlas size", true, $"{fontAsset.atlasWidth}x{fontAsset.atlasHeight}");

                int characterCountBeforeClear = fontAsset.characterTable.Count;
                fontAsset.ClearFontAssetData(setAtlasSizeToZero: false);
                LogApplyStep(
                    context,
                    StepClearAtlas,
                    "clear font asset data",
                    true,
                    $"characterTable {characterCountBeforeClear} -> {fontAsset.characterTable.Count}");

                bool added = fontAsset.TryAddCharacters(chars, out string missing);
                int characterCountAfterAdd = fontAsset.characterTable.Count;
                bool addStepOk = added && (chars.Length == 0 || characterCountAfterAdd > 0);
                string addDetail = $"TryAddCharacters={added}, characterTable={characterCountAfterAdd}";
                MissingAnalysis missingAnalysis = AnalyzeMissing(sourceFont, missing);
                if (!string.IsNullOrEmpty(missing))
                {
                    addDetail += $", missing={missing}";
                    addDetail += $", {BuildMissingAnalysisDetail(missingAnalysis)}";
                    addDetail +=
                        $", atlas={fontAsset.atlasWidth}x{fontAsset.atlasHeight}, atlasTextures={fontAsset.atlasTextureCount}";
                }

                LogApplyStep(context, StepAddCharacters, "TryAddCharacters", addStepOk, addDetail);
                if (!addStepOk)
                {
                    _ = summary.AppendLine(
                        $"  FAIL {entry.Bucket}/{entry.Role}: {fontAsset.name} <- {assetPath} ({addDetail})");
                    return ApplyEntryResult.Failed;
                }

                if (!string.IsNullOrEmpty(missing))
                {
                    Debug.LogWarning(
                        $"Tmp Font Pipeline: partial missing glyphs for {context}: {missing} " +
                        $"({BuildMissingAnalysisDetail(missingAnalysis)}, " +
                        $"atlas={fontAsset.atlasWidth}x{fontAsset.atlasHeight}, atlasTextures={fontAsset.atlasTextureCount})");

                    if (fontAsset.atlasWidth >= DefaultAtlasSize && missingAnalysis.SourceHasGlyphCount > 0)
                    {
                        string fontAssetPath = AssetDatabase.GetAssetPath(fontAsset);
                        Debug.LogWarning(
                            $"Tmp Font Pipeline: atlas {fontAsset.atlasWidth}x{fontAsset.atlasHeight} is still insufficient for {context}. " +
                            $"sourceHasGlyph={missingAnalysis.SourceHasGlyphCount}, missingUnique={missingAnalysis.MissingUniqueCount}. " +
                            $"Consider splitting bucket/profile for this font. assetPath={fontAssetPath}");
                    }
                }

                // TMP 내부 UpdateFontAssetsInUpdateQueue()는 internal이라 직접 호출할 수 없습니다.
                // TryAddCharacters 과정에서 변경된 atlas texture를 공개 API로 Apply 처리합니다.
                int appliedAtlasTextureCount = 0;
                Texture2D[] atlasTextures = fontAsset.atlasTextures;
                if (atlasTextures != null)
                {
                    for (int t = 0; t < atlasTextures.Length; t++)
                    {
                        Texture2D atlasTexture = atlasTextures[t];
                        if (atlasTexture == null || !atlasTexture.isReadable)
                        {
                            continue;
                        }

                        atlasTexture.Apply(false, false);
                        appliedAtlasTextureCount++;
                    }
                }

                LogApplyStep(
                    context,
                    StepFlushAtlas,
                    "flush atlas texture updates",
                    appliedAtlasTextureCount >= 0,
                    $"applied {appliedAtlasTextureCount} atlas texture(s)");

                if (!TryRestoreStaticMode(fontAsset, out string restoreFailDetail))
                {
                    LogApplyStep(context, StepRestoreStaticMode, "restore Static mode", false, restoreFailDetail);
                    _ = summary.AppendLine($"  FAIL {entry.Bucket}/{entry.Role}: {restoreFailDetail}");
                    return ApplyEntryResult.Failed;
                }

                restoredStatic = true;
                LogApplyStep(context, StepRestoreStaticMode, "restore Static mode", true, "sourceFontFile cleared");

                bool verified = chars.Length == 0 || fontAsset.characterTable.Count > 0;
                LogApplyStep(
                    context,
                    StepVerifyResult,
                    "verify character table",
                    verified,
                    $"{fontAsset.characterTable.Count} characters, {fontAsset.glyphTable.Count} glyphs");

                EditorUtility.SetDirty(fontAsset);

                if (!verified)
                {
                    _ = summary.AppendLine(
                        $"  FAIL {entry.Bucket}/{entry.Role}: {fontAsset.name} <- {assetPath} (empty character table after apply)");
                    return ApplyEntryResult.Failed;
                }

                _ = summary.AppendLine(
                    $"  OK {entry.Bucket}/{entry.Role}: {fontAsset.name} <- {assetPath} ({fontAsset.characterTable.Count} characters in table)");
                return ApplyEntryResult.Success;
            }
            finally
            {
                if (!restoredStatic && fontAsset.atlasPopulationMode != AtlasPopulationMode.Static)
                {
                    if (TryRestoreStaticMode(fontAsset, out string restoreFailDetail))
                    {
                        LogApplyStep(context, StepRestoreStaticMode, "restore Static mode (finally)", true, string.Empty);
                        EditorUtility.SetDirty(fontAsset);
                    }
                    else
                    {
                        LogApplyStep(context, StepRestoreStaticMode, "restore Static mode (finally)", false, restoreFailDetail);
                    }
                }
            }
        }

        private static bool TryLoadSourceFont(
            TMP_FontAsset fontAsset,
            out Font sourceFont,
            out string sourceFontPath,
            out string failDetail)
        {
            sourceFont = null;
            sourceFontPath = string.Empty;
            failDetail = string.Empty;

            var serialized = new SerializedObject(fontAsset);
            string guid = serialized.FindProperty("m_SourceFontFileGUID").stringValue;
            if (string.IsNullOrEmpty(guid))
            {
                guid = fontAsset.creationSettings.sourceFontFileGUID;
            }

            if (string.IsNullOrEmpty(guid))
            {
                failDetail = "no m_SourceFontFileGUID or creationSettings.sourceFontFileGUID";
                return false;
            }

            sourceFontPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(sourceFontPath))
            {
                failDetail = $"GUID not found: {guid}";
                return false;
            }

            sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourceFontPath);
            if (sourceFont == null)
            {
                failDetail = $"Font not loaded at {sourceFontPath}";
                return false;
            }

            return true;
        }

        private static bool TryLinkEditorSourceFont(TMP_FontAsset fontAsset, Font sourceFont, out string failDetail)
        {
            failDetail = string.Empty;

            Type fontAssetType = typeof(TMP_FontAsset);
            PropertyInfo editorRefProperty = fontAssetType.GetProperty(
                "SourceFont_EditorRef",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (editorRefProperty != null)
            {
                editorRefProperty.SetValue(fontAsset, sourceFont);
                return true;
            }

            FieldInfo editorRefField = fontAssetType.GetField(
                "m_SourceFontFile_EditorRef",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (editorRefField != null)
            {
                editorRefField.SetValue(fontAsset, sourceFont);
                return true;
            }

            failDetail = "SourceFont_EditorRef/m_SourceFontFile_EditorRef member not found";
            return false;
        }

        private static bool TrySetDynamicMode(TMP_FontAsset fontAsset, out string failDetail)
        {
            failDetail = string.Empty;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            if (fontAsset.sourceFontFile != null)
            {
                return true;
            }

            if (!TryGetEditorSourceFontRef(fontAsset, out Font editorRefFont))
            {
                failDetail = "editor source font ref is null after link";
                return false;
            }

            FieldInfo sourceFontField = typeof(TMP_FontAsset).GetField(
                "m_SourceFontFile",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (sourceFontField == null)
            {
                failDetail = "m_SourceFontFile field not found";
                return false;
            }

            sourceFontField.SetValue(fontAsset, editorRefFont);

            if (fontAsset.sourceFontFile == null)
            {
                failDetail = "sourceFontFile still null after Dynamic switch";
                return false;
            }

            return true;
        }

        private static bool TryGetEditorSourceFontRef(TMP_FontAsset fontAsset, out Font editorRefFont)
        {
            editorRefFont = null;
            Type fontAssetType = typeof(TMP_FontAsset);

            PropertyInfo editorRefProperty = fontAssetType.GetProperty(
                "SourceFont_EditorRef",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (editorRefProperty != null)
            {
                editorRefFont = editorRefProperty.GetValue(fontAsset) as Font;
                if (editorRefFont != null)
                {
                    return true;
                }
            }

            FieldInfo editorRefField = fontAssetType.GetField(
                "m_SourceFontFile_EditorRef",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (editorRefField != null)
            {
                editorRefFont = editorRefField.GetValue(fontAsset) as Font;
            }

            return editorRefFont != null;
        }

        private static bool TryRestoreStaticMode(TMP_FontAsset fontAsset, out string failDetail)
        {
            failDetail = string.Empty;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

            if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Static)
            {
                failDetail = "atlasPopulationMode is not Static after restore";
                return false;
            }

            return true;
        }

        private static bool TrySetAtlasSize(TMP_FontAsset fontAsset, int size, out string failDetail)
        {
            failDetail = string.Empty;

            fontAsset.creationSettings = new TMPro.FontAssetCreationSettings
            {
                sourceFontFileName = fontAsset.creationSettings.sourceFontFileName,
                sourceFontFileGUID = fontAsset.creationSettings.sourceFontFileGUID,
                faceIndex = fontAsset.creationSettings.faceIndex,
                pointSizeSamplingMode = fontAsset.creationSettings.pointSizeSamplingMode,
                pointSize = fontAsset.creationSettings.pointSize,
                padding = fontAsset.creationSettings.padding,
                paddingMode = fontAsset.creationSettings.paddingMode,
                packingMode = fontAsset.creationSettings.packingMode,
                atlasWidth = size,
                atlasHeight = size,
                characterSetSelectionMode = fontAsset.creationSettings.characterSetSelectionMode,
                characterSequence = fontAsset.creationSettings.characterSequence,
                referencedFontAssetGUID = fontAsset.creationSettings.referencedFontAssetGUID,
                referencedTextAssetGUID = fontAsset.creationSettings.referencedTextAssetGUID,
                fontStyle = fontAsset.creationSettings.fontStyle,
                fontStyleModifier = fontAsset.creationSettings.fontStyleModifier,
                renderMode = fontAsset.creationSettings.renderMode,
                includeFontFeatures = fontAsset.creationSettings.includeFontFeatures
            };

            var serialized = new SerializedObject(fontAsset);
            SerializedProperty atlasWidthProperty = serialized.FindProperty("m_AtlasWidth");
            SerializedProperty atlasHeightProperty = serialized.FindProperty("m_AtlasHeight");
            if (atlasWidthProperty == null || atlasHeightProperty == null)
            {
                failDetail = "m_AtlasWidth or m_AtlasHeight property not found";
                return false;
            }

            atlasWidthProperty.intValue = size;
            atlasHeightProperty.intValue = size;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static void LogApplyStep(
            string context,
            int step,
            string stepName,
            bool success,
            string detail)
        {
            string status = success ? "OK" : "FAIL";
            var message = new StringBuilder();
            _ = message.Append($"Tmp Font Pipeline apply [{context}] step {step}/{TotalApplySteps} {stepName}: {status}");
            if (!string.IsNullOrEmpty(detail))
            {
                _ = message.Append($" — {detail}");
            }

            if (success)
            {
                Debug.Log(message.ToString());
            }
            else
            {
                Debug.LogWarning(message.ToString());
            }
        }

        private static void LogCharacterDiff(FontAtlasApplyEntry entry, TMP_FontAsset fontAsset, string targetChars)
        {
            var targetCodePoints = new HashSet<int>();
            StringTextSanitizer.AddCodePoints(targetCodePoints, targetChars);

            var currentCodePoints = new HashSet<int>();
            string currentChars = TMP_FontAsset.GetCharacters(fontAsset);
            if (!string.IsNullOrEmpty(currentChars))
            {
                StringTextSanitizer.AddCodePoints(currentCodePoints, currentChars);
            }

            int added = 0;
            int removed = 0;
            foreach (int codePoint in targetCodePoints)
            {
                if (!currentCodePoints.Contains(codePoint))
                {
                    added++;
                }
            }

            foreach (int codePoint in currentCodePoints)
            {
                if (!targetCodePoints.Contains(codePoint))
                {
                    removed++;
                }
            }

            if (added > 0 || removed > 0)
            {
                Debug.Log(
                    $"Tmp Font Pipeline diff {entry.Bucket}/{entry.Role}: +{added} -{removed} (full rebuild planned)");
            }
        }

        private static string AssetPathToAbsolute(string assetPath)
        {
            string normalized = assetPath.Replace('\\', '/');
            if (normalized.StartsWith("Assets/", StringComparison.Ordinal))
            {
                string relative = normalized.Substring("Assets/".Length);
                return Path.Combine(Application.dataPath, relative.Replace('/', Path.DirectorySeparatorChar));
            }

            return Path.GetFullPath(assetPath);
        }

        private static string BuildMissingAnalysisDetail(MissingAnalysis analysis)
        {
            return $"missingUnique={analysis.MissingUniqueCount}, sourceHasGlyph={analysis.SourceHasGlyphCount}, sourceNoGlyph={analysis.SourceNoGlyphCount}, nonBmp={analysis.NonBmpCount}";
        }

        private static MissingAnalysis AnalyzeMissing(Font sourceFont, string missing)
        {
            if (string.IsNullOrEmpty(missing))
            {
                return MissingAnalysis.Empty;
            }

            int uniqueCount = 0;
            int sourceHasGlyphCount = 0;
            int sourceNoGlyphCount = 0;
            int nonBmpCount = 0;
            var seen = new HashSet<int>();

            for (int i = 0; i < missing.Length; i++)
            {
                int codePoint = char.ConvertToUtf32(missing, i);
                if (char.IsHighSurrogate(missing[i]))
                {
                    i++;
                }

                if (!seen.Add(codePoint))
                {
                    continue;
                }

                uniqueCount++;
                if (codePoint > 0xFFFF)
                {
                    nonBmpCount++;
                    continue;
                }

                if (sourceFont != null && sourceFont.HasCharacter((char)codePoint))
                {
                    sourceHasGlyphCount++;
                }
                else
                {
                    sourceNoGlyphCount++;
                }
            }

            return new MissingAnalysis(uniqueCount, sourceHasGlyphCount, sourceNoGlyphCount, nonBmpCount);
        }

        private readonly struct MissingAnalysis
        {
            public static readonly MissingAnalysis Empty = new MissingAnalysis(0, 0, 0, 0);

            public MissingAnalysis(int missingUniqueCount, int sourceHasGlyphCount, int sourceNoGlyphCount, int nonBmpCount)
            {
                MissingUniqueCount = missingUniqueCount;
                SourceHasGlyphCount = sourceHasGlyphCount;
                SourceNoGlyphCount = sourceNoGlyphCount;
                NonBmpCount = nonBmpCount;
            }

            public int MissingUniqueCount { get; }
            public int SourceHasGlyphCount { get; }
            public int SourceNoGlyphCount { get; }
            public int NonBmpCount { get; }
        }

        private enum ApplyEntryResult
        {
            Success,
            Skipped,
            Failed,
        }
    }
}