using System;

namespace Naninovel
{
    /// <summary>
    /// Configures nested callback of a <see cref="Choice"/>.
    /// </summary>
    [Serializable]
    public struct NestedChoiceCallback
    {
        /// <summary>
        /// Playback spot of the "@choice" command hosting the nested callback.
        /// </summary>
        public PlaybackSpot HostedAt;
        /// <summary>
        /// Identifier of a <see cref="IScriptTrack"/> on which to execute the callback.
        /// </summary>
        public string TrackId;
    }
}
