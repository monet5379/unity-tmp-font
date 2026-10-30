using System;
using System.Collections.Generic;
using TMPro;
using TmpFontPipeline;
using UnityEngine;

namespace TmpFontPipeline.Sample.Demo
{
    // Package Manager Demo 샘플 Import 후 FontRoleCatalog·FontWarmupService와 함께 붙이는 최소 IFontWarmupTarget 예시입니다.
    public sealed class SampleFontWarmupTarget : MonoBehaviour, IFontWarmupTarget
    {
        [SerializeField] private FontRoleCatalog _fontCatalog;

        public IReadOnlyList<TMP_FontAsset> GetFontsForWarmup(string languageId)
        {
            if (_fontCatalog == null)
            {
                return Array.Empty<TMP_FontAsset>();
            }

            return _fontCatalog.GetFontsForWarmup(languageId);
        }

        public TMP_FontAsset GetFontForWarmup(string languageId, FontUsageRole role)
        {
            if (_fontCatalog == null)
            {
                return null;
            }

            return _fontCatalog.GetFont(languageId, role);
        }

        public string GetSampleText(string languageId, FontUsageRole role)
        {
            return FontWarmupSampleText.GetForLanguage(languageId, role);
        }

        public void PreloadSpriteAssets(string languageId)
        {
        }
    }
}
