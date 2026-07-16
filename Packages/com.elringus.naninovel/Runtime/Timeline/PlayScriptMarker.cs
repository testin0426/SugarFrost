#if TIMELINE_AVAILABLE

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Naninovel
{
    [CustomStyle("NaninovelScriptMarker")]
    public class PlayScriptMarker : Marker, INotification
    {
        [field: SerializeField, TextArea(3, 10), Tooltip("The scenario script text to execute.\n\nInstead of '@goto' commands use 'Goto Script' property below to navigate to a script after the scenario text is executed.")]
        public string ScriptText { get; private set; }
        [field: SerializeField, Tooltip("Scenario script to navigate to after executing the script text (if any)."), ScriptAssetRef]
        public string GotoScript { get; private set; }
        [field: SerializeField, Tooltip("Label to navigate to when using 'Goto Script'.")]
        public string GotoLabel { get; private set; }
        [field: SerializeField, Tooltip("Whether to enter the dialogue mode before starting the script execution.")]
        public bool EnterDialogue { get; private set; } = true;
        [field: SerializeField, Tooltip("Whether to pause the playable director while in the dialogue mode.")]
        public bool PauseDirector { get; private set; } = true;
        [field: SerializeField, Tooltip("Whether to complete executing commands on 'Continue' input. Default is controlled in the script player configuration.")]
        public DefaultSwitch CompleteOnContinue { get; private set; } = DefaultSwitch.Disable;
        [field: SerializeField, Tooltip("Whether to ignore the 'await input' requests (click to continue prompts) while executing the script.")]
        public bool DisableAwaitInput { get; private set; } = true;
        [field: SerializeField, Tooltip("Whether to ignore the auto play feature while executing the script.")]
        public bool DisableAutoPlay { get; private set; }
        [field: SerializeField, Tooltip("Whether to ignore the skip (fast-forward) feature while executing the script.")]
        public bool DisableSkip { get; private set; }

        PropertyName INotification.id { get; } = "NaninovelPlayScript";
    }
}

#endif
