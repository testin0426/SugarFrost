using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ILocalizationManager"/>
    [InitializeAtRuntime]
    public class LocalizationManager : IStatefulService<SettingsStateMap>, ILocalizationManager
    {
        [Serializable]
        public class Settings
        {
            public string SelectedLocale;
        }

        private readonly struct OnChangeTask
        {
            public readonly Func<LocaleChangedArgs, Awaitable> Handler;
            public readonly int Priority;

            public OnChangeTask (Func<LocaleChangedArgs, Awaitable> handler, int priority)
            {
                Handler = handler;
                Priority = priority;
            }
        }

        public event Action<LocaleChangedArgs> OnLocaleChanged;

        public virtual LocalizationConfiguration Configuration { get; }
        public virtual string SelectedLocale { get; private set; }
        IReadOnlyCollection<string> ILocalizationManager.AvailableLocales => AvailableLocales;
        IReadOnlyList<IResourceProvider> ILocalizationManager.Providers => Providers;

        protected virtual HashSet<string> AvailableLocales { get; } = new();
        protected virtual List<IResourceProvider> Providers { get; } = new();

        private readonly IResourceProviderManager resources;
        private readonly List<OnChangeTask> changeTasks = new();

        public LocalizationManager (LocalizationConfiguration cfg, IResourceProviderManager resources)
        {
            Configuration = cfg;
            this.resources = resources;
        }

        public virtual Awaitable InitializeService ()
        {
            resources.CollectProviders(Providers, Configuration.Loader.ProviderTypes);
            RetrieveAvailableLocales();
            return Async.Completed;
        }

        public virtual void ResetService () { }

        public virtual void DestroyService () { }

        public virtual void SaveServiceState (SettingsStateMap stateMap)
        {
            var settings = new Settings {
                SelectedLocale = SelectedLocale
            };
            stateMap.SetState(settings);
        }

        public virtual async Awaitable LoadServiceState (SettingsStateMap stateMap)
        {
            var locale = stateMap.GetState<Settings>()?.SelectedLocale;
            if (string.IsNullOrWhiteSpace(locale)) locale = GetDefaultLocale();
            await SelectLocale(locale);
        }

        public virtual void CollectAvailableLocales (ICollection<string> tags)
        {
            foreach (var tag in AvailableLocales)
                tags.Add(tag);
        }

        public virtual bool LocaleAvailable (string locale) => AvailableLocales.Contains(locale);

        public virtual async Awaitable SelectLocale (string locale)
        {
            if (!LocaleAvailable(locale))
            {
                Engine.Warn($"Failed to select locale: Locale '{locale}' is not available.");
                return;
            }

            if (locale == SelectedLocale) return;

            var eventArgs = new LocaleChangedArgs(locale, SelectedLocale);
            SelectedLocale = locale;

            using (new InteractionBlocker())
                foreach (var task in changeTasks.OrderBy(t => t.Priority))
                    await task.Handler(eventArgs);

            OnLocaleChanged?.Invoke(eventArgs);
        }

        public virtual void AddChangeLocaleTask (Func<LocaleChangedArgs, Awaitable> handler, int priority)
        {
            if (!changeTasks.Any(t => t.Handler == handler))
                changeTasks.Add(new(handler, priority));
        }

        public virtual void RemoveChangeLocaleTask (Func<LocaleChangedArgs, Awaitable> handler)
        {
            changeTasks.RemoveAll(t => t.Handler == handler);
        }

        /// <summary>
        /// Retrieves available localizations by locating folders inside the localization resources root.
        /// Folder names should correspond to the <see cref="Languages"/> tag entries (RFC5646).
        /// </summary>
        protected virtual void RetrieveAvailableLocales ()
        {
            AvailableLocales.Clear();
            var match = $"{Configuration.Loader.PathPrefix}/";
            foreach (var provider in Providers)
                using (provider.RentPaths(out var paths, match))
                    foreach (var path in paths)
                        if (path.GetBetween(match, "/") is { } tag && Languages.ContainsTag(tag))
                            AvailableLocales.Add(tag);
            if (Configuration.ExposeSourceLocale)
                AvailableLocales.Add(Configuration.SourceLocale);
        }

        protected virtual string GetDefaultLocale ()
        {
            if (Configuration.AutoDetectLocale && TryMapSystemLocale(out var locale))
                return locale;
            if (!string.IsNullOrEmpty(Configuration.DefaultLocale))
                return Configuration.DefaultLocale;
            return Configuration.SourceLocale;
        }

        protected virtual bool TryMapSystemLocale (out string locale)
        {
            var lang = Application.systemLanguage.ToString();
            foreach (var (tag, name) in Languages.NameByTag)
                if (name.EqualsIgnoreCase(lang))
                    return LocaleAvailable(locale = tag);
            locale = null;
            return false;
        }
    }
}
