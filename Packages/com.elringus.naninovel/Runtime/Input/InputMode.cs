namespace Naninovel
{
    public enum InputMode
    {
        Mouse,
        Keyboard,
        Gamepad,
        Touch
    }

    public static class InputModeExtensions
    {
        public static bool IsPointerBased (this InputMode mode)
        {
            return mode == InputMode.Mouse || mode == InputMode.Touch;
        }
    }
}
