using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Manages <see cref="Script"/> execution (playback) across <see cref="IScriptTrack"/> instances.
    /// </summary>
    public interface IScriptPlayer : IEngineService<ScriptPlayerConfiguration>
    {
        /// <summary>
        /// Occurs when the track starts or resumes playing <see cref="IScriptTrack.PlayedScript"/>.
        /// </summary>
        event Action<IScriptTrack> OnPlay;
        /// <summary>
        /// Occurs when the track stops playing <see cref="IScriptTrack.PlayedScript"/>.
        /// </summary>
        event Action<IScriptTrack> OnStop;
        /// <summary>
        /// Occurs when the track starts executing <see cref="IScriptTrack.PlayedCommand"/>.
        /// </summary>
        event Action<IScriptTrack> OnExecute;
        /// <summary>
        /// Occurs when the track finishes executing <see cref="IScriptTrack.PlayedCommand"/>.
        /// </summary>
        event Action<IScriptTrack> OnExecuted;
        /// <summary>
        /// Occurs when the <see cref="IScriptTrack.AwaitingInput"/> of the track changes.
        /// </summary>
        event Action<IScriptTrack> OnAwaitInput;
        /// <summary>
        /// Occurs when the <see cref="AutoPlaying"/> changes.
        /// </summary>
        event Action<bool> OnAutoPlay;
        /// <summary>
        /// Occurs when the <see cref="Skipping"/> changes.
        /// </summary>
        event Action<bool> OnSkip;

        /// <summary>
        /// Baseline track responsible for playing scenario scripts.
        /// </summary>
        /// <remarks>
        /// While the main track is used in most cases, additional tracks are required to execute scripts in parallel
        /// with the main track. For example, this is needed when using the '@async' command or when playing a transient
        /// scenario, such as with the <see cref="PlayScript"/> component. In contrast to these additional tracks, the
        /// main track is always available and can't be removed.
        /// </remarks>
        [NotNull] IScriptTrack MainTrack { get; }
        /// <summary>
        /// Current count of the track instances managed by the player.
        /// </summary>
        int TrackCount { get; }
        /// <summary>
        /// Whether any of the managed tracks is currently <see cref="IScriptTrack.Playing"/>.
        /// </summary>
        bool Playing { get; }
        /// <summary>
        /// Whether any of the managed tracks is currently <see cref="IScriptTrack.Executing"/>.
        /// </summary>
        bool Executing { get; }
        /// <summary>
        /// Whether any of the managed tracks is <see cref="IScriptTrack.AwaitingInput"/>.
        /// </summary>
        bool AwaitingInput { get; }
        /// <summary>
        /// Whether autoplay mode is currently active.
        /// </summary>
        bool AutoPlaying { get; }
        /// <summary>
        /// Whether fast-forward (skip) playback is currently active.
        /// </summary>
        bool Skipping { get; }
        /// <summary>
        /// The skip mode in which the playback is fast-forwarded (skipped).
        /// </summary>
        PlayerSkipMode SkipMode { get; set; }
        /// <summary>
        /// Total number of unique commands ever played by any of the tracks managed by the player (global state scope).
        /// </summary>
        int PlayedCommandsCount { get; }
        /// <summary>
        /// Whether the script hot-reload feature is enabled and <see cref="Reload"/> can be used.
        /// </summary>
        bool ReloadEnabled { get; set; }

        /// <summary>
        /// Adds a new track instance with the specified unique identifier,
        /// allowing to play scenario scripts in parallel with the <see cref="MainTrack"/>.
        /// </summary>
        /// <remarks>
        /// Don't forget to remove unused tracks, as each track is serialized with the game state.
        /// </remarks>
        IScriptTrack AddTrack (string id, PlaybackOptions options = default);
        /// <summary>
        /// Cancels played commands and removes a track with the specified identifier.
        /// </summary>
        void RemoveTrack (string id);
        /// <summary>
        /// Returns managed track instance with the specified identifier or null when not found.
        /// </summary>
        [CanBeNull] IScriptTrack GetTrack (string id);
        /// <summary>
        /// Collects all the managed track instances to the specified collection.
        /// </summary>
        void CollectTracks (ICollection<IScriptTrack> tracks);
        /// <summary>
        /// Enables or disables the auto play mode.
        /// </summary>
        void SetAutoPlay (bool enabled);
        /// <summary>
        /// Enables or disables the fast-forward (skip) playback.
        /// </summary>
        void SetSkip (bool enabled);
        /// <summary>
        /// Adds a task to perform before <see cref="IScriptTrack.PlayedCommand"/> of the track is executed.
        /// </summary>
        void AddPreExecutionTask (Func<IScriptTrack, Awaitable> task);
        /// <summary>
        /// Removes a task to perform before <see cref="IScriptTrack.PlayedCommand"/> of the track is executed.
        /// </summary>
        void RemovePreExecutionTask (Func<IScriptTrack, Awaitable> task);
        /// <summary>
        /// Adds a task to perform after <see cref="IScriptTrack.PlayedCommand"/> of the track is executed.
        /// </summary>
        void AddPostExecutionTask (Func<IScriptTrack, Awaitable> task);
        /// <summary>
        /// Removes a task to perform after <see cref="IScriptTrack.PlayedCommand"/> of the track is executed.
        /// </summary>
        void RemovePostExecutionTask (Func<IScriptTrack, Awaitable> task);
        /// <summary>
        /// Whether any of the tracks managed by the player has ever played a command at the specified
        /// script path and playlist index (global state). When index is not specified, will check
        /// if the script has ever played, at any index.
        /// </summary>
        bool HasPlayed (string scriptPath, int? playlistIndex = null);
        /// <summary>
        /// Reloads played scripts and associated resources. Make sure <see cref="ReloadEnabled"/> before using.
        /// Use to refresh the state after modifying the scenario script resources at runtime, aka "Hot Reload".
        /// </summary>
        Awaitable Reload ();
    }
}
