#if SPRITE_DICING_AVAILABLE

using System;
using SpriteDicing;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using "SpriteDicing" extension to represent the actor.
    /// </summary>
    [ActorResources(typeof(DicedSpriteAtlas), false)]
    public class DicedSpriteCharacter : DicedSpriteActor<CharacterMetadata>, ICharacterActor
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

        public DicedSpriteCharacter (string id, CharacterMetadata meta, EmbeddedAppearanceLoader<DicedSpriteAtlas> loader)
            : base(id, meta, loader) { }

        public Awaitable ChangeLookDirection (CharacterLookDirection lookDirection, Tween tween, AsyncToken token = default)
        {
            OnLookDirectionChanged?.Invoke(lookDirection);
            return TransitionalRenderer.ChangeLookDirection(lookDirection,
                ActorMeta.BakedLookDirection, tween, token);
        }
    }
}

#endif
