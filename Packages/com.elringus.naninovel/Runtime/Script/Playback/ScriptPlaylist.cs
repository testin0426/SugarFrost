using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents a list of <see cref="Command"/> based on the contents of a <see cref="Script"/>.
    /// </summary>
    [Serializable]
    public class ScriptPlaylist : IReadOnlyList<Command>
    {
        /// <summary>
        /// Local resource path of the script from which the contained commands were extracted.
        /// </summary>
        public string ScriptPath => scriptPath;
        /// <summary>
        /// Number of commands in the playlist.
        /// </summary>
        public int Count => commands.Count;

        [SerializeField] private string scriptPath;
        [SerializeReference] private List<Command> commands;

        /// <summary>
        /// Creates a new instance from the specified commands collection.
        /// </summary>
        public ScriptPlaylist (string scriptPath, List<Command> commands)
        {
            this.scriptPath = scriptPath;
            this.commands = commands;
        }

        public Command this [int index] => commands[index];
        public IEnumerator<Command> GetEnumerator () => commands.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator () => GetEnumerator();
        public Command Find (Predicate<Command> predicate) => commands.Find(predicate);
        public int FindIndex (Predicate<Command> predicate) => commands.FindIndex(predicate);
        public List<Command> GetRange (int index, int count) => commands.GetRange(index, count);
        public bool IsIndexValid (int index) => commands.IsIndexValid(index);

        /// <summary>
        /// Preloads and holds all the resources required to execute
        /// <see cref="Command.IPreloadable"/> commands contained in this list.
        /// </summary>
        public async Awaitable LoadResources () => await LoadResources(0, Count - 1);

        /// <summary>
        /// Preloads and holds resources required to execute
        /// <see cref="Command.IPreloadable"/> commands in the specified range.
        /// </summary>
        public async Awaitable LoadResources (int startCommandIndex, int endCommandIndex, Action<float> onProgress = default)
        {
            if (Count == 0)
            {
                onProgress?.Invoke(1);
                return;
            }

            if (!IsIndexValid(startCommandIndex) || !IsIndexValid(endCommandIndex) || endCommandIndex < startCommandIndex)
                throw new Error($"Failed to preload '{ScriptPath}' script resources: [{startCommandIndex}, {endCommandIndex}] is not a valid range.");

            var count = endCommandIndex + 1 - startCommandIndex;
            var commandsToHold = GetRange(startCommandIndex, count).OfType<Command.IPreloadable>().ToArray();

            if (commandsToHold.Length == 0)
            {
                onProgress?.Invoke(1);
                return;
            }

            onProgress?.Invoke(0);
            var heldCommands = 0;
            await Async.All(commandsToHold.Select(PreloadCommand));

            async Awaitable PreloadCommand (Command.IPreloadable command)
            {
                await command.PreloadResources();
                onProgress?.Invoke(++heldCommands / (float)commandsToHold.Length);
            }
        }

        /// <summary>
        /// Releases all the held resources required to execute
        /// <see cref="Command.IPreloadable"/> commands contained in this list.
        /// </summary>
        public virtual void ReleaseResources () => ReleaseResources(0, commands.Count - 1);

        /// <summary>
        /// Releases all the held resources required to execute
        /// <see cref="Command.IPreloadable"/> commands in the specified range.
        /// </summary>
        public void ReleaseResources (int startCommandIndex, int endCommandIndex)
        {
            if (Count == 0) return;

            if (!IsIndexValid(startCommandIndex) || !IsIndexValid(endCommandIndex) || endCommandIndex < startCommandIndex)
                throw new Error($"Failed to unload '{ScriptPath}' script resources: [{startCommandIndex}, {endCommandIndex}] is not a valid range.");

            var commandsToRelease = GetRange(startCommandIndex, (endCommandIndex + 1) - startCommandIndex).OfType<Command.IPreloadable>();
            foreach (var cmd in commandsToRelease)
                cmd.ReleaseResources();
        }

        /// <summary>
        /// Returns a <see cref="Command"/> at the specified playlist/playback index; null if not found.
        /// </summary>
        public Command GetCommandByIndex (int index) =>
            IsIndexValid(index) ? commands[index] : null;

        /// <summary>
        /// Finds a <see cref="Command"/> that was created from a <see cref="CommandLine"/>
        /// with specified line and inline indexes; null if not found.
        /// </summary>
        public Command GetCommandByLine (int lineIndex, int inlineIndex) =>
            Find(a => a.PlaybackSpot.LineIndex == lineIndex && a.PlaybackSpot.InlineIndex == inlineIndex);

        /// <summary>
        /// Finds a <see cref="Command"/> that was created from a <see cref="CommandLine"/>
        /// located at or after the specified line and inline indexes; null if not found.
        /// </summary>
        public Command GetCommandAfterLine (int lineIndex, int inlineIndex) =>
            commands.FirstOrDefault(a => a.PlaybackSpot.LineIndex >= lineIndex && a.PlaybackSpot.InlineIndex >= inlineIndex);

        /// <summary>
        /// Finds a <see cref="Command"/> that was created from a <see cref="CommandLine"/>
        /// located at or before the specified line and inline indexes; null if not found.
        /// </summary>
        public Command GetCommandBeforeLine (int lineIndex, int inlineIndex) =>
            commands.LastOrDefault(a => a.PlaybackSpot.LineIndex <= lineIndex && a.PlaybackSpot.InlineIndex <= inlineIndex);

        /// <summary>
        /// Returns the first command in the list or null when the list is empty.
        /// </summary>
        public Command GetFirstCommand () => commands.FirstOrDefault();

        /// <summary>
        /// Returns the last command in the list or null when the list is empty.
        /// </summary>
        public Command GetLastCommand () => commands.LastOrDefault();

        /// <summary>
        /// Finds index of a contained command with the specified playback spot or -1 when not found.
        /// </summary>
        public int IndexOf (PlaybackSpot spot) => FindIndex(c => c.PlaybackSpot == spot);

        /// <summary>
        /// Finds playback (command) index at or after specified line and inline indexes or -1 when not found.
        /// </summary>
        public int GetIndexByLine (int lineIndex, int? inlineIndex = null)
        {
            var startCommand = GetCommandAfterLine(lineIndex, inlineIndex ?? -1);
            return startCommand != null ? this.IndexOf(startCommand) : -1;
        }

        /// <summary>
        /// Checks whether a command at the specified playback index has any nested commands.
        /// </summary>
        public bool HasNested (int hostIndex)
        {
            var hostCmd = GetCommandByIndex(hostIndex);
            var nextCmd = GetCommandByIndex(hostIndex + 1);
            return hostCmd != null && nextCmd != null && nextCmd.Indent > hostCmd.Indent;
        }

        /// <summary>
        /// Checks whether the specified command has any nested commands.
        /// </summary>
        public bool HasNested (Command command)
        {
            return HasNested(IndexOf(command.PlaybackSpot));
        }

        /// <summary>
        /// Given command under the specified playback index is nested (ie, has indent above 0), 
        /// finds index of the command with the specified indent level which hosts the nested command.
        /// When host indent is not specified, looks for the nearest host. Throws when not found.
        /// </summary>
        public int GetNestedHostIndex (int nestedIndex, int? hostIndent = null)
        {
            var nested = GetCommandByIndex(nestedIndex);
            if (nested == null) throw new Error($"Failed to get host of the command nested at '{nestedIndex}' index: invalid playlist index.");
            var indent = hostIndent ?? nested.Indent - 1;
            for (int i = nestedIndex - 1; i >= 0; i--)
                if (this[i].Indent == indent && this[i] is Command.INestedHost)
                    return i;
            throw Engine.Fail("Failed to find host of nested command. Make sure scenario script is indented correctly.", nested.PlaybackSpot);
        }

        /// <summary>
        /// Given command under the specified playback index is nested (ie, has indent above 0), 
        /// finds the command with the specified indent level which hosts the nested command.
        /// When host indent is not specified, looks for the nearest host. Throws when not found.
        /// </summary>
        public Command GetNestedHost (int nestedIndex, int? hostIndent = null)
        {
            return this[GetNestedHostIndex(nestedIndex, hostIndent)];
        }

        /// <summary>
        /// Given specified command is nested (ie, has indent above 0), finds the command
        /// which hosts the nested command. Returns null when the host is not found, the
        /// command is not indented, or the command is not part of this playlist.
        /// </summary>
        [CanBeNull]
        public Command FindNestedHost (Command command)
        {
            if (command.Indent == 0) return null;
            var idx = commands.IndexOf(command);
            return idx < 0 ? null : GetNestedHost(idx);
        }

        /// <summary>
        /// Checks whether the specified command is nested at any level under
        /// the specified host command, ie the nested command is either direct
        /// or indirect descender of the host command.
        /// </summary>
        public bool IsNestedUnder (Command nested, Command under)
        {
            var host = FindNestedHost(nested);
            while (host is not null)
                if (host == under) return true;
                else host = FindNestedHost(host);
            return false;
        }

        /// <summary>
        /// Checks whether command after specified index has higher indentation level
        /// than command at the specified index, ie playback would enter nested block.
        /// </summary>
        public bool IsEnteringNestedAt (int index)
        {
            var command = GetCommandByIndex(index);
            var nextCommand = GetCommandByIndex(index + 1);
            if (command == null || nextCommand == null) return false;
            return nextCommand.Indent > command.Indent;
        }

        /// <summary>
        /// Checks whether command after specified index is exiting nested block
        /// of specified indent level.
        /// </summary>
        public bool IsExitingNestedAt (int index, int hostIndent)
        {
            var command = GetCommandByIndex(index);
            var nextCommand = GetCommandByIndex(index + 1);
            if (command == null) return false;
            if (nextCommand == null) return command.Indent > 0;
            return nextCommand.Indent <= hostIndent;
        }

        /// <summary>
        /// Given command under the specified index is nested under host with specified indent,
        /// finds index of the last command in the nested block.
        /// </summary>
        public int GetNestedExitIndexAt (int index, int hostIndent)
        {
            for (int i = index; i < Count; i++)
                if (IsExitingNestedAt(i, hostIndent))
                    return i;
            return Count - 1;
        }

        /// <summary>
        /// Given command under the specified index is nested (ie, has indent above 0) 
        /// and is the last command in the nested block with specified indent, resolves index
        /// of the next command to execute based on specifics of the outer block(s), if any.
        /// In case no outer blocks are found, returns the next index.
        /// </summary>
        /// <remarks>
        /// This is the default exit handler for nest hosts, which don't have any special
        /// exit behaviour; basically, this checks if the current host is nested under another
        /// host, which may have custom exit behaviour (eg, @choice) and invoke it.
        /// </remarks>
        public int ExitNestedAt (int nestedIndex, int hostIndent)
        {
            // Resolve index via the host of the played command's host (outer host),
            // as next command may be n levels of nesting below the current block, eg:
            // ```
            // @if (outer host 1)
            //     @if (outer host 2)
            //         @if (this host)
            //             (this command)
            // (next command)
            // ```
            // — if we resolved host of the next command, exit behaviour of outer host 2 would be skipped.

            if (hostIndent == 0) return nestedIndex + 1;
            var outerHost = GetNestedHost(nestedIndex, hostIndent - 1);
            return outerHost.GetNextPlaybackIndex(this, nestedIndex);
        }

        /// <summary>
        /// Given command under the specified index is nested under host with specified indent level,
        /// returns first index after exiting the block, based on specifics of the outer block(s), if any.
        /// </summary>
        public int SkipNestedAt (int nestedIndex, int hostIndent)
        {
            var exitIndex = GetNestedExitIndexAt(nestedIndex, hostIndent);
            return ExitNestedAt(exitIndex, hostIndent);
        }

        /// <summary>
        /// Returns the next playback index, while taking into account nesting behaviour in case
        /// moved index is under nested host or is the host itself. Returns -1 in case no further
        /// navigation is possible (ie, script playback is finished) or the specified index is invalid.
        /// </summary>
        public int MoveAt (int index)
        {
            if (GetCommandByIndex(index) is not { } cmd) return -1;
            if (cmd is Command.INestedHost && IsEnteringNestedAt(index))
                return cmd.GetNextPlaybackIndex(this, index);
            if (cmd.Indent == 0) return cmd.GetNextPlaybackIndex(this, index);
            return GetNestedHost(index).GetNextPlaybackIndex(this, index);
        }
    }
}
