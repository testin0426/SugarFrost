using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using <see cref="LayeredActorBehaviour"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(LayeredCharacterBehaviour), false)]
    public class LayeredCharacter : LayeredActor<LayeredCharacterBehaviour, CharacterMetadata>, ICharacterActor, Commands.LipSync.IReceiver
    {
        public event Action<CharacterLookDirection> OnLookDirectionChanged;

        public CharacterLookDirection LookDirection { get => GetLookDirection(); set => SetLookDirection(value); }

        private CharacterLipSyncer lipSyncer;

        public LayeredCharacter (string id, CharacterMetadata meta, EmbeddedAppearanceLoader<GameObject> loader)
            : base(id, meta, loader) { }

        public override async Awaitable Initialize ()
        {
            await base.Initialize();

            lipSyncer = new(Id, Behaviour.NotifyIsSpeakingChanged);
        }

        public Awaitable ChangeLookDirection (CharacterLookDirection lookDirection, Tween tween, AsyncToken token = default)
        {
            OnLookDirectionChanged?.Invoke(lookDirection);
            Behaviour.NotifyLookDirectionChanged(lookDirection);
            return TransitionalRenderer.ChangeLookDirection(lookDirection,
                ActorMeta.BakedLookDirection, tween, token);
        }

        public override void Dispose ()
        {
            base.Dispose();

            lipSyncer?.Dispose();
        }

        public void AllowLipSync (bool active) => lipSyncer.SyncAllowed = active;

        protected virtual CharacterLookDirection GetLookDirection ()
        {
            return TransitionalRenderer.GetLookDirection(ActorMeta.BakedLookDirection);
        }

        protected virtual void SetLookDirection (CharacterLookDirection value)
        {
            OnLookDirectionChanged?.Invoke(value);
            Behaviour.NotifyLookDirectionChanged(value);
            TransitionalRenderer.SetLookDirection(value, ActorMeta.BakedLookDirection);
        }
    }
}
