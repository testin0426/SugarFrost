#if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE

using UnityEngine;
using UnityEngine.InputSystem;

namespace Naninovel
{
    // ReSharper disable ConvertToConstant.Global (input system requires public fields)
    public class ScrollInteraction : IInputInteraction
    {
        public enum ScrollDirection { Up, Down }

        [Tooltip("The direction of the scroll which activates the input.")]
        public ScrollDirection Direction = ScrollDirection.Up;

        public void Process (ref InputInteractionContext ctx)
        {
            if (ctx.ReadValue<float>() > 0)
            {
                if (Direction == ScrollDirection.Up) ctx.Performed();
                else ctx.Canceled();
            }
            else if (ctx.ReadValue<float>() < 0)
            {
                if (Direction == ScrollDirection.Down) ctx.Performed();
                else ctx.Canceled();
            }
            else ctx.Canceled();
        }

        public void Reset () { }
    }
}

#endif
