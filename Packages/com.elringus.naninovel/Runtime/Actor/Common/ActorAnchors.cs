using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Naninovel
{
    /// <summary>
    /// Manages <see cref="IActorAnchor"/> instances at runtime.
    /// </summary>
    public static class ActorAnchors
    {
        private static readonly Dictionary<string, Dictionary<string, IActorAnchor>>
            anchorByIdByActorId = new(StringComparer.OrdinalIgnoreCase);

        [CanBeNull]
        public static IActorAnchor Get (string actorId, string anchorId)
        {
            return anchorByIdByActorId.GetValueOrDefault(actorId)?.GetValueOrDefault(anchorId);
        }

        public static void Set (string actorId, string anchorId, IActorAnchor anchor)
        {
            if (!anchorByIdByActorId.TryGetValue(actorId, out var anchorById))
                anchorByIdByActorId[actorId] = anchorById = new(StringComparer.OrdinalIgnoreCase);
            anchorById[anchorId] = anchor;
        }

        public static void Remove (string actorId, string anchorId)
        {
            if (anchorByIdByActorId.TryGetValue(actorId, out var anchorById))
                anchorById.Remove(anchorId);
        }
    }
}
