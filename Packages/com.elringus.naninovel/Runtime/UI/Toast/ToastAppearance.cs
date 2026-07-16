using System;
using UnityEngine;
using UnityEngine.Events;

namespace Naninovel.UI
{
    /// <summary>
    /// Represents a <see cref="ToastUI"/> appearance.
    /// </summary>
    public class ToastAppearance : MonoBehaviour
    {
        [Serializable]
        private class TextChangedEvent : UnityEvent<string> { }

        [SerializeField] private TextChangedEvent onTextChanged;
        [SerializeField] private UnityEvent onSelected;
        [SerializeField] private UnityEvent onDeselected;

        private LocalizableText text;

        public virtual void SetText (LocalizableText text) => onTextChanged?.Invoke(this.text = text);

        public virtual void SetSelected (bool selected)
        {
            gameObject.SetActive(selected);
            if (selected) onSelected?.Invoke();
            else onDeselected?.Invoke();
        }

        protected virtual void OnEnable ()
        {
            if (Engine.TryGetService<ILocalizationManager>(out var l10n))
                l10n.OnLocaleChanged += HandleLocaleChanged;
        }

        protected virtual void OnDisable ()
        {
            if (Engine.TryGetService<ILocalizationManager>(out var l10n))
                l10n.OnLocaleChanged -= HandleLocaleChanged;
        }

        protected virtual void HandleLocaleChanged (LocaleChangedArgs _)
        {
            if (!text.IsEmpty) onTextChanged?.Invoke(text);
        }
    }
}
