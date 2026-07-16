#if NANINOVEL_ENABLE_SPINE

using System;
using System.Threading.Tasks;
using Naninovel.FX;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="MonoBehaviourActor{TMeta}"/> using <see cref="SpineController"/> to represent the actor.
    /// </summary>
    /// <remarks>
    /// Spine prefab is expected to have a <see cref="SpineController"/> component attached to the root object.
    /// </remarks>
    public abstract class SpineActor<TMeta> : MonoBehaviourActor<TMeta>, Blur.IBlurable
        where TMeta : OrthoActorMetadata
    {
        /// <summary>
        /// Controller component of the instantiated spine prefab associated with the actor.
        /// </summary>
        public virtual SpineController Controller { get; private set; }
        public override string Appearance { get => base.Appearance; set => SetAppearance(value); }
        public override bool Visible { get => base.Visible; set => SetVisibility(value); }

        protected virtual TransitionalRenderer Renderer { get; private set; }
        protected virtual SpineDrawer Drawer { get; private set; }

        private readonly LocalizableResourceLoader<GameObject> prefabLoader;

        protected SpineActor (string id, TMeta meta, EmbeddedAppearanceLoader<GameObject> loader)
            : base(id, meta)
        {
            prefabLoader = loader;
        }

        public override async Awaitable Initialize ()
        {
            await base.Initialize();

            Controller = await InitializeController();
            Renderer = TransitionalRenderer.CreateFor(ActorMeta, GameObject, true);
            Drawer = new SpineDrawer(Controller);

            SetVisibility(false);

            Engine.Behaviour.OnUpdate += DrawSpine;
        }

        public override void Dispose ()
        {
            if (Engine.Behaviour != null)
                Engine.Behaviour.OnUpdate -= DrawSpine;

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
            base.Appearance = appearance;
            if (Controller)
                Controller.ChangeAppearance(appearance, tween, transition);
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
            if (Controller)
                Controller.ChangeAppearance(appearance, new(0));
        }

        protected virtual void SetVisibility (bool visible) => ChangeVisibility(visible, new(0)).Forget();

        protected override Color GetBehaviourTintColor () => Renderer.TintColor;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            if (!Visible) // Handle visibility-controlled alpha of the tint color.
                tintColor.a = Renderer.TintColor.a;
            Renderer.TintColor = tintColor;
        }

        protected virtual void DrawSpine () => Drawer.DrawTo(Renderer, ActorMeta.PixelsPerUnit);

        protected virtual async Task<SpineController> InitializeController ()
        {
            var prefabResource = await prefabLoader.Load(Id, this);
            if (!prefabResource.Valid)
                throw new Exception($"Failed to load Spine prefab for '{Id}' actor. Make sure the resource is set up correctly in the actor configuration.");
            var controller = (await Engine.Instantiate(prefabResource.Object)).GetComponent<SpineController>();
            controller.gameObject.name = Id;
            controller.transform.SetParent(Transform);
            return controller;
        }
    }
}

#endif
