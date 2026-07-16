using System;
using JetBrains.Annotations;

namespace Naninovel
{
    /// <summary>
    /// Configures transient callback of a <see cref="Choice"/>.
    /// </summary>
    [Serializable]
    public struct TransientChoiceCallback
    {
        /// <summary>
        /// Raw scenario text to play as a transient script on choice.
        /// </summary>
        public string Scenario;
        /// <summary>
        /// Optional playback preferences.
        /// </summary>
        public PlaybackOptions Playback;
        /// <summary>
        /// When assigned, will resume script playback of a track with the specified identifier
        /// after the asynchronous callback execution is finished.
        /// </summary>
        [CanBeNull] public string ResumeTrackId;
    }
}
