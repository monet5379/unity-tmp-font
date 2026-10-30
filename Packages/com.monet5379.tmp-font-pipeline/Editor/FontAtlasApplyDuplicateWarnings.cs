using System;
using System.Collections.Generic;
using TMPro;
using TmpFontPipeline;
using UnityEditor;
using UnityEngine;

namespace TmpFontPipeline.Editor
{
    public static class FontAtlasApplyDuplicateWarnings
    {
        public static void LogEnabledDuplicateFontAssets(IReadOnlyList<FontAtlasApplyEntry> entries)
        {
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                string message = TryGetDuplicateWarning(entries, i);
                if (message != null)
                {
                    Debug.LogWarning($"Tmp Font Pipeline: duplicate apply entry (same TMP_FontAsset): {message}");
                }
            }
        }

        public static IReadOnlyList<string> GetEnabledDuplicateFontAssetWarnings(IReadOnlyList<FontAtlasApplyEntry> entries)
        {
            var warnings = new List<string>();
            if (entries == null)
            {
                return warnings;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                string message = TryGetDuplicateWarning(entries, i);
                if (message != null)
                {
                    warnings.Add(message);
                }
            }

            return warnings;
        }

        private static string TryGetDuplicateWarning(IReadOnlyList<FontAtlasApplyEntry> entries, int index)
        {
            FontAtlasApplyEntry entry = entries[index];
            if (entry == null || !entry.Enabled || entry.FontAsset == null)
            {
                return null;
            }

            string key = ResolveFontAssetKey(entry.FontAsset);
            for (int j = 0; j < index; j++)
            {
                FontAtlasApplyEntry prior = entries[j];
                if (prior == null || !prior.Enabled || prior.FontAsset == null)
                {
                    continue;
                }

                if (string.Equals(key, ResolveFontAssetKey(prior.FontAsset), StringComparison.Ordinal))
                {
                    return $"{entry.FontAsset.name} ({key})";
                }
            }

            return null;
        }

        private static string ResolveFontAssetKey(TMP_FontAsset fontAsset)
        {
            string path = AssetDatabase.GetAssetPath(fontAsset);
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }

            return fontAsset.GetInstanceID().ToString();
        }
    }
}
