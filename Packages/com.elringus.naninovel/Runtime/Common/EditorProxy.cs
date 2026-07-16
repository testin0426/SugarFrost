namespace Naninovel
{
    /// <summary>
    /// Shares editor state with the runtime.
    /// </summary>
    public static class EditorProxy
    {
        /// <summary>
        /// Whether the story editor is currently focused.
        /// </summary>
        /// <remarks>
        /// This hack is required because we can't move the editor focus out of the game view when user starts
        /// interacting with the story editor's embedded webview without stealing the OS focus from the webview.
        /// The issue is that the OS focus may be on the webview, but editor's focus remains on the game view,
        /// hence Unity continues processing keyboard events in-game. This flag is used to mute the in-game
        /// input processing while user is interacting with the webview.
        /// </remarks>
        public static bool StoryEditorFocused { get; set; }
    }
}
