using System;
using System.Collections.Generic;
using Naninovel.FX;
using UnityEngine;
using UnityEngine.Video;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IActor"/> implementation using <see cref="VideoClip"/> to represent the actor.
    /// </summary>
    public abstract class VideoActor<TMeta> : MonoBehaviourActor<TMeta>, Blur.IBlurable
        where TMeta : OrthoActorMetadata
    {
        public override string Appearance { get => base.Appearance; set => SetAppearance(value); }
        public override bool Visible { get => base.Visible; set => SetVisibility(value); }

        protected virtual TransitionalRenderer TransitionalRenderer { get; private set; }
        protected virtual Dictionary<string, VideoAppearance> Appearances { get; } = new();
        protected virtual int TextureDepthBuffer => 24;
        protected virtual RenderTextureFormat TextureFormat => RenderTextureFormat.ARGB32;
        protected virtual string MixerGroup => Configuration.GetOrDefault<AudioConfiguration>().MasterGroupPath;

        private readonly Tweener<FloatTween> volumeTweener = new();
        private readonly StandaloneAppearanceLoader<VideoClip> videoLoader;
        private readonly string streamExtension;

        protected VideoActor (string id, TMeta meta, StandaloneAppearanceLoader<VideoClip> loader)
            : base(id, meta)
        {
            videoLoader = loader;
            streamExtension = Engine.GetConfiguration<ResourceProviderConfiguration>().VideoStreamExtension;
        }

        public override async Awaitable Initialize ()
        {
            await base.Initialize();

            videoLoader.OnLocalized += HandleAppearanceLocalized;
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
            var prevAppearance = base.Appearance;
            base.Appearance = appearance;

            if (string.IsNullOrEmpty(appearance))
            {
                foreach (var vid in Appearances.Values)
                    vid.Video.Stop();
                return;
            }

            foreach (var (id, vid) in Appearances)
                if (id != appearance && id != prevAppearance)
                    vid.Video.Stop();

            var videoAppearance = await GetAppearance(appearance, token);
            var video = videoAppearance.Video;

            if (!video.isPrepared)
            {
                video.Prepare();
                // Player could be invalid, as we're invoking this from sync version of change appearance.
                while (token.EnsureNotCanceled(video) && !video.isPrepared)
                    await Async.NextFrame();
                if (!video) return;
            }

            var prevTexture = video.targetTexture;
            videoAppearance.Video.targetTexture =
                RenderTexture.GetTemporary((int)video.width, (int)video.height, TextureDepthBuffer, TextureFormat);
            videoAppearance.Video.Play();

            foreach (var (app, vid) in Appearances)
                vid.TweenVolume(app == appearance && Visible ? 1 : 0, tween.Duration, token).Forget();
            await TransitionalRenderer.TransitionTo(video.targetTexture, tween, transition, token);
            if (!video) return;

            if (prevTexture)
                RenderTexture.ReleaseTemporary(prevTexture);
            if (prevAppearance != Appearance)
                ReleaseAppearance(prevAppearance);
        }

        public override async Awaitable ChangeVisibility (bool visible, Tween tween, AsyncToken token = default)
        {
            base.Visible = visible;

            foreach (var app in Appearances.Values)
                app.TweenVolume(visible && app.Video.isPlaying ? 1 : 0, tween.Duration, token).Forget();
            await TransitionalRenderer.FadeTo(visible ? TintColor.a : 0, tween, token);
        }

        public override void Dispose ()
        {
            base.Dispose();

            foreach (var videoAppearance in Appearances.Values)
            {
                if (!videoAppearance.Video) continue;
                RenderTexture.ReleaseTemporary(videoAppearance.Video.targetTexture);
                ObjectUtils.DestroyOrImmediate(videoAppearance.GameObject);
            }

            Appearances.Clear();

            if (videoLoader != null)
            {
                videoLoader.OnLocalized -= HandleAppearanceLocalized;
                videoLoader.ReleaseAll(this);
            }
        }

        protected virtual void SetAppearance (string appearance) => ChangeAppearance(appearance, new(0)).Forget();

        protected virtual void SetVisibility (bool visible) => ChangeVisibility(visible, new(0)).Forget();

        protected override Color GetBehaviourTintColor () => TransitionalRenderer.TintColor;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            if (!Visible) tintColor.a = TransitionalRenderer.TintColor.a;
            TransitionalRenderer.TintColor = tintColor;
        }

        protected virtual async Awaitable<VideoAppearance> GetAppearance (string videoName, AsyncToken token = default)
        {
            if (Appearances.TryGetValue(videoName, out var cached)) return cached;

            var player = Engine.CreateObject<VideoPlayer>(new() { Name = videoName, Parent = Transform });
            player.playOnAwake = false;
            player.isLooping = ShouldLoopAppearance(videoName);
            player.renderMode = VideoRenderMode.RenderTexture;
            if (Application.platform == RuntimePlatform.WebGLPlayer && !Application.isEditor)
            {
                player.source = VideoSource.Url;
                player.url = PathUtils.Combine(Application.streamingAssetsPath,
                    $"{ActorMeta.Loader.PathPrefix}/{Id}/{videoName}") + streamExtension;
                await Async.NextFrame(token);
            }
            else
            {
                var videoClip = await videoLoader.LoadOrErr(videoName, this);
                token.ThrowIfCanceled();
                player.source = VideoSource.VideoClip;
                player.clip = videoClip;
            }

            var videoAppearance = new VideoAppearance(player, SetupAudioSource(player));
            Appearances[videoName] = videoAppearance;

            return videoAppearance;
        }

        protected virtual bool ShouldLoopAppearance (string appearance)
        {
            return !appearance.EndsWith("NoLoop", StringComparison.OrdinalIgnoreCase);
        }

        protected virtual AudioSource SetupAudioSource (VideoPlayer player)
        {
            var src = player.gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.bypassReverbZones = true;
            src.bypassEffects = true;
            if (Engine.GetService<IAudioManager>()?.GetGroup(MixerGroup) is { } group)
                src.outputAudioMixerGroup = group;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.SetTargetAudioSource(0, src);
            return src;
        }

        protected virtual void ReleaseAppearance (string appearance)
        {
            if (string.IsNullOrEmpty(appearance)) return;

            videoLoader.Release(appearance, this);

            if (videoLoader.CountHolders(appearance) == 0 &&
                Appearances.Remove(appearance, out var player))
                DisposeAppearancePlayer(player);
        }

        protected virtual void DisposeAppearancePlayer (VideoAppearance player)
        {
            player.Video.Stop();
            RenderTexture.ReleaseTemporary(player.Video.targetTexture);
            ObjectUtils.DestroyOrImmediate(player.GameObject);
        }

        protected virtual void HandleAppearanceLocalized (Resource<VideoClip> resource)
        {
            if (Appearance == videoLoader.GetLocalPath(resource))
                Appearance = Appearance;
        }
    }
}
