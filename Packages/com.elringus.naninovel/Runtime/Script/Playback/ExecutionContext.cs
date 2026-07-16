namespace Naninovel
{
    /// <summary>
    /// Script <see cref="Command"/> playback context associated with
    /// the <see cref="IScriptTrack"/> that is executing the command.
    /// </summary>
    public readonly struct ExecutionContext
    {
        /// <summary>
        /// Script track that executes the command.
        /// </summary>
        public IScriptTrack Track { get; }
        /// <summary>
        /// Controls the asynchronous execution of the command;
        /// see <see href="https://naninovel.com/guide/custom-commands#asynctoken"/> for more info.
        /// </summary>
        public AsyncToken Token { get; }

        public ExecutionContext (IScriptTrack track, AsyncToken token = default)
        {
            Track = track;
            Token = token;
        }
    }
}
