namespace Naninovel.StoryEditor
{
    /// <summary>
    /// Describes <see cref="StoryEditor"/> initialization phase.
    /// </summary>
    public enum StoryEditorState
    {
        /// <summary>
        /// The editor is deinitialized and can't process any commands except initialization.
        /// </summary>
        Standby = 0,
        /// <summary>
        /// The editor is currently initializing and can't process any commands.
        /// </summary>
        Initializing,
        /// <summary>
        /// The editor's native lib is initialized and ready to process a subset of commands,
        /// but the webview and JavaScript IPC are still initializing.
        /// </summary>
        NativeReady,
        /// <summary>
        /// The editor is fully initialized and ready to process all commands.
        /// </summary>
        Initialized,
        /// <summary>
        /// The editor is currently deinitializing and can't process any commands.
        /// </summary>
        Deinitializing
    }
}
