using TMPro;
using UnityEngine;

namespace TmpFontPipeline.Demo
{
    // Phase 2: language change event 후 font·text 갱신.
    public sealed class DemoLocalizedLabel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private string _stringKey;

        public void Refresh(string languageId, string text)
        {
            if (_label != null)
            {
                _label.text = text;
            }
        }
    }
}
