using System.Collections.Generic;

namespace Naninovel.UI
{
    public class GameSettingsSkipModeDropdown : ScriptableDropdown
    {
        [ManagedText("DefaultUI")]
        protected static string ReadOnly = "Read Only";
        [ManagedText("DefaultUI")]
        protected static string Everything = "Everything";

        private IScriptPlayer player;

        protected override void Awake ()
        {
            base.Awake();

            player = Engine.GetServiceOrErr<IScriptPlayer>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            InitializeOptions();

            if (Engine.TryGetService<ILocalizationManager>(out var l10n))
                l10n.OnLocaleChanged += HandleLocaleChanged;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (Engine.TryGetService<ILocalizationManager>(out var l10n))
                l10n.OnLocaleChanged -= HandleLocaleChanged;
        }

        protected override void OnValueChanged (int value)
        {
            player.SkipMode = (PlayerSkipMode)value;
        }

        protected virtual void InitializeOptions ()
        {
            var options = new List<string> { ReadOnly, Everything };
            UIComponent.ClearOptions();
            UIComponent.AddOptions(options);
            UIComponent.value = (int)player.SkipMode;
            UIComponent.RefreshShownValue();
        }

        protected virtual void HandleLocaleChanged (LocaleChangedArgs _) => InitializeOptions();
    }
}
