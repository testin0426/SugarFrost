using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IActor"/> implementation using <typeparamref name="TBehaviour"/> to represent the actor.
    /// </summary>
    /// <remarks>
    /// Resource prefab should have a <typeparamref name="TBehaviour"/> component attached to the root object.
    /// Appearance and other property changes are routed to the events of the <typeparamref name="TBehaviour"/>.
    /// </remarks>
    public abstract class GenericActor<TBehaviour, TMeta> : MonoBehaviourActor<TMeta>
        where TBehaviour : GenericActorBehaviour
        where TMeta : ActorMetadata
    {
        /// <summary>
        /// Behaviour component of the instantiated generic prefab associated with the actor.
        /// </summary>
        public virtual TBehaviour Behaviour { get; private set; }
        public override string Appearance { get => base.Appearance; set => SetAppearance(value); }
        public override bool Visible { get => base.Visible; set => SetVisibility(value); }

        private readonly EmbeddedAppearanceLoader<GameObject> prefabLoader;
        private Color tintColor = Color.white;

        protected GenericActor (string id, TMeta meta, EmbeddedAppearanceLoader<GameObject> loader)
            : base(id, meta)
        {
            prefabLoader = loader;
        }

        public override async Awaitable Initialize ()
        {
            await base.Initialize();

            var res = await prefabLoader.LoadOrErr(Id, this);
            var obj = await Engine.Instantiate(res.Object, new() { Name = res.Object.name, Parent = Transform });
            Behaviour = obj.GetComponent<TBehaviour>();

            SetVisibility(false);
        }

        public override Awaitable ChangeAppearance (string appearance, Tween tween,
            Transition? transition = default, AsyncToken token = default)
        {
            SetAppearance(appearance);
            return Async.Completed;
        }

        public override Awaitable ChangeVisibility (bool visible, Tween tween, AsyncToken token = default)
        {
            SetVisibility(visible);
            return Async.Completed;
        }

        protected virtual void SetAppearance (string appearance)
        {
            base.Appearance = appearance;
            if (string.IsNullOrEmpty(appearance)) return;

            if (appearance.IndexOf(',') >= 0)
                foreach (var part in appearance.Split(','))
                    Behaviour.InvokeAppearanceChangedEvent(part);
            else Behaviour.InvokeAppearanceChangedEvent(appearance);
        }

        protected virtual void SetVisibility (bool visible)
        {
            base.Visible = visible;

            Behaviour.InvokeVisibilityChangedEvent(visible);
        }

        protected override Color GetBehaviourTintColor () => tintColor;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            this.tintColor = tintColor;

            Behaviour.InvokeTintColorChangedEvent(tintColor);
        }

        public override void Dispose ()
        {
            prefabLoader?.ReleaseAll(this);

            base.Dispose();
        }
    }
}
