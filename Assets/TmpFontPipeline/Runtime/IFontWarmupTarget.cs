using System.Collections.Generic;
using TMPro;

namespace TmpFontPipeline
{
    // Demo·게임 코드가 구현하는 워밍업 대상 계약입니다.
    public interface IFontWarmupTarget
    {
        IReadOnlyList<TMP_FontAsset> GetFontsForWarmup(string languageId);

        string GetSampleText(string languageId);

        void PreloadSpriteAssets(string languageId);
    }
}
