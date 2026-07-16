namespace Naninovel.UI
{
    public class TitleSettingsButton : ScriptableButton
    {
        protected override void OnButtonClick ()
        {
            Engine.GetServiceOrErr<IUIManager>().GetUI<ISettingsUI>()?.Show();
        }
    }
}
