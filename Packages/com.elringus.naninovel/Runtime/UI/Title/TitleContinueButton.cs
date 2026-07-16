namespace Naninovel.UI
{
    public class TitleContinueButton : ScriptableButton
    {
        private IStateManager state;
        private IUIManager uis;

        protected override void Awake ()
        {
            base.Awake();

            state = Engine.GetServiceOrErr<IStateManager>();
            uis = Engine.GetServiceOrErr<IUIManager>();
        }

        protected override void Start ()
        {
            base.Start();

            ControlInteractability(default);
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            state.GameSlotManager.OnSaved += ControlInteractability;
            state.GameSlotManager.OnDeleted += ControlInteractability;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            state.GameSlotManager.OnSaved -= ControlInteractability;
            state.GameSlotManager.OnDeleted -= ControlInteractability;
        }

        protected override void OnButtonClick ()
        {
            uis.GetUI<ISaveLoadUI>()?.ShowLoad();
        }

        private void ControlInteractability (string _) => UIComponent.interactable = state.AnyGameSaveExists;
    }
}
