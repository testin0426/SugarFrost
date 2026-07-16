using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Base class for routing essential Naninovel APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    public abstract class UnityEvents : MonoBehaviour
    {
        protected virtual void OnEnable ()
        {
            if (Engine.Initialized)
            {
                HandleEngineInitialized();
                Engine.OnDestroyed += HandleEngineDestroyed;
            }
            else
            {
                HandleEngineDestroyed();
                Engine.OnInitializationFinished += HandleEngineInitialized;
            }
        }

        protected virtual void OnDisable ()
        {
            Engine.OnInitializationFinished -= HandleEngineInitialized;
            Engine.OnDestroyed -= HandleEngineDestroyed;
        }

        protected abstract void HandleEngineInitialized ();
        protected abstract void HandleEngineDestroyed ();
    }
}
