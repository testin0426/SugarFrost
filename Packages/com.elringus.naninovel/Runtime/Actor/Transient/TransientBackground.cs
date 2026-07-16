using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A transient <see cref="IBackgroundActor"/> implementation with lifecycle managed outside of Naninovel.
    /// </summary>
    [ActorResources(null, false)]
    [AddComponentMenu("Naninovel/ Actors/Transient Background")]
    public class TransientBackground : TransientActor<IBackgroundManager, BackgroundMetadata>, IBackgroundActor { }
}
