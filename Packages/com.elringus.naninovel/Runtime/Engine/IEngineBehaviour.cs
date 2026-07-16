using System;
using System.Collections;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to represent Unity's <see cref="MonoBehaviour"/> proxy.
    /// </summary>
    public interface IEngineBehaviour
    {
        /// <summary>
        /// Occurs on each render loop update phase.
        /// </summary>
        event Action OnUpdate;
        /// <summary>
        /// Occurs on each render loop late update phase.
        /// </summary>
        event Action OnLateUpdate;
        /// <summary>
        /// Occurs when the behaviour is destroyed.
        /// </summary>
        event Action OnDestroy;

        /// <summary>
        /// Returns root game object of the behaviour.
        /// </summary>
        GameObject GetRoot ();
        /// <summary>
        /// Destroys the behaviour.
        /// </summary>
        void Destroy ();
        /// <summary>
        /// Starts specified coroutine over behaviour.
        /// </summary>
        Coroutine StartCoroutine (IEnumerator routine);
        /// <summary>
        /// Stops a coroutine started with <see cref="StartCoroutine(IEnumerator)"/>.
        /// </summary>
        void StopCoroutine (Coroutine routine);
    }
}
