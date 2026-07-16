using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IEngineBehaviour"/> implementation for <see cref="EditorApplication"/> environment.
    /// Behaviour will be destroyed when play mode state changes.
    /// </summary>
    public class EditorBehaviour : IEngineBehaviour
    {
        public event Action OnUpdate;
        public event Action OnLateUpdate;
        public event Action OnDestroy;

        private readonly GameObject root;

        public EditorBehaviour ()
        {
            root = new("Naninovel<Editor>");
            root.hideFlags = HideFlags.DontSaveInEditor;

            EditorApplication.update += HandleEditorUpdate;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        public GameObject GetRoot () => root;

        public void Destroy ()
        {
            EditorApplication.update -= HandleEditorUpdate;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            UnityEngine.Object.DestroyImmediate(root);
        }

        public Coroutine StartCoroutine (IEnumerator routine)
        {
            throw new NotImplementedException();
        }

        public void StopCoroutine (Coroutine routine)
        {
            throw new NotImplementedException();
        }

        private void HandlePlayModeStateChanged (PlayModeStateChange change)
        {
            OnDestroy?.Invoke();
            Destroy();
        }

        private void HandleEditorUpdate ()
        {
            OnUpdate?.Invoke();
            OnLateUpdate?.Invoke();
        }
    }
}
