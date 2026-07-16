using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace Naninovel.UI
{
    /// <summary>
    /// Routes essential <see cref="IUIManager"/> APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/UI Events")]
    public class UIEvents : UnityEvents
    {
        [ResourcePopup(UIConfiguration.DefaultUIPathPrefix), CanBeNull]
        [Tooltip("Specify a UI name to be notified about the specific events of that UI.")]
        public string UIName;

        [Space]
        [Tooltip("Occurs when availability of the UI manager engine service changes.")]
        public BoolUnityEvent ServiceAvailable;
        [Tooltip("Occurs when the UI font changes.")]
        public StringUnityEvent FontNameChanged;
        [Tooltip("Occurs when the size of the UI font changes.")]
        public IntUnityEvent FontSizeChanged;
        [Tooltip("Occurs when availability of a UI with the specified name changes.")]
        public BoolUnityEvent UIAvailable;
        [Tooltip("Occurs when visibility of a UI with the specified name changes.")]
        public BoolUnityEvent UIVisible;
        [Tooltip("Occurs when UI with the specified name is shown.")]
        public UnityEvent UIShown;
        [Tooltip("Occurs when UI with the specified name is hidden.")]
        public UnityEvent UIHidden;

        public void SetFontName (string name)
        {
            if (Engine.TryGetService<IUIManager>(out var uis))
                uis.FontName = name;
        }

        public void SetFontSize (int size)
        {
            if (Engine.TryGetService<IUIManager>(out var uis))
                uis.FontSize = size;
        }

        public void ShowUI (string name)
        {
            if (Engine.TryGetService<IUIManager>(out var uis))
                uis.GetUI(name)?.Show();
        }

        public void HideUI (string name)
        {
            if (Engine.TryGetService<IUIManager>(out var uis))
                uis.GetUI(name)?.Hide();
        }

        protected override void HandleEngineInitialized ()
        {
            if (Engine.TryGetService<IUIManager>(out var uis))
            {
                ServiceAvailable?.Invoke(true);

                FontNameChanged?.Invoke(uis.FontName);
                uis.OnFontNameChanged -= FontNameChanged.SafeInvoke;
                uis.OnFontNameChanged += FontNameChanged.SafeInvoke;

                FontSizeChanged?.Invoke(uis.FontSize);
                uis.OnFontSizeChanged -= FontSizeChanged.SafeInvoke;
                uis.OnFontSizeChanged += FontSizeChanged.SafeInvoke;

                if (!string.IsNullOrEmpty(UIName) && uis.TryGetUI(UIName, out var ui))
                {
                    UIAvailable?.Invoke(true);
                    UIVisible?.Invoke(ui.Visible);
                    ui.OnVisibilityChanged -= HandleVisibilityChanged;
                    ui.OnVisibilityChanged += HandleVisibilityChanged;
                }
                else UIAvailable?.Invoke(false);
            }
            else ServiceAvailable?.Invoke(false);
        }

        protected override void HandleEngineDestroyed ()
        {
            ServiceAvailable?.Invoke(false);
            UIAvailable?.Invoke(false);
            UIVisible?.Invoke(false);
        }

        protected virtual void HandleVisibilityChanged (bool visible)
        {
            UIVisible?.Invoke(visible);
            if (visible) UIShown?.Invoke();
            else UIHidden?.Invoke();
        }
    }
}
