using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TmpFontPipeline;
using UnityEditor;
using UnityEngine;

namespace TmpFontPipeline.Editor
{
    public static class StringJsonCharacterExtractor
    {
        public static readonly string[] CjkLanguageFieldNames = FontAtlasFileNames.CjkLanguageFieldNames;
        public static readonly string[] EuropeanLanguageFieldNames = FontAtlasFileNames.EuropeanLanguageFieldNames;
        public static readonly string[] AllLanguageFieldNames = FontAtlasFileNames.AllLanguageFieldNames;

        // String*.json을 스캔해 언어 버킷별 unique_chars_*.txt를 생성합니다. 성공 시 true.
        public static bool Extract(string jsonSearchPath, string outputPath)
        {
            return Extract(jsonSearchPath, outputPath, EuropeanExtractSettings.Mode);
        }

        public static bool Extract(string jsonSearchPath, string outputPath, EuropeanExtractMode europeanMode)
        {
            if (string.IsNullOrWhiteSpace(jsonSearchPath) || string.IsNullOrWhiteSpace(outputPath))
            {
                Debug.LogError("Tmp Font Pipeline: JSON folder and output folder must be set.");
                return false;
            }

            JsonParserMode parserMode = JsonParserSettings.Mode;
            Debug.Log($"Tmp Font Pipeline: extracting with parser={parserMode}, european={europeanMode}");

            string jsonDir = AssetPathToAbsolute(jsonSearchPath);
            if (!Directory.Exists(jsonDir))
            {
                Debug.LogError($"String JSON directory not found: {jsonDir}");
                return false;
            }

            string[] jsonFiles = Directory.GetFiles(jsonDir, "String*.json", SearchOption.TopDirectoryOnly);
            if (jsonFiles.Length == 0)
            {
                Debug.LogError($"No String*.json files found in: {jsonDir}");
                return false;
            }

            FontExtractionBucket defaultBucket = CreateExtractionBucket();
            FontExtractionBucket dialogueBucket = CreateExtractionBucket();
            var dialogueSourceFiles = new List<string>();

            for (int i = 0; i < jsonFiles.Length; i++)
            {
                string filePath = jsonFiles[i];
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                if (IsDialogueJsonFile(fileName))
                {
                    dialogueSourceFiles.Add(fileName);
                    CollectCodePointsFromJsonFile(filePath, dialogueBucket, parserMode, europeanMode);
                }
                else
                {
                    CollectCodePointsFromJsonFile(filePath, defaultBucket, parserMode, europeanMode);
                }
            }

            string outputDir = AssetPathToAbsolute(outputPath);
            Directory.CreateDirectory(outputDir);

            var summary = new StringBuilder();
            _ = summary.AppendLine($"Extracted unique characters (parser={parserMode}, european={europeanMode}):");

            WriteExtractionBucket(outputDir, defaultBucket, string.Empty, "Default", summary, europeanMode);

            if (dialogueBucket.ParsedFileCount > 0)
            {
                string dialogueLabel = $"StringDialogue ({string.Join(", ", dialogueSourceFiles.OrderBy(name => name))})";
                WriteExtractionBucket(
                    outputDir,
                    dialogueBucket,
                    FontAtlasFileNames.DialogueOutputSuffix,
                    dialogueLabel,
                    summary,
                    europeanMode);
            }

            AssetDatabase.Refresh();
            Debug.Log(summary.ToString());
            return true;
        }

        private static bool IsDialogueJsonFile(string fileNameWithoutExtension)
        {
            return fileNameWithoutExtension.StartsWith("StringDialogue", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileNameWithoutExtension, "StringExtraDialogue", StringComparison.OrdinalIgnoreCase);
        }

        private static FontExtractionBucket CreateExtractionBucket()
        {
            var bucket = new FontExtractionBucket
            {
                LanguageCodePoints = new Dictionary<string, HashSet<int>>(),
            };

            for (int i = 0; i < AllLanguageFieldNames.Length; i++)
            {
                bucket.LanguageCodePoints[AllLanguageFieldNames[i]] = new HashSet<int>();
            }

            bucket.EuropeanCodePoints = new HashSet<int>();

            return bucket;
        }

        private static void CollectCodePointsFromJsonFile(
            string filePath,
            FontExtractionBucket bucket,
            JsonParserMode parserMode,
            EuropeanExtractMode europeanMode)
        {
            string json = File.ReadAllText(filePath, Encoding.UTF8);

            IReadOnlyList<IStringTableRow> rows = parserMode switch
            {
                JsonParserMode.Newtonsoft => NewtonsoftStringTableParser.TryParse(json, filePath),
                _ => JsonUtilityStringTableParser.TryParse(json, filePath),
            };

            if (rows == null)
            {
                return;
            }

            bucket.ParsedFileCount++;

            for (int j = 0; j < rows.Count; j++)
            {
                IStringTableRow entry = rows[j];
                if (entry == null)
                {
                    continue;
                }

                bucket.ParsedEntryCount++;
                CollectCodePointsFromEntry(entry, bucket, europeanMode);
            }
        }

