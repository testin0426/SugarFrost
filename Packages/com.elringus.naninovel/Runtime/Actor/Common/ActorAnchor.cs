using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A virtual reference point associated with a <see cref="MonoBehaviourActor{TMeta}"/> game object.
    /// </summary>
    public interface IActorAnchor
    {
        /// <summary>
        /// Occurs when the anchor's <see cref="Position"/> changes.
        /// </summary>
        event Action<Vector3> OnPositionChanged;

        /// <summary>
        /// Current world-space position of the anchor.
        /// </summary>
        Vector3 Position { get; }
    }

    /// <summary>
    /// Allows assigning an <see cref="IActorAnchor"/> by attaching the component to game objects.
    /// </summary>
    [AddComponentMenu("Naninovel/ Actors/Actor Anchor")]
    public class ActorAnchor : MonoBehaviour, IActorAnchor
    {
        public event Action<Vector3> OnPositionChanged;

        [Tooltip("The identifier of the associated actor.")]
        public string ActorId;
        [Tooltip("The identifier of the anchor.")]
        public string AnchorId;

        public Vector3 Position => transform.position;

        private Vector3 lastPosition;

        private void Start ()
        {
            ActorAnchors.Set(ActorId, AnchorId, this);
        }

        private void OnDestroy ()
        {
            ActorAnchors.Remove(ActorId, AnchorId);
        }

        private void LateUpdate ()
        {
            var currentPosition = Position;
            if (currentPosition == lastPosition) return;
            OnPositionChanged?.Invoke(currentPosition);
            lastPosition = currentPosition;
        }
    }
}
