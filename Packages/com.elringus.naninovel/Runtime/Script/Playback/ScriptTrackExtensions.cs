using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="IScriptTrack"/>.
    /// </summary>
    public static class ScriptTrackExtensions
    {
        private static readonly Dictionary<string, Command> transientCommandsById = new(StringComparer.Ordinal);

        /// <summary>
        /// Loads a script with the specified local resource path and starts executing it from the specified playlist index.
        /// </summary>
        /// <remarks>Load progress is reported by <see cref="IScriptLoader.OnLoadProgress"/> event.</remarks>
        /// <param name="scriptPath">Local resource path of the script to preload and execute.</param>
        /// <param name="playlistIndex">Playlist index of the executed script to start from.</param>
        public static async Awaitable LoadAndPlay (this IScriptTrack track, string scriptPath, int playlistIndex = 0)
        {
            var loader = Engine.GetServiceOrErr<IScriptLoader>();
            await loader.Load(track.Id, scriptPath, playlistIndex);
            track.Play(scriptPath, playlistIndex);
        }

        /// <summary>
        /// Loads a script with the specified local resource path and starts executing at the specified line and inline indexes.
        /// </summary>
        /// <remarks>Load progress is reported by <see cref="IScriptLoader.OnLoadProgress"/> event.</remarks>
        /// <param name="scriptPath">Local resource path of the script to preload and execute.</param>
        /// <param name="lineIndex">Line index to start playback from.</param>
        /// <param name="inlineIndex">Command inline index to start playback from.</param>
        public static async Awaitable LoadAndPlayAtLine (this IScriptTrack track, string scriptPath,
            int lineIndex, int inlineIndex)
        {
            var scripts = Engine.GetServiceOrErr<IScriptManager>();
            var script = (Script)await scripts.ScriptLoader.LoadOrErr(scriptPath, track);
            var playIdx = script.Playlist.GetIndexByLine(lineIndex, inlineIndex);
            if (playIdx == -1) throw new Error($"Failed to start '{scriptPath}' script playback at line #{lineIndex}.{inlineIndex}: no playable commands found at or after the line.");
            await track.LoadAndPlay(scriptPath, playIdx);
        }

        /// <summary>
        /// Loads a script with the specified local resource path and starts executing from the specified label.
        /// </summary>
        /// <remarks>Load progress is reported by <see cref="IScriptLoader.OnLoadProgress"/> event.</remarks>
        /// <param name="scriptPath">Local resource path of the script to preload and execute.</param>
        /// <param name="label">Label within the script to start playback from.</param>
        public static async Awaitable LoadAndPlayAtLabel (this IScriptTrack track, string scriptPath, string label)
        {
            if (label.StartsWith(Compiler.Symbols.LabelLine[0])) label = label[1..];
            var scripts = Engine.GetServiceOrErr<IScriptManager>();
            var script = (Script)await scripts.ScriptLoader.LoadOrErr(scriptPath, track);
            var lineIdx = script.GetLineIndexForLabel(label);
            if (lineIdx == -1) throw new Error($"Failed to start '{scriptPath}' script playback from '{label}' label: label not found.");
            await track.LoadAndPlayAtLine(scriptPath, lineIdx, 0);
        }

        /// <summary>
        /// Starts playback of a script with the specified local resource path, starting at specified line and inline indexes.
        /// </summary>
        /// <remarks>
        /// Make sure script resource at the specified path is loaded via <see cref="IScriptManager.ScriptLoader"/> before attempting to play it.
        /// </remarks>
        public static void PlayAtLine (this IScriptTrack track, string scriptPath, int lineIndex, int inlineIndex)
        {
            var scripts = Engine.GetServiceOrErr<IScriptManager>();
            var script = scripts.ScriptLoader.GetLoadedOrErr(scriptPath);
            var playIdx = script.Playlist.GetIndexByLine(lineIndex, inlineIndex);
            if (playIdx == -1) throw new Error($"Failed to start '{scriptPath}' script playback at line #{lineIndex}.{inlineIndex}: no playable commands found at or after the line.");
            track.Play(scriptPath, playIdx);
        }

        /// <summary>
        /// Starts playback of a script with the specified local resource path, starting at the specified label.
        /// </summary>
        /// <remarks>
        /// Make sure script resource at the specified path is loaded via <see cref="IScriptManager.ScriptLoader"/> before attempting to play it.
        /// </remarks>
        public static void PlayAtLabel (this IScriptTrack track, string scriptPath, string label)
        {
            var scripts = Engine.GetServiceOrErr<IScriptManager>();
            if (label.StartsWith(Compiler.Symbols.LabelLine[0])) label = label[1..];
            var script = scripts.ScriptLoader.GetLoadedOrErr(scriptPath);
            var lineIdx = script.GetLineIndexForLabel(label);
            if (lineIdx == -1) throw new Error($"Failed to start '{scriptPath}' script playback from '{label}' label: label not found.");
            track.PlayAtLine(scriptPath, lineIdx, 0);
        }

        /// <summary>
        /// Resumes <see cref="PlayedScript"/> playback at specified line and inline indexes.
        /// </summary>
        /// <param name="lineIndex">Line index to start playback from.</param>
        /// <param name="inlineIndex">Command inline index to start playback from.</param>
        public static void ResumeAtLine (this IScriptTrack track, int lineIndex, int inlineIndex = 0)
        {
            if (!track.PlayedScript) throw new Error("Failed to resume playback: the track doesn't have a played script.");
            var playIdx = track.PlayedScript.Playlist.GetIndexByLine(lineIndex, inlineIndex);
            if (playIdx == -1) throw new Error($"Failed to resume '{track.PlayedScript.Path}' script playback at line #{lineIndex}.{inlineIndex}: no playable commands found at or after the line.");
            track.Resume(playIdx);
        }

        /// <summary>
        /// Resumes <see cref="PlayedScript"/> playback at specified label.
        /// </summary>
        /// <param name="label">Label within the script to start playback from.</param>
        public static void ResumeAtLabel (this IScriptTrack track, string label)
        {
            if (label.StartsWith(Compiler.Symbols.LabelLine[0])) label = label[1..];
            if (!track.PlayedScript) throw new Error("Failed to resume playback: the track doesn't have a played script.");
            var lineIdx = track.PlayedScript.GetLineIndexForLabel(label);
            if (lineIdx == -1) throw new Error($"Failed to start '{track.PlayedScript.Path}' script playback from '{label}' label: label not found.");
            track.ResumeAtLine(lineIdx);
        }

        /// <summary>
        /// Checks whether the currently played command has a lower indentation level
        /// than the next one, ie playback would enter nested block.
        /// </summary>
        public static bool IsEnteringNested (this IScriptTrack track)
        {
            return track.Playlist.IsEnteringNestedAt(track.PlayedIndex);
        }

        /// <summary>
        /// Rents a pooled list and collects all the commands that are currently being executed,
        /// including un-awaited commands playing concurrently with the <see cref="PlayedCommand"/>.
        /// </summary>
        public static IDisposable RentExecutedCommands (this IScriptTrack track, out List<Command> commands)
        {
            var rent = ListPool<Command>.Rent(out commands);
            track.CollectExecutedCommands(commands);
            return rent;
        }

        /// <summary>
        /// Compiles specified raw command body text and executes the command on the script track.
        /// Use this method to execute one-off script commands generated at runtime.
        /// </summary>
        /// <param name="text">The source scenario text of the command body (without the leading '@').</param>
        /// <param name="name">An optional label used to identify the activity in error logs.</param>
        /// <param name="token">An optional async control token that can prematurely complete or cancel the execution.</param>
        /// <returns>A playback task that completes when the command execution finishes.</returns>
        public static Awaitable ExecuteTransientCommand (this IScriptTrack track, string text,
            string name = null, AsyncToken token = default)
        {
            var id = $"~{name}{(name != null ? "-" : "")}{text}";
            var command = CompileOrGetCached(id, text);
            return command.Execute(new(track, token));

            static Command CompileOrGetCached (string id, string text)
            {
                if (transientCommandsById.TryGetValue(id, out var cached)) return cached;
                if (Compiler.CompileCommand(text, new(id, 0, 0)) is not { } cmd)
                    throw new Error($"Failed to execute '{text}' transient command.");
                return transientCommandsById[id] = cmd;
            }
        }
    }
}
