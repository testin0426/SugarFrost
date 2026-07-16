using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Naninovel.Commands;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IStateManager"/>
    [InitializeAtRuntime(int.MinValue), Goto.DontResetAttribute]
    public class StateManager : IStateManager
    {
        public virtual event Action<GameSaveLoadArgs> OnGameLoadStarted;
        public virtual event Action<GameSaveLoadArgs> OnGameLoadFinished;
        public virtual event Action<GameSaveLoadArgs> OnGameSaveStarted;
        public virtual event Action<GameSaveLoadArgs> OnGameSaveFinished;
        public virtual event Action OnResetStarted;
        public virtual event Action OnResetFinished;
        public virtual event Action OnRollbackStarted;
        public virtual event Action OnRollbackFinished;

        public virtual StateConfiguration Configuration { get; }
        public virtual GlobalStateMap GlobalState { get; private set; }
        public virtual SettingsStateMap SettingsState { get; private set; }
        public virtual GameStateMap GameState { get; private set; }
        public virtual ISaveSlotManager<SettingsStateMap> SettingsSlotManager { get; }
        public virtual ISaveSlotManager<GlobalStateMap> GlobalSlotManager { get; }
        public virtual ISaveSlotManager<GameStateMap> GameSlotManager { get; }
        public virtual bool QuickLoadAvailable => HasGameSlotWithMask(Configuration.QuickSaveSlotMask);
        public virtual bool AutoLoadAvailable => HasGameSlotWithMask(Configuration.AutoSaveSlotMask);
        public virtual bool AnyGameSaveExists => GameSlotManager.AnySaveExists();
        public virtual bool RollbackInProgress => rollbackTaskQueue.Count > 0;

        protected virtual string LastQuickSaveSlotId => Configuration.IndexToQuickSaveSlotId(1);
        protected virtual string LastAutoSaveSlotId => Configuration.IndexToAutoSaveSlotId(1);
        protected virtual StateRollbackStack RollbackStack { get; }
        [CanBeNull] protected virtual GameStateMap RecoveryState { get; private set; }

        private static bool savedOnQuit;
        private readonly Queue<GameStateMap> rollbackTaskQueue = new();
        private readonly List<Action<GameStateMap>> onGameSerializeTasks = new();
        private readonly List<Func<GameStateMap, Awaitable>> onGameDeserializeTasks = new();
        private IInputHandle rollbackInput;
        private IScriptPlayer player;
        private ICameraManager camera;

        // Remember to not reference any other engine services to make sure this service is always initialized first.
        // This is required for the post engine initialization tasks to be performed before any others.
        public StateManager (StateConfiguration cfg)
        {
            Configuration = cfg;
            RollbackStack = new(Mathf.Max(1, cfg.EnableStateRollback || Application.isEditor ? cfg.StateRollbackSteps : 1));

            var saveDir = Application.isEditor ? $"{TransientRootResolver.Resolve().GetAfterFirst("Assets/")}/{cfg.SaveFolderName}" : cfg.SaveFolderName;
            GameSlotManager = (ISaveSlotManager<GameStateMap>)Activator.CreateInstance(Type.GetType(cfg.GameStateHandler), cfg, saveDir);
            GlobalSlotManager = (ISaveSlotManager<GlobalStateMap>)Activator.CreateInstance(Type.GetType(cfg.GlobalStateHandler), cfg, saveDir);
            SettingsSlotManager = (ISaveSlotManager<SettingsStateMap>)Activator.CreateInstance(Type.GetType(cfg.SettingsStateHandler), cfg, saveDir);

            Engine.AddPostInitializationTask(PerformPostEngineInitializationTasks);
        }

        public virtual Awaitable InitializeService ()
        {
            player = Engine.GetServiceOrErr<IScriptPlayer>();
            camera = Engine.GetServiceOrErr<ICameraManager>();
            Application.wantsToQuit += HandleApplicationQuit;
            return Async.Completed;
        }

        public virtual void ResetService ()
        {
            RollbackStack?.Clear();
        }

        public virtual void DestroyService ()
        {
            if (player != null)
                player.OnExecute -= HandleCommandExecute;

            if (rollbackInput != null)
                rollbackInput.OnStart -= HandleRollbackInputStart;

            Engine.RemovePostInitializationTask(PerformPostEngineInitializationTasks);
            Application.wantsToQuit -= HandleApplicationQuit;
        }

        public virtual void AddOnGameSerializeTask (Action<GameStateMap> task) => onGameSerializeTasks.Insert(0, task);

        public virtual void RemoveOnGameSerializeTask (Action<GameStateMap> task) => onGameSerializeTasks.Remove(task);

        public virtual void AddOnGameDeserializeTask (Func<GameStateMap, Awaitable> task) => onGameDeserializeTasks.Insert(0, task);

        public virtual void RemoveOnGameDeserializeTask (Func<GameStateMap, Awaitable> task) => onGameDeserializeTasks.Remove(task);

        public virtual async Awaitable<GameStateMap> SaveGame (string slotId)
        {
            var type = GetSlotType(slotId);

            OnGameSaveStarted?.Invoke(new(slotId, type));

            var state = new GameStateMap();

            var saved = false;
            var completeTracksCount = 0;
            using var _ = player.RentTracks(out var tracks);
            using var __ = Async.Rent(out var tasks);
            foreach (var track in tracks)
                tasks.Add(track.Complete(cmd => cmd is not Commands.AutoSave, HandleTrackComplete));
            await Async.All(tasks);

            OnGameSaveFinished?.Invoke(new(slotId, type));

            return state;

            async Awaitable HandleTrackComplete ()
            {
                // Perform serialization once, on last track completion;
                // other tracks just wait for the save operation to finish.
                if (++completeTracksCount < tracks.Count)
                {
                    await Async.Until(() => saved);
                    return;
                }

                PeekRollbackStack()?.ForceSerialize();

                state.SaveDateTime = DateTime.Now;
                state.Thumbnail = await camera.CaptureThumbnail();

                SaveAllServicesToState<IStatefulService<GameStateMap>, GameStateMap>(state);
                PerformOnGameSerializeTasks(state);
                state.RollbackStackJson = SerializeRollbackStack();

                if (Configuration.RecoveryRollback && RecoveryState != null)
                    state.RecoveryJson = JsonUtility.ToJson(RecoveryState);

                await GameSlotManager.Save(slotId, state);

                // Also save global state on every game save.
                await SaveGlobal();

                saved = true;
            }
        }

        public virtual Awaitable<GameStateMap> QuickSave ()
        {
            return PushSaveGame(Configuration.QuickSaveSlotLimit, Configuration.IndexToQuickSaveSlotId);
        }

        public virtual Awaitable<GameStateMap> AutoSave ()
        {
            return PushSaveGame(Configuration.AutoSaveSlotLimit, Configuration.IndexToAutoSaveSlotId);
        }

        public virtual async Awaitable<GameStateMap> LoadGame (string slotId)
        {
            if (string.IsNullOrEmpty(slotId) || !GameSlotManager.SaveSlotExists(slotId))
                throw new Error($"Slot '{slotId}' not found when loading '{typeof(GameStateMap)}' data.");

            var type = GetSlotType(slotId);

            OnGameLoadStarted?.Invoke(new(slotId, type));

            Engine.Reset();
            var state = await GameSlotManager.Load(slotId);

            if (Configuration.RecoveryRollback && state.PlaybackSpot.Valid && state.RecoveryJson != null &&
                JsonUtility.FromJson<GameStateMap>(state.RecoveryJson) is { } recovered &&
                state.PlaybackSpot.ScriptPath == recovered.PlaybackSpot.ScriptPath &&
                (await Engine.GetServiceOrErr<IScriptManager>().ScriptLoader
                    .LoadOrErr(state.PlaybackSpot.ScriptPath)).Object.Hash != recovered.PlayedHash)
            {
                state = recovered;
                Engine.Warn("Loading recovery state because the played script was changed after the save was made. " +
                            "The 'RecoveryRollback' feature can be disabled in the state configuration.");
            }

            await LoadAllServicesFromStateAsync<IStatefulService<GameStateMap>, GameStateMap>(state);
            RollbackStack?.OverrideFromJson(state.RollbackStackJson);
            await PerformOnGameDeserializeTasks(state);

            OnGameLoadFinished?.Invoke(new(slotId, type));

            return state;
        }

        public virtual async Awaitable<GameStateMap> QuickLoad () => await LoadGame(LastQuickSaveSlotId);

        public virtual async Awaitable<GameStateMap> AutoLoad () => await LoadGame(LastAutoSaveSlotId);

        public virtual async Awaitable SaveGlobal ()
        {
            SaveAllServicesToState<IStatefulService<GlobalStateMap>, GlobalStateMap>(GlobalState);
            await GlobalSlotManager.Save(Configuration.DefaultGlobalSlotId, GlobalState);
        }

        public virtual async Awaitable SaveSettings ()
        {
            SaveAllServicesToState<IStatefulService<SettingsStateMap>, SettingsStateMap>(SettingsState);
            await SettingsSlotManager.Save(Configuration.DefaultSettingsSlotId, SettingsState);
        }

        public virtual async Awaitable ResetState (params Func<Awaitable>[] tasks)
        {
            await ResetState(default(Type[]), tasks);
        }

        public virtual async Awaitable ResetState (IReadOnlyCollection<string> exclude, params Func<Awaitable>[] tasks)
        {
            var serviceTypes = Engine.Services.Select(s => s.GetType());
            var excludeTypes = serviceTypes.Where(t => exclude.Contains(t.Name) || t.GetInterfaces().Any(i => exclude.Contains(i.Name))).ToArray();
            await ResetState(excludeTypes, tasks);
        }

        public virtual async Awaitable ResetState (IReadOnlyCollection<Type> exclude, params Func<Awaitable>[] tasks)
        {
            OnResetStarted?.Invoke();
            using (new InteractionBlocker())
            {
                using var _ = player.RentTracks(out var tracks);
                foreach (var track in tracks)
                    track.Playlist?.ReleaseResources();
                Engine.Reset(exclude);
                await PerformOnGameDeserializeTasks(new());
                if (tasks != null)
                    foreach (var task in tasks)
                        if (task != null)
                            await task.Invoke();
            }
            OnResetFinished?.Invoke();
        }

        public virtual void PushRollbackSnapshot (bool allowPlayerRollback)
        {
            if (RollbackStack is null) return;

            var state = new GameStateMap();
            state.SaveDateTime = DateTime.Now;
            state.PlayerRollbackAllowed = allowPlayerRollback;

            SaveAllServicesToState<IStatefulService<GameStateMap>, GameStateMap>(state);
            PerformOnGameSerializeTasks(state);
            RollbackStack.Push(state);

            if (Configuration.RecoveryRollback &&
                RecoveryState?.PlaybackSpot.ScriptPath != player.MainTrack.PlayedScript?.Path)
                RecoveryState = state;
        }

        public virtual async Awaitable<bool> Rollback (Predicate<GameStateMap> predicate, bool exhaustive = false)
        {
            var state = exhaustive ? RollbackStack?.PopExhaustive(predicate) : RollbackStack?.Pop(predicate);
            if (state is null) return false;
            await RollbackToState(state);
            return true;
        }

        public virtual GameStateMap PeekRollbackStack () => RollbackStack?.Peek();

        public virtual bool CanRollbackTo (Predicate<GameStateMap> predicate) => RollbackStack?.Contains(predicate) ?? false;

        public virtual void PurgeRollbackData () => RollbackStack?.ForEach(s => s.PlayerRollbackAllowed = false);

        protected virtual SaveType GetSlotType (string slotId)
        {
            return slotId.StartsWithOrdinal(Configuration.QuickSaveSlotMask.GetBefore("{")) ? SaveType.Quick
                : slotId.StartsWithOrdinal(Configuration.AutoSaveSlotMask.GetBefore("{")) ? SaveType.Auto
                : SaveType.Normal;
        }

        protected virtual bool HasGameSlotWithMask (string mask)
        {
            var openIdx = mask.IndexOf('{');
            var closeIdx = mask.LastIndexOf('}');
            if (openIdx < 0 || closeIdx < 0 || openIdx >= closeIdx) return false;

            var prefixLen = openIdx;
            var suffixIdx = closeIdx + 1;
            var suffixLen = mask.Length - suffixIdx;

            using var _ = ListPool<string>.Rent(out var ids);
            GameSlotManager.CollectSlotIds(ids);
            foreach (var id in ids)
            {
                if (id.Length < prefixLen + suffixLen) continue;
                if (string.CompareOrdinal(id, 0, mask, 0, prefixLen) != 0) continue;
                if (string.CompareOrdinal(id, id.Length - suffixLen, mask, suffixIdx, suffixLen) == 0) return true;
            }
            return false;
        }

        protected virtual async Awaitable<GameStateMap> PushSaveGame (int slotLimit, Func<int, string> getSlotIdFromIndex)
        {
            // Free first save slot by shifting existing ones by one.
            for (var i = slotLimit; i > 0; i--)
            {
                var curSlotId = getSlotIdFromIndex(i);
                var prevSlotId = getSlotIdFromIndex(i + 1);
                GameSlotManager.RenameSaveSlot(curSlotId, prevSlotId);
            }

            // Delete the last slot in case it's out of the limit.
            var outOfLimitSlotId = getSlotIdFromIndex(slotLimit + 1);
            if (GameSlotManager.SaveSlotExists(outOfLimitSlotId))
                GameSlotManager.DeleteSaveSlot(outOfLimitSlotId);

            var firstSlotId = getSlotIdFromIndex(1);
            return await SaveGame(firstSlotId);
        }

        protected virtual string SerializeRollbackStack ()
        {
            if (RollbackStack is null) return string.Empty;
            return RollbackStack.ToJson(Configuration.SavedRollbackSteps, ShouldSerializeSnapshot);
        }

        protected virtual bool ShouldSerializeSnapshot (GameStateMap state)
        {
            return state.ForcedSerialize || state.PlayerRollbackAllowed;
        }

        protected virtual async Awaitable RollbackToState (GameStateMap state)
        {
            rollbackTaskQueue.Enqueue(state);
            OnRollbackStarted?.Invoke();

            while (rollbackTaskQueue.Peek() != state)
                await Async.NextFrame();

            await LoadAllServicesFromStateAsync<IStatefulService<GameStateMap>, GameStateMap>(state);

            await PerformOnGameDeserializeTasks(state);

            rollbackTaskQueue.Dequeue();
            OnRollbackFinished?.Invoke();
        }

        protected virtual void SaveAllServicesToState<TService, TState> (TState state)
            where TService : class, IStatefulService<TState>
            where TState : StateMap, new()
        {
            foreach (var service in Engine.Services.OfType<TService>())
                service.SaveServiceState(state);
        }

        protected virtual async Awaitable LoadAllServicesFromStateAsync<TService, TState> (TState state)
            where TService : class, IStatefulService<TState>
            where TState : StateMap, new()
        {
            foreach (var service in Engine.Services.OfType<TService>())
                await service.LoadServiceState(state);
        }

        protected virtual void PerformOnGameSerializeTasks (GameStateMap state)
        {
            for (var i = onGameSerializeTasks.Count - 1; i >= 0; i--)
                onGameSerializeTasks[i](state);
            GameState = state;
        }

        protected virtual async Awaitable PerformOnGameDeserializeTasks (GameStateMap state)
        {
            GameState = state;
            for (var i = onGameDeserializeTasks.Count - 1; i >= 0; i--)
                await onGameDeserializeTasks[i](state);
        }

        protected virtual bool HandleApplicationQuit ()
        {
            Application.wantsToQuit -= HandleApplicationQuit;
            if (savedOnQuit || !Configuration.AutoSaveOnQuit || Application.isEditor) return true;
            if (Engine.GetService<IUIManager>()?.GetUI<ITitleUI>() is { Visible: true }) return true;
            savedOnQuit = true;
            AutoSave().Then(_ => Application.Quit()).Forget();
            return false;
        }

        protected virtual async void HandleRollbackInputStart ()
        {
            if (!Configuration.EnableStateRollback || !CanRollbackTo(s => s.PlayerRollbackAllowed)) return;
            await Rollback(s => s.PlayerRollbackAllowed);
        }

        protected virtual async Awaitable PerformPostEngineInitializationTasks ()
        {
            SettingsState = await SettingsSlotManager.LoadOrDefault(Configuration.DefaultSettingsSlotId);
            await LoadAllServicesFromStateAsync<IStatefulService<SettingsStateMap>, SettingsStateMap>(SettingsState);
            if (!Engine.Initializing) return;

            GlobalState = await GlobalSlotManager.LoadOrDefault(Configuration.DefaultGlobalSlotId);
            await LoadAllServicesFromStateAsync<IStatefulService<GlobalStateMap>, GlobalStateMap>(GlobalState);
            if (!Engine.Initializing) return;

            if (Configuration.EnableStateRollback || Application.isEditor)
                InitializeRollback();
        }

        protected virtual void InitializeRollback ()
        {
            player.OnExecute += HandleCommandExecute;
            rollbackInput = Engine.GetService<IInputManager>().GetRollback();
            if (rollbackInput != null)
                rollbackInput.OnStart += HandleRollbackInputStart;
        }

        protected virtual void HandleCommandExecute (IScriptTrack _)
        {
            PushRollbackSnapshot(false);
        }
    }
}
