using System;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class VariableInputPanel : CustomUI, IVariableInputUI
    {
        [Serializable]
        public new class GameState
        {
            public string VariableName;
            public CustomVariableValueType ValueType;
            public LocalizableText SummaryText;
            public LocalizableText PredefinedValue;
            public string InputFieldText;
            public string ResumeTrackId;
        }

        protected virtual string VariableName { get; private set; }
        protected virtual LocalizableText Summary { get; private set; }
        protected virtual LocalizableText PredefinedValue { get; private set; }
        protected virtual CustomVariableValueType ValueType { get; private set; }
        protected virtual string ResumeTrackId { get; private set; }
        protected virtual bool ResumeOnSubmit => !string.IsNullOrEmpty(ResumeTrackId);
        protected virtual TMP_InputField InputField => inputField;
        protected virtual Button SubmitButton => submitButton;
        protected virtual bool ActivateOnShow => activateOnShow;
        protected virtual bool SubmitOnInput => submitOnInput;
        protected virtual GameObject SummaryContainer => summaryContainer;
        protected virtual IScriptPlayer Player { get; private set; }
        protected virtual ICustomVariableManager Vars { get; private set; }
        protected virtual IStateManager State { get; private set; }
        protected virtual IInputManager Input { get; private set; }

        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button submitButton;
        [Tooltip("Whether to automatically select and activate input field when the UI is shown.")]
        [SerializeField] private bool activateOnShow = true;
        [Tooltip("Whether to attempt submit input field value when a 'Submit' input is activated.")]
        [SerializeField] private bool submitOnInput = true;
        [Tooltip("When assigned, the game object will be de-/activated based on whether summary is assigned.")]
        [SerializeField] private GameObject summaryContainer;
        [SerializeField] private StringUnityEvent onSummaryChanged;
        [SerializeField] private StringUnityEvent onPredefinedValueChanged;

        public virtual void Show (string variableName, IVariableInputUI.Options options = default)
        {
            VariableName = variableName;
            SetSummary(options.Summary);
            ValueType = options.ValueType ?? ResolveType(variableName);
            SetInputValidation(ValueType);
            SetPredefinedValue(options.PredefinedValue, variableName);
            ResumeTrackId = options.ResumeTrackId;

            Show();

            if (ActivateOnShow) ActivateInputFieldDelayed().Forget();
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(InputField, SubmitButton);

            Player = Engine.GetServiceOrErr<IScriptPlayer>();
            Vars = Engine.GetServiceOrErr<ICustomVariableManager>();
            State = Engine.GetServiceOrErr<IStateManager>();
            Input = Engine.GetServiceOrErr<IInputManager>();

            SubmitButton.interactable = false;
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            InputField.onSelect.AddListener(HandleInputSelected);
            InputField.onDeselect.AddListener(HandleInputDeselected);
            InputField.onEndEdit.AddListener(HandleInputDeselected);
            InputField.onValueChanged.AddListener(HandleInputChanged);
            InputField.onSubmit.AddListener(HandleInputSubmit);
            SubmitButton.onClick.AddListener(HandleSubmit);

            if (SubmitOnInput && Input.GetSubmit() is { } submit)
                submit.OnStart += HandleSubmit;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            InputField.onSelect.RemoveListener(HandleInputSelected);
            InputField.onDeselect.RemoveListener(HandleInputDeselected);
            InputField.onEndEdit.RemoveListener(HandleInputDeselected);
            InputField.onValueChanged.RemoveListener(HandleInputChanged);
            InputField.onSubmit.RemoveListener(HandleInputSubmit);
            SubmitButton.onClick.RemoveListener(HandleSubmit);

            if (Input?.GetSubmit() is { } submit)
                submit.OnStart -= HandleSubmit;
        }

        protected override void SerializeState (GameStateMap stateMap)
        {
            base.SerializeState(stateMap);

            var state = new GameState {
                VariableName = VariableName,
                ValueType = ValueType,
                SummaryText = Summary,
                PredefinedValue = PredefinedValue,
                InputFieldText = InputField.text,
                ResumeTrackId = ResumeTrackId
            };
            stateMap.SetState(state);
        }

        protected override async Awaitable DeserializeState (GameStateMap stateMap)
        {
            await base.DeserializeState(stateMap);

            var state = stateMap.GetState<GameState>();
            if (state is null)
            {
                InputField.text = "";
                SetSummary(LocalizableText.Empty);
                return;
            }

            VariableName = state.VariableName;
            ValueType = state.ValueType;
            await state.SummaryText.Load(this);
            await state.PredefinedValue.Load(this);
            SetSummary(state.SummaryText);
            SetInputValidation(ValueType);
            SetPredefinedValue(state.PredefinedValue, state.VariableName);
            InputField.text = state.InputFieldText;
            ResumeTrackId = state.ResumeTrackId;
        }

        protected virtual void SetSummary (LocalizableText value)
        {
            Summary = Summary.Juggle(value, this);
            onSummaryChanged?.Invoke(value);
            if (SummaryContainer)
                SummaryContainer.SetActive(!value.IsEmpty);
        }

        protected virtual void SetPredefinedValue (LocalizableText value, [CanBeNull] string variableName)
        {
            PredefinedValue = PredefinedValue.Juggle(value.IsEmpty
                ? ResolvePredefinedValue(variableName) : value, this);
            onPredefinedValueChanged?.Invoke(PredefinedValue);
        }

        protected virtual void SetInputValidation (CustomVariableValueType type)
        {
            if (type == CustomVariableValueType.Numeric)
                InputField.characterValidation = TMP_InputField.CharacterValidation.Decimal;
            else InputField.characterValidation = TMP_InputField.CharacterValidation.None;
        }

        protected virtual async Awaitable ActivateInputFieldDelayed ()
        {
            // Delay to prevent unintended submit after activating 'Continue' input
            // to disable wait input and next command shows the variable input UI,
            // at which point EventSystem registers the submit key pressed within the frame
            // and submits the input field.
            await Async.Frames(1);
            if (!InputField || !Visible) return;
            InputField.Select();
            InputField.ActivateInputField();
        }

        protected virtual void HandleInputSelected (string text)
        {
            // don't use 'this', as it's already added as the UI muter
            Input.AddMuter(InputField);
        }

        protected virtual void HandleInputDeselected (string text)
        {
            Input.RemoveMuter(InputField);
        }

        protected virtual void HandleInputChanged (string text)
        {
            SubmitButton.interactable = !string.IsNullOrWhiteSpace(text);
        }

        protected virtual void HandleInputSubmit (string text)
        {
            HandleSubmit();
        }

        protected virtual void HandleSubmit ()
        {
            if (!Visible || string.IsNullOrWhiteSpace(InputField.text)) return;

            State.PeekRollbackStack()?.AllowPlayerRollback();

            var value = ParseValue(InputField.text);
            Vars.SetVariableValue(VariableName, value);

            ClearFocus();
            Hide();

            if (ResumeOnSubmit) ResumePlayback();
        }

        protected virtual CustomVariableValue ParseValue (string inputText)
        {
            if (ValueType == CustomVariableValueType.Numeric)
                return ParseUtils.TryInvariantFloat(inputText, out var num) ? new CustomVariableValue(num)
                    : throw new Error($"Incorrect input: '{inputText}' is not a number.");
            if (ValueType == CustomVariableValueType.Boolean)
                return bool.TryParse(inputText, out var boo) ? new CustomVariableValue(boo)
                    : throw new Error($"Incorrect input: '{inputText}' is not a boolean.");
            return new(inputText);
        }

        protected virtual void ResumePlayback ()
        {
            var track = Player.GetTrackOrErr(ResumeTrackId);
            if (track.Playlist == null) throw new Error("Failed to continue playback on variable input submit: invalid playlist.");
            var nextIndex = track.Playlist.MoveAt(track.PlayedIndex);
            if (nextIndex >= 0) track.Resume(nextIndex);
        }

        protected virtual CustomVariableValueType ResolveType (string variableName)
        {
            if (Engine.GetServiceOrErr<ICustomVariableManager>().TryGetVariableValue(variableName, out var value))
                return value.Type;
            return CustomVariableValueType.String;
        }

        protected virtual LocalizableText ResolvePredefinedValue ([CanBeNull] string variableName)
        {
            if (string.IsNullOrEmpty(variableName)) return LocalizableText.Empty;
            var vals = Engine.GetServiceOrErr<ICustomVariableManager>();
            if (!vals.VariableExists(variableName)) return LocalizableText.Empty;
            var val = vals.GetVariableValue(variableName);
            if (val.Type == CustomVariableValueType.String) return val.String;
            return LocalizableText.Empty;
        }
    }
}
