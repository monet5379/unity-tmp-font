using System;
using System.Collections.Generic;
using TMPro;
using TmpFontPipeline;
using UnityEngine;

namespace TmpFontPipeline.Demo
{
    // IFontWarmupTarget 구현 + FontWarmupService 보장. 부팅 RequestWarmup은 DemoLanguageSwitcher가 담당합니다.
    public sealed class DemoFontWarmupBootstrap : MonoBehaviour, IFontWarmupTarget
    {
        [SerializeField] private FontRoleCatalog _fontCatalog;

        public FontRoleCatalog FontCatalog => _fontCatalog;

        private void Awake()
        {
            EnsureWarmupService();
        }

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
            // Demo has no sprite assets to preload.
        }

        private void EnsureWarmupService()
        {
            if (FontWarmupService.Instance != null)
            {
                return;
            }

            if (GetComponent<FontWarmupService>() != null)
            {
                return;
            }

            gameObject.AddComponent<FontWarmupService>();
        }
    }
}
