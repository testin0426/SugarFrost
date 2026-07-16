#if NANINOVEL_ENABLE_LIVE2D

using System.Threading.Tasks;
using Naninovel.FX;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="MonoBehaviourActor{TMeta}"/> using <see cref="Live2DController"/> to represent the actor.
    /// </summary>
    /// <remarks>
    /// Live2D actor prefab is expected to have a <see cref="Controller"/> component attached to the root object.
    /// </remarks>
    public abstract class Live2DActor<TMeta> : MonoBehaviourActor<TMeta>, Blur.IBlurable
        where TMeta : OrthoActorMetadata
    {
        /// <summary>
        /// Controller component of the instantiated Live2D prefab associated with the actor.
        /// </summary>
        public virtual Live2DController Controller { get; private set; }
        public override string Appearance { get => base.Appearance; set => SetAppearance(value); }
        public override bool Visible { get => base.Visible; set => SetVisibility(value); }

        protected virtual TransitionalRenderer Renderer { get; private set; }
        protected virtual Live2DDrawer Drawer { get; private set; }

        private readonly LocalizableResourceLoader<GameObject> prefabLoader;

        protected Live2DActor (string id, TMeta meta, EmbeddedAppearanceLoader<GameObject> loader)
            : base(id, meta)
        {
            prefabLoader = loader;
        }

        public override async Awaitable Initialize ()
        {
            await base.Initialize();

            Controller = await InitializeController(Id, Transform);
            Renderer = TransitionalRenderer.CreateFor(ActorMeta, GameObject, true);
            Drawer = new Live2DDrawer(Controller);

            SetVisibility(false);

            Engine.Behaviour.OnUpdate += DrawLive2D;
        }

        public override void Dispose ()
        {
            if (Engine.Behaviour != null)
                Engine.Behaviour.OnUpdate -= DrawLive2D;

            Drawer.Dispose();

            base.Dispose();

            prefabLoader?.ReleaseAll(this);
        }

        public virtual Awaitable Blur (float intensity, Tween tween, AsyncToken token = default)
        {
            return Renderer.Blur(intensity, tween, token);
        }

        public override Awaitable ChangeAppearance (string appearance, Tween tween,
            Transition? transition = default, AsyncToken token = default)
        {
            SetAppearance(appearance);
            return Async.Completed;
        }

        public override async Awaitable ChangeVisibility (bool visible, Tween tween, AsyncToken token = default)
        {
            base.Visible = visible;

            await Renderer.FadeTo(visible ? TintColor.a : 0, tween, token);
        }

        protected virtual void SetAppearance (string appearance)
        {
            base.Appearance = appearance;
            if (!Controller || string.IsNullOrEmpty(appearance)) return;

            if (appearance.IndexOf(',') >= 0)
                foreach (var part in appearance.Split(','))
                    Controller.SetAppearance(part);
            else Controller.SetAppearance(appearance);
        }

        protected virtual void SetVisibility (bool visible) => ChangeVisibility(visible, new(0)).Forget();

        protected override Color GetBehaviourTintColor () => Renderer.TintColor;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            if (!Visible) // Handle visibility-controlled alpha of the tint color.
                tintColor.a = Renderer.TintColor.a;
            Renderer.TintColor = tintColor;
        }

        protected virtual void DrawLive2D () => Drawer.DrawTo(Renderer, ActorMeta.PixelsPerUnit);

        protected virtual async Task<Live2DController> InitializeController (string actorId, Transform transform)
        {
            var resource = await prefabLoader.Load(actorId, this);
            if (!resource.Valid) throw new Error($"Failed to load Live2D model prefab for '{actorId}' actor. Make sure the resource is set up correctly in the character configuration.");
            var controller = (await Engine.Instantiate(resource.Object)).GetComponent<Live2DController>();
            controller.gameObject.name = actorId;
            controller.transform.SetParent(transform);
            return controller;
        }
    }
}

#endif
