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

        private static readonly BindingFlags TmpInstanceNonPublic =
            BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly PropertyInfo SourceFontEditorRefProperty =
            typeof(TMP_FontAsset).GetProperty("SourceFont_EditorRef", TmpInstanceNonPublic);

        private static readonly FieldInfo SourceFontEditorRefField =
            typeof(TMP_FontAsset).GetField("m_SourceFontFile_EditorRef", TmpInstanceNonPublic);

        private static readonly FieldInfo SourceFontFileField =
            typeof(TMP_FontAsset).GetField("m_SourceFontFile", TmpInstanceNonPublic);

        public static bool Apply(FontAtlasApplyProfile profile)
        {
            if (profile == null)
            {
                Debug.LogError($"{EditorKoDisplayNames.LogPrefix}: Apply 프로필이 지정되지 않았습니다.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(profile.GeneratedFolder))
            {
                Debug.LogError(
                    $"{EditorKoDisplayNames.LogPrefix}: Apply 프로필의 Generated 폴더가 비어 있습니다. 프로젝트 폴더를 지정하세요 (예: Assets/.../Generated).");
                return false;
            }

            IReadOnlyList<FontAtlasApplyEntry> entries = profile.Entries;
            if (entries == null || entries.Count == 0)
            {
                Debug.LogWarning($"{EditorKoDisplayNames.LogPrefix}: Apply 프로필에 엔트리가 없습니다.");
                return false;
            }

            FontAtlasApplyDuplicateWarnings.LogEnabledDuplicateFontAssets(entries);

            int successCount = 0;
            int skipCount = 0;
            int failCount = 0;
            var summary = new StringBuilder();
            _ = summary.AppendLine($"{EditorKoDisplayNames.LogPrefix}: Generated 문자 적용");

            for (int i = 0; i < entries.Count; i++)
            {
                FontAtlasApplyEntry entry = entries[i];
                if (entry == null || !entry.Enabled)
                {
                    skipCount++;
                    continue;
                }

                switch (ApplyEntry(profile, entry, summary))
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

            _ = summary.AppendLine($"완료: 성공={successCount}, 건너뜀={skipCount}, 실패={failCount}");
            if (failCount > 0)
            {
                Debug.LogWarning(
                    $"{EditorKoDisplayNames.LogPrefix}: {failCount}개 엔트리 적용 실패. 위 단계 로그와 요약을 확인하세요.");
            }

            Debug.Log(summary.ToString());
            return failCount == 0;
        }

        private static ApplyEntryResult ApplyEntry(
            FontAtlasApplyProfile profile,
            FontAtlasApplyEntry entry,
            StringBuilder summary)
        {
            string context = EditorKoDisplayNames.EntryContext(entry);

            if (entry.FontAsset == null)
            {
                _ = summary.AppendLine($"  건너뜀 {EditorKoDisplayNames.EntryLabel(entry)}: FontAsset이 null입니다");
                return ApplyEntryResult.Skipped;
            }

            if (entry.FontAsset.atlasPopulationMode != AtlasPopulationMode.Static)
            {
                _ = summary.AppendLine(
                    $"  건너뜀 {EditorKoDisplayNames.EntryLabel(entry)}: {entry.FontAsset.name}이(가) Static이 아닙니다");
                return ApplyEntryResult.Skipped;
            }

            string assetPath = profile.GetResolvedPath(entry);
            if (string.IsNullOrEmpty(assetPath))
            {
                _ = summary.AppendLine($"  실패 {EditorKoDisplayNames.EntryLabel(entry)}: 프로필에 Generated 폴더가 없습니다");
                return ApplyEntryResult.Failed;
            }

            string absolutePath = AssetPathToAbsolute(assetPath);
            if (!File.Exists(absolutePath))
            {
                LogApplyStep(context, StepReadGeneratedTxt, "Generated txt 읽기", false, $"없음 {assetPath}");
                _ = summary.AppendLine($"  실패 {EditorKoDisplayNames.EntryLabel(entry)}: 없음 {assetPath}");
                return ApplyEntryResult.Failed;
            }

            string chars = FontAtlasMandatoryCharacters.MergeIntoCharacterString(
                File.ReadAllText(absolutePath, Encoding.UTF8));
            LogApplyStep(context, StepReadGeneratedTxt, "Generated txt 읽기", true, $"{assetPath} ({chars.Length}글자)");
            LogCharacterDiff(entry, entry.FontAsset, chars);

            return TryBakeWithDynamicAndRestoreStatic(entry, assetPath, context, entry.FontAsset, chars, summary);
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
                    return FailEntryStep(entry, summary, context, StepLoadSourceFont, "GUID로 소스 TTF 로드", loadFailDetail);
                }

                LogApplyStep(context, StepLoadSourceFont, "GUID로 소스 TTF 로드", true, sourceFontPath);

                if (!TryLinkEditorSourceFont(fontAsset, sourceFont, out string linkFailDetail))
                {
                    return FailEntryStep(
                        entry,
                        summary,
                        context,
                        StepLinkSourceFont,
                        "m_SourceFontFile_EditorRef 연결",
                        linkFailDetail);
                }

                LogApplyStep(context, StepLinkSourceFont, "m_SourceFontFile_EditorRef 연결", true, sourceFont.name);

                if (!TrySetDynamicMode(fontAsset, out string dynamicFailDetail))
                {
                    return FailEntryStep(entry, summary, context, StepSetDynamicMode, "bake용 Dynamic 전환", dynamicFailDetail);
                }

                LogApplyStep(context, StepSetDynamicMode, "bake용 Dynamic 전환", true, fontAsset.sourceFontFile.name);

                if (!TrySetAtlasSize(fontAsset, DefaultAtlasSize, out string atlasSizeFailDetail))
                {
                    return FailEntryStep(entry, summary, context, StepSetDynamicMode, "아틀라스 크기 설정", atlasSizeFailDetail);
                }

                LogApplyStep(
                    context,
                    StepSetDynamicMode,
                    "아틀라스 크기 설정",
                    true,
                    $"{fontAsset.atlasWidth}x{fontAsset.atlasHeight}");

                int characterCountBeforeClear = fontAsset.characterTable.Count;
                fontAsset.ClearFontAssetData(setAtlasSizeToZero: false);
                LogApplyStep(
                    context,
                    StepClearAtlas,
                    "폰트 에셋 데이터 초기화",
                    true,
                    $"characterTable {characterCountBeforeClear} → {fontAsset.characterTable.Count}");

                bool added = fontAsset.TryAddCharacters(chars, out string missing);
                int characterCountAfterAdd = fontAsset.characterTable.Count;
                bool addStepOk = added && (chars.Length == 0 || characterCountAfterAdd > 0);
                string addDetail = BuildAddCharactersDetail(fontAsset, added, characterCountAfterAdd, missing, sourceFont);
                LogApplyStep(context, StepAddCharacters, "문자 추가 (TryAddCharacters)", addStepOk, addDetail);
                if (!addStepOk)
                {
                    _ = summary.AppendLine(
                        $"  실패 {EditorKoDisplayNames.EntryLabel(entry)}: {fontAsset.name} ← {assetPath} ({addDetail})");
                    return ApplyEntryResult.Failed;
                }

                LogPartialMissingWarning(context, fontAsset, missing, sourceFont);

                int appliedAtlasTextureCount = FlushReadableAtlasTextures(fontAsset);
                LogApplyStep(
                    context,
                    StepFlushAtlas,
                    "아틀라스 텍스처 갱신 반영",
                    true,
                    $"적용 {appliedAtlasTextureCount}개 아틀라스 텍스처");

                if (!TryRestoreStaticMode(fontAsset, out string restoreFailDetail))
                {
                    return FailEntryStep(entry, summary, context, StepRestoreStaticMode, "Static 모드 복원", restoreFailDetail);
                }

                restoredStatic = true;
                LogApplyStep(context, StepRestoreStaticMode, "Static 모드 복원", true, "sourceFontFile 비움");

                bool verified = chars.Length == 0 || fontAsset.characterTable.Count > 0;
                LogApplyStep(
                    context,
                    StepVerifyResult,
                    "문자 테이블 검증",
                    verified,
                    $"{fontAsset.characterTable.Count}문자, {fontAsset.glyphTable.Count}글리프");

                EditorUtility.SetDirty(fontAsset);

                if (!verified)
                {
                    _ = summary.AppendLine(
                        $"  실패 {EditorKoDisplayNames.EntryLabel(entry)}: {fontAsset.name} ← {assetPath} (적용 후 문자 테이블이 비어 있음)");
                    return ApplyEntryResult.Failed;
                }

                _ = summary.AppendLine(
                    $"  성공 {EditorKoDisplayNames.EntryLabel(entry)}: {fontAsset.name} ← {assetPath} (테이블 {fontAsset.characterTable.Count}문자)");
                return ApplyEntryResult.Success;
            }
            finally
            {
                if (!restoredStatic && fontAsset.atlasPopulationMode != AtlasPopulationMode.Static)
                {
                    if (TryRestoreStaticMode(fontAsset, out string restoreFailDetail))
                    {
                        LogApplyStep(context, StepRestoreStaticMode, "Static 모드 복원 (finally)", true, string.Empty);
                        EditorUtility.SetDirty(fontAsset);
                    }
                    else
                    {
                        LogApplyStep(context, StepRestoreStaticMode, "Static 모드 복원 (finally)", false, restoreFailDetail);
                    }
                }
            }
        }

        private static ApplyEntryResult FailEntryStep(
            FontAtlasApplyEntry entry,
            StringBuilder summary,
            string context,
            int step,
            string stepName,
            string failDetail)
        {
            LogApplyStep(context, step, stepName, false, failDetail);
            _ = summary.AppendLine($"  실패 {EditorKoDisplayNames.EntryLabel(entry)}: {failDetail}");
            return ApplyEntryResult.Failed;
        }

        private static string BuildAddCharactersDetail(
            TMP_FontAsset fontAsset,
            bool added,
            int characterCountAfterAdd,
            string missing,
            Font sourceFont)
        {
            string addDetail = $"TryAddCharacters={added}, 문자테이블={characterCountAfterAdd}";
            if (string.IsNullOrEmpty(missing))
            {
                return addDetail;
            }

            MissingAnalysis missingAnalysis = AnalyzeMissing(sourceFont, missing);
            addDetail += $", 누락={missing}";
            addDetail += $", {BuildMissingAnalysisDetail(missingAnalysis)}";
            addDetail +=
                $", 아틀라스={fontAsset.atlasWidth}x{fontAsset.atlasHeight}, 아틀라스텍스처={fontAsset.atlasTextureCount}";
            return addDetail;
        }

        private static void LogPartialMissingWarning(
            string context,
            TMP_FontAsset fontAsset,
            string missing,
            Font sourceFont)
        {
            if (string.IsNullOrEmpty(missing))
            {
                return;
            }

            MissingAnalysis missingAnalysis = AnalyzeMissing(sourceFont, missing);
            Debug.LogWarning(
                $"{EditorKoDisplayNames.LogPrefix}: {context} — 일부 글리프 누락: {missing} " +
                $"({BuildMissingAnalysisDetail(missingAnalysis)}, " +
                $"아틀라스={fontAsset.atlasWidth}x{fontAsset.atlasHeight}, 아틀라스텍스처={fontAsset.atlasTextureCount})");

            if (fontAsset.atlasWidth < DefaultAtlasSize || missingAnalysis.SourceHasGlyphCount <= 0)
            {
                return;
            }

            string fontAssetPath = AssetDatabase.GetAssetPath(fontAsset);
            Debug.LogWarning(
                $"{EditorKoDisplayNames.LogPrefix}: {context} — 아틀라스 {fontAsset.atlasWidth}x{fontAsset.atlasHeight}로도 부족합니다. " +
                $"소스글리프있음={missingAnalysis.SourceHasGlyphCount}, 누락고유={missingAnalysis.MissingUniqueCount}. " +
                $"이 폰트는 버킷/프로필 분할을 검토하세요. assetPath={fontAssetPath}");
        }

        private static int FlushReadableAtlasTextures(TMP_FontAsset fontAsset)
        {
            int appliedCount = 0;
            Texture2D[] atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures == null)
            {
                return appliedCount;
            }

            for (int t = 0; t < atlasTextures.Length; t++)
            {
                Texture2D atlasTexture = atlasTextures[t];
                if (atlasTexture == null || !atlasTexture.isReadable)
                {
                    continue;
                }

                atlasTexture.Apply(false, false);
                appliedCount++;
            }

            return appliedCount;
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
                failDetail = "m_SourceFontFileGUID 또는 creationSettings.sourceFontFileGUID 없음";
                return false;
            }

            sourceFontPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(sourceFontPath))
            {
                failDetail = $"GUID를 찾을 수 없음: {guid}";
                return false;
            }

            sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourceFontPath);
            if (sourceFont == null)
            {
                failDetail = $"Font 로드 실패: {sourceFontPath}";
                return false;
            }

            return true;
        }

        private static bool TryLinkEditorSourceFont(TMP_FontAsset fontAsset, Font sourceFont, out string failDetail)
        {
            failDetail = string.Empty;

            if (SourceFontEditorRefProperty != null)
            {
                SourceFontEditorRefProperty.SetValue(fontAsset, sourceFont);
                return true;
            }

            if (SourceFontEditorRefField != null)
            {
                SourceFontEditorRefField.SetValue(fontAsset, sourceFont);
                return true;
            }

            failDetail = "SourceFont_EditorRef/m_SourceFontFile_EditorRef 멤버 없음";
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
                failDetail = "연결 후 에디터 소스 폰트 ref가 null";
                return false;
            }

            if (SourceFontFileField == null)
            {
                failDetail = "m_SourceFontFile 필드 없음";
                return false;
            }

            SourceFontFileField.SetValue(fontAsset, editorRefFont);

            if (fontAsset.sourceFontFile == null)
            {
                failDetail = "Dynamic 전환 후에도 sourceFontFile이 null";
                return false;
            }

            return true;
        }

        private static bool TryGetEditorSourceFontRef(TMP_FontAsset fontAsset, out Font editorRefFont)
        {
            editorRefFont = null;

            if (SourceFontEditorRefProperty != null)
            {
                editorRefFont = SourceFontEditorRefProperty.GetValue(fontAsset) as Font;
                if (editorRefFont != null)
                {
                    return true;
                }
            }

            if (SourceFontEditorRefField != null)
            {
                editorRefFont = SourceFontEditorRefField.GetValue(fontAsset) as Font;
            }

            return editorRefFont != null;
        }

        private static bool TryRestoreStaticMode(TMP_FontAsset fontAsset, out string failDetail)
        {
            failDetail = string.Empty;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

            if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Static)
            {
                failDetail = "복원 후 atlasPopulationMode가 Static이 아님";
                return false;
            }

            return true;
        }

        private static bool TrySetAtlasSize(TMP_FontAsset fontAsset, int size, out string failDetail)
        {
            failDetail = string.Empty;

            TMPro.FontAssetCreationSettings previous = fontAsset.creationSettings;
            fontAsset.creationSettings = new TMPro.FontAssetCreationSettings
            {
                sourceFontFileName = previous.sourceFontFileName,
                sourceFontFileGUID = previous.sourceFontFileGUID,
                faceIndex = previous.faceIndex,
                pointSizeSamplingMode = previous.pointSizeSamplingMode,
                pointSize = previous.pointSize,
                padding = previous.padding,
                paddingMode = previous.paddingMode,
                packingMode = previous.packingMode,
                atlasWidth = size,
                atlasHeight = size,
                characterSetSelectionMode = previous.characterSetSelectionMode,
                characterSequence = previous.characterSequence,
                referencedFontAssetGUID = previous.referencedFontAssetGUID,
                referencedTextAssetGUID = previous.referencedTextAssetGUID,
                fontStyle = previous.fontStyle,
                fontStyleModifier = previous.fontStyleModifier,
                renderMode = previous.renderMode,
                includeFontFeatures = previous.includeFontFeatures
            };

            var serialized = new SerializedObject(fontAsset);
            SerializedProperty atlasWidthProperty = serialized.FindProperty("m_AtlasWidth");
            SerializedProperty atlasHeightProperty = serialized.FindProperty("m_AtlasHeight");
            if (atlasWidthProperty == null || atlasHeightProperty == null)
            {
                failDetail = "m_AtlasWidth 또는 m_AtlasHeight 속성 없음";
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
            string status = success ? "성공" : "실패";
            var message = new StringBuilder();
            _ = message.Append(
                $"{EditorKoDisplayNames.LogPrefix} 적용 [{context}] {step}/{TotalApplySteps} {stepName}: {status}");
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

            int added = CountCodePointsNotIn(targetCodePoints, currentCodePoints);
            int removed = CountCodePointsNotIn(currentCodePoints, targetCodePoints);

            if (added > 0 || removed > 0)
            {
                Debug.Log(
                    $"{EditorKoDisplayNames.LogPrefix} {EditorKoDisplayNames.EntryLabel(entry)} 문자 변경: +{added} -{removed} (전체 재빌드 예정)");
            }
        }

        private static int CountCodePointsNotIn(HashSet<int> source, HashSet<int> excluded)
        {
            int count = 0;
            foreach (int codePoint in source)
            {
                if (!excluded.Contains(codePoint))
                {
                    count++;
                }
            }

            return count;
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
            return
                $"누락고유={analysis.MissingUniqueCount}, 소스글리프있음={analysis.SourceHasGlyphCount}, 소스글리프없음={analysis.SourceNoGlyphCount}, 비BMP={analysis.NonBmpCount}";
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
