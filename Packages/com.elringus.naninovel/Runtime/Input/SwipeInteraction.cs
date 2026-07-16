#if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE

using UnityEngine;
using UnityEngine.InputSystem;

namespace Naninovel
{
    // ReSharper disable ConvertToConstant.Global (input system requires public fields)
    public class SwipeInteraction : IInputInteraction
    {
        public enum SwipeDirection { Up, Down, Left, Right }

        [Tooltip("The direction of the swipe.")]
        public SwipeDirection Direction = SwipeDirection.Up;
        [Tooltip("When the swipe distance is below this value (in inches), the action won't trigger.")]
        public float MinDistance = 1f;
        [Tooltip("When the swipe duration is above this value (in seconds), the action won't trigger.")]
        public float MaxTime = 0.5f;

        private const float dpiFallback = 100f;
        private Vector2 startPos;
        private double startTime;
        private bool swiping;

        public void Process (ref InputInteractionContext ctx)
        {
            var pressed = ctx.ReadValue<float>() > 0;
            if (!swiping && pressed) HandlePress(ref ctx);
            else if (swiping && !pressed) HandleRelease(ref ctx);
        }

        public void Reset () { }

        private void HandlePress (ref InputInteractionContext ctx)
        {
            swiping = true;
            startPos = GetPointerPosition();
            startTime = ctx.time;
            ctx.Started();
        }

        private void HandleRelease (ref InputInteractionContext ctx)
        {
            swiping = false;
            if (HasSwiped(ref ctx)) ctx.Performed();
            else ctx.Canceled();
        }

        private bool HasSwiped (ref InputInteractionContext ctx)
        {
            var pos = GetPointerPosition();
            var dpi = Screen.dpi > 0 ? Screen.dpi : dpiFallback;
            var distance = Vector2.Distance(startPos, pos) / dpi;
            var duration = ctx.time - startTime;
            if (distance < MinDistance || duration > MaxTime) return false;
            var vec = (pos - startPos).normalized;
            return Direction switch {
                SwipeDirection.Up => vec.y > 0.7f && Mathf.Abs(vec.x) < 0.5f,
                SwipeDirection.Down => vec.y < -0.7f && Mathf.Abs(vec.x) < 0.5f,
                SwipeDirection.Right => vec.x > 0.7f && Mathf.Abs(vec.y) < 0.5f,
                SwipeDirection.Left => vec.x < -0.7f && Mathf.Abs(vec.y) < 0.5f,
                _ => false
            };
        }

        private static Vector2 GetPointerPosition ()
        {
            return Pointer.current?.position.ReadValue() ?? Vector2.zero;
        }
    }
}

#endif
