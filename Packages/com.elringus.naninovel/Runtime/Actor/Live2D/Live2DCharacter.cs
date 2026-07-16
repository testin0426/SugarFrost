#if NANINOVEL_ENABLE_LIVE2D

using System;
using Naninovel.Commands;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using a <see cref="Live2DController"/> to represent an actor.
    /// </summary>
    [ActorResources(typeof(Live2DController), false)]
    public class Live2DCharacter : Live2DActor<CharacterMetadata>, ICharacterActor, LipSync.IReceiver
    {
        public event Action<CharacterLookDirection> OnLookDirectionChanged;

        public virtual CharacterLookDirection LookDirection { get => lookDirection; set => SetLookDirection(value); }

        protected virtual CharacterLipSyncer LipSyncer { get; private set; }

        private CharacterLookDirection lookDirection;

        public Live2DCharacter (string id, CharacterMetadata meta, EmbeddedAppearanceLoader<GameObject> loader)
            : base(id, meta, loader) { }

        public override async Awaitable Initialize ()
        {
            await base.Initialize();
            LipSyncer = new CharacterLipSyncer(Id, Controller.SetIsSpeaking);
        }

        public override void Dispose ()
        {
            base.Dispose();
            LipSyncer?.Dispose();
        }

        public virtual Awaitable ChangeLookDirection (CharacterLookDirection lookDirection, Tween tween, AsyncToken token = default)
        {
            SetLookDirection(lookDirection);
            return Async.Completed;
        }

        public virtual void AllowLipSync (bool active) => LipSyncer.SyncAllowed = active;

        protected virtual void SetLookDirection (CharacterLookDirection lookDirection)
        {
            this.lookDirection = lookDirection;
            OnLookDirectionChanged?.Invoke(lookDirection);

            if (Controller)
                Controller.SetLookDirection(lookDirection);
        }
    }
}

#endif
