using UnityEngine;

namespace TmpFontPipeline.Editor
{
    public enum JsonParserMode
    {
        [InspectorName("Unity JsonUtility")]
        JsonUtility = 0,

        [InspectorName("Newtonsoft.Json")]
        Newtonsoft = 1,
    }
}
