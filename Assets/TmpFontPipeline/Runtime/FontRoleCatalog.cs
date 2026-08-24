using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TmpFontPipeline
{
    [CreateAssetMenu(fileName = "FontRoleCatalog", menuName = "Tmp Font Pipeline/Font Role Catalog")]
    public sealed class FontRoleCatalog : ScriptableObject
    {
        [SerializeField] private LanguageFontRoleGroup[] _languages = Array.Empty<LanguageFontRoleGroup>();

        public IReadOnlyList<LanguageFontRoleGroup> Languages => _languages;

        // languageId와 역할에 맞는 Static Font Asset을 반환합니다.
        public TMP_FontAsset GetFont(string languageId, FontUsageRole role)
        {
            if (string.IsNullOrEmpty(languageId))
            {
                return null;
            }

            for (int i = 0; i < _languages.Length; i++)
            {
                LanguageFontRoleGroup group = _languages[i];
                if (group == null || !string.Equals(group.LanguageId, languageId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return role == FontUsageRole.Dialogue ? group.DialogueFont : group.UiFont;
            }

            return null;
        }

        // 워밍업 대상 Ui·Dialogue Font Asset 목록을 반환합니다.
        public IReadOnlyList<TMP_FontAsset> GetFontsForWarmup(string languageId)
        {
            var fonts = new List<TMP_FontAsset>(2);
            AddUniqueFont(fonts, GetFont(languageId, FontUsageRole.Ui));
            AddUniqueFont(fonts, GetFont(languageId, FontUsageRole.Dialogue));
            return fonts;
        }

        private static void AddUniqueFont(List<TMP_FontAsset> fonts, TMP_FontAsset font)
        {
            if (font == null || fonts.Contains(font))
            {
                return;
            }

            fonts.Add(font);
        }
    }

    [Serializable]
    public sealed class LanguageFontRoleGroup
    {
        public string LanguageId;
        public TMP_FontAsset UiFont;
        public TMP_FontAsset DialogueFont;
    }
}