        private static void CollectCodePointsFromEntry(
            IStringTableRow entry,
            FontExtractionBucket bucket,
            EuropeanExtractMode europeanMode)
        {
            for (int i = 0; i < AllLanguageFieldNames.Length; i++)
            {
                string fieldName = AllLanguageFieldNames[i];
                if (europeanMode == EuropeanExtractMode.MergedEuropean
                    && FontAtlasFileNames.IsEuropeanLanguageFieldName(fieldName))
                {
                    CollectCodePointsFromField(entry, fieldName, bucket.EuropeanCodePoints);
                }
                else if (europeanMode != EuropeanExtractMode.MergedEuropean
                    || !FontAtlasFileNames.IsEuropeanLanguageFieldName(fieldName))
                {
                    CollectCodePointsFromField(entry, fieldName, bucket.LanguageCodePoints[fieldName]);
                }
            }
        }

        private static void CollectCodePointsFromField(IStringTableRow entry, string fieldName, HashSet<int> target)
        {
            string raw = entry.GetField(fieldName);
            string sanitized = StringTextSanitizer.SanitizeForFont(raw);
            if (string.IsNullOrEmpty(sanitized))
            {
                return;
            }

            StringTextSanitizer.AddCodePoints(target, sanitized);
        }

        private static void WriteExtractionBucket(
            string outputDir,
            FontExtractionBucket bucket,
            string fileNameSuffix,
            string logLabel,
            StringBuilder summary,
            EuropeanExtractMode europeanMode)
        {
            _ = summary.AppendLine($"[{logLabel}] {bucket.ParsedFileCount} files, {bucket.ParsedEntryCount} entries");

            bool includeMandatoryGlyphs = !FontAtlasFileNames.IsDialogueFileNameSuffix(fileNameSuffix);
            string[] outputFieldNames = europeanMode == EuropeanExtractMode.MergedEuropean
                ? CjkLanguageFieldNames
                : AllLanguageFieldNames;

            for (int i = 0; i < outputFieldNames.Length; i++)
            {
                string fieldName = outputFieldNames[i];
                string outputFile = WriteCodePointsFile(
                    outputDir,
                    $"{FontAtlasFileNames.OutputPrefix}{fieldName}{fileNameSuffix}.txt",
                    bucket.LanguageCodePoints[fieldName],
                    includeMandatoryGlyphs);
                _ = summary.AppendLine(
                    $"  {fieldName}: {bucket.LanguageCodePoints[fieldName].Count} chars -> {ToAssetPath(outputFile)}");
            }

            if (europeanMode == EuropeanExtractMode.MergedEuropean)
            {
                string europeanFileName =
                    $"{FontAtlasFileNames.OutputPrefix}{nameof(FontAtlasBucket.European)}{fileNameSuffix}.txt";
                string europeanOutputFile = WriteCodePointsFile(
                    outputDir,
                    europeanFileName,
                    bucket.EuropeanCodePoints,
                    includeMandatoryGlyphs);
                _ = summary.AppendLine(
                    $"  European (EN+FR+DE+IT+ES): {bucket.EuropeanCodePoints.Count} chars -> {ToAssetPath(europeanOutputFile)}");
            }
        }

        private static string WriteCodePointsFile(
            string outputDir,
            string fileName,
            HashSet<int> codePoints,
            bool includeMandatoryGlyphs = true)
        {
            if (includeMandatoryGlyphs)
            {
                StringTextSanitizer.AddCodePoints(codePoints, FontAtlasMandatoryCharacters.AtlasCharacters);
            }

            string outputFilePath = Path.Combine(outputDir, fileName);
            string content = CodePointsToString(codePoints);
            File.WriteAllText(outputFilePath, content, new UTF8Encoding(false));
            return outputFilePath;
        }

        private static string CodePointsToString(HashSet<int> codePoints)
        {
            int[] sorted = codePoints.OrderBy(codePoint => codePoint).ToArray();
            var sb = new StringBuilder(sorted.Length);
            for (int i = 0; i < sorted.Length; i++)
            {
                _ = sb.Append(char.ConvertFromUtf32(sorted[i]));
            }

            return sb.ToString();
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

        private static string ToAssetPath(string absolutePath)
        {
            string normalized = absolutePath.Replace('\\', '/');
            int dataIndex = normalized.IndexOf("Assets/", StringComparison.Ordinal);
            return dataIndex >= 0 ? normalized.Substring(dataIndex) : normalized;
        }

        private sealed class FontExtractionBucket
        {
            public Dictionary<string, HashSet<int>> LanguageCodePoints;
            public HashSet<int> EuropeanCodePoints;
            public int ParsedFileCount;
            public int ParsedEntryCount;
        }
    }
}
