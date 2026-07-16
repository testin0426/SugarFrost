using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Routes essential <see cref="ILocalizationManager"/> APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Localization Events")]
    public class LocalizationEvents : UnityEvents
    {
        [Tooltip("Occurs when availability of the localization manager engine service changes.")]
        public BoolUnityEvent ServiceAvailable;
        [Tooltip("Occurs when the locale (language) changes.")]
        public StringUnityEvent LocaleChanged;

        public void SelectLocale (string locale)
        {
            if (Engine.TryGetService<ILocalizationManager>(out var l10n))
                l10n.SelectLocale(locale).Forget();
        }

        protected override void HandleEngineInitialized ()
        {
            if (Engine.TryGetService<ILocalizationManager>(out var l10n))
            {
                ServiceAvailable?.Invoke(true);

                l10n.OnLocaleChanged -= HandleLocaleChanged;
                l10n.OnLocaleChanged += HandleLocaleChanged;
            }
            else ServiceAvailable?.Invoke(false);
        }

        protected override void HandleEngineDestroyed ()
        {
            ServiceAvailable?.Invoke(false);
        }

        protected virtual void HandleLocaleChanged (LocaleChangedArgs args) => LocaleChanged?.Invoke(args.CurrentLocale);
    }
}
