using UnityEngine;
using UnityEngine.Events;

namespace Naninovel
{
    /// <summary>
    /// Routes <see cref="Dialogue"/> APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Dialogue Events")]
    public class DialogueEvents : MonoBehaviour
    {
        [Tooltip("When assigned, will play the script after entering the dialogue mode.")]
        [ScriptAssetRef] public string Script;
        [Tooltip("When 'Script' is assigned will start playing from the specified label inside the script.")]
        public string Label;

        [Tooltip("Occurs when the dialogue mode is changed: activated (true) or deactivated (false).")]
        public BoolUnityEvent DialogueActive;
        [Tooltip("Occurs when the dialogue mode is activated.")]
        public UnityEvent DialogueEntered;
        [Tooltip("Occurs when the dialogue mode is deactivated.")]
        public UnityEvent DialogueExited;

        public virtual void EnterDialogue ()
        {
            if (string.IsNullOrWhiteSpace(Script)) Dialogue.Enter().Forget();
            else Dialogue.EnterAndPlayAsset(Script, Label).Forget();
        }

        public virtual void ExitDialogue ()
        {
            Dialogue.Exit().Forget();
        }

        private void OnEnable ()
        {
            DialogueActive?.Invoke(Dialogue.Active);
            Dialogue.OnEntered += HandleEntered;
            Dialogue.OnExited += HandleExited;
        }

        private void OnDisable ()
        {
            Dialogue.OnEntered -= HandleEntered;
            Dialogue.OnExited -= HandleExited;
        }

        private void HandleEntered ()
        {
            DialogueEntered?.Invoke();
            DialogueActive?.Invoke(true);
        }

        private void HandleExited ()
        {
            DialogueExited?.Invoke();
            DialogueActive?.Invoke(false);
        }
    }
}
