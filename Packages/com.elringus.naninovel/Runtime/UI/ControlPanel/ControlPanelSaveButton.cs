namespace Naninovel.UI
{
    public class ControlPanelSaveButton : ScriptableButton
    {
        protected override void OnButtonClick ()
        {
            var uis = Engine.GetServiceOrErr<IUIManager>();
            uis.GetUI<IPauseUI>()?.Hide();
            uis.GetUI<ISaveLoadUI>()?.ShowSave();
        }
    }
}
