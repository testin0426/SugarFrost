#if NANINOVEL_ENABLE_SPINE
using System;
using Naninovel.Commands;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using <see cref="SpineController"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(SpineController), false)]
    public class SpineCharacter : SpineActor<CharacterMetadata>, ICharacterActor, LipSync.IReceiver
    {
        public event Action<CharacterLookDirection> OnLookDirectionChanged;
        
        public CharacterLookDirection LookDirection
        {
            get => Renderer.GetLookDirection(ActorMeta.BakedLookDirection);
            set
            {
                Renderer.SetLookDirection(value, ActorMeta.BakedLookDirection);
                OnLookDirectionChanged?.Invoke(value);
            }
        }

        protected virtual CharacterLipSyncer LipSyncer { get; private set; }

        public SpineCharacter (string id, CharacterMetadata meta, EmbeddedAppearanceLoader<GameObject> loader)
            : base(id, meta, loader) { }

        public override async Awaitable Initialize ()
        {
            await base.Initialize();
            LipSyncer = new CharacterLipSyncer(Id, Controller.ChangeIsSpeaking);
        }

        public override void Dispose ()
        {
            LipSyncer.Dispose();
            base.Dispose();
        }

        public virtual Awaitable ChangeLookDirection (CharacterLookDirection lookDirection, Tween tween, AsyncToken token = default)
        {
            OnLookDirectionChanged?.Invoke(lookDirection);
            return Renderer.ChangeLookDirection(lookDirection, ActorMeta.BakedLookDirection, tween, token);
        }

        public virtual void AllowLipSync (bool active) => LipSyncer.SyncAllowed = active;
    }
}
#endif
