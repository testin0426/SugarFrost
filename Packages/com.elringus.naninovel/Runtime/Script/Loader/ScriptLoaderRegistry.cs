using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Naninovel
{
    /// <summary>
    /// Manages the state associated with the activities of <see cref="ScriptLoader"/>.
    /// </summary>
    public class ScriptLoaderRegistry
    {
        [Serializable]
        public class State
        {
            [CanBeNull] public TrackState[] Tracks;
        }

        [Serializable]
        public class TrackState
        {
            [CanBeNull] public string Id;
            [CanBeNull] public string[] Paths;
        }

        private static readonly HashSet<ScriptPlaylist> emptyLists = new();
        private readonly Dictionary<string, Dictionary<string, ScriptPlaylist>> listByPathByTrackId = new();

        public void AddList (string trackId, ScriptPlaylist list)
        {
            if (!listByPathByTrackId.ContainsKey(trackId))
                listByPathByTrackId.Add(trackId, new());
            listByPathByTrackId[trackId].Add(list.ScriptPath, list);
        }

        public ScriptPlaylist GetList (string trackId, string scriptPath)
        {
            return listByPathByTrackId[trackId][scriptPath];
        }

        public bool HasList (string trackId, string scriptPath)
        {
            return listByPathByTrackId.TryGetValue(trackId, out var lists) &&
                   lists.ContainsKey(scriptPath);
        }

        public bool AnyTrackHasList (string scriptPath)
        {
            foreach (var listByPath in listByPathByTrackId.Values)
                if (listByPath.ContainsKey(scriptPath))
                    return true;
            return false;
        }

        public void RemoveTrack (string trackId)
        {
            listByPathByTrackId.Remove(trackId);
        }

        public void RemoveList (string trackId, string scriptPath)
        {
            listByPathByTrackId.GetValueOrDefault(trackId)?.Remove(scriptPath);
        }

        public IDisposable RentTracks (out HashSet<string> trackIds)
        {
            var rent = SetPool<string>.Rent(out trackIds);
            trackIds.UnionWith(listByPathByTrackId.Keys);
            return rent;
        }

        public IDisposable RentLists (string trackId, out HashSet<ScriptPlaylist> lists)
        {
            if (!listByPathByTrackId.TryGetValue(trackId, out var listByPath))
            {
                lists = emptyLists;
                return new DeferNoop();
            }
            var rent = SetPool<ScriptPlaylist>.Rent(out lists);
            lists.UnionWith(listByPath.Values);
            return rent;
        }

        public void Clear ()
        {
            listByPathByTrackId.Clear();
        }

        public State Serialize ()
        {
            var tracks = new TrackState[listByPathByTrackId.Count];
            var trackIdx = 0;
            foreach (var (id, lists) in listByPathByTrackId)
                if (lists.Count > 0)
                    tracks[trackIdx++] = new() { Id = id, Paths = lists.Keys.ToArray() };
            return new() { Tracks = tracks };
        }

        public void GetDelta (State state,
            ICollection<(string Id, string Path)> toRelease,
            ICollection<(string Id, string Path)> toLoad)
        {
            using var _ = SetPool<(string Id, string Path)>.Rent(out var loaded);
            foreach (var track in state?.Tracks ?? Array.Empty<TrackState>())
            foreach (var path in track?.Paths ?? Array.Empty<string>())
            {
                if (track?.Paths == null || track.Paths.Length == 0) continue;
                loaded.Add((track.Id, path));
                if (!HasList(track.Id, path))
                    toLoad.Add((track.Id, path));
            }

            foreach (var (id, lists) in listByPathByTrackId)
            foreach (var path in lists.Keys)
                if (!loaded.Contains((id, path)))
                    toRelease.Add((id, path));
        }
    }
}
