using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Routes essential <see cref="IInputManager"/> APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Input Events")]
    public class InputEvents : UnityEvents
    {
        [Space]
        [Tooltip("Occurs when availability of the input manager engine service changes.")]
        public BoolUnityEvent ServiceAvailable;
        [Tooltip("Occurs when input is muted (true) or un-muted (false).")]
        public BoolUnityEvent InputMuted;
        [Tooltip("Occurs when input mode is changed. Integer is mapped as follows: 1 = mouse, 2 = keyboard, 3 = Gamepad, 4 = touch.")]
        public IntUnityEvent InputModeChanged;

        public void EnableInput () => SetInputEnabled(true);
        public void DisableInput () => SetInputEnabled(false);
        public void SetInputEnabled (bool enabled)
        {
            if (Engine.TryGetService<IInputManager>(out var input))
                input.Enabled = enabled;
        }

        public void SetInputMode (int mode)
        {
            if (Engine.TryGetService<IInputManager>(out var input))
                input.InputMode = (InputMode)mode;
        }

        public void MuteInput (string inputName)
        {
            if (Engine.TryGetService<IInputManager>(out var input))
                input.GetInputOrErr(inputName).Muted = true;
        }

        public void UnmuteInput (string inputName)
        {
            if (Engine.TryGetService<IInputManager>(out var input))
                input.GetInputOrErr(inputName).Muted = false;
        }

        public void PulseInput (string inputName)
        {
            if (Engine.TryGetService<IInputManager>(out var input))
                input.GetInputOrErr(inputName).Pulse();
        }

        protected override void HandleEngineInitialized ()
        {
            if (Engine.TryGetService<IInputManager>(out var input))
            {
                ServiceAvailable?.Invoke(true);

                input.OnMutedChanged -= InputMuted.SafeInvoke;
                input.OnMutedChanged += InputMuted.SafeInvoke;

                input.OnInputModeChanged -= HandleInputModeChanged;
                input.OnInputModeChanged += HandleInputModeChanged;
            }
            else ServiceAvailable?.Invoke(false);
        }

        protected override void HandleEngineDestroyed ()
        {
            ServiceAvailable?.Invoke(false);
        }

        protected virtual void HandleInputModeChanged (InputMode mode)
        {
            InputModeChanged?.Invoke((int)mode);
        }
    }
}
