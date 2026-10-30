using System;
using System.Collections.Generic;
using UnityEngine;

namespace TmpFontPipeline.Editor
{
    public static class JsonUtilityStringTableParser
    {
        public static IReadOnlyList<IStringTableRow> TryParse(string json, string filePathForLog)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"{EditorKoDisplayNames.LogPrefix}: 빈 JSON 건너뜀: {filePathForLog}");
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
                Debug.LogWarning($"{EditorKoDisplayNames.LogPrefix}: JsonUtility 파싱 실패, 건너뜀: {filePathForLog}\n{ex.Message}");
                return null;
            }

            if (file?.Items == null || file.Items.Length == 0)
            {
                Debug.LogWarning($"{EditorKoDisplayNames.LogPrefix}: Items가 없는 JSON 건너뜀 (JsonUtility): {filePathForLog}");
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
