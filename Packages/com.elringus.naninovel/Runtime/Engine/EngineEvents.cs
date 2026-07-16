using UnityEngine;
using UnityEngine.Events;

namespace Naninovel
{
    /// <summary>
    /// Routes essential <see cref="Engine"/> APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Engine Events")]
    public class EngineEvents : UnityEvents
    {
        [Tooltip("Occurs when availability (initialized status) of the engine changes.")]
        public BoolUnityEvent EngineAvailable;
        [Tooltip("Occurs when the engine's initialization is finished.")]
        public UnityEvent EngineInitialized;
        [Tooltip("Occurs when the engine is de-initialized (destroyed).")]
        public UnityEvent EngineDestroyed;
        [Tooltip("Occurs when the engine's initialization progress changes, in 0.0 to 1.0 range.")]
        public FloatUnityEvent InitializationProgressed;

        public void InitializeEngine ()
        {
            RuntimeInitializer.Initialize().Forget();
        }

        public void DestroyEngine ()
        {
            Engine.Destroy();
        }

        public void ResetEngine ()
        {
            Engine.Reset();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();
            Engine.OnInitializationProgress += InitializationProgressed.SafeInvoke;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();
            Engine.OnInitializationProgress -= InitializationProgressed.SafeInvoke;
        }

        protected override void HandleEngineInitialized ()
        {
            EngineAvailable?.Invoke(true);
            EngineInitialized?.Invoke();
            InitializationProgressed?.Invoke(1);
        }

        protected override void HandleEngineDestroyed ()
        {
            EngineAvailable?.Invoke(false);
            EngineDestroyed?.Invoke();
            InitializationProgressed?.Invoke(0);
        }
    }
}
