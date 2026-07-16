namespace Naninovel.UI
{
    public class ControlPanelQuickSaveButton : ScriptableButton
    {
        protected override void OnButtonClick () => QuickSave();

        private async void QuickSave ()
        {
            var state = Engine.GetServiceOrErr<IStateManager>();
            UIComponent.interactable = false;
            await state.QuickSave();
            UIComponent.interactable = true;
        }
    }
}
