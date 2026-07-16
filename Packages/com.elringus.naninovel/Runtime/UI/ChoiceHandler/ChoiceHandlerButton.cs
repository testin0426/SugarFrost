using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Naninovel.UI
{
    [RequireComponent(typeof(Button))]
    public class ChoiceHandlerButton : ScriptableButton
    {
        [Serializable] private class SummaryTextChangedEvent : UnityEvent<string> { }
        [Serializable] private class OnLockEvent : UnityEvent<bool> { }

        /// <summary>
        /// Invoked when the choice summary text is changed.
        /// </summary>
        public event Action<string> OnSummaryTextChanged;
        /// <summary>
        /// Invoked when lock status is changed: true when locked/disabled and vice-versa.
        /// </summary>
        public event Action<bool> OnLock;

        public virtual Choice Choice { get; private set; }

        protected virtual Action<Choice> Callback { get; private set; }
        protected virtual float InitializeTime { get; private set; }
        protected virtual float DebounceDelay => debounceDelay;

        [Tooltip("Invoked when the choice summary text is changed.")]
        [SerializeField] private SummaryTextChangedEvent onSummaryTextChanged;
        [Tooltip("Invoked when lock status is changed: true when locked/disabled and vice-versa.")]
        [SerializeField] private OnLockEvent onLock;
        [Tooltip("Button won't invoke the selection callback for the specified time (in seconds, unscaled) after being added to prevent unintended activation.")]
        [SerializeField] private float debounceDelay = .1f;

        protected override void OnDestroy ()
        {
            base.OnDestroy();
            if (!string.IsNullOrEmpty(Choice.ButtonPath) &&
                Engine.TryGetService<IChoiceHandlerManager>(out var choices))
                choices.ChoiceButtonLoader.Release(Choice.ButtonPath, this);
        }

        public virtual void Initialize (Choice choice, Action<Choice> callback)
        {
            Choice = choice;
            Callback = callback;
            InitializeTime = Engine.Time.UnscaledTime;

            OnSummaryTextChanged?.Invoke(choice.Summary);
            onSummaryTextChanged?.Invoke(choice.Summary);

            OnLock?.Invoke(choice.Locked);
            onLock?.Invoke(choice.Locked);

            if (!string.IsNullOrEmpty(choice.ButtonPath) &&
                Engine.TryGetService<IChoiceHandlerManager>(out var choices))
                choices.ChoiceButtonLoader.Hold(choice.ButtonPath, this);
        }

        // DON'T REMOVE THIS METHOD — IT'S ASSIGNED TO THE UNITY EVENT ON THE BUTTON PREFAB
        public virtual void HandleLockChanged (bool locked)
        {
            SetInteractable(!locked);
        }

        protected override void OnButtonClick ()
        {
            base.OnButtonClick();
            if (Engine.Time.UnscaledTime - InitializeTime > DebounceDelay)
                Callback?.Invoke(Choice);
        }
    }
}
