using UnityEngine;

namespace TmpFontPipeline.Editor
{
    public enum EuropeanExtractMode
    {
        [InspectorName("언어별 (기본)")]
        PerLanguage,

        [InspectorName("유럽어 통합")]
        MergedEuropean,
    }
}
