using System;
using TmpFontPipeline;
using UnityEngine;

namespace TmpFontPipeline.Demo
{
    // Demo용 String*.json 조회 — Catalog languageId(EN) ↔ JSON 필드(English).
    public sealed class DemoStringTable
    {
        [Serializable]
        private sealed class RowTable
        {
            public Row[] rows;
        }

        [Serializable]
        private sealed class Row
        {
            public string Key;
            public string Korean;
            public string English;
            public string SimplifiedChinese;
            public string TraditionalChinese;
            public string Japanese;
            public string French;
            public string German;
            public string Italian;
            public string Spanish;
        }

        private Row[] _uiRows = Array.Empty<Row>();
        private Row[] _dialogueRows = Array.Empty<Row>();

        public void Load(TextAsset uiJson, TextAsset dialogueJson)
        {
            _uiRows = ParseRows(uiJson);
            _dialogueRows = ParseRows(dialogueJson);
        }

        public string Get(string languageId, string key, FontUsageRole role)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            Row[] rows = role == FontUsageRole.Dialogue ? _dialogueRows : _uiRows;
            for (int i = 0; i < rows.Length; i++)
            {
                Row row = rows[i];
                if (row == null || !string.Equals(row.Key, key, StringComparison.Ordinal))
                {
                    continue;
                }

                return ResolveField(row, languageId) ?? string.Empty;
            }

            return string.Empty;
        }

        private static Row[] ParseRows(TextAsset asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.text))
            {
                return Array.Empty<Row>();
            }

            string wrapped = "{\"rows\":" + asset.text + "}";
            RowTable table = JsonUtility.FromJson<RowTable>(wrapped);
            return table?.rows ?? Array.Empty<Row>();
        }

        private static string ResolveField(Row row, string languageId)
        {
            if (string.IsNullOrEmpty(languageId))
            {
                return row.English;
            }

            if (languageId.Equals("EN", StringComparison.OrdinalIgnoreCase)
                || languageId.Equals(nameof(FontAtlasBucket.English), StringComparison.OrdinalIgnoreCase))
            {
                return row.English;
            }

            if (languageId.Equals("KO", StringComparison.OrdinalIgnoreCase)
                || languageId.Equals(nameof(FontAtlasBucket.Korean), StringComparison.OrdinalIgnoreCase))
            {
                return row.Korean;
            }

            if (languageId.Equals("JP", StringComparison.OrdinalIgnoreCase)
                || languageId.Equals(nameof(FontAtlasBucket.Japanese), StringComparison.OrdinalIgnoreCase))
            {
                return row.Japanese;
            }

            if (languageId.Equals("SC", StringComparison.OrdinalIgnoreCase)
                || languageId.Equals(nameof(FontAtlasBucket.SimplifiedChinese), StringComparison.OrdinalIgnoreCase))
            {
                return row.SimplifiedChinese;
            }

            if (languageId.Equals("TC", StringComparison.OrdinalIgnoreCase)
                || languageId.Equals(nameof(FontAtlasBucket.TraditionalChinese), StringComparison.OrdinalIgnoreCase))
            {
                return row.TraditionalChinese;
            }

            if (languageId.Equals("FR", StringComparison.OrdinalIgnoreCase)
                || languageId.Equals(nameof(FontAtlasBucket.French), StringComparison.OrdinalIgnoreCase))
            {
                return row.French;
            }

            if (languageId.Equals("DE", StringComparison.OrdinalIgnoreCase)
                || languageId.Equals(nameof(FontAtlasBucket.German), StringComparison.OrdinalIgnoreCase))
            {
                return row.German;
            }

            if (languageId.Equals("IT", StringComparison.OrdinalIgnoreCase)
                || languageId.Equals(nameof(FontAtlasBucket.Italian), StringComparison.OrdinalIgnoreCase))
            {
                return row.Italian;
            }

            if (languageId.Equals("ES", StringComparison.OrdinalIgnoreCase)
                || languageId.Equals(nameof(FontAtlasBucket.Spanish), StringComparison.OrdinalIgnoreCase))
            {
                return row.Spanish;
            }

            return row.English;
        }
    }
}
