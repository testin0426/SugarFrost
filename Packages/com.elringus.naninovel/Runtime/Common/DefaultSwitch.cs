namespace Naninovel
{
    /// <summary>
    /// Represents a boolean switch with a default/undefined state.
    /// </summary>
    public enum DefaultSwitch
    {
        /// <summary>
        /// Undefined, inherited from a default.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Explicitly enabled (on/true).
        /// </summary>
        Enable = 1,
        /// <summary>
        /// Explicitly disabled (off/false).
        /// </summary>
        Disable = 2
    }

    public static class DefaultSwitchExtensions
    {
        /// <summary>
        /// Resolves the switch to a boolean value.
        /// </summary>
        /// <param name="defaultValue">The value to return when the switch is set to default.</param>
        public static bool WithDefault (this DefaultSwitch switchValue, bool defaultValue) =>
            switchValue switch {
                DefaultSwitch.Enable => true,
                DefaultSwitch.Disable => false,
                _ => defaultValue
            };
    }
}
