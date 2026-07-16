using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents '.nani' scenario script file serialized as Unity asset.
    /// </summary>
    [Serializable]
    public class Script : ScriptableObject
    {
        /// <summary>
        /// Unique (project-wide) local resource path of the script.
        /// </summary>
        public string Path => path;
        /// <summary>
        /// Persistent hash code of the script content.
        /// </summary>
        public string Hash => hash;
        /// <summary>
        /// Map of the identified (localizable) text contained in the script.
        /// </summary>
        public ScriptTextMap TextMap => textMap;
        /// <summary>
        /// The list of lines this script contains, in order.
        /// </summary>
        public IReadOnlyList<ScriptLine> Lines => lines;
        /// <summary>
        /// Playlist associated with the script.
        /// </summary>
        public ScriptPlaylist Playlist => playlist;

        [SerializeField] private string path;
        [SerializeField] private string hash;
        [SerializeField] private ScriptTextMap textMap;
        [SerializeField] private ScriptPlaylist playlist;
        [SerializeReference] private ScriptLine[] lines;

        /// <summary>
        /// Creates new script asset from the specified compiled lines.
        /// </summary>
        /// <param name="path">Unique (project-wide) local resource path of the script.</param>
        /// <param name="lines">Compiled lines of the script, in order.</param>
        /// <param name="textMap">Map of identified (localizable) text contained in the script.</param>
        public static Script Create (string path, ScriptLine[] lines, ScriptTextMap textMap)
        {
            var asset = CreateInstance<Script>();
            asset.name = System.IO.Path.GetFileName(path);
            asset.path = path;
            asset.lines = lines;
            asset.textMap = textMap;
            asset.hash = ComputeHash(lines);
            asset.playlist = new(path, asset.ExtractCommands());
            return asset;
        }

        /// <summary>
        /// Collects all the contained commands (preserving the order).
        /// </summary>
        public List<Command> ExtractCommands ()
        {
            var commands = new List<Command>();
            foreach (var line in lines)
                if (line is CommandLine commandLine)
                    commands.Add(commandLine.Command);
                else if (line is GenericLine genericLine)
                    commands.AddRange(genericLine.InlinedCommands);
            return commands;
        }

        /// <summary>
        /// Returns first script line of <typeparamref name="TLine"/> filtered by <paramref name="predicate"/> or null.
        /// </summary>
        public TLine FindLine<TLine> (Predicate<TLine> predicate) where TLine : ScriptLine
        {
            return lines.FirstOrDefault(l => l is TLine tl && predicate(tl)) as TLine;
        }

        /// <summary>
        /// Returns all the script lines of <typeparamref name="TLine"/> filtered by <paramref name="predicate"/>.
        /// </summary>
        public List<TLine> FindLines<TLine> (Predicate<TLine> predicate) where TLine : ScriptLine
        {
            return lines.Where(l => l is TLine tl && predicate(tl)).Cast<TLine>().ToList();
        }

        /// <summary>
        /// Checks whether a <see cref="LabelLine"/> with the specified value exists in this script.
        /// </summary>
        public bool LabelExists (string label)
        {
            foreach (var line in lines)
                if (line is LabelLine labelLine && labelLine.LabelText.EqualsOrdinal(label))
                    return true;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve index of a <see cref="LabelLine"/> with the specified <see cref="LabelLine.LabelText"/>.
        /// Returns -1 in case the label is not found.
        /// </summary>
        public int GetLineIndexForLabel (string label)
        {
            foreach (var line in lines)
                if (line is LabelLine labelLine && labelLine.LabelText.EqualsOrdinal(label))
                    return labelLine.LineIndex;
            return -1;
        }

        /// <summary>
        /// Returns <see cref="ScriptLine"/> associated with the specified <see cref="Playlist"/> index
        /// or -1 when specified playback index is invalid.
        /// </summary>
        public int GetLineIndexForPlaylistIndex (int playlistIndex)
        {
            if (!Playlist.IsIndexValid(playlistIndex)) return -1;
            var lineIndex = Playlist[playlistIndex].PlaybackSpot.LineIndex;
            return Lines.IsIndexValid(lineIndex) ? lineIndex : -1;
        }

        /// <summary>
        /// Returns <see cref="ScriptLine.LineHash"/> associated with the specified <see cref="Playlist"/> index
        /// or null when specified playback index is invalid.
        /// </summary>
        [CanBeNull]
        public string GetLineHashForPlaylistIndex (int playlistIndex)
        {
            var lineIndex = GetLineIndexForPlaylistIndex(playlistIndex);
            return lineIndex >= 0 ? Lines[lineIndex].LineHash : null;
        }

        /// <summary>
        /// Returns first <see cref="LabelLine.LabelText"/> located above line with the specified index.
        /// Returns null when not found.
        /// </summary>
        public string GetLabelForLine (int lineIndex)
        {
            if (!lines.IsIndexValid(lineIndex)) return null;
            for (var i = lineIndex; i >= 0; i--)
                if (lines[i] is LabelLine labelLine)
                    return labelLine.LabelText;
            return null;
        }

        /// <summary>
        /// Returns first <see cref="CommentLine.CommentText"/> located above line with the specified index.
        /// Returns null when not found.
        /// </summary>
        public string GetCommentForLine (int lineIndex)
        {
            if (!lines.IsIndexValid(lineIndex)) return null;
            for (var i = lineIndex; i >= 0; i--)
                if (lines[i] is CommentLine commentLine)
                    return commentLine.CommentText;
            return null;
        }

        /// <summary>
        /// Attempts a <see cref="Playlist"/> index of a <see cref="LabelLine"/> with the specified <see cref="LabelLine.LabelText"/>.
        /// Returns -1 in case the label is not found.
        /// </summary>
        public int GetPlaylistIndexForLabel (string label)
        {
            var lineIdx = GetLineIndexForLabel(label);
            return lineIdx == -1 ? -1 : Playlist.GetIndexByLine(lineIdx);
        }

        private static string ComputeHash (IReadOnlyList<ScriptLine> lines)
        {
            if (lines.Count == 0) return CryptoUtils.PersistentHexCode("");
            var builder = new System.Text.StringBuilder();
            foreach (var line in lines)
                builder.Append(line.LineHash);
            return CryptoUtils.PersistentHexCode(builder.ToString());
        }
    }
}
