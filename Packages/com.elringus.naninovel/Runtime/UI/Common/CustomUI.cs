using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Naninovel.UI
{
    /// <summary>
    /// Base implementation of <see cref="IManagedUI"/> for building custom UIs.
    /// </summary>
    [AddComponentMenu("Naninovel/ UI/Custom UI")]
    public class CustomUI : ScriptableUIBehaviour, IManagedUI
    {
        [Serializable]
        public class GameState
        {
            public bool Visible;
        }

        [Serializable]
        public class FontChangeConfiguration
        {
            [Tooltip("The container game object with text components, which should be affected by font changes.")]
            public GameObject Object;
            [Tooltip("Whether to affect container children game objects; when disabled only text component on the specified container object will be affected.")]
            public bool IncludeChildren;
            [Tooltip("Whether to allow changing font of the text component.")]
            public bool AllowFontChange = true;
            [Tooltip("Whether to allow changing font size of the text component.")]
            public bool AllowFontSizeChange = true;
            [Tooltip("Sizes list should contain actual font sizes to apply for text component. Each element in the list corresponds to font size dropdown list index: Small -> 0, Default -> 1, Large -> 2, Extra Large -> 3 (can be changed via SettingsUI). Default value will be ignored and font size initially set in the prefab will be used instead.")]
            public FontSizes FontSizes;
            [NonSerialized]
            public List<TMP_Text> Components = new();
            [NonSerialized]
            public Dictionary<TMP_Text, int> DefaultSizes = new();
            [NonSerialized]
            public Dictionary<TMP_Text, TMP_FontAsset> DefaultFonts = new();
        }

        public struct SelectableData
        {
            public Selectable Selectable;
            public Navigation Navigation;
        }

        /// <summary>
        /// Optional <see cref="CustomUI.BindInput(string,System.Action,System.Nullable{Naninovel.UI.CustomUI.BindInputOptions})"/> configuration.
        /// </summary>
        public struct BindInputOptions
        {
            /// <summary>
            /// Whether to invoke bound handler when input ends activation (key released);
            /// by default invoked when input starts activation (key pressed).
            /// </summary>
            public bool OnEnd;
            /// <summary>
            /// Whether to invoke bound handler while the UI is hidden;
            /// by default invoked only when the UI is visible.
            /// </summary>
            public bool WhenHidden;
            /// <summary>
            /// Whether to invoke bound handler while the UI is blocked by a modal;
            /// by default not invoked when a modal UI is active, unless the UI itself is the modal.
            /// </summary>
            public bool WhenBlocked;
        }

        /// <summary>
        /// Optional <see cref="CustomUI.BindInput(string,System.Action{float},System.Nullable{Naninovel.UI.CustomUI.BindInputChangeOptions})"/> configuration.
        /// </summary>
        public struct BindInputChangeOptions
        {
            /// <inheritdoc cref="BindInputOptions.WhenHidden"/>
            public bool WhenHidden;
            /// <inheritdoc cref="BindInputOptions.WhenBlocked"/>
            public bool WhenBlocked;
        }

        public override GameObject FocusObject { get => base.FocusObject ? base.FocusObject : FindFocusObject(); set => base.FocusObject = value; }
        public virtual bool LazyInitialize => lazyInitialize;
        public virtual bool HideOnLoad => hideOnLoad;
        public virtual bool HideInThumbnail => hideInThumbnail;
        public virtual bool SaveVisibilityState => saveVisibilityState;
        public virtual bool BlockInputWhenVisible => blockInputWhenVisible;
        public virtual bool DisableBlockedInputFeatures => disableBlockedInputFeatures;
        public virtual bool AdaptToInputMode => adaptToInputMode;
        public virtual bool ModalUI => modalUI;
        [CanBeNull] public virtual string ModalGroup => modalGroup;

        protected virtual bool WasLazyInitialized { get; private set; }
        protected virtual InputMode InputMode { get; private set; }
        protected virtual List<SelectableData> Selectables { get; } = new();
        protected virtual List<FontChangeConfiguration> FontChangeConfigurations => fontChangeConfiguration;
        protected virtual string[] AllowedInputs => allowedInputs;
        [CanBeNull] protected virtual GameObject ButtonControls => buttonControls;
        [CanBeNull] protected virtual GameObject KeyboardControls => keyboardControls;
        [CanBeNull] protected virtual GameObject GamepadControls => gamepadControls;

        [Tooltip("Whether to delay the UI initialization until its shown.")]
        [SerializeField] private bool lazyInitialize;
        [Tooltip("Whether to automatically hide the UI when loading game or resetting state.")]
        [SerializeField] private bool hideOnLoad = true;
        [Tooltip("Whether to hide the UI when capturing thumbnail for save-load slots.")]
        [SerializeField] private bool hideInThumbnail;
        [Tooltip("Whether to preserve visibility of the UI when saving/loading game.")]
        [SerializeField] private bool saveVisibilityState = true;
        [Tooltip("Whether to halt user input processing while the UI is visible. Will also exit auto read and skip script player modes when the UI becomes visible.")]
        [SerializeField] private bool blockInputWhenVisible;
        [FormerlySerializedAs("allowedSamplers"), Tooltip("IDs of the inputs that should still be allowed in case the input is blocked while the UI is visible.")]
        [SerializeField] private string[] allowedInputs;
        [Tooltip("Whether to also disable features associated with the blocked inputs while the UI is visible, such as auto play and skip.")]
        [SerializeField] private bool disableBlockedInputFeatures = true;
        [Tooltip("Whether to modify the UI based on current input mode (mouse and keyboard, gamepad, touch, etc). Will take control of focus mode and navigation of the underlying selectables.")]
        [SerializeField] private bool adaptToInputMode = true;
        [Tooltip("Whether to make all the other managed UIs not interactable while the UI is visible.")]
        [SerializeField] private bool modalUI;
        [Tooltip("When assigned, will not yield the UI from being modal when another UI is made modal with the same group. Assign `*` to never yield the UI from being modal.")]
        [SerializeField] private string modalGroup;
        [Tooltip("Control buttons associated with the UI. Will be hidden unless input mode is mouse or touch.")]
        [SerializeField] private GameObject buttonControls;
        [Tooltip("Labels indicating keyboard controls associated with the UI. Will be hidden unless input mode is keyboard.")]
        [SerializeField] private GameObject keyboardControls;
        [Tooltip("Labels indicating gamepad controls associated with the UI. Will be hidden unless input mode is gamepad.")]
        [SerializeField] private GameObject gamepadControls;
        [Tooltip("Setup which game objects should be affected by font and text size changes (set in game settings).")]
        [SerializeField] private List<FontChangeConfiguration> fontChangeConfiguration;

        private IScriptPlayer player;
        private IStateManager state;
        private IInputManager input;
        private IUIManager uis;

        public virtual Awaitable Initialize () => Async.Completed;

        public virtual void SetFont (TMP_FontAsset font)
        {
            FontChanger.ChangeFont(font, FontChangeConfigurations);
        }

        public virtual void SetFontSize (int dropdownIndex)
        {
            FontChanger.ChangeFontSize(dropdownIndex, FontChangeConfigurations);
        }

        /// <remarks>
        /// Default implementation is naive using <see cref="SerializeState"/> followed by
        /// <see cref="DeserializeState"/> forcing reinitialization with the current state.
        /// In cases when such re-serialization contain lots of unrelated operations,
        /// consider overriding the method for more granular behaviour.
        /// </remarks>
        public virtual Awaitable HandleLocalizationChanged (LocaleChangedArgs _)
        {
            var map = new GameStateMap();
            SerializeState(map);
            return DeserializeState(map);
        }

        protected override void Awake ()
        {
            player = Engine.GetServiceOrErr<IScriptPlayer>();
            state = Engine.GetServiceOrErr<IStateManager>();
            input = Engine.GetServiceOrErr<IInputManager>();
            uis = Engine.GetServiceOrErr<IUIManager>();

            base.Awake();

            InitializeFontChangeConfiguration();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            if (HideOnLoad)
            {
                state.OnGameLoadStarted += HandleGameLoadStarted;
                state.OnResetStarted += Hide;
            }

            state.AddOnGameSerializeTask(SerializeState);
            state.AddOnGameDeserializeTask(DeserializeState);

            if (AdaptToInputMode)
            {
                foreach (var selectable in GetComponentsInChildren<Selectable>(true))
                    Selectables.Add(new() { Selectable = selectable, Navigation = selectable.navigation });
                input.OnInputModeChanged += HandleInputModeChanged;
                HandleInputModeChanged(input.InputMode);
            }
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (HideOnLoad && state != null)
            {
                state.OnGameLoadStarted -= HandleGameLoadStarted;
                state.OnResetStarted -= Hide;
            }

            if (state != null)
            {
                state.RemoveOnGameSerializeTask(SerializeState);
                state.RemoveOnGameDeserializeTask(DeserializeState);
            }

            if (input != null)
            {
                input.OnInputModeChanged -= HandleInputModeChanged;
                input.RemoveMuter(this);
            }

            Selectables.Clear();
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();
            Selectables?.Clear();
            FontChangeConfigurations?.Clear();
        }

        protected virtual void SerializeState (GameStateMap stateMap)
        {
            if (SaveVisibilityState)
            {
                var state = new GameState {
                    Visible = Visible
                };
                stateMap.SetState(state, name);
            }
        }

        protected virtual Awaitable DeserializeState (GameStateMap stateMap)
        {
            if (SaveVisibilityState)
            {
                var state = stateMap.GetState<GameState>(name);
                if (state is null) return Async.Completed;
                Visible = state.Visible;
            }
            return Async.Completed;
        }

        protected virtual Awaitable EnsureInitialized ()
        {
            if (!LazyInitialize || WasLazyInitialized) return Async.Completed;
            WasLazyInitialized = true;
            return Initialize();
        }

        protected override void HandleVisibilityChanged (bool visible)
        {
            base.HandleVisibilityChanged(visible);

            if (visible) EnsureInitialized();

            if (ModalUI)
            {
                if (visible) uis.AddModal(this);
                else uis.RemoveModal(this);
            }

            if (BlockInputWhenVisible)
            {
                if (visible) input.AddMuter(this, AllowedInputs);
                else input.RemoveMuter(this);

                if (visible && DisableBlockedInputFeatures)
                {
                    if (player.Skipping && !(AllowedInputs?.Contains(Inputs.Skip) ?? false))
                        player.SetSkip(false);
                    if (player.AutoPlaying && !(AllowedInputs?.Contains(Inputs.AutoPlay) ?? false))
                        player.SetAutoPlay(false);
                }
            }
        }

        [CanBeNull]
        protected virtual GameObject FindFocusObject ()
        {
            foreach (var sel in Selectables)
                if (sel.Navigation.mode != Navigation.Mode.None && sel.Selectable.gameObject.activeInHierarchy)
                    return sel.Selectable.gameObject;
            return null;
        }

        protected override void SetVisibilityOnAwake ()
        {
            // Visible on awake UIs are shown by UI manager after engine initialization.
            if (!Engine.Initialized) SetVisibility(false);
            else base.SetVisibilityOnAwake();
        }

        protected virtual void InitializeFontChangeConfiguration ()
        {
            FontChanger.InitializeConfiguration(FontChangeConfigurations);
        }

        protected virtual void HandleInputModeChanged (InputMode mode)
        {
            InputMode = mode;
            if (ButtonControls) ButtonControls.SetActive(mode != InputMode.Gamepad && mode != InputMode.Keyboard);
            if (KeyboardControls) KeyboardControls.SetActive(mode == InputMode.Keyboard);
            if (GamepadControls) GamepadControls.SetActive(mode == InputMode.Gamepad);
            var doNav = mode == InputMode.Gamepad || mode == InputMode.Keyboard;
            var noNav = new Navigation { mode = Navigation.Mode.None };
            foreach (var selectable in Selectables)
                selectable.Selectable.navigation = doNav ? selectable.Navigation : noNav;
            FocusModeType = doNav ? FocusMode.Visibility : FocusMode.Navigation;
            if (Visible && doNav && FocusObject)
                EventUtils.Select(FocusObject);
        }

        /// <summary>
        /// Bounds specified handler to an input with the specified name to be invoked while the UI is enabled, visible and
        /// not blocked by a modal (or is the modal itself). The conditions can be configured via the "options" parameter.
        /// </summary>
        protected virtual void BindInput (string inputName, Action handler, BindInputOptions? options = null)
        {
            if (!this.input.TryGetInput(inputName, out var input)) return;
            if (options is not { OnEnd: true }) input.OnStart += HandleInput;
            else input.OnEnd += HandleInput;

            void HandleInput ()
            {
                if (ShouldHandleBoundInput(options?.WhenHidden ?? false, options?.WhenBlocked ?? false))
                    handler();
            }
        }

        /// <inheritdoc cref="BindInput(string,System.Action,System.Nullable{Naninovel.UI.CustomUI.BindInputOptions})"/>
        protected virtual void BindInput (string inputName, Action<Vector2> handler, BindInputChangeOptions? options = null)
        {
            if (this.input.TryGetInput(inputName, out var input))
                input.OnChange += HandleInputChange;

            void HandleInputChange (Vector2 force)
            {
                if (ShouldHandleBoundInput(options?.WhenHidden ?? false, options?.WhenBlocked ?? false))
                    handler(force);
            }
        }

        protected virtual bool ShouldHandleBoundInput (bool whenHidden, bool whenBlocked)
        {
            if (!isActiveAndEnabled) return false;
            if (!whenHidden && !Visible) return false;
            if (whenBlocked) return true;
            return !uis.AnyModal || uis.IsActiveModal(this);
        }

        protected virtual void HandleGameLoadStarted (GameSaveLoadArgs args) => Hide();
    }
}
