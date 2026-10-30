using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace TmpFontPipeline
{
    // 언어별 TMP 폰트를 스플래시·언어 변경 시점에 미리 워밍업합니다.
    // Warmup ≠ glyph SSOT(Static 추출); 한 프레임 전 폰트 일괄 금지; input block은 소비자 책임.
    public sealed class FontWarmupService : MonoBehaviour
    {
        private const float HIDDEN_TEXT_OFFSCREEN_POSITION = 10000f;

        private static readonly ProfilerMarker WarmupFontMarker = new("FontWarmupService.WarmupFont");
        private static readonly ProfilerMarker PreloadSpriteAssetsMarker = new("FontWarmupService.PreloadSpriteAssets");

        private readonly HashSet<int> _warmedFontInstanceIds = new();
        private readonly HashSet<string> _completedLanguages = new(StringComparer.OrdinalIgnoreCase);

        private Coroutine _warmupCoroutine;
        private Action _pendingOnComplete;
        private Action _pendingOnSuperseded;
        private TextMeshProUGUI _warmupText;

        public static FontWarmupService Instance { get; private set; }

        public bool IsWarmingUp { get; private set; }

        private void Awake()
        {
            Instance = this;
            CreateHiddenCanvas();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 지정 언어의 폰트 워밍업을 요청합니다. 이미 완료된 언어는 즉시 콜백을 호출합니다.
        public void RequestWarmup(
            string languageId,
            IFontWarmupTarget target,
            Action onComplete = null,
            Action onSuperseded = null)
        {
            if (string.IsNullOrEmpty(languageId) || target == null)
            {
                Debug.LogWarning("[FontWarmupService] languageId 또는 target이 비어 있습니다.");
                onComplete?.Invoke();
                return;
            }

            if (IsLanguageFullyWarmed(languageId, target))
            {
                onComplete?.Invoke();
                return;
            }

            CancelActiveWarmup();

            _pendingOnComplete = onComplete;
            _pendingOnSuperseded = onSuperseded;
            _warmupCoroutine = StartCoroutine(CoWarmup(languageId, target));
        }

        // 해당 언어의 모든 고유 폰트가 이미 워밍업되었는지 여부를 반환합니다.
        public bool IsLanguageFullyWarmed(string languageId, IFontWarmupTarget target = null)
        {
            if (string.IsNullOrEmpty(languageId) || _completedLanguages.Contains(languageId))
            {
                return true;
            }

            if (target == null)
            {
                return false;
            }

            return CollectUnwarmedWarmupEntries(languageId, target).Count == 0;
        }

        private IEnumerator CoWarmup(string languageId, IFontWarmupTarget target)
        {
            IsWarmingUp = true;

            using (PreloadSpriteAssetsMarker.Auto())
            {
                target.PreloadSpriteAssets(languageId);
            }

            List<FontWarmupEntry> entriesToWarm = CollectUnwarmedWarmupEntries(languageId, target);
            for (int i = 0; i < entriesToWarm.Count; i++)
            {
                FontWarmupEntry entry = entriesToWarm[i];
                string sampleText = target.GetSampleText(languageId, entry.Role);
                if (string.IsNullOrEmpty(sampleText))
                {
                    sampleText = FontWarmupSampleText.GetForLanguage(languageId, entry.Role);
                }

                WarmupFont(entry.Font, sampleText);
                _warmedFontInstanceIds.Add(entry.Font.GetInstanceID());
                yield return null;
            }

            _completedLanguages.Add(languageId);
            CompleteWarmup();
        }

        private void CancelActiveWarmup()
        {
            if (_warmupCoroutine == null)
            {
                return;
            }

            StopCoroutine(_warmupCoroutine);
            _warmupCoroutine = null;
            IsWarmingUp = false;
            InvokePendingSupersededCallback();
        }

        private void CompleteWarmup()
        {
            IsWarmingUp = false;
            _warmupCoroutine = null;
            InvokePendingCompleteCallback();
        }

        private void InvokePendingSupersededCallback()
        {
            Action callback = _pendingOnSuperseded;
            _pendingOnComplete = null;
            _pendingOnSuperseded = null;
            callback?.Invoke();
        }

        private void InvokePendingCompleteCallback()
        {
            Action callback = _pendingOnComplete;
            _pendingOnComplete = null;
            _pendingOnSuperseded = null;
            callback?.Invoke();
        }

        private void WarmupFont(TMP_FontAsset font, string sampleText)
        {
            using (WarmupFontMarker.Auto())
            {
                if (_warmupText == null || font == null)
                {
                    return;
                }

                _warmupText.text = string.Empty;
                _warmupText.font = font;
                _warmupText.SetText(sampleText);
                _warmupText.ForceMeshUpdate(true, true);
                Canvas.ForceUpdateCanvases();
            }
        }

        private List<FontWarmupEntry> CollectUnwarmedWarmupEntries(string languageId, IFontWarmupTarget target)
        {
            var entries = new List<FontWarmupEntry>(2);
            if (target == null || string.IsNullOrEmpty(languageId))
            {
                return entries;
            }

            TryAddUnwarmedEntry(entries, target.GetFontForWarmup(languageId, FontUsageRole.Ui), FontUsageRole.Ui);
            TryAddUnwarmedEntry(entries, target.GetFontForWarmup(languageId, FontUsageRole.Dialogue), FontUsageRole.Dialogue);
            return entries;
        }

        private void TryAddUnwarmedEntry(List<FontWarmupEntry> entries, TMP_FontAsset font, FontUsageRole role)
        {
            if (font == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Font == font)
                {
                    return;
                }
            }

            if (!_warmedFontInstanceIds.Contains(font.GetInstanceID()))
            {
                entries.Add(new FontWarmupEntry(font, role));
            }
        }

        private readonly struct FontWarmupEntry
        {
            public FontWarmupEntry(TMP_FontAsset font, FontUsageRole role)
            {
                Font = font;
                Role = role;
            }

            public TMP_FontAsset Font { get; }

            public FontUsageRole Role { get; }
        }

        private void CreateHiddenCanvas()
        {
            GameObject canvasObject = new("@FontWarmupCanvas");
            canvasObject.transform.SetParent(transform, false);
            DontDestroyOnLoad(canvasObject);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MinValue;

            CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            canvasObject.AddComponent<CanvasScaler>();

            GameObject textObject = new("WarmupText");
            textObject.transform.SetParent(canvasObject.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(HIDDEN_TEXT_OFFSCREEN_POSITION, HIDDEN_TEXT_OFFSCREEN_POSITION);

            _warmupText = textObject.AddComponent<TextMeshProUGUI>();
            _warmupText.raycastTarget = false;
        }
    }
}
