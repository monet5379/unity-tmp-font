using UnityEngine;

namespace TmpFontPipeline.Demo
{
    // Phase 2: 언어 변경 → input block → warmup → UI refresh → unblock.
    public sealed class DemoLanguageSwitcher : MonoBehaviour
    {
        [SerializeField] private DemoFontSet _fontSet;

        public void SwitchLanguage(string languageId)
        {
            // Phase 2: supersede-safe language switch with warmup callback.
        }
    }
}
