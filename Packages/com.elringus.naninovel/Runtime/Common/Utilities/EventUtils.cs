using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Naninovel
{
    public static class EventUtils
    {
        /// <summary>
        /// Currently focused game object or null.
        /// </summary>
        public static GameObject Selected => !EventSystem.current ? null : EventSystem.current.currentSelectedGameObject;

        /// <summary>
        /// Focuses specified game object with the current event system;
        /// does nothing when it's not and blurs (removes focus) when go is null.
        /// </summary>
        public static void Select ([CanBeNull] GameObject go)
        {
            var eventSystem = EventSystem.current;
            if (!eventSystem) return;
            eventSystem.SetSelectedGameObject(go);
        }

        /// <summary>
        /// Returns top-most game object over the current pointer position, or null when none.
        /// </summary>
        [CanBeNull]
        public static GameObject GetHoveredObject ()
        {
            if (!EventSystem.current || Engine.GetService<IInputManager>()?.GetPointerPosition() is not { } pos) return null;
            using var _ = ListPool<RaycastResult>.Rent(out var results);
            EventSystem.current.RaycastAll(new(EventSystem.current) { position = pos }, results);
            var topmost = default(RaycastResult?);
            foreach (var result in results)
                if (!topmost.HasValue || topmost.Value.depth > result.depth)
                    topmost = result;
            return topmost?.gameObject;
        }

        public static void SafeInvoke (this Action act) => act?.Invoke();
        public static void SafeInvoke<T0> (this Action<T0> act, T0 a0) => act?.Invoke(a0);
        public static void SafeInvoke<T0, T1> (this Action<T0, T1> act, T0 a0, T1 a1) => act?.Invoke(a0, a1);
        public static void SafeInvoke<T0, T1, T2> (this Action<T0, T1, T2> act, T0 a0, T1 a1, T2 a2) => act?.Invoke(a0, a1, a2);
        public static void SafeInvoke (this UnityEvent act) => act?.Invoke();
        public static void SafeInvoke<T0> (this UnityEvent<T0> act, T0 a0) => act?.Invoke(a0);
        public static void SafeInvoke<T0, T1> (this UnityEvent<T0, T1> act, T0 a0, T1 a1) => act?.Invoke(a0, a1);
        public static void SafeInvoke<T0, T1, T2> (this UnityEvent<T0, T1, T2> act, T0 a0, T1 a1, T2 a2) => act?.Invoke(a0, a1, a2);
    }

    [Serializable]
    public class StringUnityEvent : UnityEvent<string> { }

    [Serializable]
    public class FloatUnityEvent : UnityEvent<float> { }

    [Serializable]
    public class IntUnityEvent : UnityEvent<int> { }

    [Serializable]
    public class BoolUnityEvent : UnityEvent<bool> { }

    [Serializable]
    public class Vector3UnityEvent : UnityEvent<Vector3> { }

    [Serializable]
    public class Vector2UnityEvent : UnityEvent<Vector2> { }

    [Serializable]
    public class QuaternionUnityEvent : UnityEvent<Quaternion> { }

    [Serializable]
    public class ColorUnityEvent : UnityEvent<Color> { }
}
