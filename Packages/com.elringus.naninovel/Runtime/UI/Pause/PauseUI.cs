using UnityEngine;

namespace Naninovel.UI
{
    /// <inheritdoc cref="IPauseUI"/>
    public class PauseUI : CustomUI, IPauseUI
    {
        public override Awaitable Initialize ()
        {
            BindInput(Inputs.Pause, ToggleVisibility, new() { WhenHidden = true });
            BindInput(Inputs.Cancel, Hide, new() { OnEnd = true });
            return Async.Completed;
        }
    }
}
