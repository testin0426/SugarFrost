using UnityEngine;

namespace Naninovel.UI
{
    public class ControlPanelTitleButton : ScriptableButton
    {
        [ManagedText("DefaultUI")]
        protected static string ConfirmationMessage = "Are you sure you want to quit to the title screen?<br>Any unsaved game progress will be lost.";

        private IScriptPlayer player;
        private IStateManager state;
        private IUIManager uis;

        protected override void Awake ()
        {
            base.Awake();

            player = Engine.GetServiceOrErr<IScriptPlayer>();
            state = Engine.GetServiceOrErr<IStateManager>();
            uis = Engine.GetServiceOrErr<IUIManager>();
        }

        protected override void OnButtonClick ()
        {
            uis.GetUI<IPauseUI>()?.Hide();

            ExitToTitle().Forget();
        }

        protected virtual async Awaitable ExitToTitle ()
        {
            if (uis.GetUI<IConfirmationUI>() is { } cui &&
                !await cui.Confirm(ConfirmationMessage)) return;

            if (state.Configuration.AutoSaveOnQuit)
                using (new InteractionBlocker())
                    await state.AutoSave();

            await player.MainTrack.ExecuteTransientCommand(
                nameof(Commands.ExitToTitle), nameof(ControlPanelTitleButton));
        }
    }
}
