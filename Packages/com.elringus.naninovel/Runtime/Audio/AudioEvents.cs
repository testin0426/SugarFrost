using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Routes essential <see cref="IAudioManager"/> APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Audio Events")]
    public class AudioEvents : UnityEvents
    {
        [Tooltip("Occurs when availability of the audio manager engine service changes.")]
        public BoolUnityEvent ServiceAvailable;
        [Tooltip("Occurs when a background music with the specified resource path starts playing.")]
        public StringUnityEvent BgmStarted;
        [Tooltip("Occurs when a background music with the specified resource path stops playing.")]
        public StringUnityEvent BgmStopped;
        [Tooltip("Occurs when a sound effect with the specified resource path starts playing.")]
        public StringUnityEvent SfxStarted;
        [Tooltip("Occurs when a sound effect with the specified resource path stops playing.")]
        public StringUnityEvent SfxStopped;
        [Tooltip("Occurs when a voice clip with the specified resource path starts playing.")]
        public StringUnityEvent VoiceStarted;
        [Tooltip("Occurs when a voice clip with the specified resource path stops playing.")]
        public StringUnityEvent VoiceStopped;

        public void PlayBgm (string path)
        {
            if (Engine.TryGetService<IAudioManager>(out var audio))
                audio.PlayBgm(path).Forget();
        }

        public void StopBgm (string path)
        {
            if (Engine.TryGetService<IAudioManager>(out var audio))
                audio.StopBgm(path).Forget();
        }

        public void StopAllBgm ()
        {
            if (Engine.TryGetService<IAudioManager>(out var audio))
                audio.StopAllBgm().Forget();
        }

        public void PlaySfx (string path)
        {
            if (Engine.TryGetService<IAudioManager>(out var audio))
                audio.PlaySfx(path).Forget();
        }

        public void StopSfx (string path)
        {
            if (Engine.TryGetService<IAudioManager>(out var audio))
                audio.StopSfx(path).Forget();
        }

        public void StopAllSfx ()
        {
            if (Engine.TryGetService<IAudioManager>(out var audio))
                audio.StopAllSfx().Forget();
        }

        public void PlayVoice (string path)
        {
            if (Engine.TryGetService<IAudioManager>(out var audio))
                audio.PlayVoice(path).Forget();
        }

        public void StopVoice ()
        {
            if (Engine.TryGetService<IAudioManager>(out var audio))
                audio.StopVoice();
        }

        public void SetMaterVolume (float volume)
        {
            if (Engine.TryGetService<IAudioManager>(out var audio))
                audio.MasterVolume = volume;
        }

        public void SetBgmVolume (float volume)
        {
            if (Engine.TryGetService<IAudioManager>(out var audio))
                audio.BgmVolume = volume;
        }

        public void SeSfxVolume (float volume)
        {
            if (Engine.TryGetService<IAudioManager>(out var audio))
                audio.SfxVolume = volume;
        }

        public void SetVoiceVolume (float volume)
        {
            if (Engine.TryGetService<IAudioManager>(out var audio))
                audio.VoiceVolume = volume;
        }

        public void SetVoiceLocale (string locale)
        {
            if (Engine.TryGetService<IAudioManager>(out var audio))
                audio.VoiceLocale = locale;
        }

        protected override void HandleEngineInitialized ()
        {
            if (Engine.TryGetService<IAudioManager>(out var audio))
            {
                ServiceAvailable?.Invoke(true);

                audio.OnPlayBgm -= BgmStarted.SafeInvoke;
                audio.OnPlayBgm += BgmStarted.SafeInvoke;
                audio.OnStopBgm -= BgmStopped.SafeInvoke;
                audio.OnStopBgm += BgmStopped.SafeInvoke;

                audio.OnPlaySfx -= SfxStarted.SafeInvoke;
                audio.OnPlaySfx += SfxStarted.SafeInvoke;
                audio.OnStopSfx -= SfxStopped.SafeInvoke;
                audio.OnStopSfx += SfxStopped.SafeInvoke;

                audio.OnPlayVoice -= VoiceStarted.SafeInvoke;
                audio.OnPlayVoice += VoiceStarted.SafeInvoke;
                audio.OnStopVoice -= VoiceStopped.SafeInvoke;
                audio.OnStopVoice += VoiceStopped.SafeInvoke;
            }
            else ServiceAvailable?.Invoke(false);
        }

        protected override void HandleEngineDestroyed ()
        {
            ServiceAvailable?.Invoke(false);
        }
    }
}
