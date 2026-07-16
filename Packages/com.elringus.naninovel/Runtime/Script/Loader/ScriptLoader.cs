using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IScriptLoader"/>
    [InitializeAtRuntime]
    public class ScriptLoader : IStatefulService<GameStateMap>, IScriptLoader
    {
        [Serializable]
        public class GameState
        {
            public ScriptLoaderRegistry.State Registry;
        }

        public event Action<float> OnLoadProgress;

        protected virtual IScriptManager Scripts { get; }
        protected virtual IScriptPlayer Player { get; }
        protected virtual ResourcePolicy Policy { get; }
        protected virtual int LazyBuffer { get; }
        protected virtual ThreadPriority LazyPriority { get; }
        protected virtual bool ShouldRemoveActors { get; }
        protected virtual ScriptLoaderRegistry Registry { get; } = new();
        protected virtual HashSet<PlaybackSpot> LazyLoadingSpots { get; } = new();

        public ScriptLoader (ResourceProviderConfiguration cfg, IScriptManager scripts, IScriptPlayer player)
        {
            Scripts = scripts;
            Player = player;
            Policy = cfg.ResourcePolicy;
            LazyBuffer = cfg.LazyBuffer;
            LazyPriority = cfg.LazyPriority;
            ShouldRemoveActors = cfg.RemoveActors;
        }

        public virtual Awaitable InitializeService ()
        {
            if (Policy == ResourcePolicy.Lazy)
            {
                Player.OnExecuted += LazyLoadNext;
                Player.AddPreExecutionTask(WaitLazyLoading);
                Application.backgroundLoadingPriority = LazyPriority;
            }
            return Async.Completed;
        }

        public virtual void ResetService ()
        {
            UnloadAll();
        }

        public virtual void DestroyService ()
        {
            UnloadAll();

            if (Policy == ResourcePolicy.Lazy && Player != null)
            {
                Player.OnExecuted -= LazyLoadNext;
                Player.RemovePreExecutionTask(WaitLazyLoading);
            }
        }

        public virtual void SaveServiceState (GameStateMap stateMap)
        {
            stateMap.SetState(new GameState { Registry = Registry.Serialize() });
        }

        public virtual Awaitable LoadServiceState (GameStateMap stateMap)
        {
            if (stateMap.GetState<GameState>()?.Registry is not { } state)
            {
                UnloadAll();
                return Async.Completed;
            }

            using var _ = ListPool<(string Id, string Path)>.Rent(out var toRelease);
            using var __ = ListPool<(string Id, string Path)>.Rent(out var toLoad);
            Registry.GetDelta(state, toRelease, toLoad);

            foreach (var (id, path) in toRelease)
                ReleaseList(id, path);
            if (toLoad.Count == 0) return Async.Completed;

            using var ___ = Async.Rent(out var tasks);
            foreach (var (id, path) in toLoad)
                tasks.Add(LoadList(id, path));
            return Async.All(tasks);
        }

        public virtual async Awaitable Load (string trackId, string scriptPath, int startIndex = 0)
        {
            // Script being already loaded means it was loaded as a dependency, so do nothing.
            if (IsLoaded(trackId, scriptPath)) return;

            // In conservative, unload after loading to prevent re-loading shared resources.
            if (Policy == ResourcePolicy.Conservative)
            {
                var playlist = await LoadAndHoldScript(scriptPath);
                using var _ = Registry.RentLists(trackId, out var prevLists);
                Registry.RemoveTrack(trackId);
                await LoadList(trackId, playlist, startIndex);
                await LoadDependencies(trackId, playlist);
                foreach (var prevList in prevLists)
                    if (!Registry.AnyTrackHasList(prevList.ScriptPath))
                        UnloadList(prevList);
                if (ShouldRemoveActors) RemoveUnusedActors();
            }

            // In optimistic loads are sparse, so prefer re-loading shared resources instead
            // of keeping resources from both previous and next script batches while loading.
            if (Policy == ResourcePolicy.Optimistic)
            {
                ReleaseTack(trackId);
                if (ShouldRemoveActors) RemoveUnusedActors();
                var playlist = await LoadAndHoldScript(scriptPath);
                await LoadList(trackId, playlist, startIndex);
                await LoadDependencies(trackId, playlist);
            }

            // In lazy just load the script itself, as it's required to resolve localizable text;
            // also unload and preload the few buffer commands between the scripts.
            // Delegate actual resource management to LazyLoadNext() invoked during the script playback.
            if (Policy == ResourcePolicy.Lazy)
            {
                Application.backgroundLoadingPriority = ThreadPriority.High;
                var played = Player.GetTrackOrErr(trackId).PlayedScript?.Path == scriptPath;
                if (!played) ReleaseTack(trackId);
                if (ShouldRemoveActors) RemoveUnusedActors();
                var list = await LoadAndHoldScript(scriptPath);
                var endIndex = Mathf.Min(list.Count - 1, startIndex + LazyBuffer);
                if (played) await list.LoadResources(startIndex, endIndex, OnLoadProgress);
                else await LoadList(trackId, list, startIndex, endIndex);
                Application.backgroundLoadingPriority = LazyPriority;
            }
        }

        public virtual void Release (string trackId)
        {
            ReleaseTack(trackId);
        }

        protected virtual bool IsLoaded (string trackId, string scriptPath)
        {
            if (Policy == ResourcePolicy.Lazy) return false; // lazy drops inter-script resources, so always load
            if (string.IsNullOrWhiteSpace(scriptPath)) return false;
            return Registry.HasList(trackId, scriptPath);
        }

        protected virtual async Awaitable LoadList (string trackId, string scriptPath)
        {
            var playlist = await LoadAndHoldScript(scriptPath);
            await LoadList(trackId, playlist, 0);
        }

        protected virtual Awaitable LoadList (string trackId, ScriptPlaylist list, int startIndex, int? endIndex = null)
        {
            if (IsLoaded(trackId, list.ScriptPath)) return Async.Completed;
            Registry.AddList(trackId, list);
            return list.LoadResources(startIndex, endIndex ?? list.Count - 1, OnLoadProgress);
        }

        protected virtual async Awaitable LoadDependencies (string trackId, ScriptPlaylist list)
        {
            using var _ = SetPool<string>.Rent(out var loadedPaths);
            await LoadDependencies(trackId, list, loadedPaths);
        }

        protected virtual Awaitable LoadDependencies (string trackId, ScriptPlaylist list, ISet<string> loadedPaths)
        {
            using var _ = Async.Rent(out var tasks);
            loadedPaths.Add(list.ScriptPath);
            foreach (var command in list)
                if (GetDependency(command) is { } scriptPath &&
                    !IsLoaded(trackId, scriptPath) && loadedPaths.Add(scriptPath))
                    tasks.Add(LoadDependency(trackId, scriptPath, loadedPaths));
            return Async.All(tasks);
        }

        protected virtual async Awaitable LoadDependency (string trackId, string scriptPath, ISet<string> loadedPaths)
        {
            var playlist = await LoadAndHoldScript(scriptPath);
            await LoadDependencies(trackId, playlist, loadedPaths);
            await LoadList(trackId, playlist, 0);
        }

        [CanBeNull]
        protected virtual string GetDependency (Command cmd)
        {
            if (cmd is not Command.INavigator nav || string.IsNullOrEmpty(nav.ScriptPath)) return null;
            if (Policy == ResourcePolicy.Optimistic && nav.ReleaseResources) return null;
            if (Policy == ResourcePolicy.Conservative && !nav.HoldResources) return null;
            if (!Scripts.TryResolveEndpoint(nav.ScriptPath, cmd.PlaybackSpot.ScriptPath, out var end)) return null;
            if (string.IsNullOrEmpty(end.ScriptPath) || end.ScriptPath.EqualsOrdinal(cmd.PlaybackSpot.ScriptPath)) return null;
            return end.ScriptPath;
        }

        protected virtual void ReleaseTack (string trackId)
        {
            using var _ = Registry.RentLists(trackId, out var lists);
            foreach (var list in lists)
                ReleaseList(trackId, list.ScriptPath);
            Registry.RemoveTrack(trackId);
        }

        protected virtual void ReleaseList (string trackId, string scriptPath)
        {
            var list = Registry.GetList(trackId, scriptPath);
            Registry.RemoveList(trackId, scriptPath);
            if (!Registry.AnyTrackHasList(scriptPath)) UnloadList(list);
        }

        protected virtual void UnloadList (ScriptPlaylist list)
        {
            list.ReleaseResources();
            Scripts.ScriptLoader.Release(list.ScriptPath, this);
        }

        protected virtual void UnloadAll ()
        {
            using var _ = Registry.RentTracks(out var trackIds);
            foreach (var trackId in trackIds)
            {
                using var __ = Registry.RentLists(trackId, out var lists);
                foreach (var list in lists)
                    list.ReleaseResources();
            }
            Scripts.ScriptLoader.ReleaseAll(this);
            Registry.Clear();
            LazyLoadingSpots.Clear();
        }

        protected virtual async Awaitable<ScriptPlaylist> LoadAndHoldScript (string scriptPath)
        {
            var script = await Scripts.ScriptLoader.LoadOrErr(scriptPath, this);
            return script.Object.Playlist;
        }

        protected virtual void RemoveUnusedActors ()
        {
            foreach (var service in Engine.Services)
                if (service is IActorManager manager)
                {
                    using var _ = manager.RentActors(out var actors);
                    foreach (var actor in actors)
                        if (!actor.Visible &&
                            // Single holder means the actor is holding its own resources and is effectively unused.
                            manager.GetAppearanceLoader(actor.Id) is { } loader && loader.CountHolders() == 1)
                            manager.RemoveActor(actor.Id);
                }
        }

        protected virtual void LazyLoadNext (IScriptTrack track)
        {
            if (track.PlayedCommand is Command.IPreloadable played)
                played.ReleaseResources();
            var nextIdx = track.PlayedIndex + LazyBuffer;
            if (track.Playlist.GetCommandByIndex(nextIdx) is { } cmd && cmd is Command.IPreloadable pre)
                if (LazyLoadingSpots.Add(cmd.PlaybackSpot))
                    pre.PreloadResources().Then(() => LazyLoadingSpots.Remove(cmd.PlaybackSpot)).Forget();
        }

        protected virtual async Awaitable WaitLazyLoading (IScriptTrack track)
        {
            if (!LazyLoadingSpots.Contains(track.PlaybackSpot)) return;
            OnLoadProgress?.Invoke(0);
            Application.backgroundLoadingPriority = ThreadPriority.High;
            while (LazyLoadingSpots.Contains(track.PlaybackSpot))
                await Async.NextFrame(Engine.DestroyToken);
            Application.backgroundLoadingPriority = LazyPriority;
            OnLoadProgress?.Invoke(1);
        }
    }
}
