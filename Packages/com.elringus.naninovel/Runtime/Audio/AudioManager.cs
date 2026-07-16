using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Audio;

namespace Naninovel
{
    /// <inheritdoc cref="IAudioManager"/>
    [InitializeAtRuntime]
    public class AudioManager : IStatefulService<SettingsStateMap>, IStatefulService<GameStateMap>, IAudioManager
    {
        [Serializable]
        public class Settings
        {
            public float MasterVolume;
            public float BgmVolume;
            public float SfxVolume;
            public float VoiceVolume;
            public string VoiceLocale;
            public List<NamedFloat> AuthorVolume;
        }

        [Serializable]
        public class GameState
        {
            public List<AudioClipState> BgmClips;
            public List<AudioClipState> SfxClips;
        }

        private class AuthorSource
        {
            public CharacterMetadata Metadata;
            public AudioSource Source;
        }

        public virtual event Action<string> OnPlayBgm;
        public virtual event Action<string> OnStopBgm;
        public virtual event Action<string> OnPlaySfx;
        public virtual event Action<string> OnStopSfx;
        public virtual event Action<string> OnPlayVoice;
        public virtual event Action<string> OnStopVoice;

        public virtual AudioConfiguration Configuration { get; }
        public virtual AudioMixer AudioMixer { get; }
        public virtual float MasterVolume { get => GetMixerVolume(Configuration.MasterVolumeHandleName); set => SetMasterVolume(value); }
        public virtual float BgmVolume { get => GetMixerVolume(Configuration.BgmVolumeHandleName); set => SetBgmVolume(value); }
        public virtual float SfxVolume { get => GetMixerVolume(Configuration.SfxVolumeHandleName); set => SetSfxVolume(value); }
        public virtual float VoiceVolume { get => GetMixerVolume(Configuration.VoiceVolumeHandleName); set => SetVoiceVolume(value); }
        public virtual string VoiceLocale { get => voiceLoader.OverrideLocale; set => voiceLoader.OverrideLocale = value; }
        public virtual IResourceLoader AudioLoader => audioLoader;
        public virtual IResourceLoader VoiceLoader => voiceLoader;

        private readonly IResourceProviderManager resources;
        private readonly ILocalizationManager l10n;
        private readonly ICharacterManager chars;
        private readonly Dictionary<string, AudioClipState> bgmMap = new();
        private readonly Dictionary<string, AudioClipState> sfxMap = new();
        private readonly Dictionary<string, float> authorVolume = new();
        private readonly Dictionary<string, AuthorSource> authorSources = new();
        private AudioMixerGroup masterGroup, bgmGroup, sfxGroup, voiceGroup;
        private LocalizableResourceLoader<AudioClip> audioLoader, voiceLoader;
        private IAudioPlayer player;
        private AudioClipState? voiceClip;

        public AudioManager (AudioConfiguration cfg, IResourceProviderManager resources,
            ILocalizationManager l10n, ICharacterManager chars)
        {
            Configuration = cfg;
            this.resources = resources;
            this.l10n = l10n;
            this.chars = chars;
            AudioMixer = cfg.CustomAudioMixer ? cfg.CustomAudioMixer :
                Engine.LoadInternalResource<AudioMixer>("DefaultMixer");
        }

        public virtual Awaitable InitializeService ()
        {
            if (AudioMixer)
            {
                masterGroup = AudioMixer.FindMatchingGroups(Configuration.MasterGroupPath)?.FirstOrDefault();
                bgmGroup = AudioMixer.FindMatchingGroups(Configuration.BgmGroupPath)?.FirstOrDefault();
                sfxGroup = AudioMixer.FindMatchingGroups(Configuration.SfxGroupPath)?.FirstOrDefault();
                voiceGroup = AudioMixer.FindMatchingGroups(Configuration.VoiceGroupPath)?.FirstOrDefault();
            }

            audioLoader = Configuration.AudioLoader.CreateLocalizableFor<AudioClip>(resources, l10n);
            voiceLoader = Configuration.VoiceLoader.CreateLocalizableFor<AudioClip>(resources, l10n);
            var playerType = Type.GetType(Configuration.AudioPlayer);
            if (playerType is null) throw new Error($"Failed to get type of '{Configuration.AudioPlayer}' audio player.");
            player = (IAudioPlayer)Activator.CreateInstance(playerType);

            return Async.Completed;
        }

