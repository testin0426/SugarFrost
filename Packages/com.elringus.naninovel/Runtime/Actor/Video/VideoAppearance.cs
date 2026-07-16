using UnityEngine;
using UnityEngine.Video;

namespace Naninovel
{
    public readonly struct VideoAppearance
    {
        public VideoPlayer Video { get; }
        public AudioSource Audio { get; }
        public GameObject GameObject => Video.gameObject;

        private readonly Tweener<FloatTween> volumeTweener;

        public VideoAppearance (VideoPlayer video, AudioSource audio)
        {
            Video = video;
            Audio = audio;
            volumeTweener = new();
        }

        public Awaitable TweenVolume (float value, float duration, AsyncToken token = default)
        {
            var source = Audio;
            var tween = new FloatTween(Audio.volume, value, new(duration), v => source.volume = v);
            return volumeTweener.Run(tween, token, Audio);
        }
    }
}
