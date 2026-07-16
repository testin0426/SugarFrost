namespace Naninovel
{
    /// <summary>
    /// The type of a <see cref="Choice"/> callback.
    /// </summary>
    public enum ChoiceCallbackType
    {
        /// <summary>
        /// Specified as static directives to execute when the choice is handled.
        /// </summary>
        Directive,
        /// <summary>
        /// Specified as a raw scenario text, which is executed asynchronously
        /// as a transient script when the choice is handled.
        /// </summary>
        Transient,
        /// <summary>
        /// Specified as a reference to a playback spot of a "@choice" command,
        /// which hosts a script content to execute when the choice is handled.
        /// </summary>
        Nested
    }
}
