using System;

namespace Naninovel
{
    /// <summary>
    /// Script playback preferences for <see cref="IScriptTrack"/> behaviour.
    /// </summary>
    [Serializable]
    public struct PlaybackOptions
    {
        /// <summary>
        /// Whether to complete executing commands on <see cref="Inputs.Continue"/> input.
        /// Default is controlled by <see cref="ScriptPlayerConfiguration.CompleteOnContinue"/>.
        /// </summary>
        public DefaultSwitch CompleteOnContinue;
        /// <summary>
        /// Whether to ignore <see cref="IScriptTrack.SetAwaitInput"/>
        /// and never wait for input before proceeding to execute next command.
        /// </summary>
        public bool DisableAwaitInput;
        /// <summary>
        /// Whether to completely disable the autoplay feature
        /// and ignore <see cref="IScriptPlayer.SetAutoPlay"/>.
        /// </summary>
        public bool DisableAutoPlay;
        /// <summary>
        /// Whether to completely disable the fast-forward playback feature
        /// and ignore <see cref="IScriptPlayer.SetSkip"/>.
        /// </summary>
        public bool DisableSkip;
        /// <summary>
        /// Whether to resume playing the script from the playback index when playback is finished,
        /// looping until the track is disposed with <see cref="IScriptPlayer.RemoveTrack"/>.
        /// </summary>
        public int? LoopAt;
        /// <summary>
        /// Whether the track should self-dispose when script playback is finished and all the commands
        /// (including un-awaited) are executed. Has no effect when <see cref="Loop"/> is enabled.
        /// </summary>
        public bool Dispose;
    }
}
