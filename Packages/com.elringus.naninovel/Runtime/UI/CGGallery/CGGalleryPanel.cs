using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CGGalleryPanel : CustomUI, ICGGalleryUI
    {
        public const string CGPrefix = "CG";

        protected virtual ResourceLoaderConfiguration[] CGSources => cgSources;
        protected virtual CGViewerPanel ViewerPanel => viewerPanel;
        protected virtual CGGalleryGrid Grid => grid;
        protected virtual CGGalleryGridSlot LastViewed { get; set; }

        [Tooltip("The specified resource loaders will be used to retrieve the available CG slots and associated textures.")]
        [SerializeField] private ResourceLoaderConfiguration[] cgSources = {
            new() { PathPrefix = $"{UnlockablesConfiguration.DefaultPathPrefix}/{CGPrefix}" },
            new() { PathPrefix = $"{BackgroundsConfiguration.DefaultPathPrefix}/{BackgroundsConfiguration.MainActorId}/{CGPrefix}" }
        };
        [Tooltip("Used to view selected CG slots.")]
        [SerializeField] private CGViewerPanel viewerPanel;
        [Tooltip("Used to host and navigate selectable CG preview thumbnails.")]
        [SerializeField] private CGGalleryGrid grid;
        [SerializeField] private IntUnityEvent onPageChanged;

        private IResourceProviderManager resources;
        private ILocalizationManager l10n;

        public override async Awaitable Initialize ()
        {
            var slotData = new List<CGSlotData>();
            foreach (var src in CGSources)
                InitializeLoader(src);
            await Grid.Initialize(viewerPanel, slotData);
            if (Visible) Engine.GetServiceOrErr<IUIManager>().FocusTop();

            BindInput(Inputs.Page, HandlePageInput);
            BindInput(Inputs.Cancel, HandleCancelInput, new() { OnEnd = true });

            void InitializeLoader (ResourceLoaderConfiguration loaderConfig)
            {
                var loader = loaderConfig.CreateLocalizableFor<Texture2D>(resources, l10n);
                using var _ = loader.RentPaths(out var resourcePaths);
                var pathsBySlots = resourcePaths.OrderBy(p => p).GroupBy(CGPathToSlotId);
                foreach (var pathsBySlot in pathsBySlots)
                    AddSlotData(pathsBySlot, loader);
            }

            string CGPathToSlotId (string cgPath)
            {
                if (cgPath.Contains(CGPrefix + "/"))
                    cgPath = cgPath.GetAfterFirst(CGPrefix + "/");
                if (!cgPath.Contains("_") ||
                    !ParseUtils.TryInvariantInt(cgPath.GetAfter("_"), out _)) return cgPath;
                return cgPath.GetBeforeLast("_");
            }

            void AddSlotData (IGrouping<string, string> pathsBySlot, IResourceLoader<Texture2D> loader)
            {
                var id = pathsBySlot.Key;
                if (slotData.Any(s => s.Id == id)) return;
                var data = new CGSlotData(id, pathsBySlot.OrderBy(p => p), loader);
                slotData.Add(data);
            }
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(Grid, ViewerPanel);

            resources = Engine.GetServiceOrErr<IResourceProviderManager>();
            l10n = Engine.GetServiceOrErr<ILocalizationManager>();
        }

        protected virtual void HandleCancelInput ()
        {
            if (ViewerPanel.Visible) ViewerPanel.Hide();
            else Hide();
        }

        protected virtual void HandlePageInput (Vector2 force)
        {
            if (ViewerPanel.Visible) return;
            if (force.x < 0) Grid.SelectPreviousPage();
            if (force.x > 0) Grid.SelectNextPage();
            EventUtils.Select(FindFocusObject());
            onPageChanged?.Invoke(Grid.CurrentPage);
        }

        protected override GameObject FindFocusObject ()
        {
            if (InputMode.IsPointerBased()) return null;
            if (!Grid || Grid.Slots == null || Grid.Slots.Count == 0) return null;

            var slotToFocus = default(CGGalleryGridSlot);
            foreach (var slot in Grid.Slots)
                if (slot.gameObject.activeInHierarchy && (!slotToFocus || slot.LastSelectTime > slotToFocus.LastSelectTime))
                    slotToFocus = slot;

            return slotToFocus ? slotToFocus.gameObject : null;
        }
    }
}
