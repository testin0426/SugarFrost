using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Represents a view for choosing between a set of choices.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ChoiceHandlerPanel : CustomUI, IManagedUI, ILocalizableUI
    {
        [Serializable]
        public new class GameState
        {
            // Saving buttons separately from handler actor choices, as they're destroyed dependently.
            public List<Choice> Buttons = new();
        }

        /// <summary>
        /// Invoked when one of active choices are chosen.
        /// </summary>
        public event Action<Choice> OnChoice;

        protected virtual List<ChoiceHandlerButton> ChoiceButtons { get; } = new();
        protected virtual RectTransform ButtonsContainer => buttonsContainer;
        protected virtual ChoiceHandlerButton DefaultButtonPrefab => defaultButtonPrefab;
        protected virtual IResourceLoader<GameObject> CustomButtonLoader { get; private set; }
        protected virtual bool FocusChoiceButtons => focusChoiceButtons;

        [Tooltip("Container that will hold spawned choice buttons.")]
        [SerializeField] private RectTransform buttonsContainer;
        [Tooltip("Button prototype to use by default.")]
        [SerializeField] private ChoiceHandlerButton defaultButtonPrefab;
        [Tooltip("Whether to focus added choice buttons for keyboard and gamepad control.")]
        [SerializeField] private bool focusChoiceButtons = true;

        private ChoiceHandlerButton focusedButton;
        private bool removeAllButtonsPending;

        Awaitable IManagedUI.ChangeVisibility (bool visible, float? duration, AsyncToken token)
        {
            Engine.Err("@showUI and @hideUI commands can't be used with choice handlers; use @show/hide commands instead");
            return Async.Completed;
        }

        public virtual void AddChoiceButton (Choice choice)
        {
            if (removeAllButtonsPending)
            {
                removeAllButtonsPending = false;
                RemoveAllChoiceButtons();
            }

            if (ChoiceButtons.Any(b => b.Choice.Id == choice.Id)) return; // Could happen on rollback.

            var prefab = string.IsNullOrWhiteSpace(choice.ButtonPath)
                ? defaultButtonPrefab
                : LoadCustomButtonPrefab(choice.ButtonPath);
            var button = Instantiate(prefab, buttonsContainer, false);
            button.Initialize(choice, OnChoice);

            if (choice.ButtonPosition.HasValue)
                button.transform.localPosition = choice.ButtonPosition.Value;

            ChoiceButtons.Add(button);

            if (ShouldFocusAddedButton(button))
                FocusAddedButton(button);
        }

        public virtual void RemoveChoiceButton (string id)
        {
            var buttons = ChoiceButtons.FindAll(c => c.Choice.Id == id);
            if (buttons.Count == 0) return;

            foreach (var button in buttons)
            {
                if (button) Destroy(button.gameObject);
                ChoiceButtons.Remove(button);
            }
        }

        /// <summary>
        /// Will remove the buttons before the next <see cref="AddChoiceButton(Choice)"/> call.
        /// </summary>
        public virtual void RemoveAllChoiceButtonsDelayed ()
        {
            ChoiceButtons?.ForEach(HideIfValid);
            removeAllButtonsPending = true;

            static void HideIfValid (ChoiceHandlerButton button)
            {
                if (button) button.Hide();
            }
        }

        public virtual void RemoveAllChoiceButtons ()
        {
            for (int i = 0; i < ChoiceButtons.Count; i++)
                Destroy(ChoiceButtons[i].gameObject);
            ChoiceButtons.Clear();
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(defaultButtonPrefab, buttonsContainer);

            CustomButtonLoader = Engine.GetServiceOrErr<IChoiceHandlerManager>().ChoiceButtonLoader;
        }

        protected virtual ChoiceHandlerButton LoadCustomButtonPrefab (string path)
        {
            var prefab = CustomButtonLoader.GetLoadedOrErr(path);
            if (prefab.TryGetComponent<ChoiceHandlerButton>(out var b2)) return b2;
            throw new Error($"Failed to add custom '{path}' choice button. " +
                            $"Make sure the button prefab has 'ChoiceHandlerButton' component and is stored under " +
                            $"'Resources/Naninovel/{ChoiceHandlersConfiguration.DefaultButtonPathPrefix}' folder " +
                            "or custom loader in choices configuration is set up correctly.");
        }

        protected virtual bool ShouldFocusAddedButton (ChoiceHandlerButton button)
        {
            // Focus only the first added non-locked button.
            return FocusChoiceButtons && !focusedButton && !button.Choice.Locked;
        }

        protected virtual void FocusAddedButton (ChoiceHandlerButton button)
        {
            focusedButton = button;

            switch (FocusModeType)
            {
                case FocusMode.Visibility:
                    EventUtils.Select(button.gameObject);
                    break;
                case FocusMode.Navigation:
                    FocusOnNavigation = button.gameObject;
                    break;
            }
        }

        protected override void HandleVisibilityChanged (bool visible)
        {
            base.HandleVisibilityChanged(visible);
            if (!visible) focusedButton = null;

            if (visible && removeAllButtonsPending) // in case shown with @show while buttons pending removal
            {
                removeAllButtonsPending = false;
                RemoveAllChoiceButtons();
            }
        }

        protected override GameObject FindFocusObject ()
        {
            if (!FocusChoiceButtons) return null;
            return focusedButton ? focusedButton.gameObject : null;
        }

        public override async Awaitable HandleLocalizationChanged (LocaleChangedArgs _)
        {
            await Async.All(ChoiceButtons.Select(LocalizeChoice));

            async Awaitable LocalizeChoice (ChoiceHandlerButton button)
            {
                await button.Choice.Summary.Load(); // held by the choice handler actor
                button.Initialize(button.Choice, OnChoice);
            }
        }

        protected override void SerializeState (GameStateMap stateMap)
        {
            base.SerializeState(stateMap);

            var state = new GameState {
                // Don't save removeAllButtonsPending, as it'll result in summary choice text resolve error on load.
                Buttons = removeAllButtonsPending ? new() : ChoiceButtons.Select(b => b.Choice).ToList()
            };
            stateMap.SetState(state, name);
        }

        protected override async Awaitable DeserializeState (GameStateMap stateMap)
        {
            await base.DeserializeState(stateMap);

            var state = stateMap.GetState<GameState>(name);
            if (state is null) return;

            var existingButtonIds = ChoiceButtons.Select(b => b.Choice.Id).ToList();
            foreach (var buttonId in existingButtonIds)
                if (state.Buttons.All(s => s.Id != buttonId))
                    RemoveChoiceButton(buttonId);

            foreach (var buttonState in state.Buttons)
                if (ChoiceButtons.All(b => b.Choice != buttonState))
                {
                    if (!string.IsNullOrEmpty(buttonState.ButtonPath))
                        await CustomButtonLoader.LoadOrErr(buttonState.ButtonPath);
                    AddChoiceButton(buttonState);
                }
        }
    }
}
