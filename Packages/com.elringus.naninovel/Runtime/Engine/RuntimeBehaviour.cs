using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IEngineBehaviour"/> implementation using <see cref="MonoBehaviour"/> for runtime environment.
    /// </summary>
    public class RuntimeBehaviour : MonoBehaviour, IEngineBehaviour
    {
        public event Action OnUpdate;
        public event Action OnLateUpdate;
        event Action IEngineBehaviour.OnDestroy { add => onDestroy += value; remove => onDestroy -= value; }

        private event Action onDestroy;
        private GameObject root;
        private MonoBehaviour behaviour;

        /// <param name="dontDestroyOnLoad">Whether behaviour lifetime should be independent of the loaded Unity scenes.</param>
        public static RuntimeBehaviour Create (bool dontDestroyOnLoad = true)
        {
            var go = new GameObject("Naninovel<Runtime>");
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(go);
            var behaviourComp = go.AddComponent<RuntimeBehaviour>();
            behaviourComp.root = go;
            behaviourComp.behaviour = behaviourComp;
            return behaviourComp;
        }

        public GameObject GetRoot () => root;

        public void Destroy ()
        {
            if (behaviour && behaviour.gameObject)
                Destroy(behaviour.gameObject);
        }

        private void Update ()
        {
            OnUpdate?.Invoke();
        }

        private void LateUpdate ()
        {
            OnLateUpdate?.Invoke();
        }

        private void OnDestroy ()
        {
            onDestroy?.Invoke();
        }
    }
}
