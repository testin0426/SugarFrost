using System;
using UnityEngine;
using UnityEngine.Events;

namespace Naninovel
{
    /// <summary>
    /// Represent a prefab resource used by <see cref="LayeredCharacter"/> actors.
    /// </summary>
    public class LayeredCharacterBehaviour : LayeredActorBehaviour
    {
        [Serializable]
        private class LookDirectionChangedEvent : UnityEvent<CharacterLookDirection> { }

        /// <summary>
        /// Invoked when the character becomes or cease to be the author of the last printed text message.
        /// </summary>
        public event Action<bool> OnIsSpeakingChanged;
        /// <summary>
        /// Invoked when look direction of the character is changed.
        /// </summary>
        public event Action<CharacterLookDirection> OnLookDirectionChanged;

        [Tooltip("Invoked when the character becomes the author of the printed text message.")]
        [SerializeField] private UnityEvent onStartedSpeaking;
        [Tooltip("Invoked after `On Started Speaking` when the message is fully revealed or (when auto voicing is enabled) voice clip finish playing.")]
        [SerializeField] private UnityEvent onFinishedSpeaking;
        [Tooltip("Invoked when look direction of the character is changed.")]
        [SerializeField] private LookDirectionChangedEvent onLookDirectionChanged;

        public virtual void NotifyIsSpeakingChanged (bool value)
        {
            OnIsSpeakingChanged?.Invoke(value);

            if (value) onStartedSpeaking?.Invoke();
            else onFinishedSpeaking?.Invoke();
        }

        public virtual void NotifyLookDirectionChanged (CharacterLookDirection value)
        {
            OnLookDirectionChanged?.Invoke(value);
            onLookDirectionChanged?.Invoke(value);
        }
    }
}
