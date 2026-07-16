namespace Naninovel
{
    /// <summary>
    /// Implementation is able to transform source scenario text into <see cref="Script"/>.
    /// </summary>
    public interface IScriptCompiler
    {
        /// <summary>
        /// Creates a new <see cref="Script"/> by compiling the specified source scenario text.
        /// </summary>
        /// <param name="path">Unique (project-wide) local resource path of the script.</param>
        /// <param name="text">The source scenario text to compile.</param>
        /// <param name="options">Optional preferences for the compiler behaviour.</param>
        Script CompileScript (string path, string text, CompileOptions options = default);
        /// <summary>
        /// Creates a new <see cref="Command"/> by compiling the specified source command body text.
        /// </summary>
        /// <param name="text">The source text of the command body (without the leading '@').</param>
        /// <param name="spot">Optional playback spot to associate with the command.</param>
        /// <param name="options">Optional preferences for the compiler behaviour.</param>
        Command CompileCommand (string text, PlaybackSpot spot = default, CompileOptions options = default);
    }
}
