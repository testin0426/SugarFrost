using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="IScriptPlayer"/>.
    /// </summary>
    public static class ScriptPlayerExtensions
    {
        /// <summary>
        /// Whether the player currently has a track with the specified identifier.
        /// </summary>
        public static bool HasTrack (this IScriptPlayer player, string id)
        {
            return player.GetTrack(id) != null;
        }

        /// <summary>
        /// Returns a managed track instance with the specified identifier; throws when not found.
        /// </summary>
        public static IScriptTrack GetTrackOrErr (this IScriptPlayer player, string id)
        {
            return player.GetTrack(id) ?? throw new Error($"Failed to get '{id}' track: not found.");
        }

        /// <summary>
        /// Returns a managed track instance with the specified identifier; adds it when not found.
        /// </summary>
        public static IScriptTrack GetOrAddTrack (this IScriptPlayer player, string id, PlaybackOptions options = default)
        {
            return player.GetTrack(id) ?? player.AddTrack(id, options);
        }

        /// <summary>
        /// Rents a pooled list and collects all the managed track instances.
        /// </summary>
        public static IDisposable RentTracks (this IScriptPlayer player, out List<IScriptTrack> tracks)
        {
            var rent = ListPool<IScriptTrack>.Rent(out tracks);
            player.CollectTracks(tracks);
            return rent;
        }

        /// <summary>
        /// Compiles specified raw scenario text and plays the script asynchronously on a dedicated <see cref="IScriptTrack"/>.
        /// Use this method to execute one-off scenario scripts generated at runtime.
        /// </summary>
        /// <remarks>
        /// The method will register the generated script with <see cref="IScriptManager.AddTransientScript"/>,
        /// create a dedicated track to execute the script asynchronously, play it to the end, and remove the track.
        /// The transient script is unregistered (removed) automatically by <see cref="IScriptManager"/> when it's
        /// released by the last holder, such as the backlog holding reference to the printed text from the script.
        /// </remarks>
        /// <param name="text">The scenario script text to parse and execute.</param>
        /// <param name="name">An optional script label used to identify the script in error logs.</param>
        /// <param name="options">Optional playback preferences.</param>
        /// <param name="token">An optional async control token that can prematurely complete or cancel the playback.</param>
        /// <returns>A playback task that completes when the playback finishes.</returns>
        public static async Awaitable PlayTransient (this IScriptPlayer player, string text, string name = null,
            PlaybackOptions options = default, AsyncToken token = default)
        {
            var scriptPath = $"~{name}{(name != null ? "-" : "")}{CryptoUtils.PersistentHexCode(text)}";
            var trackId = $"~{name}{(name != null ? "-" : "")}{Guid.NewGuid():N}";
            var track = player.AddTrack(trackId, options);

            var scripts = Engine.GetServiceOrErr<IScriptManager>();
            if (!scripts.HasTransientScript(scriptPath))
                scripts.AddTransientScript(scriptPath, text);

            var loader = Engine.GetServiceOrErr<IScriptLoader>();
            await loader.Load(trackId, scriptPath);

            track.Play(scriptPath);
            try
            {
                while (token.EnsureNotCanceled() && (track.Executing || track.AwaitingInput))
                    if (token.Completed && !track.Completing && track.Executing) await track.Complete();
                    else await Async.NextFrame(token);
            }
            catch (OperationCanceledException) { return; }

            loader.Release(trackId);
            if (player.HasTrack(trackId))
                player.RemoveTrack(trackId);
        }
    }
}
