using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Serializable state of <see cref="ICharacterActor"/>.
    /// </summary>
    [System.Serializable]
    public class CharacterState : ActorState<ICharacterActor>
    {
        /// <inheritdoc cref="ICharacterActor.LookDirection"/>
        public CharacterLookDirection LookDirection => lookDirection;

        [SerializeField] private CharacterLookDirection lookDirection;

        public CharacterState () { }
        public CharacterState ([CanBeNull] string appearance = null, bool? visible = null, Vector3? position = null,
            Quaternion? rotation = null, Vector3? scale = null, Color? tintColor = null,
            CharacterLookDirection? lookDirection = null)
            : base(appearance, visible, position, rotation, scale, tintColor)
        {
            this.lookDirection = lookDirection ?? default;
        }

        public override void OverwriteFromActor (ICharacterActor actor)
        {
            base.OverwriteFromActor(actor);

            lookDirection = actor.LookDirection;
        }

        public override Awaitable ApplyToActor (ICharacterActor actor)
        {
            base.ApplyToActor(actor);

            actor.LookDirection = lookDirection;

            return Async.Completed;
        }
    }
}
