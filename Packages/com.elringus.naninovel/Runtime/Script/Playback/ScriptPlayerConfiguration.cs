using UnityEngine;

namespace Naninovel
{
    [EditInProjectSettings]
    public class ScriptPlayerConfiguration : Configuration
    {
        [Tooltip("Default skip mode to set when the game is first started.")]
        public PlayerSkipMode DefaultSkipMode = PlayerSkipMode.ReadOnly;
        [Tooltip("Time scale to use when in skip (fast-forward) mode. Set to 1 to disable changing the time scale on skip."), Range(1f, 100f)]
        public float SkipTimeScale = 10f;
        [Tooltip("Minimum seconds to wait before executing next command while in auto play mode.")]
        public float MinAutoPlayDelay = 1f;
        [Tooltip("Whether to instantly complete blocking (`wait!`) commands performed over time (eg, animations, hide/reveal, tint changes, etc) when `Continue` input is activated.")]
        public bool CompleteOnContinue = true;
        [Tooltip("Whether to show player debug window on engine initialization.")]
        public bool ShowDebugOnInit;
        [Tooltip("Whether to wait the played commands when the `wait` parameter is not explicitly specified. Only applicable to the awaitable (asynchronous) commands." +
                 "\n\nWARNING: Don't enable in new projects, as this option is kept for backward compatibility and will be removed in the next release.")]
        public bool WaitByDefault;
        [Tooltip("Whether to automatically show `ILoadingUI` during the script pre-/loading and engine reset operations. Allows masking resource loading process with the loading screen.")]
        public bool ShowLoadingUI;
    }
}
