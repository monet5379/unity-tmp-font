using System;
using System.Collections.Generic;
using UnityEngine;

namespace TmpFontPipeline.Editor
{
    public static class JsonUtilityStringTableParser
    {
        // 고정 언어 컬럼 JSON을 JsonUtility로 파싱합니다. 실패 시 null.
        public static IReadOnlyList<IStringTableRow> TryParse(string json, string filePathForLog)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"Skipped empty JSON: {filePathForLog}");
                return null;
            }

            string trimmed = json.TrimStart();
            string wrapped = trimmed.StartsWith("[", StringComparison.Ordinal)
                ? "{\"Items\":" + json + "}"
                : json;

            StringTableFile file;
            try
            {
                file = JsonUtility.FromJson<StringTableFile>(wrapped);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Skipped invalid JSON (JsonUtility): {filePathForLog}\n{ex.Message}");
                return null;
            }

            if (file?.Items == null || file.Items.Length == 0)
            {
                Debug.LogWarning($"Skipped JSON with no Items (JsonUtility): {filePathForLog}");
                return null;
            }

            var rows = new List<IStringTableRow>(file.Items.Length);
            for (int i = 0; i < file.Items.Length; i++)
            {
                StringTableRow row = file.Items[i];
                if (row != null)
                {
                    rows.Add(row);
                }
            }

            return rows;
        }

        [Serializable]
        private sealed class StringTableFile
        {
            public StringTableRow[] Items;
        }

        [Serializable]
        private sealed class StringTableRow : IStringTableRow
        {
            public string Key;
            public string Korean;
            public string English;
            public string SimplifiedChinese;
            public string TraditionalChinese;
            public string French;
            public string German;
            public string Italian;
            public string Spanish;
            public string Japanese;

            public string GetField(string languageFieldName)
            {
                return languageFieldName switch
                {
                    "Korean" => Korean,
                    "English" => English,
                    "SimplifiedChinese" => SimplifiedChinese,
                    "TraditionalChinese" => TraditionalChinese,
                    "French" => French,
                    "German" => German,
                    "Italian" => Italian,
                    "Spanish" => Spanish,
                    "Japanese" => Japanese,
                    _ => null,
                };
            }
        }
    }
}
