namespace Naninovel.UI
{
    public class ControlPanelHideButton : ScriptableLabeledButton
    {
        protected override void OnButtonClick ()
        {
            Engine.GetServiceOrErr<IUIManager>().SetUIVisibleWithToggle(false);
        }
    }
}
