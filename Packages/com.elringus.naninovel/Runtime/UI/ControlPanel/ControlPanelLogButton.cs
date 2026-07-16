namespace Naninovel.UI
{
    public class ControlPanelLogButton : ScriptableLabeledButton
    {
        protected override void OnButtonClick ()
        {
            var uis = Engine.GetServiceOrErr<IUIManager>();
            uis.GetUI<IPauseUI>()?.Hide();
            uis.GetUI<IBacklogUI>()?.Show();
        }
    }
}
