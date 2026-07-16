using Naninovel.FX;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// An <see cref="IActor"/> implementation representing a procedurally generated actor placeholder.
    /// </summary>
    public abstract class PlaceholderActor<TBehaviour, TMeta> : MonoBehaviourActor<TMeta>, Blur.IBlurable
        where TBehaviour : LayeredActorBehaviour
        where TMeta : OrthoActorMetadata
    {
        public virtual TBehaviour Behaviour { get; private set; }
        public override string Appearance { get => base.Appearance; set => SetAppearance(value); }
        public override bool Visible { get => base.Visible; set => SetVisibility(value); }

        protected abstract string ResourcePath { get; }
        protected virtual TransitionalRenderer TransitionalRenderer { get; private set; }

        private RenderTexture appearanceTexture;
        private string defaultAppearance;

        protected PlaceholderActor (string id, TMeta meta)
            : base(id, meta) { }

        public override async Awaitable Initialize ()
        {
            await base.Initialize();

            TransitionalRenderer = TransitionalRenderer.CreateFor(ActorMeta, GameObject, true);
            SetVisibility(false);
            var res = Engine.LoadInternalResource<GameObject>(ResourcePath);
            var obj = await Engine.Instantiate(res, new() { Name = $"{Id}Placeholder", Parent = Transform });
            Behaviour = obj.GetComponent<TBehaviour>();
            defaultAppearance = Behaviour.DefaultAppearance;
            await ChangeAppearance(defaultAppearance, new(0));
            Engine.Behaviour.OnUpdate += RenderAppearance;
        }

        public virtual Awaitable Blur (float intensity, Tween tween, AsyncToken token = default)
        {
            return TransitionalRenderer.Blur(intensity, tween, token);
        }

        public override async Awaitable ChangeAppearance (string appearance, Tween tween,
            Transition? transition = default, AsyncToken token = default)
        {
            if (string.IsNullOrEmpty(appearance)) appearance = defaultAppearance;
            Behaviour.NotifyAppearanceChanged(base.Appearance = appearance);
            var previousTexture = appearanceTexture;
            appearanceTexture = Behaviour.Render(ActorMeta.PixelsPerUnit);
            await TransitionalRenderer.TransitionTo(appearanceTexture, tween, transition, token);
            if (previousTexture) RenderTexture.ReleaseTemporary(previousTexture);
        }

        public override async Awaitable ChangeVisibility (bool visible, Tween tween, AsyncToken token = default)
        {
            base.Visible = visible;
            await TransitionalRenderer.FadeTo(visible ? TintColor.a : 0, tween, token);
        }

        public override void Dispose ()
        {
            if (Engine.Behaviour != null) Engine.Behaviour.OnUpdate -= RenderAppearance;
            if (appearanceTexture) RenderTexture.ReleaseTemporary(appearanceTexture);
            if (Behaviour) ObjectUtils.DestroyOrImmediate(Behaviour.gameObject);
            base.Dispose();
        }

        protected virtual void SetAppearance (string appearance)
        {
            ChangeAppearance(appearance, new(0)).Forget();
        }

        protected virtual void SetVisibility (bool visible)
        {
            ChangeVisibility(visible, new(0)).Forget();
        }

        protected override Color GetBehaviourTintColor ()
        {
            return TransitionalRenderer.TintColor;
        }

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            if (!Visible) tintColor.a = TransitionalRenderer.TintColor.a;
            TransitionalRenderer.TintColor = tintColor;
        }

        protected virtual void RenderAppearance ()
        {
            if (!appearanceTexture) return;
            var texture = Behaviour.Render(ActorMeta.PixelsPerUnit, appearanceTexture);
            if (texture != appearanceTexture)
            {
                RenderTexture.ReleaseTemporary(appearanceTexture);
                appearanceTexture = texture;
                TransitionalRenderer.MainTexture = texture;
            }
        }
    }
}
