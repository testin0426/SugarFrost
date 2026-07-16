using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Contain common playback controls embedded inside text printers.
    /// </summary>
    public class ControlPanel : MonoBehaviour
    {
        [Tooltip("Activated when input mode is not gamepad.")]
        [SerializeField] private GameObject buttons;
        [Tooltip("Activated when input mode is gamepad.")]
        [SerializeField] private GameObject gamepad;
        [Tooltip("Activated when input mode is keyboard.")]
        [SerializeField] private GameObject keyboard;

        protected virtual void OnEnable ()
        {
            var input = Engine.GetServiceOrErr<IInputManager>();
            input.OnInputModeChanged += HandleInputModeChanged;
            HandleInputModeChanged(input.InputMode);
        }

        protected void OnDisable ()
        {
            if (Engine.GetService<IInputManager>() is { } input)
                input.OnInputModeChanged -= HandleInputModeChanged;
        }

        protected virtual void HandleInputModeChanged (InputMode mode)
        {
            if (buttons) buttons.SetActive(mode != InputMode.Gamepad && mode != InputMode.Keyboard);
            if (gamepad) gamepad.SetActive(mode == InputMode.Gamepad);
            if (keyboard) keyboard.SetActive(mode == InputMode.Keyboard);
        }
    }
}
