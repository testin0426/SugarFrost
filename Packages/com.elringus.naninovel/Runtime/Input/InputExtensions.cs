namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="IInputHandle"/>.
    /// </summary>
    public static class InputExtensions
    {
        /// <inheritdoc cref="IInputHandle.Activate"/>
        public static void Activate (this IInputHandle i, float force) => i.Activate(new(force, force));

        /// <summary>
        /// Simulates a momentary on->off activation of the input and triggers associated callbacks.
        /// </summary>
        public static void Pulse (this IInputHandle i)
        {
            i.Activate(1);
            i.Activate(0);
        }
    }
}
