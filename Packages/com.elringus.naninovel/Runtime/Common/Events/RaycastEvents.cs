using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Naninovel
{
    /// <summary>
    /// Provides general-purpose raycast utilities routed to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Raycast Events")]
    public class RaycastEvents : MonoBehaviour
    {
        [Tooltip("The raycast component to use, usually applied to the main camera. When not assigned will sphere-cast from the host transform origin.")]
        public BaseRaycaster Raycaster;
        [Tooltip("The raycast position on screen, where [0.0, 0.0] is the top-left corner and [1.0, 1.0] is the bottom-right corner or the origin offset when 'Raycaster' is not assigned.")]
        public Vector3 Position = new(.5f, .5f);
        [Min(.001f), Tooltip("The maximum distance of the raycast, in units.")]
        public float Distance = 3f;

        [Tooltip("Occurs when the raycast has entered a trigger collider.")]
        public UnityEvent RaycastTriggerEntered;
        [Tooltip("Occurs when the raycast has exited a trigger collider.")]
        public UnityEvent RaycastTriggerExited;

        private readonly List<RaycastResult> rays = new();
        private readonly List<TriggerEvents> exits = new();
        private readonly HashSet<TriggerEvents> hits = new();
        private readonly HashSet<TriggerEvents> entered = new();
        #if PHYSICS_AVAILABLE
        private readonly Collider[] phys = new Collider[100];
        #endif

        private PointerEventData data;

        private void OnDisable ()
        {
            entered.Clear();
        }

        private void Update ()
        {
            rays.Clear();
            hits.Clear();

            if (Raycaster)
            {
                data ??= new(EventSystem.current);
                data.position = new(Screen.width * Position.x, Screen.height * Position.y);
                Raycaster.Raycast(data, rays);
            }
            #if PHYSICS_AVAILABLE
            else
            {
                var count = Physics.OverlapSphereNonAlloc(transform.position + Position, Distance, phys);
                for (int i = 0; i < count; i++)
                    rays.Add(new() { gameObject = phys[i].gameObject, distance = 0 });
            }
            #endif

            if (rays.Count > 0) EnterTriggers();
            if (entered.Count > 0) ExitTriggers();
        }

        private void EnterTriggers ()
        {
            var count = entered.Count;
            foreach (var ray in rays)
                if (ray.distance < Distance && ray.gameObject.TryGetComponent<TriggerEvents>(out var hit))
                    if (hits.Add(hit) && entered.Add(hit))
                        hit.OnRaycastEnter(this);
            if (entered.Count > count)
                RaycastTriggerEntered?.Invoke();
        }

        private void ExitTriggers ()
        {
            exits.Clear();
            foreach (var trigger in entered)
                if (!hits.Contains(trigger))
                    exits.Add(trigger);
            foreach (var exit in exits)
                if (entered.Remove(exit))
                    exit.OnRaycastExit(this);
            if (exits.Count > 0)
                RaycastTriggerExited?.Invoke();
        }
    }
}
