using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Handles camera transform offset when camera look mode is activated.
    /// </summary>
    public class CameraLookController
    {
        /// <summary>
        /// Whether the controller is active and is controlling the camera offset.
        /// </summary>
        public bool Enabled { get => enabled; set => SetEnabled(value); }
        /// <summary>
        /// A bound box with X,Y sizes in units from the initial camera position, describing how far the camera can be moved.
        /// </summary>
        public Vector2 LookZone { get; set; }
        /// <summary>
        /// Camera movement sensitivity by X,Y axes.
        /// </summary>
        public Vector2 LookSpeed { get; set; }
        /// <summary>
        /// Whether to automatically move camera to the initial position when the look input is not active 
        /// (eg, mouse is in the center of the screen or analog stick is in default position).
        /// </summary>
        public bool Gravity { get; set; }

        private Vector2 position { get => transform.localPosition; set => transform.localPosition = value; }
        private Vector2 origin => Vector2.zero;
        [CanBeNull] private readonly IInputHandle input;
        private readonly Transform transform;
        private readonly Tweener<VectorTween> gravitateTweener;
        private bool enabled;

        public CameraLookController (Transform transform, [CanBeNull] IInputHandle input)
        {
            this.transform = transform;
            this.input = input;
            gravitateTweener = new();
        }

        public CameraLookState GetState () => new(Enabled, Gravity, LookZone, LookSpeed);

        public void Update ()
        {
            if (!Enabled) return;

            var offsetX = (input?.Force.x ?? 0) / 100f * LookSpeed.x;
            var offsetY = (input?.Force.y ?? 0) / 100f * LookSpeed.y;

            if (Gravity && position != origin)
            {
                var gravX = (position.x - origin.x) * LookSpeed.x * Engine.Time.DeltaTime;
                var gravY = (position.y - origin.y) * LookSpeed.y * Engine.Time.DeltaTime;
                offsetX = offsetX != 0 && Mathf.Abs(gravX) > Mathf.Abs(offsetX) ? 0 : offsetX - gravX;
                offsetY = offsetY != 0 && Mathf.Abs(gravY) > Mathf.Abs(offsetY) ? 0 : offsetY - gravY;
            }

            var bounds = new Rect(origin - LookZone / 2f, LookZone);

            if (position.x + offsetX < bounds.xMin)
                offsetX = bounds.xMin - position.x;
            else if (position.x + offsetX > bounds.xMax)
                offsetX = bounds.xMax - position.x;

            if (position.y + offsetY < bounds.yMin)
                offsetY = bounds.yMin - position.y;
            else if (position.y + offsetY > bounds.yMax)
                offsetY = bounds.yMax - position.y;

            position += new Vector2(offsetX, offsetY);
        }

        private void SetEnabled (bool value)
        {
            enabled = value;

            if (gravitateTweener.Running)
                gravitateTweener.Stop();

            if (enabled || position == origin) return;

            if (!Gravity || LookSpeed.magnitude <= 0)
            {
                position = origin;
                return;
            }

            var time = Vector3.Distance(position, origin) / LookSpeed.magnitude;
            var tween = new VectorTween(position, origin, new(time, EasingType.SmoothStep), p => position = p);
            gravitateTweener.Run(tween, target: transform).Forget();
        }
    }
}
