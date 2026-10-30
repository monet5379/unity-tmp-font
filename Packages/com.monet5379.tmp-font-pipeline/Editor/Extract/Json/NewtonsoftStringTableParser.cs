using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TmpFontPipeline.Editor
{
    public static class NewtonsoftStringTableParser
    {
        public static IReadOnlyList<IStringTableRow> TryParse(string json, string filePathForLog)
        {
            JArray entries;
            try
            {
                entries = JArray.Parse(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{EditorKoDisplayNames.LogPrefix}: Newtonsoft 파싱 실패, 건너뜀: {filePathForLog}\n{ex.Message}");
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
                Debug.LogWarning($"{EditorKoDisplayNames.LogPrefix}: 객체 행이 없는 JSON 건너뜀 (Newtonsoft): {filePathForLog}");
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
