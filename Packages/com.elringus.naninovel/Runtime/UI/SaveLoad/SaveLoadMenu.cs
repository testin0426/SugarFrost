using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class SaveLoadMenu : CustomUI, ISaveLoadUI
    {
        [Serializable]
        private class GlobalState
        {
            public SaveType LastSaveType;
            [CanBeNull] public string LastSlotId;
        }

        [ManagedText("DefaultUI")]
        protected static string OverwriteSaveSlotMessage = "Are you sure you want to overwrite save slot?";
        [ManagedText("DefaultUI")]
        protected static string DeleteSaveSlotMessage = "Are you sure you want to delete save slot?";

        protected virtual Toggle QuickLoadToggle => quickLoadToggle;
        protected virtual Toggle AutoLoadToggle => autoLoadToggle;
        protected virtual Toggle SaveToggle => saveToggle;
        protected virtual Toggle LoadToggle => loadToggle;
        protected virtual GameStateSlotsGrid QuickLoadGrid => quickLoadGrid;
        protected virtual GameStateSlotsGrid AutoLoadGrid => autoLoadGrid;
        protected virtual GameStateSlotsGrid SaveGrid => saveGrid;
        protected virtual GameStateSlotsGrid LoadGrid => loadGrid;

        [Header("Tabs")]
        [SerializeField] private Toggle quickLoadToggle;
        [SerializeField] private Toggle autoLoadToggle;
        [SerializeField] private Toggle saveToggle;
        [SerializeField] private Toggle loadToggle;

        [Header("Grids")]
        [SerializeField] private GameStateSlotsGrid quickLoadGrid;
        [SerializeField] private GameStateSlotsGrid autoLoadGrid;
        [SerializeField] private GameStateSlotsGrid saveGrid;
        [SerializeField] private GameStateSlotsGrid loadGrid;

        [Header("Events")]
        [SerializeField] private IntUnityEvent onTabChanged;
        [SerializeField] private IntUnityEvent onPageChanged;
        [SerializeField] private StringUnityEvent onSlotClicked;

        private IStateManager state;
        private IConfirmationUI confirmationUI;
        private ISaveSlotManager<GameStateMap> slots => state?.GameSlotManager;
        private int tabIndex;

        public override async Awaitable Initialize ()
        {
            state.OnGameSaveFinished += HandleGameSaveFinished;

            await Async.All(
                QuickLoadGrid.Initialize(state.Configuration.QuickSaveSlotLimit,
                    HandleQuickLoadSlotClicked, HandleDeleteQuickLoadSlotClicked, LoadQuickSaveSlot),
                AutoLoadGrid.Initialize(state.Configuration.AutoSaveSlotLimit,
                    HandleAutoLoadSlotClicked, HandleDeleteAutoLoadSlotClicked, LoadAutoSaveSlot),
                SaveGrid.Initialize(state.Configuration.SaveSlotLimit,
                    HandleSaveSlotClicked, HandleDeleteSlotClicked, LoadSaveSlot),
                LoadGrid.Initialize(state.Configuration.SaveSlotLimit,
                    HandleLoadSlotClicked, HandleDeleteSlotClicked, LoadSaveSlot)
            );
            if (Visible) Engine.GetServiceOrErr<IUIManager>().FocusTop();

            BindInput(Inputs.Page, HandlePageInput);
            BindInput(Inputs.Tab, HandleTabInput);
            BindInput(Inputs.Cancel, Hide, new() { OnEnd = true });
        }

        public virtual void ShowLoad ()
        {
            LoadToggle.gameObject.SetActive(true);
            QuickLoadToggle.gameObject.SetActive(true);
            AutoLoadToggle.gameObject.SetActive(true);

            var type = state.GlobalState.GetState<GlobalState>()?.LastSaveType ?? SaveType.Normal;
            if (type == SaveType.Quick) QuickLoadToggle.isOn = true;
            else if (type == SaveType.Auto) AutoLoadToggle.isOn = true;
            else LoadToggle.isOn = true;

            SaveToggle.gameObject.SetActive(false);

            Show();
        }

        [CanBeNull]
        public virtual string GetLastSlotId ()
        {
            return state.GlobalState.GetState<GlobalState>()?.LastSlotId;
        }

        public virtual Awaitable LoadLastSave ()
        {
            if (GetLastSlotId() is { } slotId)
                return HandleLoadSlotClicked(slotId);
            throw new Error("Failed to load last save: never saved.");
        }

        public virtual void ShowSave ()
        {
            SaveToggle.gameObject.SetActive(true);
            SaveToggle.isOn = true;

            LoadToggle.gameObject.SetActive(false);
            QuickLoadToggle.gameObject.SetActive(false);
            AutoLoadToggle.gameObject.SetActive(false);

            Show();
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(QuickLoadToggle, SaveToggle, LoadToggle, QuickLoadGrid, SaveGrid, LoadGrid);

            state = Engine.GetServiceOrErr<IStateManager>();
            confirmationUI = Engine.GetServiceOrErr<IUIManager>().GetUIOrErr<IConfirmationUI>();

            state.OnGameSaveStarted += HandleGameSaveStarted;
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();

            if (state != null)
            {
                state.OnGameSaveStarted -= HandleGameSaveStarted;
                state.OnGameSaveFinished -= HandleGameSaveFinished;
            }
        }

        protected virtual void HandleLoadSlotClicked (int slotNumber)
        {
            var slotId = state.Configuration.IndexToSaveSlotId(slotNumber);
            HandleLoadSlotClicked(slotId).Forget();
        }

        protected virtual void HandleQuickLoadSlotClicked (int slotNumber)
        {
            var slotId = state.Configuration.IndexToQuickSaveSlotId(slotNumber);
            HandleLoadSlotClicked(slotId).Forget();
        }

        protected virtual void HandleAutoLoadSlotClicked (int slotNumber)
        {
            var slotId = state.Configuration.IndexToAutoSaveSlotId(slotNumber);
            HandleLoadSlotClicked(slotId).Forget();
        }

        protected virtual async Awaitable HandleLoadSlotClicked (string slotId)
        {
            if (!slots.SaveSlotExists(slotId)) return;
            onSlotClicked?.Invoke(slotId);
            await TitleScreen.PlayCallback(TitleScreen.LoadCallbackLabel);
            Hide();
            Engine.GetService<IUIManager>()?.GetUI<ITitleUI>()?.Hide();
            using (Engine.GetConfiguration<StateConfiguration>().ShowLoadingUI ? await LoadingScreen.Show() : null)
                await state.LoadGame(slotId);
        }

        protected virtual void HandleSaveSlotClicked (int slotNumber)
        {
            var slotId = state.Configuration.IndexToSaveSlotId(slotNumber);
            HandleSaveSlotClicked(slotId, slotNumber);
        }

        protected virtual void HandleQuickSaveSlotClicked (int slotNumber)
        {
            var slotId = state.Configuration.IndexToQuickSaveSlotId(slotNumber);
            HandleSaveSlotClicked(slotId, slotNumber);
        }

        protected virtual async void HandleSaveSlotClicked (string slotId, int slotNumber)
        {
            if (slots.SaveSlotExists(slotId) &&
                !await confirmationUI.Confirm(OverwriteSaveSlotMessage)) return;

            onSlotClicked?.Invoke(slotId);

            using (new InteractionBlocker())
            {
                var state = await this.state.SaveGame(slotId);
                SaveGrid.BindSlot(slotNumber, state);
                LoadGrid.BindSlot(slotNumber, state);
            }
        }

        protected virtual async void HandleDeleteSlotClicked (int slotNumber)
        {
            var slotId = state.Configuration.IndexToSaveSlotId(slotNumber);
            if (!slots.SaveSlotExists(slotId)) return;

            if (!await confirmationUI.Confirm(DeleteSaveSlotMessage)) return;

            slots.DeleteSaveSlot(slotId);
            SaveGrid.BindSlot(slotNumber, null);
            LoadGrid.BindSlot(slotNumber, null);
        }

        protected virtual async void HandleDeleteQuickLoadSlotClicked (int slotNumber)
        {
            var slotId = state.Configuration.IndexToQuickSaveSlotId(slotNumber);
            if (!slots.SaveSlotExists(slotId)) return;

            if (!await confirmationUI.Confirm(DeleteSaveSlotMessage)) return;

            slots.DeleteSaveSlot(slotId);
            QuickLoadGrid.BindSlot(slotNumber, null);
        }

        protected virtual async void HandleDeleteAutoLoadSlotClicked (int slotNumber)
        {
            var slotId = state.Configuration.IndexToAutoSaveSlotId(slotNumber);
            if (!slots.SaveSlotExists(slotId)) return;

            if (!await confirmationUI.Confirm(DeleteSaveSlotMessage)) return;

            slots.DeleteSaveSlot(slotId);
            AutoLoadGrid.BindSlot(slotNumber, null);
        }

        protected virtual void HandleGameSaveStarted (GameSaveLoadArgs args)
        {
            state.GlobalState.SetState<GlobalState>(new() {
                LastSaveType = args.Type,
                LastSlotId = args.SlotId
            });
        }

        protected virtual void HandleGameSaveFinished (GameSaveLoadArgs args)
        {
            if (args.Type == SaveType.Quick) ShiftSlots(QuickLoadGrid, args.SlotId).Forget();
            if (args.Type == SaveType.Auto) ShiftSlots(AutoLoadGrid, args.SlotId).Forget();
        }

        protected virtual async Awaitable ShiftSlots (GameStateSlotsGrid grid, string slotId)
        {
            for (int i = grid.Slots.Count - 2; i >= 0; i--)
            {
                var currSlot = grid.Slots[i];
                var prevSlot = grid.Slots[i + 1];
                prevSlot.Bind(prevSlot.SlotNumber, currSlot.State);
            }
            var slotState = await state.GameSlotManager.Load(slotId);
            grid.BindSlot(1, slotState);
        }

        protected virtual async Awaitable<GameStateMap> LoadSaveSlot (int slotNumber)
        {
            var slotId = this.state.Configuration.IndexToSaveSlotId(slotNumber);
            var state = slots.SaveSlotExists(slotId) ? await slots.Load(slotId) : null;
            return state;
        }

        protected virtual async Awaitable<GameStateMap> LoadQuickSaveSlot (int slotNumber)
        {
            var slotId = this.state.Configuration.IndexToQuickSaveSlotId(slotNumber);
            var state = slots.SaveSlotExists(slotId) ? await slots.Load(slotId) : null;
            return state;
        }

        protected virtual async Awaitable<GameStateMap> LoadAutoSaveSlot (int slotNumber)
        {
            var slotId = this.state.Configuration.IndexToAutoSaveSlotId(slotNumber);
            var state = slots.SaveSlotExists(slotId) ? await slots.Load(slotId) : null;
            return state;
        }

        protected virtual void HandlePageInput (Vector2 force)
        {
            var grid = GetActiveGrid();
            if (force.x < 0) grid.SelectPreviousPage();
            if (force.x > 0) grid.SelectNextPage();
            EventUtils.Select(FindFocusObject());
            onPageChanged?.Invoke(grid.CurrentPage);
        }

        protected virtual void HandleTabInput (Vector2 force)
        {
            using var _ = ListPool<Toggle>.Rent(out var toggles);
            if (SaveToggle && SaveToggle.gameObject.activeInHierarchy) toggles.Add(SaveToggle);
            if (QuickLoadToggle && QuickLoadToggle.gameObject.activeInHierarchy) toggles.Add(QuickLoadToggle);
            if (AutoLoadToggle && AutoLoadToggle.gameObject.activeInHierarchy) toggles.Add(AutoLoadToggle);
            if (LoadToggle && LoadToggle.gameObject.activeInHierarchy) toggles.Add(LoadToggle);
            if (toggles.Count <= 1) return;

            if (force.x < 0) tabIndex--;
            if (force.x > 0) tabIndex++;
            tabIndex = Mathf.Clamp(tabIndex, 0, toggles.Count - 1);
            for (int i = 0; i < toggles.Count; i++)
                toggles[i].isOn = i == tabIndex;
            EventUtils.Select(FindFocusObject());
            onTabChanged?.Invoke(tabIndex);
        }

        protected virtual GameStateSlotsGrid GetActiveGrid ()
        {
            if (SaveToggle.isOn) return SaveGrid;
            if (LoadToggle.isOn) return LoadGrid;
            if (AutoLoadToggle.isOn) return AutoLoadGrid;
            return QuickLoadGrid;
        }

        protected override GameObject FindFocusObject ()
        {
            if (InputMode.IsPointerBased()) return null;
            var grid = GetActiveGrid();
            if (!grid || grid.Slots == null || grid.Slots.Count == 0) return null;

            var slotToFocus = default(GameStateSlot);
            foreach (var slot in grid.Slots)
                if (slot.gameObject.activeInHierarchy && (!slotToFocus || slot.LastSelectTime > slotToFocus.LastSelectTime))
                    slotToFocus = slot;

            return slotToFocus ? slotToFocus.gameObject : null;
        }
    }
}
