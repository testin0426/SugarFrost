using System;
using UnityEngine;
using UnityEngine.Video;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using <see cref="VideoClip"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(VideoClip), true)]
    public class VideoCharacter : VideoActor<CharacterMetadata>, ICharacterActor
    {
        public event Action<CharacterLookDirection> OnLookDirectionChanged;

        public CharacterLookDirection LookDirection
        {
            get => TransitionalRenderer.GetLookDirection(ActorMeta.BakedLookDirection);
            set
            {
                TransitionalRenderer.SetLookDirection(value, ActorMeta.BakedLookDirection);
                OnLookDirectionChanged?.Invoke(value);
            }
        }

        protected override string MixerGroup => Configuration.GetOrDefault<AudioConfiguration>().VoiceGroupPath;

        public VideoCharacter (string id, CharacterMetadata meta, StandaloneAppearanceLoader<VideoClip> loader)
            : base(id, meta, loader) { }

        public Awaitable ChangeLookDirection (CharacterLookDirection lookDirection, Tween tween, AsyncToken token = default)
        {
            OnLookDirectionChanged?.Invoke(lookDirection);
            return TransitionalRenderer.ChangeLookDirection(lookDirection,
                ActorMeta.BakedLookDirection, tween, token);
        }
    }
}
