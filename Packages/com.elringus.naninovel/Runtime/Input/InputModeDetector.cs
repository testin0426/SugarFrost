using System;

namespace Naninovel
{
    /// <summary>
    /// Handles <see cref="InputMode"/> detection based on last active input device.
    /// </summary>
    public class InputModeDetector : IDisposable
    {
        // ReSharper disable once NotAccessedField.Local (used with new input)
        protected virtual IInputManager Manager { get; }

        public InputModeDetector (IInputManager manager)
        {
            Manager = manager;
        }

        public virtual void Start ()
        {
            #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
            UnityEngine.InputSystem.InputSystem.onEvent += HandleInputEvent;
            #endif
        }

        public virtual void Stop ()
        {
            #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
            UnityEngine.InputSystem.InputSystem.onEvent -= HandleInputEvent;
            #endif
        }

        public virtual void Dispose () => Stop();

        #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
        protected virtual void HandleInputEvent (UnityEngine.InputSystem.LowLevel.InputEventPtr ptr,
            UnityEngine.InputSystem.InputDevice device)
        {
            if (ShouldChangeToMouse(device)) Manager.InputMode = InputMode.Mouse;
            else if (ShouldChangeToKeyboard(device)) Manager.InputMode = InputMode.Keyboard;
            else if (ShouldChangeToTouch(device)) Manager.InputMode = InputMode.Touch;
            else if (ShouldChangeToGamepad(device)) Manager.InputMode = InputMode.Gamepad;
        }

        protected virtual bool ShouldChangeToMouse (UnityEngine.InputSystem.InputDevice device)
        {
            if (Manager.InputMode == InputMode.Mouse) return false;
            if (device is not UnityEngine.InputSystem.Mouse mouse) return false;
            return mouse.leftButton.isPressed || mouse.rightButton.isPressed ||
                   mouse.middleButton.isPressed;
        }

        protected virtual bool ShouldChangeToKeyboard (UnityEngine.InputSystem.InputDevice device)
        {
            if (Manager.InputMode == InputMode.Keyboard) return false;
            if (device is not UnityEngine.InputSystem.Keyboard board) return false;
            // Change only on navigation keys, as others are used as hotkeys in mouse input mode.
            return board.upArrowKey.isPressed || board.downArrowKey.isPressed ||
                   board.leftArrowKey.isPressed || board.rightArrowKey.isPressed;
        }

        protected virtual bool ShouldChangeToTouch (UnityEngine.InputSystem.InputDevice device)
        {
            if (Manager.InputMode == InputMode.Touch) return false;
            return device is UnityEngine.InputSystem.Touchscreen touch && touch.primaryTouch.isInProgress;
        }

        protected virtual bool ShouldChangeToGamepad (UnityEngine.InputSystem.InputDevice device)
        {
            if (Manager.InputMode == InputMode.Gamepad) return false;
            if (device is not UnityEngine.InputSystem.Gamepad pad) return false;
            return pad.buttonNorth.isPressed ||
                   pad.buttonEast.isPressed ||
                   pad.buttonSouth.isPressed ||
                   pad.buttonWest.isPressed ||
                   pad.dpad.down.isPressed ||
                   pad.dpad.up.isPressed ||
                   pad.dpad.left.isPressed ||
                   pad.dpad.right.isPressed ||
                   pad.leftStick.down.isPressed ||
                   pad.leftStick.up.isPressed ||
                   pad.leftStick.left.isPressed ||
                   pad.leftStick.right.isPressed ||
                   pad.rightStick.down.isPressed ||
                   pad.rightStick.up.isPressed ||
                   pad.rightStick.left.isPressed ||
                   pad.rightStick.right.isPressed ||
                   pad.leftTrigger.isPressed ||
                   pad.rightTrigger.isPressed ||
                   pad.leftShoulder.isPressed ||
                   pad.rightShoulder.isPressed ||
                   pad.selectButton.isPressed ||
                   pad.startButton.isPressed;
        }
        #endif
    }
}
