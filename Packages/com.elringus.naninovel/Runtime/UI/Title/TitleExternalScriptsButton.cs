namespace Naninovel.UI
{
    public class TitleExternalScriptsButton : ScriptableButton
    {
        protected override void Start ()
        {
            base.Start();

            if (!Engine.GetConfiguration<ScriptsConfiguration>().EnableCommunityModding)
                gameObject.SetActive(false);
        }

        protected override void OnButtonClick ()
        {
            Engine.GetServiceOrErr<IUIManager>().GetUI<IExternalScriptsUI>()?.Show();
        }
    }
}
