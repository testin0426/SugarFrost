using System.Linq;
using JetBrains.Annotations;
using Naninovel.FX;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="MonoBehaviourActor{TMeta}"/> using <see cref="TransitionalSpriteRenderer"/> to represent appearance of the actor.
    /// </summary>
    public abstract class SpriteActor<TMeta> : MonoBehaviourActor<TMeta>, Blur.IBlurable
        where TMeta : OrthoActorMetadata
    {
        public override string Appearance { get => base.Appearance; set => SetAppearance(value); }
        public override bool Visible { get => base.Visible; set => SetVisibility(value); }

        protected virtual StandaloneAppearanceLoader<Texture2D> AppearanceLoader { get; }
        protected virtual TransitionalRenderer TransitionalRenderer { get; private set; }

        [CanBeNull] private static Texture2D missingTextureCache;
        private string defaultAppearancePath;
        private Resource<Texture2D> defaultAppearance;

        protected SpriteActor (string id, TMeta meta, StandaloneAppearanceLoader<Texture2D> loader)
            : base(id, meta)
        {
            AppearanceLoader = loader;
        }

        public override async Awaitable Initialize ()
        {
            await base.Initialize();

            AppearanceLoader.OnLocalized += HandleAppearanceLocalized;
            TransitionalRenderer = TransitionalRenderer.CreateFor(ActorMeta, GameObject, false);
            SetVisibility(false);
        }

        public virtual Awaitable Blur (float intensity, Tween tween, AsyncToken token = default)
        {
            return TransitionalRenderer.Blur(intensity, tween, token);
        }

        public override async Awaitable ChangeAppearance (string appearance, Tween tween,
            Transition? transition = default, AsyncToken token = default)
        {
            var previousAppearance = base.Appearance;
            base.Appearance = appearance;

            var appearanceTexture = string.IsNullOrWhiteSpace(appearance)
                ? await LoadDefaultAppearance(token)
                : await LoadAppearance(appearance, token);

            // Happens when the appearance was changed multiple times concurrently, in which case discarding the stale appearance.
            if (base.Appearance != appearance)
            {
                if (!string.IsNullOrWhiteSpace(appearance))
                    AppearanceLoader?.Release(appearance, this);
            }
            else await TransitionalRenderer.TransitionTo(appearanceTexture, tween, transition, token);

            if (!string.IsNullOrEmpty(previousAppearance) && previousAppearance != appearance && previousAppearance != defaultAppearancePath)
                AppearanceLoader?.Release(previousAppearance, this);
        }

        public override async Awaitable ChangeVisibility (bool visible, Tween tween, AsyncToken token = default)
        {
            base.Visible = visible;

            // When appearance is not set (and default one is not preloaded for some reason, eg when using dynamic parameters) 
            // and revealing the actor — attempt to load default appearance texture.
            if (!Visible && visible && string.IsNullOrWhiteSpace(Appearance) && !AppearanceLoader.IsLoaded(defaultAppearance?.FullPath))
                await ChangeAppearance(null, new(0), token: token);

            await TransitionalRenderer.FadeTo(visible ? TintColor.a : 0, tween, token);
        }

        public override void Dispose ()
        {
            base.Dispose();

            if (AppearanceLoader != null)
            {
                AppearanceLoader.OnLocalized -= HandleAppearanceLocalized;
                AppearanceLoader.ReleaseAll(this);
            }
        }

        protected virtual void SetAppearance (string appearance) => ChangeAppearance(appearance, new(0)).Forget();

        protected virtual void SetVisibility (bool visible) => ChangeVisibility(visible, new(0)).Forget();

        protected override Color GetBehaviourTintColor () => TransitionalRenderer.TintColor;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            if (!Visible) // Handle visibility-controlled alpha of the tint color.
                tintColor.a = TransitionalRenderer.TintColor.a;
            TransitionalRenderer.TintColor = tintColor;
        }

        protected virtual async Awaitable<Texture2D> LoadAppearance (string appearance, AsyncToken token)
        {
            var texture = await AppearanceLoader.Load(appearance, this);
            token.ThrowIfCanceled(GameObject);
            if (!texture.Valid)
            {
                Engine.Warn($"Failed to load '{appearance}' appearance resource for '{Id}' sprite actor. " +
                            "Make sure the resource is registered. Will use a default texture for the time being.");
                return GetMissingTexture();
            }
            ApplyTextureSettings(texture);
            return texture;
        }

        protected virtual async Awaitable<Texture2D> LoadDefaultAppearance (AsyncToken token)
        {
            if (defaultAppearance != null && defaultAppearance.Valid) return defaultAppearance;

            defaultAppearancePath = LocateDefaultAppearance();
            if (!string.IsNullOrEmpty(defaultAppearancePath))
            {
                defaultAppearance = await AppearanceLoader.LoadOrErr(defaultAppearancePath, this);
                token.ThrowIfCanceled(GameObject);
            }
            else
            {
                Engine.Warn($"Failed to resolve default appearance for '{Id}' sprite actor. " +
                            "Either add a 'Default' appearance resource or explicitly specify an " +
                            "appearance when showing the actor for the first time. " +
                            "Will use a default texture for the time being.");
                return GetMissingTexture();
            }

            ApplyTextureSettings(defaultAppearance);

            if (!TransitionalRenderer.MainTexture)
                TransitionalRenderer.MainTexture = defaultAppearance;

            return defaultAppearance;
        }

        protected virtual string LocateDefaultAppearance ()
        {
            using var _ = AppearanceLoader.RentPaths(out var texturePaths);
            if (texturePaths.Count > 0)
            {
                // First, look for an appearance with a name, equal to actor's ID.
                if (texturePaths.Any(t => t.EqualsOrdinal(Id)))
                    return texturePaths.First(t => t.EqualsOrdinal(Id));
                // Then, try a 'Default' appearance.
                if (texturePaths.Any(t => t.EqualsOrdinal("Default")))
                    return texturePaths.First(t => t.EqualsOrdinal("Default"));
                // Finally, fallback to a first defined appearance.
                return texturePaths.FirstOrDefault();
            }
            return null;
        }

        protected virtual void ApplyTextureSettings (Texture2D texture)
        {
            if (texture && texture.wrapMode != TextureWrapMode.Clamp)
                texture.wrapMode = TextureWrapMode.Clamp;
        }

        protected virtual void HandleAppearanceLocalized (Resource<Texture2D> resource)
        {
            if (Appearance == AppearanceLoader.GetLocalPath(resource))
                Appearance = Appearance;
        }

        protected virtual Texture2D GetMissingTexture ()
        {
            if (missingTextureCache) return missingTextureCache;
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var pixels = new Color32[128 * 128];
            var red = new Color32(255, 0, 0, 255);
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = red;
            texture.SetPixels32(pixels);
            texture.Apply();
            return missingTextureCache = texture;
        }
    }
}
