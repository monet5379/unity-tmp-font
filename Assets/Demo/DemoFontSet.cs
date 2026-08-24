using System;
using TMPro;
using UnityEngine;

namespace TmpFontPipeline.Demo
{
    [CreateAssetMenu(fileName = "DemoFontSet", menuName = "Tmp Font Pipeline/Demo Font Set")]
    public sealed class DemoFontSet : ScriptableObject
    {
        [SerializeField] private LanguageFontEntry[] _entries = Array.Empty<LanguageFontEntry>();

        public LanguageFontEntry[] Entries => _entries;
    }

    [Serializable]
    public sealed class LanguageFontEntry
    {
        public string LanguageId;
        public TMP_FontAsset[] Fonts;
    }
}
