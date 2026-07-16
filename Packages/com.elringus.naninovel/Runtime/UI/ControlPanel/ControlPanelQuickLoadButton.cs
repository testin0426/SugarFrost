namespace Naninovel.UI
{
    public class ControlPanelQuickLoadButton : ScriptableButton
    {
        private IStateManager state;

        protected override void Awake ()
        {
            base.Awake();

            state = Engine.GetServiceOrErr<IStateManager>();
        }

        protected override void Start ()
        {
            base.Start();

            ControlInteractability(default);
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            state.GameSlotManager.OnBeforeLoad += ControlInteractability;
            state.GameSlotManager.OnLoaded += ControlInteractability;
            state.GameSlotManager.OnBeforeSave += ControlInteractability;
            state.GameSlotManager.OnSaved += ControlInteractability;
            state.GameSlotManager.OnDeleted += ControlInteractability;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            state.GameSlotManager.OnBeforeLoad -= ControlInteractability;
            state.GameSlotManager.OnLoaded -= ControlInteractability;
            state.GameSlotManager.OnBeforeSave -= ControlInteractability;
            state.GameSlotManager.OnSaved -= ControlInteractability;
            state.GameSlotManager.OnDeleted -= ControlInteractability;
        }

        protected override void OnButtonClick ()
        {
            UIComponent.interactable = false;
            QuickLoad();
        }

        private async void QuickLoad ()
        {
            using (Engine.GetConfiguration<StateConfiguration>().ShowLoadingUI ? await LoadingScreen.Show() : null)
                await state.QuickLoad();
        }

        private void ControlInteractability (string _)
        {
            UIComponent.interactable = state.QuickLoadAvailable &&
                                       !state.GameSlotManager.Loading &&
                                       !state.GameSlotManager.Saving;
        }
    }
}
