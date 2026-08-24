using TmpFontPipeline;
using UnityEngine;

namespace TmpFontPipeline.Demo
{
    // Phase 2: Splash 시작 시 FontWarmupService.RequestWarmup 호출.
    public sealed class DemoFontWarmupBootstrap : MonoBehaviour
    {
        [SerializeField] private FontRoleCatalog _fontCatalog;

        private void Start()
        {
            // Phase 2: boot-time warmup for default language.
        }
    }
}
