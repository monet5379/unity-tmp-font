using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TmpFontPipeline
{
    [CreateAssetMenu(fileName = "FontAtlasApplyProfile", menuName = "Tmp Font Pipeline/Font Atlas Apply Profile")]
    public sealed class FontAtlasApplyProfile : ScriptableObject
    {
        [SerializeField] private string _generatedFolder = "Assets/Demo/Generated";
        [SerializeField] private FontAtlasApplyEntry[] _entries = Array.Empty<FontAtlasApplyEntry>();

        public string GeneratedFolder => _generatedFolder;
        public IReadOnlyList<FontAtlasApplyEntry> Entries => _entries;

        // 항목의 Generated txt asset 경로를 반환합니다.
        public string GetResolvedPath(FontAtlasApplyEntry entry)
        {
            string fileName = FontAtlasFileNames.ResolveFileName(entry.Bucket, entry.Role);
            return CombineAssetPath(_generatedFolder, fileName);
        }

        private static string CombineAssetPath(string folder, string fileName)
        {
            string normalized = folder.Replace('\\', '/').TrimEnd('/');
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
