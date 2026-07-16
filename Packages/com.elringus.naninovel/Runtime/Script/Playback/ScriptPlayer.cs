using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IScriptPlayer"/>
    [InitializeAtRuntime]
    public class ScriptPlayer : IScriptPlayer, IStatefulService<SettingsStateMap>, IStatefulService<GlobalStateMap>, IStatefulService<GameStateMap>
    {
        [Serializable]
        public class Settings
        {
            public PlayerSkipMode SkipMode;
        }

        [Serializable]
        public class GlobalState
        {
            public PlayedScriptRegister Played = new();
        }

        [Serializable]
        public class GameState
        {
            public List<ScriptTrack.GameState> Tracks;
        }

        public event Action<IScriptTrack> OnPlay;
        public event Action<IScriptTrack> OnStop;
        public event Action<IScriptTrack> OnExecute;
        public event Action<IScriptTrack> OnExecuted;
        public event Action<IScriptTrack> OnAwaitInput;
        public event Action<bool> OnAutoPlay;
        public event Action<bool> OnSkip;

        public virtual ScriptPlayerConfiguration Configuration { get; }
        public virtual IScriptTrack MainTrack { get; private set; }
        public virtual int TrackCount => TrackById.Count;
        public virtual bool Playing => IsAnyPlaying();
        public virtual bool Executing => IsAnyExecuting();
        public virtual bool AwaitingInput => IsAnyTrackAwaitingInput();
        public virtual bool AutoPlaying { get; private set; }
        public virtual bool Skipping { get; private set; }
        public virtual PlayerSkipMode SkipMode { get => skipMode; set => SetSkipMode(value); }
        public virtual int PlayedCommandsCount => Played.CountPlayed();
        public virtual bool ReloadEnabled { get => Reloader.ReloadEnabled; set => Reloader.ReloadEnabled = value; }

        protected virtual string MainTrackId { get; } = "Main";
        protected virtual Dictionary<string, ScriptTrack> TrackById { get; } = new();
        protected virtual List<Func<IScriptTrack, Awaitable>> PreExeTasks { get; } = new();
        protected virtual List<Func<IScriptTrack, Awaitable>> PostExeTasks { get; } = new();
        protected virtual PlayedScriptRegister Played { get; } = new();
        protected virtual ScriptReloader Reloader { get; }
        protected virtual IInputManager Input { get; }
        protected virtual IScriptManager Scripts { get; }
        protected virtual IStateManager State { get; }

        [CanBeNull] protected virtual IInputHandle ContinueInput { get; private set; }
        [CanBeNull] protected virtual IInputHandle SkipInput { get; private set; }
        [CanBeNull] protected virtual IInputHandle ToggleSkipInput { get; private set; }
        [CanBeNull] protected virtual IInputHandle AutoPlayInput { get; private set; }

        private PlayerSkipMode skipMode;

        public ScriptPlayer (ScriptPlayerConfiguration cfg, IInputManager input, IScriptManager scripts, IStateManager state)
        {
            Configuration = cfg;
            Input = input;
            Scripts = scripts;
            State = state;
            Reloader = new(this, state, scripts);
        }

        public virtual Awaitable InitializeService ()
        {
            MainTrack = AddTrack(MainTrackId);

            if ((ContinueInput = Input.GetContinue()) != null)
            {
                ContinueInput.OnStart += DisableAwaitInput;
                ContinueInput.OnStart += DisableSkip;
            }
            if ((SkipInput = Input.GetSkip()) != null)
            {
                SkipInput.OnStart += EnableSkip;
                SkipInput.OnEnd += DisableSkip;
            }
            if ((ToggleSkipInput = Input.GetToggleSkip()) != null)
                ToggleSkipInput.OnStart += ToggleSkip;
            if ((AutoPlayInput = Input.GetAutoPlay()) != null)
                AutoPlayInput.OnStart += ToggleAutoPlay;

            if (Configuration.ShowDebugOnInit)
                DebugInfoGUI.Toggle();
            return Async.Completed;
        }

        public virtual void ResetService ()
        {
            if (Configuration.SkipTimeScale > 1f && Skipping)
                Engine.Time.TimeScale = 1f;
            foreach (var track in TrackById.Values)
                track.Reset();
        }

        public virtual void DestroyService ()
        {
            ResetService();
            foreach (var track in TrackById.Values)
                track.Dispose();
            TrackById.Clear();
            Reloader.Dispose();

            if (ContinueInput != null)
            {
                ContinueInput.OnStart -= DisableAwaitInput;
                ContinueInput.OnStart -= DisableSkip;
            }
            if (SkipInput != null)
            {
                SkipInput.OnStart -= EnableSkip;
                SkipInput.OnEnd -= DisableSkip;
            }
            if (ToggleSkipInput != null)
                ToggleSkipInput.OnStart -= ToggleSkip;
            if (AutoPlayInput != null)
                AutoPlayInput.OnStart -= ToggleAutoPlay;
        }

        public virtual void SaveServiceState (SettingsStateMap stateMap)
        {
            stateMap.SetState(new Settings { SkipMode = SkipMode });
        }

        public virtual Awaitable LoadServiceState (SettingsStateMap stateMap)
        {
            var settings = stateMap.GetState<Settings>() ?? new Settings {
                SkipMode = Configuration.DefaultSkipMode
            };
            SkipMode = settings.SkipMode;
            return Async.Completed;
        }

        public virtual void SaveServiceState (GlobalStateMap stateMap)
        {
            stateMap.SetState(new GlobalState { Played = new(Played) });
        }

        public virtual Awaitable LoadServiceState (GlobalStateMap stateMap)
        {
            var global = stateMap.GetState<GlobalState>() ?? new GlobalState();
            Played.OverwriteFrom(global.Played);
            return Async.Completed;
        }

        public virtual void SaveServiceState (GameStateMap stateMap)
        {
            var tracks = new List<ScriptTrack.GameState>();
            foreach (var track in TrackById.Values)
                if (track.Save() is { } saved)
                    tracks.Add(saved);
            stateMap.SetState(new GameState { Tracks = tracks });
            stateMap.PlaybackSpot = MainTrack.PlaybackSpot;
            stateMap.PlayedHash = MainTrack.PlayedScript?.Hash;
        }

        public virtual async Awaitable LoadServiceState (GameStateMap stateMap)
        {
            using var _ = Async.Rent(out var tasks);
            using var __ = SetPool<string>.Rent(out var loadedTrackIds);
            if (stateMap.GetState<GameState>()?.Tracks is { } savedTracks)
                foreach (var saved in savedTracks)
                {
                    loadedTrackIds.Add(saved.Id);
                    if (TrackById.TryGetValue(saved.Id, out var track)) tasks.Add(track.Load(saved));
                    else tasks.Add(((ScriptTrack)AddTrack(saved.Id, saved.Options)).Load(saved));
                }
            if (TrackById.Count > loadedTrackIds.Count)
            {
                using var ___ = this.RentTracks(out var tracks);
                foreach (var track in tracks)
                    if (!loadedTrackIds.Contains(track.Id))
                        RemoveTrack(track.Id);
            }
            await Async.All(tasks);
        }

        public virtual IScriptTrack AddTrack (string id, PlaybackOptions options = default)
        {
            if (TrackById.ContainsKey(id)) throw new Error($"Failed to add '{id}' script track: track with the ID already exists.");
            var hooks = new ScriptTrack.EventHooks {
                OnPlay = t => OnPlay?.Invoke(t),
                OnStop = t => OnStop?.Invoke(t),
                OnExecute = t => OnExecute?.Invoke(t),
                OnExecuted = t => OnExecuted?.Invoke(t),
                OnAwaitInput = t => OnAwaitInput?.Invoke(t),
                PreExeTasks = PreExeTasks,
                PostExeTasks = PostExeTasks
            };
            return TrackById[id] = new(id, options, Played, hooks, this, Input, Scripts, State);
        }

        public virtual void RemoveTrack (string id)
        {
            if (MainTrackId.EqualsOrdinal(id))
                throw new Error($"Failed to remove '{id}' script track: removing main track is not allowed.");
            if (!TrackById.ContainsKey(id))
                throw new Error($"Failed to remove '{id}' script track: track with the ID not found.");
            TrackById[id].Dispose();
            TrackById.Remove(id);
        }

        public virtual IScriptTrack GetTrack (string id)
        {
            return TrackById.GetValueOrDefault(id);
        }

        public virtual void CollectTracks (ICollection<IScriptTrack> tracks)
        {
            foreach (var track in TrackById.Values)
                tracks.Add(track);
        }

        protected virtual void SetAwaitInput (bool enabled)
        {
            // don't enumerate the collection directly, as tracks could spawn on ctc
            using var _ = this.RentTracks(out var tracks);
            foreach (var track in tracks)
                track.SetAwaitInput(enabled);
        }

        public virtual void SetAutoPlay (bool enabled)
        {
            AutoPlaying = enabled;
            using var _ = this.RentTracks(out var tracks);
            foreach (var track in tracks)
                ((ScriptTrack)track).SetAutoPlay(enabled);
            OnAutoPlay?.Invoke(enabled);
        }

        public virtual void SetSkip (bool enabled)
        {
            Skipping = enabled;
            using var _ = this.RentTracks(out var tracks);
            foreach (var track in tracks)
                ((ScriptTrack)track).SetSkip(enabled);
            if (Configuration.SkipTimeScale > 1f)
                Engine.Time.TimeScale = enabled ? Configuration.SkipTimeScale : 1f;
            OnSkip?.Invoke(enabled);
        }

        public virtual void AddPreExecutionTask (Func<IScriptTrack, Awaitable> task) => PreExeTasks.Insert(0, task);
        public virtual void RemovePreExecutionTask (Func<IScriptTrack, Awaitable> task) => PreExeTasks.Remove(task);
        public virtual void AddPostExecutionTask (Func<IScriptTrack, Awaitable> task) => PostExeTasks.Insert(0, task);
        public virtual void RemovePostExecutionTask (Func<IScriptTrack, Awaitable> task) => PostExeTasks.Remove(task);

        public virtual bool HasPlayed (string scriptPath, int? playlistIndex = null)
        {
            if (playlistIndex.HasValue) return Played.IsIndexPlayed(scriptPath, playlistIndex.Value);
            return Played.IsScriptPlayed(scriptPath);
        }

        public Awaitable Reload ()
        {
            return Reloader.Reload(TrackById.Values);
        }

        protected virtual bool IsAnyPlaying ()
        {
            foreach (var track in TrackById.Values)
                if (track.Playing)
                    return true;
            return false;
        }

        protected virtual bool IsAnyExecuting ()
        {
            foreach (var track in TrackById.Values)
                if (track.Executing)
                    return true;
            return false;
        }

        protected virtual void SetSkipMode (PlayerSkipMode value)
        {
            skipMode = value;
            foreach (var track in TrackById.Values)
                track.SkipMode = value;
        }

        protected virtual bool IsAnyTrackAwaitingInput ()
        {
            foreach (var track in TrackById.Values)
                if (track.AwaitingInput)
                    return true;
            return false;
        }

        private void EnableSkip () => SetSkip(true);
        private void DisableSkip () => SetSkip(false);
        private void ToggleSkip () => SetSkip(!Skipping);
        private void ToggleAutoPlay () => SetAutoPlay(!AutoPlaying);
        private void DisableAwaitInput () => SetAwaitInput(false);
    }
}