        public virtual void ResetService ()
        {
            player.StopAll();
            bgmMap.Clear();
            sfxMap.Clear();
            voiceClip = null;

            audioLoader?.ReleaseAll(this);
            voiceLoader?.ReleaseAll(this);
        }

        public virtual void DestroyService ()
        {
            if (player is IDisposable disposable)
                disposable.Dispose();
            audioLoader?.ReleaseAll(this);
            voiceLoader?.ReleaseAll(this);
        }

        public virtual void SaveServiceState (SettingsStateMap stateMap)
        {
            var settings = new Settings {
                MasterVolume = MasterVolume,
                BgmVolume = BgmVolume,
                SfxVolume = SfxVolume,
                VoiceVolume = VoiceVolume,
                VoiceLocale = VoiceLocale,
                AuthorVolume = authorVolume.Select(kv => new NamedFloat(kv.Key, kv.Value)).ToList()
            };
            stateMap.SetState(settings);
        }

        public virtual Awaitable LoadServiceState (SettingsStateMap stateMap)
        {
            var settings = stateMap.GetState<Settings>();

            authorVolume.Clear();

            if (settings is null) // Apply default settings.
            {
                MasterVolume = Configuration.DefaultMasterVolume;
                BgmVolume = Configuration.DefaultBgmVolume;
                SfxVolume = Configuration.DefaultSfxVolume;
                VoiceVolume = Configuration.DefaultVoiceVolume;
                VoiceLocale = Configuration.VoiceLocales?.FirstOrDefault();
                return Async.Completed;
            }

            MasterVolume = settings.MasterVolume;
            BgmVolume = settings.BgmVolume;
            SfxVolume = settings.SfxVolume;
            VoiceVolume = settings.VoiceVolume;
            VoiceLocale = Configuration.VoiceLocales?.Count > 0 ?
                settings.VoiceLocale ?? Configuration.VoiceLocales.First() : null;

            foreach (var item in settings.AuthorVolume)
                authorVolume[item.Name] = item.Value;

            return Async.Completed;
        }

        public virtual void SaveServiceState (GameStateMap stateMap)
        {
            var state = new GameState {
                // Save only looped audio to prevent playing multiple clips at once when the game is saved in skip mode.
                BgmClips = bgmMap.Values.Where(s => IsBgmPlaying(s.Path) && s.Looped).ToList(),
                SfxClips = sfxMap.Values.Where(s => IsSfxPlaying(s.Path) && s.Looped).ToList()
            };
            stateMap.SetState(state);
        }

        public virtual async Awaitable LoadServiceState (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>() ?? new GameState();
            using var _ = Async.Rent(out var tasks);

            StopVoice();

            if (state.BgmClips != null && state.BgmClips.Count > 0)
            {
                foreach (var bgmPath in bgmMap.Keys.ToList())
                    if (!state.BgmClips.Exists(c => c.Path.EqualsOrdinal(bgmPath)))
                        tasks.Add(StopBgm(bgmPath));
                foreach (var clipState in state.BgmClips)
                    if (IsBgmPlaying(clipState.Path))
                        tasks.Add(ModifyBgm(clipState.Path, clipState.Volume, clipState.Looped, 0));
                    else tasks.Add(PlayBgm(clipState.Path, clipState.Volume, 0, clipState.Looped));
            }
            else tasks.Add(StopAllBgm());

            if (state.SfxClips != null && state.SfxClips.Count > 0)
            {
                foreach (var sfxPath in sfxMap.Keys.ToList())
                    if (!state.SfxClips.Exists(c => c.Path.EqualsOrdinal(sfxPath)))
                        tasks.Add(StopSfx(sfxPath));
                foreach (var clipState in state.SfxClips)
                    if (IsSfxPlaying(clipState.Path))
                        tasks.Add(ModifySfx(clipState.Path, clipState.Volume, clipState.Looped, 0));
                    else tasks.Add(PlaySfx(clipState.Path, clipState.Volume, 0, clipState.Looped));
            }
            else tasks.Add(StopAllSfx());

            await Async.All(tasks);
        }

