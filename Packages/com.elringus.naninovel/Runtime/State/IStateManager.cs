using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to handle <see cref="IEngineService"/>-related and other engine persistent data de-/serialization,
    /// provide API to save/load game state and handle state rollback feature.
    /// </summary>
    public interface IStateManager : IEngineService<StateConfiguration>
    {
        /// <summary>
        /// Occurs when a game load operation (eg, <see cref="LoadGame"/> or <see cref="QuickLoad"/>) is started.
        /// </summary>
        event Action<GameSaveLoadArgs> OnGameLoadStarted;
        /// <summary>
        /// Occurs when a game load operation (eg, <see cref="LoadGame"/> or <see cref="QuickLoad"/>) is finished.
        /// </summary>
        event Action<GameSaveLoadArgs> OnGameLoadFinished;
        /// <summary>
        /// Occurs when a game save operation (eg, <see cref="SaveGame"/> or <see cref="QuickSave"/>) is started.
        /// </summary>
        event Action<GameSaveLoadArgs> OnGameSaveStarted;
        /// <summary>
        /// Occurs when a game save operation (eg, <see cref="SaveGame"/> or <see cref="QuickSave"/>) is finished.
        /// </summary>
        event Action<GameSaveLoadArgs> OnGameSaveFinished;
        /// <summary>
        /// Occurs when a state reset operation (<see cref="Commands.ResetState"/>) is started.
        /// </summary>
        event Action OnResetStarted;
        /// <summary>
        /// Occurs when a state reset operation (<see cref="Commands.ResetState"/>) is finished.
        /// </summary>
        event Action OnResetFinished;
        /// <summary>
        /// Occurs when a state rollback operation is started.
        /// </summary>
        event Action OnRollbackStarted;
        /// <summary>
        /// Occurs when a state rollback operation is finished.
        /// </summary>
        event Action OnRollbackFinished;

        /// <summary>
        /// Last serialized global state of the engine.
        /// </summary>
        GlobalStateMap GlobalState { get; }
        /// <summary>
        /// Last serialized settings state of the engine.
        /// </summary>
        SettingsStateMap SettingsState { get; }
        /// <summary>
        /// Last serialized game state of the engine or null when the game is not started.
        /// </summary>
        [CanBeNull] GameStateMap GameState { get; }
        /// <summary>
        /// Save slots manager for game settings.
        /// </summary>
        ISaveSlotManager<SettingsStateMap> SettingsSlotManager { get; }
        /// <summary>
        /// Save slots manager for global engine state.
        /// </summary>
        ISaveSlotManager<GlobalStateMap> GlobalSlotManager { get; }
        /// <summary>
        /// Save slots manager for local engine state.
        /// </summary>
        ISaveSlotManager<GameStateMap> GameSlotManager { get; }
        /// <summary>
        /// Whether at least one quick save slot exists.
        /// </summary>
        bool QuickLoadAvailable { get; }
        /// <summary>
        /// Whether at least one auto save slot exists.
        /// </summary>
        bool AutoLoadAvailable { get; }
        /// <summary>
        /// Whether any game save slots exist.
        /// </summary>
        bool AnyGameSaveExists { get; }
        /// <summary>
        /// Whether a state rollback is in progress.
        /// </summary>
        bool RollbackInProgress { get; }

        /// <summary>
        /// Adds a task to invoke when serializing (saving) game state.
        /// Use <see cref="GameStateMap"/> to serialize arbitrary custom objects to the game save slot.
        /// </summary>
        void AddOnGameSerializeTask (Action<GameStateMap> task);
        /// <summary>
        /// Removes a task assigned via <see cref="AddOnGameSerializeTask(Action{GameStateMap})"/>.
        /// </summary>
        void RemoveOnGameSerializeTask (Action<GameStateMap> task);
        /// <summary>
        /// Adds an async task to invoke when de-serializing (loading) game state.
        /// Use <see cref="GameStateMap"/> to deserialize previously serialized custom objects from the loaded game save slot.
        /// </summary>
        void AddOnGameDeserializeTask (Func<GameStateMap, Awaitable> task);
        /// <summary>
        /// Removes a task assigned via <see cref="AddOnGameDeserializeTask(Func{GameStateMap, Awaitable})"/>.
        /// </summary>
        void RemoveOnGameDeserializeTask (Func<GameStateMap, Awaitable> task);
        /// <summary>
        /// Saves current game state to the specified save slot.
        /// </summary>
        Awaitable<GameStateMap> SaveGame (string slotId);
        /// <summary>
        /// Saves current game state to the first quick save slot.
        /// Will shift the quick save slots chain by one index before saving.
        /// </summary>
        Awaitable<GameStateMap> QuickSave ();
        /// <summary>
        /// Saves current game state to the first auto save slot.
        /// Will shift the auto save slots chain by one index before saving.
        /// </summary>
        Awaitable<GameStateMap> AutoSave ();
        /// <summary>
        /// Loads game state from the specified save slot.
        /// </summary>
        Awaitable<GameStateMap> LoadGame (string slotId);
        /// <summary>
        /// Loads game state from the most recent quick save slot.
        /// </summary>
        Awaitable<GameStateMap> QuickLoad ();
        /// <summary>
        /// Loads game state from the most recent auto save slot.
        /// </summary>
        Awaitable<GameStateMap> AutoLoad ();
        /// <summary>
        /// Persists current global state of the engine.
        /// </summary>
        Awaitable SaveGlobal ();
        /// <summary>
        /// Persists current settings state of the engine.
        /// </summary>
        Awaitable SaveSettings ();
        /// <summary>
        /// Resets engine services and unloads unused assets; will basically revert to an empty initial engine state.
        /// This will also invoke all tasks added with <see cref="AddOnGameDeserializeTask"/> with empty game state.
        /// </summary>
        /// <param name="tasks">Additional tasks to perform during the reset (will be performed in order after the engine reset).</param>
        Awaitable ResetState (params Func<Awaitable>[] tasks);
        /// <inheritdoc cref="Commands.ResetState"/>
        /// <param name="exclude">Type names of the engine services (interfaces) to exclude from reset.</param>
        Awaitable ResetState (IReadOnlyCollection<string> exclude, params Func<Awaitable>[] tasks);
        /// <inheritdoc cref="Commands.ResetState"/>
        /// <param name="exclude">Types of the engine services (interfaces) to exclude from reset.</param>
        Awaitable ResetState (IReadOnlyCollection<Type> exclude, params Func<Awaitable>[] tasks);
        /// <summary>
        /// Takes a snapshot of the current game state and adds it to the rollback stack.
        /// </summary>
        /// <param name="allowPlayerRollback">Whether player is allowed rolling back to the snapshot; see <see cref="GameStateMap.PlayerRollbackAllowed"/> for more info.</param>
        void PushRollbackSnapshot (bool allowPlayerRollback = true);
        /// <summary>
        /// Returns topmost element in the rollback stack (if any), or null.
        /// </summary>
        GameStateMap PeekRollbackStack ();
        /// <summary>
        /// Attempts to rollback (revert) all the engine services to a state evaluated with the specified predicate.
        /// Be aware, that this will discard all the state snapshots in the rollback stack until the suitable one is found.
        /// </summary>
        /// <param name="predicate">The predicate to use when finding a suitable state snapshot.</param>
        /// <param name="exhaustive">Whether to rollback to the oldest state snapshot that satisfies the predicate.</param>
        /// <returns>Whether a suitable snapshot was found and the operation succeeded.</returns>
        Awaitable<bool> Rollback (Predicate<GameStateMap> predicate, bool exhaustive = false);
        /// <summary>
        /// Checks whether a state snapshot evaluated by the specified predicate exists in the rollback stack.
        /// </summary>
        bool CanRollbackTo (Predicate<GameStateMap> predicate);
        /// <summary>
        /// Modifies existing state snapshots to prevent player from rolling back to them.
        /// </summary>
        void PurgeRollbackData ();
    }
}
