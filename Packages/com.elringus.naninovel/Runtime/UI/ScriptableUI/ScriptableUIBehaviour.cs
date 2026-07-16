using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Naninovel
{
    /// <summary>
    /// A wrapper over <see cref="UIBehaviour"/> providing various scripting utility APIs.
    /// </summary>
    public class ScriptableUIBehaviour : UIBehaviour
    {
        public enum FocusMode
        {
            Visibility,
            Navigation
        }

        /// <summary>
        /// Occurs when visibility of the UI changes.
        /// </summary>
        public event Action<bool> OnVisibilityChanged;

        /// <summary>
        /// Fade duration (in seconds) when changing visibility of the UI;
        /// requires a <see cref="UnityEngine.CanvasGroup"/> on the same game object.
        /// </summary>
        public virtual float FadeTime { get => fadeTime; set => fadeTime = value; }
        /// <summary>
        /// Whether to ignore timescale when changing visibility (fade animation).
        /// </summary>
        public virtual bool IgnoreTimeScale { get => ignoreTimeScale; set => ignoreTimeScale = value; }
        /// <summary>
        /// Whether the UI element should be visible or hidden on awake.
        /// requires a <see cref="UnityEngine.CanvasGroup"/> on the same game object.
        /// </summary>
        public virtual bool VisibleOnAwake => visibleOnAwake;
        /// <summary>
        /// Determines when to focus the object: on the UI becomes visible or on first navigation attempt (arrow keys or d-pad) while the UI is visible.
        /// </summary>
        public virtual FocusMode FocusModeType { get => focusMode; set => focusMode = value; }
        /// <summary>
        /// The object to focus (for keyboard or gamepad control) when the UI becomes visible or upon navigation.
        /// </summary>
        [CanBeNull] public virtual GameObject FocusObject { get => focusObject; set => focusObject = value; }
        /// <summary>
        /// Whether the UI is currently visible.
        /// requires a <see cref="UnityEngine.CanvasGroup"/> on the same game object.
        /// </summary>
        public virtual bool Visible { get => visibleSelf; set => SetVisibility(value); }
        /// <summary>
        /// Current opacity (alpha) of the UI element, in 0.0 to 1.0 range.
        /// requires a <see cref="UnityEngine.CanvasGroup"/> on the same game object, will always return 1.0 otherwise.
        /// </summary>
        public virtual float Opacity => CanvasGroup ? CanvasGroup.alpha : 1f;
        /// <summary>
        /// Whether the UI is currently interactable.
        /// requires a <see cref="UnityEngine.CanvasGroup"/> on the same game object.
        /// </summary>
        public virtual bool Interactable { get => !CanvasGroup || CanvasGroup.interactable; set => SetInteractable(value); }
        /// <summary>
        /// Whether interaction with the object is permanently disabled, no matter the visibility.
        /// </summary>
        public virtual bool DisableInteraction => disableInteraction;
        /// <summary>
        /// Transform used by the UI element.
        /// </summary>
        public virtual RectTransform RectTransform => GetRectTransform();
        /// <summary>
        /// Topmost parent (in the game object hierarchy) canvas component.
        /// </summary>
        public virtual Canvas TopmostCanvas => topmostCanvasCache ? topmostCanvasCache : topmostCanvasCache = gameObject.FindTopmostComponent<Canvas>();
        /// <summary>
        /// Current sort order of the UI element, as per <see cref="TopmostCanvas"/>.
        /// </summary>
        public virtual int SortingOrder { get => TopmostCanvas ? TopmostCanvas.sortingOrder : 0; set => SetSortingOrder(value); }
        /// <summary>
        /// Current render mode of the UI element, as per <see cref="TopmostCanvas"/>.
        /// </summary>
        public virtual RenderMode RenderMode { get => TopmostCanvas ? TopmostCanvas.renderMode : default; set => SetRenderMode(value); }
        /// <summary>
        /// Current render camera of the UI element, as per <see cref="TopmostCanvas"/>.
        /// </summary>
        public virtual Camera RenderCamera { get => TopmostCanvas ? TopmostCanvas.worldCamera : null; set => SetRenderCamera(value); }

        /// <summary>
        /// Game object to focus on next navigation input (eg, arrow keys on keyboard or dpad on gamepad).
        /// </summary>
        [CanBeNull] protected static GameObject FocusOnNavigation { get; set; }

        /// <summary>
        /// Canvas group component attached to the host game object, if any.
        /// </summary>
        [CanBeNull] protected virtual CanvasGroup CanvasGroup { get; private set; }
        /// <summary>
        /// Whether to change opacity (alpha) of Canvas Group in correspondence to visibility of the UI element.
        /// </summary>
        protected virtual bool ControlOpacity => controlOpacity;

        [Tooltip("Whether to permanently disable interaction with the object, no matter the visibility. Requires `Canvas Group` component on the same game object.")]
        [SerializeField] private bool disableInteraction;
        [Tooltip("Whether UI element should be visible or hidden on awake.")]
        [SerializeField] private bool visibleOnAwake = true;
        [Tooltip("Whether to change opacity (alpha) of Canvas Group in correspondence to visibility of the UI element. Requires `Canvas Group` component on the same game object.")]
        [SerializeField] private bool controlOpacity = true;
        [Tooltip("When `Control Opacity` is enabled, controls opacity fade duration (in seconds) when changing visibility.")]
        [SerializeField] private float fadeTime = .3f;
        [Tooltip("When `Control Opacity` is enabled, controls whether to ignore time scale when changing visibility.")]
        [SerializeField] private bool ignoreTimeScale = true;
        [Tooltip("When assigned, will make the object focused (for keyboard or gamepad control) when the UI becomes visible or upon navigation.")]
        [SerializeField] private GameObject focusObject;
        [Tooltip("When `Focus Object` is assigned, determines when to focus the object: on the UI becomes visible or on first navigation attempt (arrow keys or d-pad) while the UI is visible. Be aware, that gamepad support for Navigation mode requires Unity's new input system package installed.")]
        [SerializeField] private FocusMode focusMode;
        [Tooltip("Invoked when the UI element is shown.")]
        [SerializeField] private UnityEvent onShow;
        [Tooltip("Invoked when the UI element is hidden.")]
        [SerializeField] private UnityEvent onHide;

        private readonly Tweener<FloatTween> fadeTweener = new();
        private RectTransform rectTransform;
        private Canvas topmostCanvasCache;
        private bool visibleSelf;

        /// <summary>
        /// Gradually changes <see cref="Visible"/> with fade animation over <see cref="FadeTime"/>
        /// or the specified time (in seconds).
        /// </summary>
        public virtual Awaitable ChangeVisibility (bool visible, float? duration = null, AsyncToken token = default)
        {
            if (fadeTweener.Running)
                fadeTweener.Stop();

            visibleSelf = visible;

            HandleVisibilityChanged(visible);

            if (!CanvasGroup) return Async.Completed;

            if (!DisableInteraction)
            {
                CanvasGroup.interactable = visible;
                CanvasGroup.blocksRaycasts = visible;
            }

            if (!ControlOpacity) return Async.Completed;

            var fadeDuration = duration ?? FadeTime;
            var targetOpacity = visible ? 1f : 0f;

            if (Mathf.Approximately(fadeDuration, 0f))
            {
                CanvasGroup.alpha = targetOpacity;
                return Async.Completed;
            }

            var tw = new FloatTween(CanvasGroup.alpha, targetOpacity,
                new(fadeDuration, scale: !IgnoreTimeScale), SetOpacity);
            return fadeTweener.Run(tw, token, this);
        }

        /// <summary>
        /// Changes <see cref="Visible"/>.
        /// </summary>
        public virtual void SetVisibility (bool visible)
        {
            if (fadeTweener.Running)
                fadeTweener.Stop();

            visibleSelf = visible;

            HandleVisibilityChanged(visible);

            if (!CanvasGroup) return;

            if (!DisableInteraction)
            {
                CanvasGroup.interactable = visible;
                CanvasGroup.blocksRaycasts = visible;
            }

            if (ControlOpacity)
                CanvasGroup.alpha = visible ? 1f : 0f;
        }

        /// <summary>
        /// Toggles <see cref="Visible"/>.
        /// </summary>
        public virtual void ToggleVisibility ()
        {
            ChangeVisibility(!Visible).Forget();
        }

        /// <summary>
        /// Reveals the UI over <see cref="FadeTime"/>.
        /// </summary>
        [ContextMenu("Show")]
        public virtual void Show ()
        {
            ChangeVisibility(true).Forget();
        }

        /// <summary>
        /// Hides the UI over <see cref="FadeTime"/>.
        /// </summary>
        [ContextMenu("Hide")]
        public virtual void Hide ()
        {
            ChangeVisibility(false).Forget();
        }

        /// <summary>
        /// Changes <see cref="Opacity"/>; 
        /// has no effect when <see cref="CanvasGroup"/> is missing on the same game object.
        /// </summary>
        public virtual void SetOpacity (float opacity)
        {
            if (!CanvasGroup) return;
            CanvasGroup.alpha = opacity;
        }

        /// <summary>
        /// Changes <see cref="Interactable"/>; 
        /// has no effect when <see cref="CanvasGroup"/> is missing on the same game object.
        /// </summary>
        public virtual void SetInteractable (bool interactable)
        {
            if (!CanvasGroup) return;
            CanvasGroup.interactable = interactable;
        }

        /// <summary>
        /// Removes input focus from the UI element.
        /// </summary>
        public virtual void ClearFocus ()
        {
            if (EventUtils.Selected &&
                EventUtils.Selected.transform.IsChildOf(transform))
                EventUtils.Select(null);
        }

        /// <summary>
        /// Applies input focus to the UI element.
        /// </summary>
        public virtual void SetFocus ()
        {
            EventUtils.Select(gameObject);
        }

        protected override void Awake ()
        {
            base.Awake();

            CanvasGroup = GetComponent<CanvasGroup>();

            if (CanvasGroup && DisableInteraction)
            {
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
            }

            SetVisibilityOnAwake();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            if (Engine.GetService<IInputManager>()?.GetNavigate() is { } nav)
                nav.OnStart += TryFocusOnNavigation;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (Engine.GetService<IInputManager>()?.GetNavigate() is { } nav)
                nav.OnStart -= TryFocusOnNavigation;
        }

        protected virtual void SetVisibilityOnAwake ()
        {
            SetVisibility(VisibleOnAwake);
        }

        /// <summary>
        /// Invoked when visibility of the UI is changed.
        /// </summary>
        /// <param name="visible">The new visibility of the UI.</param>
        protected virtual void HandleVisibilityChanged (bool visible)
        {
            OnVisibilityChanged?.Invoke(visible);

            if (visible) onShow?.Invoke();
            else onHide?.Invoke();

            if (visible && FocusObject)
                switch (FocusModeType)
                {
                    case FocusMode.Visibility:
                        FocusOnNavigation = null;
                        FocusDelayed(FocusObject).Forget();
                        break;
                    case FocusMode.Navigation:
                        FocusOnNavigation = FocusObject;
                        break;
                }
            else if (!visible && IsFocusInside())
                Engine.GetService<IUIManager>()?.FocusTop();
        }

        protected async Awaitable FocusDelayed (GameObject go)
        {
            // Delay is required to prevent interactable UIs from being unintentionally
            // submitted when they're shown in response to continue input activation.
            // For example, player presses 'Enter' to continue and next command shows a
            // UI with a button: the button will submit, as the UI is shown and button is
            // selected in the same frame as the 'Enter' key is pressed.
            await Async.Frames(1);
            if (go && FocusObject == go)
                EventUtils.Select(FocusObject);
        }

        protected virtual bool IsFocusInside ()
        {
            return EventUtils.Selected && EventUtils.Selected.transform.IsChildOf(transform);
        }

        protected virtual void TryFocusOnNavigation ()
        {
            if (FocusModeType != FocusMode.Navigation || !FocusOnNavigation || !Visible) return;
            EventUtils.Select(FocusOnNavigation);
            FocusOnNavigation = null;
        }

        private RectTransform GetRectTransform ()
        {
            if (!rectTransform)
                rectTransform = GetComponent<RectTransform>();
            return rectTransform;
        }

        private void SetSortingOrder (int value)
        {
            if (!TopmostCanvas) return;
            TopmostCanvas.sortingOrder = value;
        }

        private void SetRenderMode (RenderMode renderMode)
        {
            if (!TopmostCanvas) return;
            TopmostCanvas.renderMode = renderMode;
        }

        private void SetRenderCamera (Camera renderCamera)
        {
            if (!TopmostCanvas) return;
            TopmostCanvas.worldCamera = renderCamera;
        }
    }
}
