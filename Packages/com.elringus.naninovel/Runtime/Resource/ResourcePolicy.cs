namespace Naninovel
{
    /// <summary>
    /// Dictates the resources load/unload behaviour during script playback.
    /// </summary>
    public enum ResourcePolicy
    {
        /// <summary>
        /// The default mode with balanced memory utilization.
        /// All the resources required for script execution are preloaded when starting 
        /// the playback and unloaded when the script has finished playing.
        /// Scripts referenced in `@gosub` commands are preloaded as well.
        /// Additional scripts can be preloaded by using `hold` parameter of `@goto` command.
        /// </summary>
        Conservative,
        /// <summary>
        /// All the resources required by the played script, as well all resources of all the scripts
        /// specified in `@goto` and `@gosub` commands are preloaded and not unloaded unless `release`
        /// parameter is specified in `@goto` command. This minimizes loading screens and allows 
        /// smooth rollback, but requires manually specifying when the resources have to be unloaded,
        /// increasing risk of out of memory exceptions.
        /// </summary>
        Optimistic,
        /// <summary>
        /// No resources are preloaded for executed scripts when starting playback, and no loading screens
        /// are automatically shown. Instead, only the resources required for the next few commands are
        /// loaded "on the fly" while the script is playing, and resources used by executed commands
        /// are immediately released. This policy requires no scenario planning or manual control and
        /// consumes the least memory, but it may result in stutters during gameplay due to resources being
        /// loaded in the background—especially when fast-forwarding (skip mode) or performing rollback.
        /// </summary>
        Lazy
    }
}