        public virtual void CollectPlayedBgm (ICollection<string> paths)
        {
            foreach (var path in bgmMap.Keys)
                if (IsBgmPlaying(path))
                    paths.Add(path);
        }

        public virtual void CollectPlayedSfx (ICollection<string> paths)
        {
            foreach (var path in sfxMap.Keys)
                if (IsSfxPlaying(path))
                    paths.Add(path);
        }

        public virtual string GetPlayedVoice ()
        {
            return IsVoicePlaying(voiceClip?.Path) ? voiceClip?.Path : null;
        }

        public virtual async Awaitable ModifyBgm (string path, float volume, bool loop, float time,
            AsyncToken token = default)
        {
            if (!bgmMap.ContainsKey(path)) return;

            bgmMap[path] = new(path, volume, loop);
            await ModifyAudio(path, volume, loop, time, token);
        }

        public virtual async Awaitable ModifySfx (string path, float volume, bool loop, float time,
            AsyncToken token = default)
        {
            if (!sfxMap.ContainsKey(path)) return;

            sfxMap[path] = new(path, volume, loop);
            await ModifyAudio(path, volume, loop, time, token);
        }

        public virtual async Awaitable PlaySfxFast (string path, float volume = 1f, string group = default,
            bool restart = true, bool additive = true)
        {
            if (!audioLoader.IsLoaded(path)) await AudioLoader.LoadOrErr(path);
            var clip = audioLoader.GetLoaded(path);
            if (player.IsPlaying(clip) && !restart && !additive) return;
            if (player.IsPlaying(clip) && restart) player.Stop(clip);
            player.Play(clip, null, volume, false, FindAudioGroupOrDefault(group, sfxGroup), null, additive);
        }

        public virtual async Awaitable PlayBgm (string path, float volume = 1f, float fadeTime = 0f, bool loop = true,
            string introPath = null, string group = default, AsyncToken token = default)
        {
            OnPlayBgm?.Invoke(path);

            var clipResource = await audioLoader.LoadOrErr(path, this);
            token.ThrowIfCanceled();

            bgmMap[path] = new(path, volume, loop);

            var introClip = default(AudioClip);
            if (!string.IsNullOrEmpty(introPath))
            {
                var introClipResource = await audioLoader.LoadOrErr(introPath, this);
                token.ThrowIfCanceled();
                introClip = introClipResource.Object;
            }

            if (fadeTime <= 0)
                player.Play(clipResource, null, volume, loop,
                    FindAudioGroupOrDefault(group, bgmGroup), introClip);
            else
                await player.Play(clipResource, fadeTime, null, volume, loop,
                    FindAudioGroupOrDefault(group, bgmGroup), introClip, token: token);
        }

        public virtual async Awaitable StopBgm (string path, float fadeTime = 0f, AsyncToken token = default)
        {
            OnStopBgm?.Invoke(path);

            if (string.IsNullOrWhiteSpace(path)) return;
            bgmMap.Remove(path);

            if (!audioLoader.IsLoaded(path)) return;
            var clipResource = audioLoader.GetLoaded(path);
            if (fadeTime <= 0) player.Stop(clipResource);
            else await player.Stop(clipResource, fadeTime, token);

            if (!IsBgmPlaying(path))
                audioLoader?.Release(path, this);
        }

        public virtual async Awaitable StopAllBgm (float fadeTime = 0f, AsyncToken token = default)
        {
            using var _ = Async.Rent(out var tasks);
            foreach (var path in bgmMap.Keys.ToArray())
                tasks.Add(StopBgm(path, fadeTime, token));
            await Async.All(tasks);
        }

