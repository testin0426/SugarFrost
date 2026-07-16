using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Naninovel
{
    /// <summary>
    /// Represents and audio source with attached clip.
    /// </summary>
    public class AudioTrack : IAudioTrack
    {
        public event Action OnPlay;
        public event Action OnStop;

        public AudioClip Clip { get; }
        public AudioClip IntroClip { get; }
        public AudioSource Source { get; }
        public bool Valid => Clip && Source;
        public bool Loop
        {
            get => Valid && Source.loop;
            set
            {
                if (Valid) Source.loop = value;
            }
        }
        public bool Playing => Valid && Source.isPlaying;
        public bool Mute
        {
            get => Valid && Source.mute;
            set
            {
                if (Valid) Source.mute = value;
            }
        }
        public float Volume
        {
            get => Valid ? Source.volume : 0f;
            set
            {
                if (Valid) Source.volume = value;
            }
        }

        private readonly Tweener<FloatTween> volumeTweener;
        private readonly Timer stopTimer;

        public AudioTrack (AudioClip clip, AudioSource source, float volume = 1f, bool loop = false,
            AudioMixerGroup mixerGroup = null, AudioClip introClip = null)
        {
            Clip = clip;
            IntroClip = introClip;
            Source = source;
            Source.clip = Clip;
            Source.volume = volume;
            Source.loop = loop;
            Source.outputAudioMixerGroup = mixerGroup;

            volumeTweener = new();
            stopTimer = new(onCompleted: InvokeOnStop);
        }

        public void Play ()
        {
            CompleteAllRunners();
            if (!Valid) return;

            if (ObjectUtils.IsValid(IntroClip))
            {
                Source.PlayOneShot(IntroClip);
                Source.PlayScheduled(AudioSettings.dspTime + IntroClip.length);
                if (!Loop) stopTimer.Run(IntroClip.length + Clip.length, target: Source);
            }
            else
            {
                Source.Play();
                if (!Loop) stopTimer.Run(Clip.length, target: Source);
            }

            OnPlay?.Invoke();
        }

        public Awaitable Play (float fadeInTime, AsyncToken token = default)
        {
            CompleteAllRunners();
            if (!Valid) return Async.Completed;

            if (!Playing) Play();
            var tween = new FloatTween(0, Volume, new(fadeInTime), volume => Volume = volume);
            return volumeTweener.Run(tween, token, Source);
        }

        public void Stop ()
        {
            CompleteAllRunners();
            if (!Valid) return;

            Source.Stop();

            OnStop?.Invoke();
        }

        public Awaitable Stop (float fadeOutTime, AsyncToken token = default)
        {
            CompleteAllRunners();
            if (!Valid) return Async.Completed;

            var tween = new FloatTween(Volume, 0, new(fadeOutTime), volume => {
                Volume = volume;
                // Can't stop after awaiting the tweener, as we expect CompleteAllRunners()
                // in Play() to stop at the same time, while it's actually delayed by a frame
                // if invoked after await, which stops the audio after it's started playing.
                if (Mathf.Approximately(volume, 0) && Playing) Stop();
            });
            return volumeTweener.Run(tween, token, Source);
        }

        public Awaitable Fade (float volume, float fadeTime, AsyncToken token = default)
        {
            CompleteAllRunners();
            if (!Valid) return Async.Completed;

            var tween = new FloatTween(Volume, volume, new(fadeTime), v => Volume = v);
            return volumeTweener.Run(tween, token, Source);
        }

        private void CompleteAllRunners ()
        {
            if (volumeTweener.Running)
                volumeTweener.Complete();
            if (stopTimer.Running)
                stopTimer.CompleteInstantly();
        }

        private void InvokeOnStop () => OnStop?.Invoke();
    }
}
