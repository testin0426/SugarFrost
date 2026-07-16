namespace Naninovel.UI
{
    public class ControlPanelLoadButton : ScriptableButton
    {
        protected override void OnButtonClick ()
        {
            var uis = Engine.GetServiceOrErr<IUIManager>();
            uis.GetUI<IPauseUI>()?.Hide();
            uis.GetUI<ISaveLoadUI>()?.ShowLoad();
        }
    }
}
