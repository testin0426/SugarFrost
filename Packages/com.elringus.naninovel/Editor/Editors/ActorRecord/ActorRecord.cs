using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Used as an alternative for editing actor metadata in editor menus.
    /// </summary>
    public abstract class ActorRecord : ScriptableObject
    {
        public string ActorId => string.IsNullOrWhiteSpace(actorId) ? name : actorId;

        [SerializeField] private string actorId;

        public abstract ActorMetadata GetMetadata ();
    }

    /// <inheritdoc cref="ActorRecord"/>
    public abstract class ActorRecord<TMeta> : ActorRecord
        where TMeta : ActorMetadata
    {
        public TMeta Metadata => metadata;

        [SerializeField] private TMeta metadata;

        public override ActorMetadata GetMetadata () => metadata;
    }
}
