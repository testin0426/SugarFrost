using UnityEngine;
using UnityEngine.Events;

namespace Naninovel.UI
{
    /// <summary>
    /// Routes essential <see cref="IStateManager"/> APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/State Events")]
    public class StateEvents : UnityEvents
    {
        [Tooltip("Occurs when availability of the state manager engine service changes.")]
        public BoolUnityEvent ServiceAvailable;
        [Tooltip("Occurs when state rollback gets enabled or disabled.")]
        public BoolUnityEvent RollbackEnabled;
        [Tooltip("Occurs when at least one game save slot becomes available or none is available.")]
        public BoolUnityEvent AnySaveExists;
        [Tooltip("Occurs when game state load is started.")]
        public UnityEvent LoadStarted;
        [Tooltip("Occurs when game state load is finished.")]
        public UnityEvent LoadFinished;
        [Tooltip("Occurs when game state save is started.")]
        public UnityEvent SaveStarted;
        [Tooltip("Occurs when game state save is finished.")]
        public UnityEvent SaveFinished;
        [Tooltip("Occurs when state reset is started.")]
        public UnityEvent ResetStarted;
        [Tooltip("Occurs when state reset is finished.")]
        public UnityEvent ResetFinished;
        [Tooltip("Occurs when state rollback is started.")]
        public UnityEvent RollbackStarted;
        [Tooltip("Occurs when state rollback is finished.")]
        public UnityEvent RollbackFinished;

        public void SaveGame (string slotId)
        {
            if (Engine.TryGetService<IStateManager>(out var state))
                state.SaveGame(slotId).Forget();
        }

        public void LoadGame (string slotId)
        {
            if (Engine.TryGetService<IStateManager>(out var state))
                state.LoadGame(slotId).Forget();
        }

        public void QuickSave ()
        {
            if (Engine.TryGetService<IStateManager>(out var state))
                state.QuickSave().Forget();
        }

        public void QuickLoad ()
        {
            if (Engine.TryGetService<IStateManager>(out var state))
                state.QuickLoad().Forget();
        }

        public void AutoSave ()
        {
            if (Engine.TryGetService<IStateManager>(out var state))
                state.AutoSave().Forget();
        }

        public void AutoLoad ()
        {
            if (Engine.TryGetService<IStateManager>(out var state))
                state.AutoLoad().Forget();
        }

        public void LoadLastSave ()
        {
            if (Engine.TryGetService<IUIManager>(out var uis) && uis.GetUI<SaveLoadMenu>() is { } menu)
                menu.LoadLastSave().Forget();
        }

        public void ResetState ()
        {
            if (Engine.TryGetService<IStateManager>(out var state))
                state.ResetState().Forget();
        }

        public void Rollback (int lineIndex)
        {
            if (Engine.TryGetService<IStateManager>(out var state))
                state.Rollback(s => s.PlaybackSpot.LineIndex <= lineIndex).Forget();
        }

        protected override void HandleEngineInitialized ()
        {
            if (Engine.TryGetService<IStateManager>(out var state))
            {
                ServiceAvailable?.Invoke(true);
                RollbackEnabled?.Invoke(state.Configuration.EnableStateRollback);
                AnySaveExists?.Invoke(state.AnyGameSaveExists);

                state.OnGameLoadStarted -= HandleLoadStarted;
                state.OnGameLoadStarted += HandleLoadStarted;
                state.OnGameLoadFinished -= HandleLoadFinished;
                state.OnGameLoadFinished += HandleLoadFinished;

                state.OnGameSaveStarted -= HandleSaveStarted;
                state.OnGameSaveStarted += HandleSaveStarted;
                state.OnGameSaveFinished -= HandleSaveFinished;
                state.OnGameSaveFinished += HandleSaveFinished;

                state.OnResetStarted -= ResetStarted.SafeInvoke;
                state.OnResetStarted += ResetStarted.SafeInvoke;
                state.OnResetFinished -= ResetFinished.SafeInvoke;
                state.OnResetFinished += ResetFinished.SafeInvoke;

                state.OnRollbackStarted -= RollbackStarted.SafeInvoke;
                state.OnRollbackStarted += RollbackStarted.SafeInvoke;
                state.OnRollbackFinished -= RollbackFinished.SafeInvoke;
                state.OnRollbackFinished += RollbackFinished.SafeInvoke;

                state.GameSlotManager.OnDeleted -= HandleGameDeleted;
                state.GameSlotManager.OnDeleted += HandleGameDeleted;
            }
            else ServiceAvailable?.Invoke(false);
        }

        protected override void HandleEngineDestroyed ()
        {
            ServiceAvailable?.Invoke(false);
            RollbackEnabled?.Invoke(false);
        }

        protected virtual void HandleLoadStarted (GameSaveLoadArgs _)
        {
            LoadStarted?.Invoke();
        }

        protected virtual void HandleLoadFinished (GameSaveLoadArgs _)
        {
            LoadFinished?.Invoke();
        }

        protected virtual void HandleSaveStarted (GameSaveLoadArgs _)
        {
            SaveStarted?.Invoke();
        }

        protected virtual void HandleSaveFinished (GameSaveLoadArgs _)
        {
            SaveFinished?.Invoke();

            if (Engine.TryGetService<IStateManager>(out var state))
                AnySaveExists?.Invoke(state.AnyGameSaveExists);
        }

        protected virtual void HandleGameDeleted (string _)
        {
            if (Engine.TryGetService<IStateManager>(out var state))
                AnySaveExists?.Invoke(state.AnyGameSaveExists);
        }
    }
}
