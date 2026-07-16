#if NANINOVEL_ENABLE_LIVE2D

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IBackgroundActor"/> implementation using <see cref="Live2DController"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(Live2DController), false)]
    public class Live2DBackground : Live2DActor<BackgroundMetadata>, IBackgroundActor
    {
        private BackgroundMatcher matcher;

        public Live2DBackground (string id, BackgroundMetadata meta, EmbeddedAppearanceLoader<GameObject> loader)
            : base(id, meta, loader) { }

        public override async Awaitable Initialize ()
        {
            await base.Initialize();
            matcher = BackgroundMatcher.CreateFor(ActorMeta, Renderer);
        }

        public override void Dispose ()
        {
            base.Dispose();
            matcher?.Stop();
        }
    }
}

#endif
