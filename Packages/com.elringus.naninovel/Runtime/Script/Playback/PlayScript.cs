using System.Globalization;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Allows to play a <see cref="Script"/> or execute script commands via Unity API.
    /// </summary>
    [AddComponentMenu("Naninovel/Play Script")]
    public class PlayScript : MonoBehaviour
    {
        public virtual string ScriptText => scriptText;
        public virtual string GotoScript => gotoScript;
        public virtual string GotoLabel => gotoLabel;
        public virtual bool PlayOnAwake => playOnAwake;
        public virtual DefaultSwitch CompleteOnContinue => completeOnContinue;
        public virtual bool DisableAwaitInput => disableAwaitInput;
        public virtual bool DisableAutoPlay => disableAutoPlay;
        public virtual bool DisableSkip => disableSkip;

        [TextArea(3, 10), Tooltip("The scenario script text to execute.\n\nWhen invoked as a callback to a Unity event, argument of the event (if any) can be injected to the script text with '{arg}' expression.\n\nInstead of '@goto' commands use 'Goto Script' property below to navigate to a script after the scenario text is executed.")]
        [SerializeField] private string scriptText;
        [Tooltip("Scenario script to navigate to after executing the script text (if any)."), ScriptAssetRef]
        [SerializeField] private string gotoScript;
        [Tooltip("Label to navigate to when using 'Goto Script'.")]
        [SerializeField] private string gotoLabel;
        [Tooltip("Whether to automatically execute the script when the game object is instantiated.")]
        [SerializeField] private bool playOnAwake;
        [Tooltip("Whether to complete executing commands on 'Continue' input. Default is controlled in the script player configuration.")]
        [SerializeField] private DefaultSwitch completeOnContinue = DefaultSwitch.Disable;
        [Tooltip("Whether to ignore the 'await input' requests (click to continue prompts) while executing the script.")]
        [SerializeField] private bool disableAwaitInput = true;
        [Tooltip("Whether to ignore the auto play feature while executing the script.")]
        [SerializeField] private bool disableAutoPlay;
        [Tooltip("Whether to ignore the skip (fast-forward) feature while executing the script.")]
        [SerializeField] private bool disableSkip;

        private string arg;

        public virtual void Play ()
        {
            arg = null;
            DoPlayScript().Forget();
        }

        public virtual void Play (string arg)
        {
            this.arg = arg;
            DoPlayScript().Forget();
        }

        public virtual void Play (float arg)
        {
            this.arg = arg.ToString(CultureInfo.InvariantCulture);
            DoPlayScript().Forget();
        }

        public virtual void Play (int arg)
        {
            this.arg = arg.ToString(CultureInfo.InvariantCulture);
            DoPlayScript().Forget();
        }

        public virtual void Play (bool arg)
        {
            this.arg = arg.ToString(CultureInfo.InvariantCulture).ToLower();
            DoPlayScript().Forget();
        }

        public virtual async void PlayDialogue ()
        {
            if (Dialogue.Active) return;
            await Dialogue.Enter();
            DoPlayScript().Forget();
        }

        protected virtual void Awake ()
        {
            if (PlayOnAwake) Play();
        }

        protected virtual async Awaitable DoPlayScript ()
        {
            var player = Engine.GetServiceOrErr<IScriptPlayer>();

            if (!string.IsNullOrWhiteSpace(ScriptText))
            {
                var id = $"PlayScript-{name}";
                var text = string.IsNullOrEmpty(arg) ? ScriptText : ScriptText.Replace("{arg}", arg);
                await player.PlayTransient(text, id, CreateOptions());
            }

            if (!string.IsNullOrEmpty(GotoScript))
            {
                if (ScriptAssets.GetPath(GotoScript) is not { } path)
                    throw Engine.Fail("Failed to navigate to a script asset with 'Play Script' component: " +
                                      $"script asset with '{GotoScript}' GUID not found.");
                if (string.IsNullOrWhiteSpace(GotoLabel)) await player.MainTrack.LoadAndPlay(path);
                else await player.MainTrack.LoadAndPlayAtLabel(path, GotoLabel);
            }
        }

        protected virtual PlaybackOptions CreateOptions () => new() {
            CompleteOnContinue = CompleteOnContinue,
            DisableAwaitInput = DisableAwaitInput,
            DisableAutoPlay = DisableAutoPlay,
            DisableSkip = DisableSkip
        };
    }
}
