using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.UI;
using TMPro;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IUIManager"/>
    [InitializeAtRuntime]
    public class UIManager : IUIManager, IStatefulService<SettingsStateMap>
    {
        [Serializable]
        public class Settings
        {
            public string FontName;
            public int FontSize = -1;
        }

        public event Action<string> OnFontNameChanged;
        public event Action<int> OnFontSizeChanged;

        public virtual UIConfiguration Configuration { get; }
        public virtual string FontName { get => fontName; set => SetFontName(value); }
        public virtual int FontSize { get => fontSize; set => SetFontSize(value); }
        public virtual bool AnyModal => Modals.Count > 0;

        protected virtual List<ManagedUI> ManagedUIs { get; } = new();
        protected virtual Dictionary<string, TMP_FontAsset> AssetByFontName { get; } = new();
        protected virtual Dictionary<Type, IManagedUI> CachedUIByType { get; } = new();
        protected virtual List<IManagedUI> Modals { get; } = new();
        protected virtual LocalizableResourceLoader<GameObject> UILoader { get; private set; }
        protected virtual LocalizableResourceLoader<TMP_FontAsset> FontLoader { get; private set; }
        protected virtual GameObject Container { get; private set; }
        protected virtual GameObject ModalContainer { get; private set; }
        protected virtual CanvasGroup ContainerGroup { get; private set; }

        private readonly ICameraManager camera;
        private readonly IInputManager input;
        private readonly IResourceProviderManager resources;
        private readonly ILocalizationManager l10n;
        private readonly ICommunityLocalization communityL10n;
        private IInputHandle toggleUIInput;
        private string fontName;
        private int fontSize = -1;

        public UIManager (UIConfiguration config, IResourceProviderManager resources,
            ILocalizationManager l10n, ICommunityLocalization communityL10n,
            ICameraManager camera, IInputManager input)
        {
            Configuration = config;
            this.resources = resources;
            this.l10n = l10n;
            this.communityL10n = communityL10n;
            this.camera = camera;
            this.input = input;

            // Instantiating the UIs after the engine initialization so that UIs
            // can use Engine API in Awake() and OnEnable() methods.
            Engine.AddPostInitializationTask(InstantiateUIs);
        }

        public virtual async Awaitable InitializeService ()
        {
            UILoader = Configuration.UILoader.CreateLocalizableFor<GameObject>(resources, l10n);
            FontLoader = Configuration.FontLoader.CreateLocalizableFor<TMP_FontAsset>(resources, l10n);

            Container = Engine.CreateObject(new() { Name = "UI" });
            ContainerGroup = Container.AddComponent<CanvasGroup>();
            ModalContainer = Engine.CreateObject(new() { Name = "ModalUI" });

            toggleUIInput = input.GetToggleUI();
            if (toggleUIInput != null)
                toggleUIInput.OnStart += ToggleUI;

            foreach (var (name, asset) in await InitializeFonts())
                AssetByFontName[name] = asset;
        }

        public virtual void ResetService () { }

        public virtual void DestroyService ()
        {
            if (toggleUIInput != null)
                toggleUIInput.OnStart -= ToggleUI;

            foreach (var ui in ManagedUIs)
                ObjectUtils.DestroyOrImmediate(ui.GameObject);
            ManagedUIs.Clear();
            CachedUIByType.Clear();
            AssetByFontName.Clear();

            ObjectUtils.DestroyOrImmediate(Container);
            ObjectUtils.DestroyOrImmediate(ModalContainer);

            UILoader?.ReleaseAll(this);
            FontLoader?.ReleaseAll(this);

            l10n.RemoveChangeLocaleTask(ApplyFontAssociatedWithLocale);

            Engine.RemovePostInitializationTask(InstantiateUIs);
        }

        public virtual void SaveServiceState (SettingsStateMap stateMap)
        {
            var settings = new Settings {
                FontName = FontName,
                FontSize = FontSize
            };
            stateMap.SetState(settings);
        }

        public virtual Awaitable LoadServiceState (SettingsStateMap stateMap)
        {
            var settings = stateMap.GetState<Settings>() ?? new Settings {
                FontName = Configuration.DefaultFont
            };
            FontName = settings.FontName;
            FontSize = settings.FontSize;

            return Async.Completed;
        }

        public virtual async Awaitable<IManagedUI> AddUI (GameObject prefab, string name = null, string group = null)
        {
            var uiComponent = await InstantiatePrefab(prefab, name, group);
            await uiComponent.Initialize();
            return uiComponent;
        }

        public virtual void CollectUIs (ICollection<IManagedUI> uis)
        {
            foreach (var managedUI in ManagedUIs)
                uis.Add(managedUI.UIComponent);
        }

        public virtual bool HasUI<T> () where T : class, IManagedUI
        {
            var type = typeof(T);
            if (CachedUIByType.ContainsKey(type)) return true;
            foreach (var managedUI in ManagedUIs)
                if (type.IsAssignableFrom(managedUI.ComponentType))
                    return true;
            return false;
        }

        public virtual bool HasUI (string name)
        {
            foreach (var managedUI in ManagedUIs)
                if (managedUI.Name == name)
                    return true;
            return false;
        }

        public virtual T GetUI<T> () where T : class, IManagedUI => GetUI(typeof(T)) as T;

        public virtual IManagedUI GetUI (Type type)
        {
            if (CachedUIByType.TryGetValue(type, out var cachedResult))
                return cachedResult;

            foreach (var managedUI in ManagedUIs)
                if (type.IsAssignableFrom(managedUI.ComponentType))
                {
                    var result = managedUI.UIComponent;
                    CachedUIByType[type] = result;
                    return managedUI.UIComponent;
                }

            return null;
        }

        public virtual IManagedUI GetUI (string name)
        {
            foreach (var managedUI in ManagedUIs)
                if (managedUI.Name == name)
                    return managedUI.UIComponent;
            return null;
        }

        public virtual bool RemoveUI (IManagedUI managedUI)
        {
            if (!TryGetManaged(managedUI, out var ui))
                return false;

            ManagedUIs.Remove(ui);
            foreach (var kv in CachedUIByType.ToList())
                if (kv.Value == managedUI)
                    CachedUIByType.Remove(kv.Key);

            ObjectUtils.DestroyOrImmediate(ui.GameObject);

            return true;
        }

        public virtual void SetUIVisibleWithToggle (bool visible, bool allowToggle = true)
        {
            camera.RenderUI = visible;

            var clickThroughPanel = GetUI<IClickThroughPanel>();
            if (clickThroughPanel is null) return;

            if (visible) clickThroughPanel.Hide();
            else
            {
                if (allowToggle) clickThroughPanel.Show(true, ToggleUI, Inputs.Submit, Inputs.ToggleUI);
                else clickThroughPanel.Show(false, null);
            }
        }

        public virtual void AddModal (IManagedUI managedUI)
        {
            if (IsModal(managedUI) || !TryGetManaged(managedUI, out var ui)) return;
            foreach (var otherModal in Modals)
                if (TryGetManaged(otherModal, out var otherModalUI) && ShouldYieldModal(otherModal, managedUI))
                    otherModalUI.GameObject.transform.SetParent(otherModalUI.Parent, false);
            ui.GameObject.transform.SetParent(ModalContainer.transform, false);
            Modals.Insert(0, managedUI);
            ContainerGroup.interactable = false;
        }

        public virtual void RemoveModal (IManagedUI managedUI)
        {
            if (!IsModal(managedUI) || !TryGetManaged(managedUI, out var ui)) return;
            ui.GameObject.transform.SetParent(ui.Parent, false);
            Modals.Remove(managedUI);
            if (Modals.Count == 0)
            {
                ContainerGroup.interactable = true;
                return;
            }

            for (var i = 0; i < Modals.Count; i++)
            {
                if (!TryGetManaged(Modals[i], out var modalUI)) continue;
                var yielded = false;
                for (var j = 0; j < i; j++)
                    if (ShouldYieldModal(Modals[i], Modals[j]))
                    {
                        yielded = true;
                        break;
                    }
                modalUI.GameObject.transform.SetParent(yielded ? modalUI.Parent : ModalContainer.transform, false);
            }
        }

        public virtual void CollectModals (ICollection<IManagedUI> uis)
        {
            foreach (var modal in Modals)
                uis.Add(modal);
        }

        public virtual bool IsActiveModal (IManagedUI managedUI)
        {
            if (!TryGetManaged(managedUI, out var ui)) return false;
            return ui.GameObject.transform.parent == ModalContainer.transform;
        }

        public TMP_FontAsset GetFontAsset (string fontName)
        {
            return AssetByFontName.TryGetValue(fontName, out var asset) ? asset :
                throw new Error($"Failed to get '{fontName}' font asset: unknown font.");
        }

        protected virtual bool HasFontAsset (string fontName)
        {
            return AssetByFontName.ContainsKey(fontName);
        }

        protected virtual bool IsModal (IManagedUI managedUI)
        {
            return Modals.Contains(managedUI);
        }

        protected virtual bool ShouldYieldModal (IManagedUI existingModal, IManagedUI newModal)
        {
            if (string.IsNullOrEmpty(existingModal.ModalGroup)) return true;
            if (existingModal.ModalGroup == "*") return false;
            return existingModal.ModalGroup != newModal.ModalGroup;
        }

        protected virtual async Awaitable<IManagedUI> InstantiatePrefab (GameObject prefab,
            string name = null, string group = null)
        {
            var layer = Configuration.OverrideObjectsLayer ? (int?)Configuration.ObjectsLayer : null;
            var parent = string.IsNullOrEmpty(group) ? Container.transform : GetOrCreateGroup(group);
            var obj = await Engine.Instantiate(prefab, new() { Name = prefab.name, Layer = layer, Parent = parent });

            if (!obj.TryGetComponent<IManagedUI>(out var uiComponent))
                throw new Error($"Failed to instantiate '{prefab.name}' UI prefab: the prefab doesn't contain a '{nameof(CustomUI)}' or '{nameof(IManagedUI)}' component on the root object.");

            if (!uiComponent.RenderCamera)
                uiComponent.RenderCamera = camera.UICamera ? camera.UICamera : camera.Camera;

            if (!string.IsNullOrEmpty(FontName)) uiComponent.SetFont(GetFontAsset(FontName));
            if (FontSize >= 0) uiComponent.SetFontSize(FontSize);

            var managedUI = new ManagedUI(name ?? prefab.name, obj, uiComponent);
            ManagedUIs.Add(managedUI);

            return uiComponent;
        }

        protected virtual Transform GetOrCreateGroup (string group)
        {
            var existing = Container.transform.Find(group);
            if (!existing)
            {
                existing = new GameObject(group).transform;
                existing.parent = Container.transform;
            }
            return existing;
        }

        protected virtual void SetFontName (string fontName)
        {
            if (FontName == fontName) return;

            if (!string.IsNullOrEmpty(fontName) && !HasFontAsset(fontName))
            {
                Engine.Warn($"Failed to set '{fontName}' font: unknown font. " +
                            $"Make sure '{fontName}' is set up in Font Options under UI configuration.");
                return;
            }

            this.fontName = fontName;

            OnFontNameChanged?.Invoke(fontName);

            if (string.IsNullOrEmpty(fontName))
            {
                foreach (var ui in ManagedUIs)
                    ui.UIComponent.SetFont(null);
                return;
            }

            var fontAsset = GetFontAsset(fontName);
            foreach (var ui in ManagedUIs)
                ui.UIComponent.SetFont(fontAsset);
        }

        protected virtual void SetFontSize (int size)
        {
            if (fontSize == size) return;

            fontSize = size;

            OnFontSizeChanged?.Invoke(size);

            foreach (var ui in ManagedUIs)
                ui.UIComponent.SetFontSize(size);
        }

        protected virtual void ToggleUI () => SetUIVisibleWithToggle(!camera.RenderUI);

        protected virtual async Awaitable InstantiateUIs ()
        {
            var resources = await UILoader.LoadAll(holder: this);
            await Async.All(resources.Select(r => InstantiatePrefab(r, UILoader.GetLocalPath(r))));
            await Async.All(ManagedUIs.Where(u => u.UIComponent is not CustomUI { LazyInitialize: true })
                .Select(u => u.UIComponent.Initialize()));

            await ApplyFontAssociatedWithLocale(default);
            l10n.AddChangeLocaleTask(NotifyLocaleChanged, 1);
            l10n.AddChangeLocaleTask(ApplyFontAssociatedWithLocale, 2);

            ShowVisibleOnAwake();
        }

        protected virtual void ShowVisibleOnAwake ()
        {
            if (Engine.GetConfiguration<ScriptsConfiguration>().ShowScriptNavigator)
                GetUI<IScriptNavigatorUI>()?.Show();
            foreach (var ui in ManagedUIs)
                if (ui.UIComponent is CustomUI custom && custom.VisibleOnAwake)
                    custom.Show();
        }

        protected virtual bool TryGetManaged (IManagedUI ui, out ManagedUI managedUI)
        {
            managedUI = default;
            foreach (var mui in ManagedUIs)
                if (mui.UIComponent == ui)
                {
                    managedUI = mui;
                    return true;
                }
            return false;
        }

        protected virtual Awaitable ApplyFontAssociatedWithLocale (LocaleChangedArgs _)
        {
            if (communityL10n.Active)
            {
                SetFontName(AssetByFontName.First().Key);
                return Async.Completed;
            }

            if (!Configuration.FontOptions.Any(o => !string.IsNullOrEmpty(o.ApplyOnLocale)))
                return Async.Completed;

            foreach (var option in Configuration.FontOptions)
                if (option.ApplyOnLocale == l10n.SelectedLocale)
                {
                    SetFontName(option.FontName);
                    return Async.Completed;
                }

            if (!string.IsNullOrEmpty(FontName)) SetFontName("");
            return Async.Completed;
        }

        protected virtual Awaitable NotifyLocaleChanged (LocaleChangedArgs _)
        {
            using var __ = Async.Rent(out var tasks);
            foreach (var ui in ManagedUIs)
                if (ui.UIComponent is ILocalizableUI locUI)
                    tasks.Add(locUI.HandleLocalizationChanged(default));
            return Async.All(tasks);
        }

        protected virtual async Awaitable<IEnumerable<(string Name, TMP_FontAsset Asset)>> InitializeFonts ()
        {
            if (communityL10n.Active)
            {
                var font = await communityL10n.LoadFont();
                return new (string, TMP_FontAsset)[] { (font.name, font) };
            }
            return (await Async.All(Configuration.FontOptions.Select(LoadFontAsset))).Where(x => x.Name != null);

            async Awaitable<(string Name, TMP_FontAsset Asset)> LoadFontAsset (UIConfiguration.FontOption option)
            {
                var resource = await FontLoader.Load(option.FontResource, this);
                if (resource.Valid) return (option.FontName, resource.Object);
                Engine.Warn($"Failed to load '{option.FontResource}' font resource. " +
                            $"Make sure '{option.FontName}' in Font Options under UI configuration has valid font resource specified.");
                return (null, null);
            }
        }
    }
}
