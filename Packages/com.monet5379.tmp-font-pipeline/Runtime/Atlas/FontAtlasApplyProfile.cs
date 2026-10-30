using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TmpFontPipeline
{
    [CreateAssetMenu(fileName = "FontAtlasApplyProfile", menuName = "TMP 폰트 파이프라인/폰트 아틀라스 Apply 프로필")]
    public sealed class FontAtlasApplyProfile : ScriptableObject
    {
        [SerializeField] private string _generatedFolder = string.Empty;
        [SerializeField] private FontAtlasApplyEntry[] _entries = Array.Empty<FontAtlasApplyEntry>();

        public string GeneratedFolder => _generatedFolder;
        public IReadOnlyList<FontAtlasApplyEntry> Entries => _entries;

        public string GetResolvedPath(FontAtlasApplyEntry entry)
        {
            if (string.IsNullOrWhiteSpace(_generatedFolder))
            {
                return null;
            }

            string fileName = FontAtlasFileNames.ResolveFileName(entry.Bucket, entry.Role);
            return CombineAssetPath(_generatedFolder, fileName);
        }

        private static string CombineAssetPath(string folder, string fileName)
        {
            string normalized = folder.Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(normalized))
            {
                return null;
            }

            return $"{normalized}/{fileName}";
        }
    }

    [Serializable]
    public sealed class FontAtlasApplyEntry
    {
        public FontAtlasBucket Bucket;
        public FontUsageRole Role;
        public TMP_FontAsset FontAsset;
        public bool Enabled = true;
    }
}
