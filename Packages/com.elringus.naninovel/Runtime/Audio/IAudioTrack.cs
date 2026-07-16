using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A playable audio track.
    /// </summary>
    public interface IAudioTrack
    {
        /// <summary>
        /// Invoked when the track has started playing.
        /// </summary>
        event Action OnPlay;
        /// <summary>
        /// Invoked when the track has finished playing or was stopped.
        /// </summary>
        event Action OnStop;

        /// <summary>
        /// Whether the track is currently playing.
        /// </summary>
        bool Playing { get; }
        /// <summary>
        /// Whether the track is looped (starts playing from start when finished).
        /// </summary>
        bool Loop { get; set; }
        /// <summary>
        /// Whether the track is muted (audio output is disabled, no matter <see cref="Volume"/> value).
        /// </summary>
        bool Mute { get; set; }
        /// <summary>
        /// Current volume of the track, in 0.0 to 1.0 range.
        /// </summary>
        float Volume { get; set; }

        /// <summary>
        /// Starts playing the track.
        /// </summary>
        void Play ();
        /// <summary>
        /// Stops playing the track.
        /// </summary>
        void Stop ();
        /// <summary>
        /// Fades <see cref="Volume"/> to the specified value over the specified time, in seconds.
        /// </summary>
        Awaitable Fade (float volume, float fadeTime, AsyncToken token = default);
    }
}
