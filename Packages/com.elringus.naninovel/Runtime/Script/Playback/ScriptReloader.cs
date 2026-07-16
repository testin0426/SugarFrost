using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Handles script reload, aka "Hot Reload" feature.
    /// </summary>
    public class ScriptReloader : IDisposable
    {
        public bool ReloadEnabled { get; set; }

        private IScriptTrack mainTrack => player.MainTrack;
        private IResourceLoader<Script> loader => scripts.ScriptLoader;
        private readonly List<string> lastHashList = new();
        private readonly IScriptManager scripts;
        private readonly IScriptPlayer player;
        private readonly IStateManager state;
        private int lastPlayedIndex;

        public ScriptReloader (IScriptPlayer player, IStateManager state, IScriptManager scripts)
        {
            this.player = player;
            this.state = state;
            this.scripts = scripts;
            this.player.OnPlay += HandlePlay;
            this.player.OnExecute += HandleExecute;
        }

        public void Dispose ()
        {
            player.OnPlay -= HandlePlay;
            player.OnExecute -= HandleExecute;
        }

        public virtual async Awaitable Reload (IReadOnlyCollection<ScriptTrack> tracks)
        {
            if (!mainTrack.PlayedScript) return;

            if (!ReloadEnabled)
            {
                Engine.Warn("Script reload was requested while the reload feature was not enabled on script player." +
                            "The feature is now enabled and subsequent reloads should work as expected.");
                ReloadEnabled = true;
                HandlePlay(mainTrack);
                return;
            }

            // Remember to not use PlayedScript/Playlist/PlaybackSpot here, as in editor the Script
            // is already reloaded in-memory. This is not covered by tests, as they mock script resources
            // via virtual provider, so manually test when changing.
            var resumeIdx = lastPlayedIndex >= lastHashList.Count - 1 ? lastHashList.Count : -1;

            // Reload and re-assign the played scripts.
            using var _ = SetPool<string>.Rent(out var paths);
            using var __ = MapPool<ScriptTrack, string>.Rent(out var pathByTrack);
            foreach (var track in tracks)
                if (track.PlayedScript)
                    paths.Add(pathByTrack[track] = track.PlayedScript.Path);
            await Async.All(paths.Select(loader.Reload));
            foreach (var (track, path) in pathByTrack)
                track.PlayedScript = loader.GetLoadedOrErr(path);

            // Find the first modified playback index in the new script before the currently played index.
            var rollbackIndex = -1;
            for (int i = 0; i <= lastPlayedIndex; i++)
                if (!mainTrack.Playlist.IsIndexValid(i)) // the updated script ends before the played index
                {
                    if (i == 0) rollbackIndex = 0; // updated script is empty — rollback to the initial state
                    else if (mainTrack.Playlist.IsIndexValid(i - 1)) rollbackIndex = i - 1;
                    break;
                }
                else if (!lastHashList.IsIndexValid(i) || lastHashList[i] !=
                         mainTrack.PlayedScript.GetLineHashForPlaylistIndex(i)) // found the modified index
                {
                    if (mainTrack.Playlist.IsIndexValid(i)) rollbackIndex = i;
                    break;
                }

            if (rollbackIndex >= 0)
                // Script been modified before the currently played index: rollback before the first modified one.
                await state.Rollback(s =>
                    mainTrack.Playlist.ScriptPath == s.PlaybackSpot.ScriptPath &&
                    mainTrack.Playlist.GetIndexByLine(s.PlaybackSpot.LineIndex,
                        s.PlaybackSpot.InlineIndex) <= rollbackIndex, rollbackIndex == 0);
            // Otherwise script has changed after the played index: do nothing.

            // Resume in case the old script has finished playing, but the new one has further content.
            if (!mainTrack.Playing && mainTrack.PlayedScript && resumeIdx >= mainTrack.PlayedIndex &&
                mainTrack.Playlist.IsIndexValid(resumeIdx)) mainTrack.Resume(resumeIdx);
            // Otherwise actualize the script content hash (OnPlay triggers when resumed above).
            else HandlePlay(mainTrack);
        }

        protected virtual void HandlePlay (IScriptTrack track)
        {
            if (!ReloadEnabled || track != mainTrack) return;
            lastHashList.Clear();
            if (track.PlayedScript)
                for (int i = 0; i < track.Playlist.Count; i++)
                    lastHashList.Add(track.PlayedScript.GetLineHashForPlaylistIndex(i));
        }

        protected virtual void HandleExecute (IScriptTrack track)
        {
            if (!ReloadEnabled || track != mainTrack) return;
            lastPlayedIndex = track.PlayedIndex;
        }
    }
}
