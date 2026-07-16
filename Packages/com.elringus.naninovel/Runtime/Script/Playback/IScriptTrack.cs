using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Naninovel.Commands;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A persistent <see cref="Script"/> playback instance managed by <see cref="IScriptPlayer"/>.
    /// </summary>
    /// <remarks>
    /// The track has two related but distinct concepts: playback and execution.<br/><br/>
    /// 
    /// Playback refers to the track "walking" through the script. When <see cref="Playing"/> is true,
    /// the track is executing <see cref="PlayedCommand"/> at <see cref="PlayedIndex"/> and will move
    /// to the next command in the <see cref="Playlist"/> once finished. When <see cref="Playing"/> is false,
    /// the track won't advance on its own, either because it was stopped or because it has finished
    /// playing the script and has nowhere further to go.<br/><br/>
    /// 
    /// Execution, on the other hand, refers to awaiting the completion of <see cref="Command.Execute"/>.
    /// The track is <see cref="Executing"/> if there is at least one command still in progress, including
    /// any un-awaited commands previously played by the track.<br/><br/>
    /// 
    /// This means the track may not be <see cref="Playing"/> while still being <see cref="Executing"/>—
    /// for example, when playback has stopped or the last command has already been played, but some
    /// earlier un-awaited commands are still running. Conversely, if the track is <see cref="Playing"/>,
    /// it is always also <see cref="Executing"/>, since the <see cref="PlayedCommand"/> is in progress.
    /// </remarks>
    public interface IScriptTrack
    {
        /// <summary>
        /// Unique persistent identifier of the track instance.
        /// </summary>
        string Id { get; }
        /// <summary>
        /// Whether the playback routine is currently running.
        /// </summary>
        bool Playing { get; }
        /// <summary>
        /// Whether the track is executing any commands, including un-awaited.
        /// </summary>
        bool Executing { get; }
        /// <summary>
        /// Whether the track is currently handling <see cref="Complete"/> request,
        /// ie waiting for all the executing async commands to finish.
        /// </summary>
        bool Completing { get; }
        /// <summary>
        /// Whether commands executed by the track are allowed to be instantly completed on 'Continue' input.
        /// </summary>
        bool CompleteOnContinue { get; }
        /// <summary>
        /// Whether 'Continue' input is currently required to execute the next script command, aka 'ctc' prompt.
        /// </summary>
        bool AwaitingInput { get; }
        /// <summary>
        /// Currently played <see cref="Script"/> or null when not playing any.
        /// </summary>
        Script PlayedScript { get; }
        /// <summary>
        /// Currently played <see cref="Command"/> or null when not playing any.
        /// </summary>
        Command PlayedCommand { get; }
        /// <summary>
        /// Currently played <see cref="Naninovel.PlaybackSpot"/> or default when not playing any.
        /// </summary>
        PlaybackSpot PlaybackSpot { get; }
        /// <summary>
        /// List of <see cref="Command"/> built upon the currently played <see cref="Script"/>.
        /// </summary>
        ScriptPlaylist Playlist { get; }
        /// <summary>
        /// Index of the currently played command within the <see cref="Playlist"/>.
        /// </summary>
        int PlayedIndex { get; }
        /// <summary>
        /// Last playback return spots stack registered by <see cref="Gosub"/> commands.
        /// <see cref="Naninovel.PlaybackSpot.Invalid"/> means the return finishes script execution.
        /// </summary>
        Stack<PlaybackSpot> GosubReturnSpots { get; }

        /// <summary>
        /// Starts executing a script with the specified local resource path.
        /// </summary>
        /// <remarks>
        /// Make sure script resource at the specified path is loaded via <see cref="IScriptManager.ScriptLoader"/>
        /// before attempting to play it.
        /// </remarks>
        /// <param name="scriptPath">Local resource path of the script to execute.</param>
        /// <param name="playlistIndex">Playlist index of the executed script to start from.</param>
        void Play (string scriptPath, int playlistIndex = 0);
        /// <summary>
        /// Resumes <see cref="PlayedScript"/> playback at <paramref name="playlistIndex"/> when specified,
        /// or at <see cref="PlayedIndex"/>.
        /// </summary>
        /// <param name="playlistIndex">The playback index in <see cref="Playlist"/> to resume playback from.</param>
        void Resume (int? playlistIndex = null);
        /// <summary>
        /// Halts the playback of the currently played script.
        /// </summary>
        void Stop ();
        /// <summary>
        /// Depending on whether the specified line index being before or after currently played command's line index,
        /// performs a fast-forward playback or state rollback of the currently loaded script.
        /// </summary>
        /// <param name="lineIndex">The line index to rewind at.</param>
        /// <returns>Whether the <paramref name="lineIndex"/> has been reached.</returns>
        Awaitable<bool> Rewind (int lineIndex);
        /// <summary>
        /// Enables or disables 'awaiting input' mode, when the track waits for a 'Continue' input before
        /// executing the next command, aka click-to-continue or 'ctc' prompt.
        /// </summary>
        void SetAwaitInput (bool enabled);
        /// <summary>
        /// Collects all the commands that are currently being executed to the specified collection,
        /// including un-awaited commands executing concurrently with the <see cref="PlayedCommand"/>.
        /// </summary>
        void CollectExecutedCommands (ICollection<Command> commands);
        /// <summary>
        /// Notifies the track that the command completes when the specified task is complete.
        /// </summary>
        /// <remarks>
        /// Command implementations are expected to register via this method when their <see cref="Command.Execute"/>
        /// method doesn't represent the full execution time. Usually this is the case when the commands are executed
        /// asynchronously due to wait parameter being disabled.
        /// </remarks>
        void RegisterCompletion (Command command, Awaitable task);
        /// <summary>
        /// Requests executing commands (including un-awaited) to be completed ASAP
        /// (via <see cref="AsyncToken.Completed"/>) and waits for them to finish before performing the specified
        /// task (if any) and executing next commands.
        /// </summary>
        /// <param name="filter">When specified, requests completion only for the commands that pass the filter.</param>
        /// <param name="onComplete">The task to invoke after the commands are completed.</param>
        Awaitable Complete ([CanBeNull] Predicate<Command> filter = null, [CanBeNull] Func<Awaitable> onComplete = null);
    }
}
