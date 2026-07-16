using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Routes essential <see cref="IBackgroundManager"/> APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Background Events")]
    public class BackgroundEvents : UnityEvents
    {
        [ActorPopup(BackgroundsConfiguration.DefaultPathPrefix), CanBeNull]
        [Tooltip("The identifier of the background actor for the events and actions.")]
        public string BackgroundId = BackgroundsConfiguration.MainActorId;

        [Space]
        [Tooltip("Occurs when availability of the background manager engine service changes.")]
        public BoolUnityEvent ServiceAvailable;
        [Tooltip("Occurs when a background with ID is added.")]
        public StringUnityEvent BackgroundAdded;
        [Tooltip("Occurs when a background with ID is removed.")]
        public StringUnityEvent BackgroundRemoved;
        [Tooltip("Occurs when availability of a background with the specified ID changes.")]
        public BoolUnityEvent BackgroundAvailable;
        [Tooltip("Occurs when appearance of a background with the specified ID changes.")]
        public StringUnityEvent BackgroundAppearanceChanged;
        [Tooltip("Occurs when visibility of a background with the specified ID changes.")]
        public BoolUnityEvent BackgroundVisibilityChanged;
        [Tooltip("Occurs when position of a background with the specified ID changes.")]
        public Vector3UnityEvent BackgroundPositionChanged;
        [Tooltip("Occurs when scale of a background with the specified ID changes.")]
        public Vector3UnityEvent BackgroundScaleChanged;
        [Tooltip("Occurs when rotation of a background with the specified ID changes.")]
        public QuaternionUnityEvent BackgroundRotationChanged;
        [Tooltip("Occurs when tint color of a background with the specified ID changes.")]
        public ColorUnityEvent BackgroundTintColorChanged;

        public async void ShowBackground ()
        {
            if (!Engine.TryGetService<IBackgroundManager>(out var manager)) return;
            var bg = await manager.GetOrAddActor(BackgroundId);
            await bg.ChangeVisibility(true, new(manager.Configuration.DefaultDuration));
        }

        public async void HideBackground ()
        {
            if (!Engine.TryGetService<IBackgroundManager>(out var manager)) return;
            if (!manager.ActorExists(BackgroundId)) return;
            await manager.GetActorOrErr(BackgroundId).ChangeVisibility(false, new(manager.Configuration.DefaultDuration));
        }

        public void HideAllBackgrounds ()
        {
            if (!Engine.TryGetService<IBackgroundManager>(out var manager)) return;
            using var _ = manager.RentActors(out var bgs);
            foreach (var bg in bgs)
                bg.ChangeVisibility(false, new(manager.Configuration.DefaultDuration)).Forget();
        }

        public async void ChangeBackgroundAppearance (string appearance)
        {
            if (!Engine.TryGetService<IBackgroundManager>(out var manager)) return;
            var bg = await manager.GetOrAddActor(BackgroundId);
            await bg.ChangeAppearance(appearance, new(manager.Configuration.DefaultDuration));
        }

        public async void ChangeBackgroundPositionX (int x)
        {
            if (!Engine.TryGetService<IBackgroundManager>(out var manager)) return;
            var bg = await manager.GetOrAddActor(BackgroundId);
            await bg.ChangePositionX(x, new(manager.Configuration.DefaultDuration));
        }

        public async void ChangeBackgroundPositionY (int y)
        {
            if (!Engine.TryGetService<IBackgroundManager>(out var manager)) return;
            var bg = await manager.GetOrAddActor(BackgroundId);
            await bg.ChangePositionY(y, new(manager.Configuration.DefaultDuration));
        }

        public async void ChangeBackgroundPositionZ (int z)
        {
            if (!Engine.TryGetService<IBackgroundManager>(out var manager)) return;
            var bg = await manager.GetOrAddActor(BackgroundId);
            await bg.ChangePositionZ(z, new(manager.Configuration.DefaultDuration));
        }

        public async void ChangeBackgroundScale (int scale)
        {
            if (!Engine.TryGetService<IBackgroundManager>(out var manager)) return;
            var bg = await manager.GetOrAddActor(BackgroundId);
            await bg.ChangeScale(Vector3.one * scale, new(manager.Configuration.DefaultDuration));
        }

        protected override void HandleEngineInitialized ()
        {
            if (Engine.TryGetService<IBackgroundManager>(out var bgs))
            {
                ServiceAvailable?.Invoke(true);

                bgs.OnActorAdded -= HandleActorAdded;
                bgs.OnActorAdded += HandleActorAdded;
                if (bgs.ActorExists(BackgroundId))
                    HandleActorAdded(BackgroundId);

                bgs.OnActorRemoved -= HandleActorRemoved;
                bgs.OnActorRemoved += HandleActorRemoved;

                if (!string.IsNullOrEmpty(BackgroundId) && bgs.ActorExists(BackgroundId))
                    BackgroundAvailable?.Invoke(true);
                else BackgroundAvailable?.Invoke(false);
            }
            else ServiceAvailable?.Invoke(false);
        }

        protected override void HandleEngineDestroyed ()
        {
            ServiceAvailable?.Invoke(false);
            BackgroundAvailable?.Invoke(false);
        }

        protected virtual void HandleActorAdded (string id)
        {
            BackgroundAdded?.Invoke(id);
            if (id == BackgroundId)
            {
                BackgroundAvailable?.Invoke(true);

                var bg = Engine.GetServiceOrErr<IBackgroundManager>().GetActorOrErr(id);
                bg.OnAppearanceChanged += BackgroundAppearanceChanged.SafeInvoke;
                bg.OnVisibilityChanged += BackgroundVisibilityChanged.SafeInvoke;
                bg.OnPositionChanged += BackgroundPositionChanged.SafeInvoke;
                bg.OnScaleChanged += BackgroundScaleChanged.SafeInvoke;
                bg.OnRotationChanged += BackgroundRotationChanged.SafeInvoke;
                bg.OnTintColorChanged += BackgroundTintColorChanged.SafeInvoke;
            }
        }

        protected virtual void HandleActorRemoved (string id)
        {
            BackgroundRemoved?.Invoke(id);
            if (id == BackgroundId) BackgroundAvailable?.Invoke(false);
        }
    }
}
