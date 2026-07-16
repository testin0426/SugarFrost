namespace Naninovel.UI
{
    public class ControlPanelSettingsButton : ScriptableButton
    {
        protected override void OnButtonClick ()
        {
            var uis = Engine.GetServiceOrErr<IUIManager>();
            uis.GetUI<IPauseUI>()?.Hide();
            uis.GetUI<ISettingsUI>()?.Show();
        }
    }
}
