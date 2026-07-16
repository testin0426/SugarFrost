using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Naninovel
{
    /// <summary>
    /// Provides general-purpose trigger utilities routed to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Trigger Events")]
    public class TriggerEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Whether the trigger should report the activation events.")]
        public bool Activatable = true;
        [Header("Activation Constraints")]
        [GameObjectTag, Tooltip("Adds a collision constraint, making the trigger activate only when colliding with a game object that has the tag.\n\nDon't forget to add a collider component with 'Is Trigger' enabled; collision won't trigger otherwise.")]
        public string CollideWith;
        [GameObjectTag, Tooltip("Adds a raycast constraint, making the trigger activate only when the trigger collider is hit by a 'Raycast Events' component attached to a game object with the specified tag.\n\nDon't forget to add a collider component with 'Is Trigger' enabled; collision won't trigger otherwise.")]
        public string RaycastFrom;
        #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
        [Tooltip("Adds an input constraint, making the trigger activate only when the assigned input is performed.")]
        public UnityEngine.InputSystem.InputActionReference PerformInput;
        #endif
        [Tooltip("Adds a hover constraint, making the trigger activate only when the trigger collider is hovered with a pointer, such as mouse cursor.\n\nDon't forget to add a physics raycaster component to the camera; hover won't trigger otherwise.")]
        public bool HoverWithPointer;
        [Header("Trigger Events"), Space(3)]
        [Tooltip("Occurs when all the trigger activation constraints are met and input is activated.")]
        public UnityEvent TriggerActivated;
        [Tooltip("Occurs when all the trigger contains, except input, are met.")]
        public UnityEvent TriggerConstraintsMet;
        [Tooltip("Occurs when some trigger constraints, except input, no longer met.")]
        public UnityEvent TriggerConstraintsUnmet;
        [Tooltip("Occurs when the collision constraint is enabled and a game object with the specified tag has entered the trigger collider.")]
        public UnityEvent TriggerColliderEntered;
        [Tooltip("Occurs when the collision constraint is enabled and a game object with the specified tag has exited the trigger collider.")]
        public UnityEvent TriggerColliderExited;
        [Tooltip("Occurs when the raycast constraint is enabled and a raycast from the specified transform has entered the trigger collider.")]
        public UnityEvent TriggerRaycastEntered;
        [Tooltip("Occurs when the raycast constraint is enabled and a raycast from the specified transform has exited the trigger collider.")]
        public UnityEvent TriggerRaycastExited;
        [Tooltip("Occurs when the hover constraint is enabled and the specified pointer has entered the trigger collider.")]
        public UnityEvent TriggerPointerEntered;
        [Tooltip("Occurs when the hover constraint is enabled and the specified pointer has exited the trigger collider.")]
        public UnityEvent TriggerPointerExited;

        private bool colliding, raycasting, hovering, met;

        public void EnableTriggerActivation ()
        {
            Activatable = true;
        }

        public void DisableTriggerActivation ()
        {
            Activatable = false;
            if (met) TriggerConstraintsUnmet?.Invoke();
            met = false;
        }

        public void OnRaycastEnter (RaycastEvents evt)
        {
            if (!IsRaycastConstraintEnabled() || !evt.CompareTag(RaycastFrom)) return;
            raycasting = true;
            TriggerRaycastEntered?.Invoke();
        }

        public void OnRaycastExit (RaycastEvents evt)
        {
            if (!IsRaycastConstraintEnabled() || !evt.CompareTag(RaycastFrom)) return;
            raycasting = false;
            TriggerRaycastExited?.Invoke();
        }

        private void Update ()
        {
            if (!Activatable) return;

            var currentlyMet = ConstraintsMet();

            if (!met && currentlyMet)
            {
                met = true;
                TriggerConstraintsMet?.Invoke();
            }

            if (met && !currentlyMet)
            {
                met = false;
                TriggerConstraintsUnmet?.Invoke();
            }

            if (met && (!IsInputConstraintEnabled() || CheckInput()))
                TriggerActivated?.Invoke();
        }

        private bool ConstraintsMet ()
        {
            if (IsCollisionConstraintEnabled() && !CheckCollision()) return false;
            if (IsRaycastConstraintEnabled() && !CheckRaycast()) return false;
            if (IsHoverConstraintEnabled() && !CheckHover()) return false;
            return true;
        }

        private bool IsCollisionConstraintEnabled ()
        {
            return !string.IsNullOrEmpty(CollideWith);
        }

        private bool CheckCollision ()
        {
            return colliding;
        }

        private bool IsRaycastConstraintEnabled ()
        {
            return !string.IsNullOrEmpty(RaycastFrom);
        }

        private bool CheckRaycast ()
        {
            return raycasting;
        }

        private bool IsInputConstraintEnabled ()
        {
            #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
            return PerformInput;
            #else
            return false;
            #endif
        }

        private bool CheckInput ()
        {
            #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
            return PerformInput.action.WasPerformedThisFrame();
            #else
            return false;
            #endif
        }

        private bool IsHoverConstraintEnabled ()
        {
            return HoverWithPointer;
        }

        private bool CheckHover ()
        {
            return hovering;
        }

        #if PHYSICS_AVAILABLE
        private void OnTriggerEnter (Collider other) => HandleColliderEnter(other.tag);
        private void OnTriggerExit (Collider other) => HandleColliderExit(other.tag);
        #endif
        #if PHYSICS_2D_AVAILABLE
        private void OnTriggerEnter2D (Collider2D other) => HandleColliderEnter(other.tag);
        private void OnTriggerExit2D (Collider2D other) => HandleColliderExit(other.tag);
        #endif

        private void HandleColliderEnter (string tag)
        {
            if (!IsCollisionConstraintEnabled() || tag != CollideWith) return;
            colliding = true;
            TriggerColliderEntered?.Invoke();
        }

        private void HandleColliderExit (string tag)
        {
            if (!IsCollisionConstraintEnabled() || tag != CollideWith) return;
            colliding = false;
            TriggerColliderExited?.Invoke();
        }

        public void OnPointerEnter (PointerEventData eventData)
        {
            if (!IsHoverConstraintEnabled()) return;
            hovering = true;
            TriggerPointerEntered?.Invoke();
        }

        public void OnPointerExit (PointerEventData eventData)
        {
            if (!IsHoverConstraintEnabled()) return;
            hovering = false;
            TriggerPointerExited?.Invoke();
        }
    }
}
