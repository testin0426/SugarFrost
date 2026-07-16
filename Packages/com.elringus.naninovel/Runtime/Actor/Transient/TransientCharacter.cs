using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A transient <see cref="ICharacterActor"/> implementation with lifecycle managed outside of Naninovel.
    /// </summary>
    [ActorResources(null, false)]
    [AddComponentMenu("Naninovel/ Actors/Transient Character")]
    public class TransientCharacter : TransientActor<ICharacterManager, CharacterMetadata>, ICharacterActor
    {
        public virtual event Action<CharacterLookDirection> OnLookDirectionChanged;

        [field: SerializeField, Tooltip("Assign a transient text printer object to link with the character. The linked printer will be used to reveal the character messages.")]
        public virtual TransientTextPrinter LinkedPrinter { get; private set; }

        public virtual CharacterLookDirection LookDirection { get => lookDirection; set => SetLookDirection(value); }

        private CharacterLookDirection lookDirection;

        public override void InitializeTransientActor ()
        {
            base.InitializeTransientActor();
            if (LinkedPrinter) Metadata.LinkedPrinter = LinkedPrinter.ActorId;
        }

        public virtual Awaitable ChangeLookDirection (CharacterLookDirection lookDirection, Tween tween, AsyncToken token = default)
        {
            SetLookDirection(lookDirection);
            return Async.Completed;
        }

        protected virtual void SetLookDirection (CharacterLookDirection lookDirection)
        {
            var changed = this.lookDirection != lookDirection;
            this.lookDirection = lookDirection;
            if (changed) OnLookDirectionChanged?.Invoke(lookDirection);
        }
    }
}
