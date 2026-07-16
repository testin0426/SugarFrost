namespace Naninovel.UI
{
    public class ControlPanelTipsButton : ScriptableLabeledButton
    {
        private IUIManager uis;

        protected override void Awake ()
        {
            base.Awake();

            uis = Engine.GetServiceOrErr<IUIManager>();
            if (Engine.Initialized) DisableIfNoTips();
            else Engine.OnInitializationFinished += DisableIfNoTips;
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();
            Engine.OnInitializationFinished -= DisableIfNoTips;
        }

        protected override void OnButtonClick ()
        {
            uis.GetUI<IPauseUI>()?.Hide();
            uis.GetUI<ITipsUI>()?.Show();
        }

        protected virtual void DisableIfNoTips ()
        {
            var ui = uis.GetUI<ITipsUI>();
            gameObject.SetActive(ui != null);
        }
    }
}
