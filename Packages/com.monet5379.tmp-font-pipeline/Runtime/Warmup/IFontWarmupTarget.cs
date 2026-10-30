using System.Collections.Generic;
using TMPro;

namespace TmpFontPipeline
{
    public interface IFontWarmupTarget
    {
        IReadOnlyList<TMP_FontAsset> GetFontsForWarmup(string languageId);

        TMP_FontAsset GetFontForWarmup(string languageId, FontUsageRole role);

        string GetSampleText(string languageId, FontUsageRole role);

        void PreloadSpriteAssets(string languageId);
    }
}
