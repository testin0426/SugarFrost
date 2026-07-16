using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to represent a character actor on scene.
    /// </summary>
    public interface ICharacterActor : IActor
    {
        /// <summary>
        /// Occurs when the <see cref="LookDirection"/> is changed.
        /// </summary>
        event Action<CharacterLookDirection> OnLookDirectionChanged;

        /// <summary>
        /// Look direction of the character.
        /// </summary>
        CharacterLookDirection LookDirection { get; set; }

        /// <summary>
        /// Changes character look direction over specified time using specified tween animation.
        /// </summary>
        Awaitable ChangeLookDirection (CharacterLookDirection lookDirection, Tween tween, AsyncToken token = default);
    }
}
