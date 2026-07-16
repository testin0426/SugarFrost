using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using <see cref="SpriteActor{TMeta}"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(Texture2D), true)]
    public class SpriteCharacter : SpriteActor<CharacterMetadata>, ICharacterActor
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

        public SpriteCharacter (string id, CharacterMetadata meta, StandaloneAppearanceLoader<Texture2D> loader)
            : base(id, meta, loader) { }

        public Awaitable ChangeLookDirection (CharacterLookDirection lookDirection, Tween tween, AsyncToken token = default)
        {
            OnLookDirectionChanged?.Invoke(lookDirection);
            return TransitionalRenderer.ChangeLookDirection(lookDirection,
                ActorMeta.BakedLookDirection, tween, token);
        }
    }
}