        public virtual async Awaitable PlaySfx (string path, float volume = 1f, float fadeTime = 0f,
            bool loop = false, string group = default, AsyncToken token = default)
        {
            OnPlaySfx?.Invoke(path);

            var clipResource = await audioLoader.LoadOrErr(path, this);
            token.ThrowIfCanceled();

            sfxMap[path] = new(path, volume, loop);

            if (fadeTime <= 0)
                player.Play(clipResource, null, volume, loop,
                    FindAudioGroupOrDefault(group, sfxGroup));
            else
                await player.Play(clipResource, fadeTime, null, volume, loop,
                    FindAudioGroupOrDefault(group, sfxGroup), token: token);
        }

        public virtual async Awaitable StopSfx (string path, float fadeTime = 0f, AsyncToken token = default)
        {
            OnStopSfx?.Invoke(path);

            if (string.IsNullOrWhiteSpace(path)) return;
            sfxMap.Remove(path);

            if (!audioLoader.IsLoaded(path)) return;
            var clipResource = audioLoader.GetLoaded(path);
            if (fadeTime <= 0) player.Stop(clipResource);
            else await player.Stop(clipResource, fadeTime, token);

            if (!IsSfxPlaying(path))
                audioLoader?.Release(path, this);
        }

        public virtual async Awaitable StopAllSfx (float fadeTime = 0f, AsyncToken token = default)
        {
            using var _ = Async.Rent(out var tasks);
            foreach (var path in sfxMap.Keys.ToArray())
                tasks.Add(StopSfx(path, fadeTime, token));
            await Async.All(tasks);
        }

        public virtual async Awaitable PlayVoice (string path, float volume = 1f, string group = default,
            string authorId = default, AsyncToken token = default)
        {
            OnPlayVoice?.Invoke(path);

            var clipResource = await voiceLoader.LoadOrErr(path, this);
            token.ThrowIfCanceled();

            if (Configuration.VoiceOverlapPolicy == VoiceOverlapPolicy.PreventOverlap)
                StopVoice();

            if (!string.IsNullOrEmpty(authorId))
            {
                var authorVolume = GetAuthorVolume(authorId);
                if (!Mathf.Approximately(authorVolume, -1))
                    volume *= authorVolume;
            }

            voiceClip = new AudioClipState(path, volume, false);

            var audioSource = !string.IsNullOrEmpty(authorId) ? await GetOrInstantiateAuthorSource(authorId) : null;
            player.Play(clipResource, audioSource, volume, false, FindAudioGroupOrDefault(group, voiceGroup));
        }

        public virtual bool IsBgmPlaying (string path)
        {
            if (string.IsNullOrEmpty(path) || !bgmMap.ContainsKey(path)) return false;
            return audioLoader.TryGetLoaded(path, out var clip) && player.IsPlaying(clip);
        }

        public virtual bool IsSfxPlaying (string path)
        {
            if (string.IsNullOrEmpty(path) || !sfxMap.ContainsKey(path)) return false;
            return audioLoader.TryGetLoaded(path, out var clip) && player.IsPlaying(clip);
        }

        public virtual bool IsVoicePlaying (string path)
        {
            if (!voiceClip.HasValue || voiceClip.Value.Path != path) return false;
            return voiceLoader.TryGetLoaded(path, out var clip) && player.IsPlaying(clip);
        }

        public virtual void StopVoice ()
        {
            if (!voiceClip.HasValue) return;
            var clipResource = voiceLoader.GetLoaded(voiceClip.Value.Path);
            OnStopVoice?.Invoke(voiceClip.Value.Path);
            player.Stop(clipResource);
            voiceLoader.Release(voiceClip.Value.Path, this);
            voiceClip = null;
        }

        public virtual IAudioTrack GetAudioTrack (string path)
        {
            var clipResource = audioLoader.GetLoaded(path);
            if (clipResource is null || !clipResource.Valid) return null;
            return GetTrack(clipResource);
        }

        public virtual IAudioTrack GetVoiceTrack (string path)
        {
            var clipResource = voiceLoader.GetLoaded(path);
            if (clipResource is null || !clipResource.Valid) return null;
            return GetTrack(clipResource);
        }

