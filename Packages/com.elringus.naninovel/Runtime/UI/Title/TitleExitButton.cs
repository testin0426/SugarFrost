namespace Naninovel.UI
{
    public class TitleExitButton : ScriptableButton
    {
        private IStateManager state;

        protected override void Awake ()
        {
            base.Awake();

            state = Engine.GetServiceOrErr<IStateManager>();
        }

        protected override async void OnButtonClick ()
        {
            using (new InteractionBlocker())
            {
                await TitleScreen.PlayCallback(TitleScreen.ExitCallbackLabel);
                await state.SaveGlobal();
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
                #else
                if (UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WebGLPlayer)
                    WebUtils.OpenURL("about:blank");
                else UnityEngine.Application.Quit();
                #endif
            }
        }
    }
}
