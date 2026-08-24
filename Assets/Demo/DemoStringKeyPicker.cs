using TMPro;
using TmpFontPipeline;
using UnityEngine;
using UnityEngine.UI;

namespace TmpFontPipeline.Demo
{
    // Play 중 Ui·Dialogue 라벨 string key 순환 — extract된 Demo JSON 키만 사용.
    public sealed class DemoStringKeyPicker : MonoBehaviour
    {
        private static readonly Color ButtonColor = new(0x40 / 255f, 0x40 / 255f, 0x40 / 255f, 1f);

        [SerializeField] private DemoLanguageSwitcher _switcher;
        [SerializeField] private string[] _uiKeys = { "Confirm", "Cancel", "StartGame", "Victory" };
        [SerializeField] private string[] _dialogueKeys = { "dlg_intro", "dlg_boss" };
        [SerializeField] private bool _buildRuntimeUi = true;

        private int _uiKeyIndex;
        private int _dialogueKeyIndex;
        private TextMeshProUGUI _uiKeyHint;
        private TextMeshProUGUI _dialogueKeyHint;

        private void Awake()
        {
            if (_switcher == null)
            {
                _switcher = GetComponent<DemoLanguageSwitcher>();
            }

            SyncIndicesFromLabels();

            if (_buildRuntimeUi)
            {
                BuildRuntimeUi();
            }
        }

        public void CycleUiKey(int delta)
        {
            if (_switcher == null || _uiKeys == null || _uiKeys.Length == 0)
            {
                return;
            }

            _uiKeyIndex = WrapIndex(_uiKeyIndex + delta, _uiKeys.Length);
            _switcher.SetLabelKey(FontUsageRole.Ui, _uiKeys[_uiKeyIndex]);
            RefreshKeyHints();
        }

        public void CycleDialogueKey(int delta)
        {
            if (_switcher == null || _dialogueKeys == null || _dialogueKeys.Length == 0)
            {
                return;
            }

            _dialogueKeyIndex = WrapIndex(_dialogueKeyIndex + delta, _dialogueKeys.Length);
            _switcher.SetLabelKey(FontUsageRole.Dialogue, _dialogueKeys[_dialogueKeyIndex]);
            RefreshKeyHints();
        }

        private void SyncIndicesFromLabels()
        {
            if (_switcher == null)
            {
                return;
            }

            DemoLocalizedLabel uiLabel = _switcher.FindLabel(FontUsageRole.Ui);
            if (uiLabel != null)
            {
                _uiKeyIndex = IndexOfKey(_uiKeys, uiLabel.StringKey);
            }

            DemoLocalizedLabel dialogueLabel = _switcher.FindLabel(FontUsageRole.Dialogue);
            if (dialogueLabel != null)
            {
                _dialogueKeyIndex = IndexOfKey(_dialogueKeys, dialogueLabel.StringKey);
            }
        }

        private void BuildRuntimeUi()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[DemoStringKeyPicker] Canvas를 찾을 수 없습니다.");
                return;
            }

            GameObject row = new("KeyPickerRow");
            row.transform.SetParent(canvas.transform, false);

            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.08f);
            rowRect.anchorMax = new Vector2(0.5f, 0.08f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.sizeDelta = new Vector2(900f, 64f);
            rowRect.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateCycleButton(row.transform, "UiPrev", "<", () => CycleUiKey(-1));
            _uiKeyHint = CreateHintText(row.transform, "UiKeyHint", 220f);
            CreateCycleButton(row.transform, "UiNext", ">", () => CycleUiKey(1));

            CreateSpacer(row.transform, 24f);

            CreateCycleButton(row.transform, "DlgPrev", "<", () => CycleDialogueKey(-1));
            _dialogueKeyHint = CreateHintText(row.transform, "DlgKeyHint", 220f);
            CreateCycleButton(row.transform, "DlgNext", ">", () => CycleDialogueKey(1));

            RefreshKeyHints();
        }

        private void RefreshKeyHints()
        {
            if (_uiKeyHint != null && _uiKeys != null && _uiKeys.Length > 0)
            {
                _uiKeyHint.text = _uiKeys[_uiKeyIndex];
            }

            if (_dialogueKeyHint != null && _dialogueKeys != null && _dialogueKeys.Length > 0)
            {
                _dialogueKeyHint.text = _dialogueKeys[_dialogueKeyIndex];
            }
        }

        private static void CreateCycleButton(Transform parent, string name, string caption, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new(name);
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = ButtonColor;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.minWidth = 72f;
            layout.preferredWidth = 88f;

            GameObject textObject = new("Label");
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.text = caption;
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }

        private static TextMeshProUGUI CreateHintText(Transform parent, string name, float width)
        {
            GameObject hintObject = new(name);
            hintObject.transform.SetParent(parent, false);

            LayoutElement layout = hintObject.AddComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;

            TextMeshProUGUI tmp = hintObject.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 20f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void CreateSpacer(Transform parent, float width)
        {
            GameObject spacer = new("Spacer");
            spacer.transform.SetParent(parent, false);

            LayoutElement layout = spacer.AddComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
        }

        private static int WrapIndex(int index, int length)
        {
            if (length <= 0)
            {
                return 0;
            }

            int wrapped = index % length;
            return wrapped < 0 ? wrapped + length : wrapped;
        }

        private static int IndexOfKey(string[] keys, string key)
        {
            if (keys == null || string.IsNullOrEmpty(key))
            {
                return 0;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                if (string.Equals(keys[i], key, System.StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return 0;
        }
    }
}
