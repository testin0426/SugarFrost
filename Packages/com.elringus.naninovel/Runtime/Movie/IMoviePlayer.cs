using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to handle movie playing.
    /// </summary>
    public interface IMoviePlayer : IEngineService<MoviesConfiguration>
    {
        /// <summary>
        /// Occurs when playback is started.
        /// </summary>
        event Action OnMoviePlay;
        /// <summary>
        /// Occurs when playback is stopped.
        /// </summary>
        event Action OnMovieStop;

        /// <summary>
        /// Whether currently playing or preparing to play a movie.
        /// </summary>
        bool Playing { get; }

        /// <summary>
        /// Starts playing a movie with the specified local resource path.
        /// Returns texture to which the movie is rendered.
        /// </summary>
        Awaitable<Texture> Play (string moviePath, AsyncToken token = default);
        /// <summary>
        /// Stops the playback.
        /// </summary>
        void Stop ();
        /// <summary>
        /// Preloads the resources required to play a movie with the specified path.
        /// </summary>
        Awaitable HoldResources (string moviePath, object holder);
        /// <summary>
        /// Unloads the resources required to play a movie with the specified path.
        /// </summary>
        void ReleaseResources (string moviePath, object holder);
    }
}
