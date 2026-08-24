using System;
using UnityEngine;

namespace TmpFontPipeline
{
    // 언어별 TMP 폰트를 스플래시·언어 변경 시점에 미리 워밍업합니다.
    public sealed class FontWarmupService : MonoBehaviour
    {
        public static FontWarmupService Instance { get; private set; }

        public bool IsWarmingUp { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 지정 언어의 폰트 워밍업을 요청합니다.
        public void RequestWarmup(
            string languageId,
            IFontWarmupTarget target,
            Action onComplete = null,
            Action onSuperseded = null)
        {
            // Phase 2: frame-split ForceMeshUpdate, supersede, hidden canvas warmup.
            onComplete?.Invoke();
        }
    }
}
