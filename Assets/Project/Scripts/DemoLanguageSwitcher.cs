using System.Collections.Generic;
using TMPro;
using TmpFontPipeline;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TmpFontPipeline.Demo
{
    // 언어 변경 → input block → warmup → UI refresh → unblock.
    public sealed class DemoLanguageSwitcher : MonoBehaviour
    {
        private static readonly string[] LanguageIds =
        {
            "EN", "KO", "JP", "SC", "TC", "FR", "DE", "IT", "ES",
        };

        private static readonly Color SelectedButtonColor = Color.white;
        private static readonly Color UnselectedButtonColor = new(0x40 / 255f, 0x40 / 255f, 0x40 / 255f, 1f);

        [SerializeField] private FontRoleCatalog _fontCatalog;
        [SerializeField] private DemoFontWarmupBootstrap _warmupTarget;
        [SerializeField] private TextAsset _uiStrings;
        [SerializeField] private TextAsset _dialogueStrings;
        [SerializeField] private CanvasGroup _inputBlock;
        [SerializeField] private DemoLocalizedLabel[] _labels;
        [SerializeField] private Transform _languageButtonsRoot;
        [SerializeField] private string _defaultLanguageId = "EN";
        [SerializeField] private bool _buildRuntimeUi = true;
        [SerializeField] private bool _enableKeyPicker = true;

        private readonly DemoStringTable _stringTable = new();
        private readonly List<LanguageButtonVisual> _languageButtons = new();
        private string _currentLanguageId;
        private string _pendingLanguageId;

        private void Awake()
        {
            if (_warmupTarget == null)
            {
                _warmupTarget = GetComponent<DemoFontWarmupBootstrap>();
            }

            if (_fontCatalog == null && _warmupTarget != null)
            {
                _fontCatalog = _warmupTarget.FontCatalog;
            }

            _stringTable.Load(_uiStrings, _dialogueStrings);

            if (_buildRuntimeUi && (_labels == null || _labels.Length == 0))
            {
                BuildRuntimeUi();
            }

            CollectLanguageButtons();
            RefreshLanguageButtonColors(_defaultLanguageId);
            EnsureKeyPicker();
        }

        private void Start()
        {
            SwitchLanguage(_defaultLanguageId);
        }

        public string CurrentLanguageId => _currentLanguageId;

        // warmup 없이 현재 언어로 _labels를 다시 그립니다.
        public void RefreshAllLabels()
        {
            if (_fontCatalog == null)
            {
                return;
            }

            string languageId = string.IsNullOrEmpty(_currentLanguageId)
                ? _defaultLanguageId
                : _currentLanguageId;
            RefreshLabels(languageId);
        }

        // 라벨 string key 변경 후 즉시 redraw합니다.
        public void SetLabelKey(DemoLocalizedLabel label, string key)
        {
            if (label == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            label.SetStringKey(key);
            RefreshAllLabels();
        }

        public void SetLabelKey(FontUsageRole role, string key)
        {
            if (_labels == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            for (int i = 0; i < _labels.Length; i++)
            {
                DemoLocalizedLabel label = _labels[i];
                if (label != null && label.Role == role)
                {
                    SetLabelKey(label, key);
                    return;
                }
            }
        }

        public DemoLocalizedLabel FindLabel(FontUsageRole role)
        {
            if (_labels == null)
            {
                return null;
            }

            for (int i = 0; i < _labels.Length; i++)
            {
                DemoLocalizedLabel label = _labels[i];
                if (label != null && label.Role == role)
                {
                    return label;
                }
            }

            return null;
        }

        // 언어를 바꾸고 warmup 완료 후 라벨을 갱신합니다.
        public void SwitchLanguage(string languageId)
        {
            if (string.IsNullOrEmpty(languageId))
            {
                return;
            }

            if (string.Equals(_currentLanguageId, languageId, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (_fontCatalog == null || _warmupTarget == null)
            {
                Debug.LogWarning("[DemoLanguageSwitcher] FontRoleCatalog 또는 WarmupTarget이 비어 있습니다.");
                return;
            }

            FontWarmupService service = FontWarmupService.Instance;
            if (service == null)
            {
                Debug.LogWarning("[DemoLanguageSwitcher] FontWarmupService를 찾을 수 없습니다.");
                return;
            }

            string requestedLanguageId = languageId;
            _pendingLanguageId = requestedLanguageId;
            SetInputBlocked(true);

            service.RequestWarmup(
                requestedLanguageId,
                _warmupTarget,
                onComplete: () =>
                {
                    if (!string.Equals(_pendingLanguageId, requestedLanguageId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    _currentLanguageId = requestedLanguageId;
                    RefreshLabels(requestedLanguageId);
                    RefreshLanguageButtonColors(requestedLanguageId);
                    SetInputBlocked(false);
                },
                onSuperseded: () =>
                {
                    // 새 SwitchLanguage가 block을 유지한다 — 여기서 unblock하지 않음.
                });
        }

        private void RefreshLabels(string languageId)
        {
            if (_labels == null)
            {
                return;
            }

            for (int i = 0; i < _labels.Length; i++)
            {
                DemoLocalizedLabel label = _labels[i];
                if (label == null)
                {
                    continue;
                }

                TMP_FontAsset font = _fontCatalog.GetFont(languageId, label.Role);
                string text = _stringTable.Get(languageId, label.StringKey, label.Role);
                label.Refresh(languageId, font, text);
            }
        }

        private void RefreshLanguageButtonColors(string selectedLanguageId)
        {
            for (int i = 0; i < _languageButtons.Count; i++)
            {
                LanguageButtonVisual visual = _languageButtons[i];
                if (visual.Image == null)
                {
                    continue;
                }

                bool selected = string.Equals(
                    visual.LanguageId,
                    selectedLanguageId,
                    System.StringComparison.OrdinalIgnoreCase);
                visual.Image.color = selected ? SelectedButtonColor : UnselectedButtonColor;
            }
        }

        private void CollectLanguageButtons()
        {
            _languageButtons.Clear();
            if (_languageButtonsRoot == null)
            {
                return;
            }

            for (int i = 0; i < _languageButtonsRoot.childCount; i++)
            {
                Transform child = _languageButtonsRoot.GetChild(i);
                if (!TryParseLanguageId(child.name, out string languageId))
                {
                    continue;
                }

                Image image = child.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                _languageButtons.Add(new LanguageButtonVisual(languageId, image));
            }
        }

        private static bool TryParseLanguageId(string objectName, out string languageId)
        {
            languageId = null;
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            // "Button (EN)" or "Btn_EN"
            int open = objectName.LastIndexOf('(');
            int close = objectName.LastIndexOf(')');
            if (open >= 0 && close > open + 1)
            {
                languageId = objectName.Substring(open + 1, close - open - 1).Trim();
                return languageId.Length > 0;
            }

            const string btnPrefix = "Btn_";
            if (objectName.StartsWith(btnPrefix, System.StringComparison.Ordinal))
            {
                languageId = objectName.Substring(btnPrefix.Length);
                return languageId.Length > 0;
            }

            return false;
        }

        private void SetInputBlocked(bool blocked)
        {
            if (_inputBlock == null)
            {
                return;
            }

            _inputBlock.interactable = !blocked;
            _inputBlock.blocksRaycasts = !blocked;
        }

        private void BuildRuntimeUi()
        {
            EnsureEventSystem();

            GameObject canvasObject = new("DemoCanvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasObject.AddComponent<GraphicRaycaster>();

            _inputBlock = canvasObject.AddComponent<CanvasGroup>();

            DemoLocalizedLabel uiLabel = CreateLabel(
                canvasObject.transform,
                "UiLabel",
                FontUsageRole.Ui,
                "Confirm",
                new Vector2(0.5f, 0.65f),
                48f);

            DemoLocalizedLabel dialogueLabel = CreateLabel(
                canvasObject.transform,
                "DialogueLabel",
                FontUsageRole.Dialogue,
                "dlg_intro",
                new Vector2(0.5f, 0.5f),
                36f);

            _labels = new[] { uiLabel, dialogueLabel };

            CreateLanguageButtons(canvasObject.transform);
            EnsureKeyPicker();
        }

        private void EnsureKeyPicker()
        {
            if (!_enableKeyPicker || GetComponent<DemoStringKeyPicker>() != null)
            {
                return;
            }

            gameObject.AddComponent<DemoStringKeyPicker>();
        }

        private DemoLocalizedLabel CreateLabel(
            Transform parent,
            string name,
            FontUsageRole role,
            string stringKey,
            Vector2 anchor,
            float fontSize)
        {
            GameObject labelObject = new(name);
            labelObject.transform.SetParent(parent, false);

            RectTransform rect = labelObject.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1200f, 120f);
            rect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI tmp = labelObject.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.text = stringKey;
            tmp.raycastTarget = false;

            DemoLocalizedLabel localized = labelObject.AddComponent<DemoLocalizedLabel>();
            localized.Configure(tmp, role, stringKey);
            return localized;
        }

        private void CreateLanguageButtons(Transform parent)
        {
            GameObject row = new("LanguageButtons");
            row.transform.SetParent(parent, false);

            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.18f);
            rowRect.anchorMax = new Vector2(0.5f, 0.18f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.sizeDelta = new Vector2(1600f, 80f);
            rowRect.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            _languageButtonsRoot = row.transform;

            for (int i = 0; i < LanguageIds.Length; i++)
            {
                string languageId = LanguageIds[i];
                CreateLanguageButton(row.transform, languageId);
            }
        }

        private void CreateLanguageButton(Transform parent, string languageId)
        {
            GameObject buttonObject = new($"Btn_{languageId}");
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = UnselectedButtonColor;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            GameObject textObject = new("Label");
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.text = languageId;
            tmp.fontSize = 28f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            string captured = languageId;
            button.onClick.AddListener(() => SwitchLanguage(captured));
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private readonly struct LanguageButtonVisual
        {
            public LanguageButtonVisual(string languageId, Image image)
            {
                LanguageId = languageId;
                Image = image;
            }

            public string LanguageId { get; }
            public Image Image { get; }
        }
    }
}
