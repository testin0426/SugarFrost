namespace Naninovel.UI
{
    public class GameSettingsFontDropdown : ScriptableDropdown
    {
        [ManagedText("DefaultUI")]
        protected static string DefaultFontName = "Default";

        private IUIManager uis;
        private ILocalizationManager l10n;
        private ICommunityLocalization communityL10n;

        protected override void Awake ()
        {
            base.Awake();

            uis = Engine.GetServiceOrErr<IUIManager>();
            l10n = Engine.GetServiceOrErr<ILocalizationManager>();
            communityL10n = Engine.GetServiceOrErr<ICommunityLocalization>();
            l10n.OnLocaleChanged += HandleLocaleChanged;
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();

            if (l10n != null) // don't un/sub on enable/disable, as the dropdown is disabled in InitializeOptions
                l10n.OnLocaleChanged -= HandleLocaleChanged;
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            InitializeOptions();
        }

        protected override void OnValueChanged (int value)
        {
            var isDefault = UIComponent.options[value].text == DefaultFontName;
            uis.FontName = isDefault ? null : UIComponent.options[value].text;
        }

        protected virtual void InitializeOptions ()
        {
            if (communityL10n.Active || uis.Configuration.FontOptions is not { Count: > 0 } options)
            {
                transform.parent.gameObject.SetActive(false);
                return;
            }

            using var _ = ListPool<string>.Rent(out var names);

            // don't show default font under non-source locales as it may not be compatible;
            // use 'Font Options' item with empty 'Apply On Locale' to show a font under all locales
            if (l10n.IsSourceLocaleSelected() && !string.IsNullOrEmpty(DefaultFontName) ||
                uis.IsDefaultFontSelected()) // but always show when the default font is already selected
                names.Add(DefaultFontName);

            foreach (var option in options)
                if (string.IsNullOrWhiteSpace(option.ApplyOnLocale) || option.ApplyOnLocale == l10n.SelectedLocale)
                    names.Add(option.FontName);

            if (names.Count <= 1)
            {
                transform.parent.gameObject.SetActive(false);
                return;
            }

            UIComponent.ClearOptions();
            UIComponent.AddOptions(names);
            UIComponent.SetValueWithoutNotify(GetCurrentIndex());
            UIComponent.RefreshShownValue();
            transform.parent.gameObject.SetActive(true);
        }

        protected virtual int GetCurrentIndex ()
        {
            for (var i = 0; i < UIComponent.options.Count; i++)
                if (UIComponent.options[i].text == uis.FontName ||
                    uis.IsDefaultFontSelected() && UIComponent.options[i].text == DefaultFontName)
                    return i;
            throw new Error("Failed to get current index of game settings dropdown");
        }

        protected virtual void HandleLocaleChanged (LocaleChangedArgs _) => InitializeOptions();
    }
}
