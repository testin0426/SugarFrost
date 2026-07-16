using System;
using UnityEngine;
using UnityEngine.Video;

namespace Naninovel
{
    /// <inheritdoc cref="IMoviePlayer"/>
    [InitializeAtRuntime]
    public class MoviePlayer : IMoviePlayer
    {
        public event Action OnMoviePlay;
        public event Action OnMovieStop;

        public virtual MoviesConfiguration Configuration { get; }
        public virtual bool Playing { get; private set; }

        protected virtual VideoPlayer Player { get; private set; }
        protected virtual AudioSource AudioSource { get; private set; }
        protected virtual bool UrlStreaming => Application.platform == RuntimePlatform.WebGLPlayer && !Application.isEditor;
        protected virtual LocalizableResourceLoader<VideoClip> Loader { get; private set; }

        private readonly IInputManager input;
        private readonly IResourceProviderManager resources;
        private readonly ILocalizationManager l10n;
        private readonly IAudioManager audio;
        private string playedMoviePath;
        private IInputHandle skipInput;
        private string streamExtension;

        public MoviePlayer (MoviesConfiguration cfg, IResourceProviderManager resources,
            ILocalizationManager l10n, IInputManager input, IAudioManager audio)
        {
            Configuration = cfg;
            this.resources = resources;
            this.l10n = l10n;
            this.input = input;
            this.audio = audio;
        }

        public virtual Awaitable InitializeService ()
        {
            Loader = Configuration.Loader.CreateLocalizableFor<VideoClip>(resources, l10n);
            streamExtension = Engine.GetConfiguration<ResourceProviderConfiguration>().VideoStreamExtension;
            skipInput = input.GetSkipMovie();

            Player = CreatePlayer();
            AudioSource = SetupAudioSource();

            if (Configuration.SkipOnInput && skipInput != null)
                skipInput.OnStart += Stop;

            return Async.Completed;
        }

        public virtual void ResetService ()
        {
            if (Playing) Stop();
            Loader?.ReleaseAll(this);
        }

        public virtual void DestroyService ()
        {
            if (Playing) Stop();
            if (Player) ObjectUtils.DestroyOrImmediate(Player.gameObject);
            if (skipInput != null) skipInput.OnStart -= Stop;
            Loader?.ReleaseAll(this);
        }

        public virtual async Awaitable<Texture> Play (string moviePath, AsyncToken token = default)
        {
            if (Playing) Stop();
            playedMoviePath = moviePath;
            SetIsPlaying(true);
            if (UrlStreaming) Player.url = BuildStreamUrl(moviePath);
            else Player.clip = await LoadMovieClip(moviePath, token);
            await PreparePlayer(token);
            Player.Play();
            return Player.texture;
        }

        public virtual void Stop ()
        {
            if (!Playing) return;

            if (Player) Player.Stop();
            Loader?.Release(playedMoviePath, this);
            playedMoviePath = null;
            SetIsPlaying(false);
        }

        public virtual async Awaitable HoldResources (string moviePath, object holder)
        {
            if (UrlStreaming) return;
            await Loader.LoadOrErr(moviePath, holder);
        }

        public virtual void ReleaseResources (string moviePath, object holder)
        {
            if (UrlStreaming) return;
            Loader?.Release(moviePath, holder);
        }

        protected virtual VideoPlayer CreatePlayer ()
        {
            var player = Engine.CreateObject<VideoPlayer>(new() { Name = nameof(MoviePlayer) });
            player.playOnAwake = false;
            player.skipOnDrop = Configuration.SkipFrames;
            player.source = UrlStreaming ? VideoSource.Url : VideoSource.VideoClip;
            player.renderMode = VideoRenderMode.APIOnly;
            player.isLooping = false;
            player.loopPointReached += _ => Stop();
            player.errorReceived += HandlePlaybackError;
            return player;
        }

        protected virtual AudioSource SetupAudioSource ()
        {
            var src = Player.gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.bypassReverbZones = true;
            src.bypassEffects = true;
            if (audio.GetGroup(audio.Configuration.MasterGroupPath) is { } group)
                src.outputAudioMixerGroup = group;
            return src;
        }

        protected virtual string BuildStreamUrl (string moviePath)
        {
            var clipPath = $"{Configuration.Loader.PathPrefix}/{moviePath}{streamExtension}";
            return PathUtils.Combine(Application.streamingAssetsPath, clipPath);
        }

        protected virtual async Awaitable<VideoClip> LoadMovieClip (string moviePath, AsyncToken token)
        {
            var videoResource = await Loader.LoadOrErr(moviePath, this);
            token.ThrowIfCanceled();
            return videoResource.Object;
        }

        protected virtual async Awaitable PreparePlayer (AsyncToken token)
        {
            Player.Prepare();
            while (Playing && !Player.isPrepared)
                await Async.NextFrame(token);

            // Can't set this in audio source setup as Unity is failing
            // to play audio after playing a clip w/o audio in such case.
            Player.controlledAudioTrackCount = 1;
            Player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            Player.SetTargetAudioSource(0, AudioSource);
        }

        protected virtual void SetIsPlaying (bool playing)
        {
            Playing = playing;
            if (playing) OnMoviePlay?.Invoke();
            else OnMovieStop?.Invoke();
        }

        protected virtual void HandlePlaybackError (VideoPlayer source, string message)
        {
            Engine.Warn($"Received an error while playing '{source?.clip?.name}': {message}");
            Stop();
        }
    }
}
