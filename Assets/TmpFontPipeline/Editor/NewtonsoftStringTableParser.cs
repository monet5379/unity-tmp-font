using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TmpFontPipeline.Editor
{
    public static class NewtonsoftStringTableParser
    {
        // 동적 필드 JSON을 Newtonsoft로 파싱합니다. 실패 시 null.
        public static IReadOnlyList<IStringTableRow> TryParse(string json, string filePathForLog)
        {
            JArray entries;
            try
            {
                entries = JArray.Parse(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Skipped invalid JSON array (Newtonsoft): {filePathForLog}\n{ex.Message}");
                return null;
            }

            var rows = new List<IStringTableRow>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] is JObject entry)
                {
                    rows.Add(new JObjectStringTableRow(entry));
                }
            }

            if (rows.Count == 0)
            {
                Debug.LogWarning($"Skipped JSON with no object rows (Newtonsoft): {filePathForLog}");
                return null;
            }

            return rows;
        }

        private sealed class JObjectStringTableRow : IStringTableRow
        {
            private readonly JObject _entry;

            public JObjectStringTableRow(JObject entry)
            {
                _entry = entry;
            }

            public string GetField(string languageFieldName)
            {
                if (!_entry.TryGetValue(languageFieldName, out JToken token))
                {
                    return null;
                }

                return token.Type == JTokenType.String ? token.Value<string>() : null;
            }
        }
    }
}
