using UnityEngine;

namespace TmpFontPipeline
{
    public enum FontAtlasBucket
    {
        [InspectorName("한국어")]
        Korean,

        [InspectorName("간체 중국어")]
        SimplifiedChinese,

        [InspectorName("번체 중국어")]
        TraditionalChinese,

        [InspectorName("일본어")]
        Japanese,

        [InspectorName("영어")]
        English,

        [InspectorName("프랑스어")]
        French,

        [InspectorName("독일어")]
        German,

        [InspectorName("이탈리아어")]
        Italian,

        [InspectorName("스페인어")]
        Spanish,

        [InspectorName("유럽어 (통합)")]
        European,
    }
}
