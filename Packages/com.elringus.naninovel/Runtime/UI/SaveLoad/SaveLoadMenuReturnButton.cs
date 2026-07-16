
namespace Naninovel.UI
{
    public class SaveLoadMenuReturnButton : ScriptableButton
    {
        private ISaveLoadUI saveLoadMenu;

        protected override void Awake ()
        {
            base.Awake();

            saveLoadMenu = GetComponentInParent<ISaveLoadUI>();
        }

        protected override void OnButtonClick () => saveLoadMenu.Hide();
    }
}
