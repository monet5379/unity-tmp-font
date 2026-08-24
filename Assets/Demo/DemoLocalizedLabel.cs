using TMPro;
using TmpFontPipeline;
using UnityEngine;

namespace TmpFontPipeline.Demo
{
    // 언어 변경 후 role별 TMP font·text를 갱신합니다.
    public sealed class DemoLocalizedLabel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private FontUsageRole _role = FontUsageRole.Ui;
        [SerializeField] private string _stringKey;

        public FontUsageRole Role => _role;
        public string StringKey => _stringKey;

        public void Configure(TextMeshProUGUI label, FontUsageRole role, string stringKey)
        {
            _label = label;
            _role = role;
            _stringKey = stringKey;
        }

        // string key만 바꿉니다. 화면 갱신은 DemoLanguageSwitcher.RefreshAllLabels가 담당합니다.
        public void SetStringKey(string key)
        {
            _stringKey = key ?? string.Empty;
        }

        public void Refresh(string languageId, TMP_FontAsset font, string text)
        {
            if (_label == null)
            {
                return;
            }

            // 이전 언어 문자열이 새 폰트에서 missing-char 경고를 내지 않도록 비운 뒤 교체합니다.
            _label.text = string.Empty;
            if (font != null)
            {
                _label.font = font;
            }

            _label.text = text ?? string.Empty;
        }
    }
}
