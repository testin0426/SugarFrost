using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class SceneTransitionUI : CustomUI, ISceneTransitionUI
    {
        protected virtual Camera Camera => cameras.Camera;
        protected virtual RawImage Image => image;
        protected virtual TransitionalMaterial Material { get; private set; }
        [CanBeNull] protected virtual RenderTexture SourceTexture { get; private set; }
        [CanBeNull] protected virtual RenderTexture TargetTexture { get; private set; }

        [SerializeField] private RawImage image;

        private readonly Tweener<FloatTween> tweener = new();
        private ICameraManager cameras;

        public virtual async Awaitable CaptureScene ()
        {
            if (SourceTexture) RenderTexture.ReleaseTemporary(SourceTexture);
            Material.TransitionProgress = 0;
            _ = RenderUtils.RentCameraRT(Camera, out var rt);
            Material.MainTexture = Image.texture = SourceTexture = rt;
            await RenderUtils.BlitCamera(Camera, SourceTexture);
            SetVisibility(true);
        }

        public virtual async Awaitable Transition (Transition transition, Tween tween, AsyncToken token = default)
        {
            if (tweener.Running) tweener.Complete();

            Material.UpdateRandomSeed();
            Material.TransitionProgress = 0;
            Material.TransitionName = transition.Name;
            Material.TransitionParams = transition.Parameters;
            if (transition.DissolveTexture) Material.DissolveTexture = transition.DissolveTexture;

            using var _ = RenderUtils.RentCameraRT(Camera, out var rt);
            Material.TransitionTexture = TargetTexture = rt;
            using var __ = RenderUtils.StartBlitCamera(Camera, TargetTexture);
            var tw = new FloatTween(Material.TransitionProgress, 1, tween, v => Material.TransitionProgress = v);
            await tweener.Run(tw, token, Material.Object);

            SetVisibility(false);
            Material.TransitionProgress = 0;
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(Image);

            Material = new(true);
            Image.material = Material.Object;
            cameras = Engine.GetServiceOrErr<ICameraManager>();
        }

        protected override void OnDestroy ()
        {
            if (SourceTexture) RenderTexture.ReleaseTemporary(SourceTexture);
            if (TargetTexture) RenderTexture.ReleaseTemporary(TargetTexture);
            ObjectUtils.DestroyOrImmediate(Material?.Object);

            base.OnDestroy();
        }
    }
}