        public virtual float GetAuthorVolume (string authorId)
        {
            if (string.IsNullOrEmpty(authorId)) return -1;
            return authorVolume.GetValueOrDefault(authorId, -1);
        }

        public virtual void SetAuthorVolume (string authorId, float volume)
        {
            if (string.IsNullOrEmpty(authorId)) return;
            authorVolume[authorId] = volume;
        }

        public AudioMixerGroup GetGroup (string groupPath)
        {
            if (groupPath.EqualsOrdinal(Configuration.MasterGroupPath)) return masterGroup;
            if (groupPath.EqualsOrdinal(Configuration.BgmGroupPath)) return bgmGroup;
            if (groupPath.EqualsOrdinal(Configuration.SfxGroupPath)) return sfxGroup;
            if (groupPath.EqualsOrdinal(Configuration.VoiceGroupPath)) return voiceGroup;
            return AudioMixer.FindMatchingGroups(groupPath)?.FirstOrDefault();
        }

        [CanBeNull]
        protected virtual IAudioTrack GetTrack (AudioClip clip)
        {
            using var _ = ListPool<IAudioTrack>.Rent(out var tracks);
            player.CollectTracksWithClip(clip, tracks);
            return tracks.FirstOrDefault();
        }

        protected virtual async Awaitable ModifyAudio (string path, float volume, bool loop,
            float time, AsyncToken token = default)
        {
            if (!audioLoader.TryGetLoaded(path, out var clip)) return;
            var track = GetTrack(clip);
            if (track is null) return;
            track.Loop = loop;
            if (time <= 0) track.Volume = volume;
            else await track.Fade(volume, time, token);
        }

        protected virtual float GetMixerVolume (string handleName)
        {
            if (AudioMixer)
            {
                AudioMixer.GetFloat(handleName, out var value);
                return MathUtils.DecibelToLinear(value);
            }
            return player.Volume;
        }

        protected virtual void SetMixerVolume (string handleName, float value)
        {
            if (AudioMixer) AudioMixer.SetFloat(handleName, MathUtils.LinearToDecibel(value));
            else player.Volume = value;
        }

        protected virtual void SetMasterVolume (float value)
        {
            if (masterGroup) SetMixerVolume(Configuration.MasterVolumeHandleName, value);
        }

        protected virtual void SetBgmVolume (float value)
        {
            if (bgmGroup) SetMixerVolume(Configuration.BgmVolumeHandleName, value);
        }

        protected virtual void SetSfxVolume (float value)
        {
            if (sfxGroup) SetMixerVolume(Configuration.SfxVolumeHandleName, value);
        }

        protected virtual void SetVoiceVolume (float value)
        {
            if (voiceGroup) SetMixerVolume(Configuration.VoiceVolumeHandleName, value);
        }

        protected virtual AudioMixerGroup FindAudioGroupOrDefault (string path, AudioMixerGroup defaultGroup)
        {
            if (string.IsNullOrEmpty(path)) return defaultGroup;
            var group = AudioMixer.FindMatchingGroups(path)?.FirstOrDefault();
            return group ? group : defaultGroup;
        }

        protected virtual async Awaitable<AudioSource> GetOrInstantiateAuthorSource (string authorId)
        {
            if (authorSources.TryGetValue(authorId, out var authorSource))
            {
                if (!authorSource.Metadata.VoiceSource) return null;
                if (authorSource.Source) return authorSource.Source;
            }
            return await Instantiate();

            async Awaitable<AudioSource> Instantiate ()
            {
                if (!chars.ActorExists(authorId)) return null;

                var metadata = chars.Configuration.GetMetadataOrDefault(authorId);
                var character = chars.GetActorOrErr(authorId) as MonoBehaviourActor<CharacterMetadata>;
                if (!metadata.VoiceSource || character is null)
                {
                    authorSources[authorId] = new() { Metadata = metadata };
                    return null;
                }

                var src = await Engine.Instantiate<AudioSource>(metadata.VoiceSource,
                    new() { Parent = character.GameObject.transform });
                authorSources[authorId] = new() { Metadata = metadata, Source = src };
                return src;
            }
        }
    }
}
