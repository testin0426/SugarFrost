using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IInputManager"/>
    [InitializeAtRuntime]
    public class InputManager : IStatefulService<GameStateMap>, IInputManager
    {
        [Serializable]
        public class GameState
        {
            public bool Muted;
            public string[] MutedInputs;
        }

        public virtual event Action<bool> OnMutedChanged;
        public virtual event Action<InputMode> OnInputModeChanged;

        public virtual InputConfiguration Configuration { get; }
        public virtual bool Enabled { get; set; }
        public virtual bool Muted { get => muted; set => SetMuted(value); }
        public virtual InputMode InputMode { get => inputMode; set => ChangeInputMode(value); }

        protected virtual Dictionary<string, InputHandle> InputsById { get; } = new(StringComparer.Ordinal);
        protected virtual InputMuteRegistry MuteRegistry { get; } = new();
        protected virtual InputModeDetector ModeDetector { get; }
        protected virtual GameObject GameObject { get; private set; }
        protected virtual List<IDisposable> DisposeOnDestroy { get; } = new();

        private bool muted;
        private InputMode inputMode;

        public InputManager (InputConfiguration config)
        {
            Configuration = config;
            ModeDetector = new(this);
        }

        public virtual async Awaitable InitializeService ()
        {
            Enabled = !Configuration.DisableInput;
            GameObject = Engine.CreateObject(new() { Name = nameof(InputManager) });
            await InitializeInputSystem();
            ChangeInputMode(GetDefaultInputMode());
            if (Configuration.DetectInputMode)
                ModeDetector.Start();
        }

        public virtual void ResetService () { }

        public virtual void DestroyService ()
        {
            ModeDetector?.Dispose();
            MuteRegistry?.Dispose();
            foreach (var disposable in DisposeOnDestroy)
                disposable.Dispose();
            DisposeOnDestroy.Clear();
            InputsById.Clear();
            ObjectUtils.DestroyOrImmediate(GameObject);
        }

        public virtual void SaveServiceState (GameStateMap map)
        {
            map.SetState(new GameState {
                Muted = Muted,
                MutedInputs = InputsById.Where(kv => kv.Value.Muted).Select(kv => kv.Key).ToArray()
            });
        }

        public virtual Awaitable LoadServiceState (GameStateMap map)
        {
            var state = map.GetState<GameState>();
            if (state is null) return Async.Completed;

            Muted = state.Muted;

            foreach (var kv in InputsById)
                kv.Value.Muted = state.MutedInputs?.Contains(kv.Key) ?? false;

            return Async.Completed;
        }

        public virtual IInputHandle GetInput (string id)
        {
            return InputsById.GetValueOrDefault(id);
        }

        public virtual void AddMuter (object muter, IReadOnlyCollection<string> allowedIds = null)
        {
            MuteRegistry.AddMuter(muter, allowedIds);
        }

        public virtual void RemoveMuter (object muter)
        {
            MuteRegistry.RemoveMuter(muter);
        }

        public virtual bool IsMuted (string id)
        {
            if (Application.isEditor && EditorProxy.StoryEditorFocused) return true;
            if (!Enabled || Muted || !MuteRegistry.IsAllowed(id)) return true;
            return !InputsById.TryGetValue(id, out var input) || input.Muted;
        }

        public virtual Vector2? GetPointerPosition ()
        {
            #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
            return UnityEngine.InputSystem.Pointer.current?.position.ReadValue();
            #else
            return null;
            #endif
        }

        protected virtual async Awaitable InitializeInputSystem ()
        {
            #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE

            var asset = Configuration.InputActions ?? Resources
                .Load<UnityEngine.InputSystem.InputActionAsset>("Naninovel/Input/DefaultControls");
            foreach (var map in Configuration.ActionMaps)
                if (asset.FindActionMap(map) is { } actions)
                    foreach (var action in actions)
                        InputsById[action.name] = CreateInput(action);

            if (Configuration.SpawnEventSystem)
            {
                var system = Configuration.EventSystem ?? Resources.Load("Naninovel/Input/DefaultEventSystem");
                await Engine.Instantiate(system, new() { Parent = GameObject.transform });
            }

            InputHandle CreateInput (UnityEngine.InputSystem.InputAction action)
            {
                var input = new InputHandle(action.name, this);
                action.Enable();
                action.started += Start;
                action.canceled += Cancel;
                action.performed += Perform;
                DisposeOnDestroy.Add(new Defer(() => {
                    action.started -= Start;
                    action.canceled -= Cancel;
                    action.performed -= Perform;
                }));
                return input;

                void Start (UnityEngine.InputSystem.InputAction.CallbackContext _) => input.Activate(Vector2.zero);
                void Cancel (UnityEngine.InputSystem.InputAction.CallbackContext _) => input.Activate(Vector2.zero);
                void Perform (UnityEngine.InputSystem.InputAction.CallbackContext ctx) => input.Activate(GetPerformForce(ctx));
                static Vector2 GetPerformForce (UnityEngine.InputSystem.InputAction.CallbackContext ctx)
                {
                    if (ctx.valueType == typeof(Vector2)) return ctx.ReadValue<Vector2>();
                    var value = ctx.ReadValue<float>();
                    if (value == 0 && ctx.action.type == UnityEngine.InputSystem.InputActionType.Button)
                        return new(1, 0); // performed button can't be zero (required for swipes)
                    return new(value, 0);
                }
            }

            #else
            await Async.NextFrame(); // required to prevent warning due to a missing 'await'
            #endif
        }

        protected virtual void SetMuted (bool muted)
        {
            this.muted = muted;
            OnMutedChanged?.Invoke(muted);
        }

        protected virtual void ChangeInputMode (InputMode mode)
        {
            inputMode = mode;
            OnInputModeChanged?.Invoke(mode);
        }

        protected virtual InputMode GetDefaultInputMode ()
        {
            if (Application.isConsolePlatform) return InputMode.Gamepad;
            if (Application.isMobilePlatform) return InputMode.Touch;
            return InputMode.Mouse;
        }

        #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterInteractions ()
        {
            UnityEngine.InputSystem.InputSystem.RegisterInteraction<SwipeInteraction>("Swipe");
            UnityEngine.InputSystem.InputSystem.RegisterInteraction<ScrollInteraction>("Scroll");
        }
        #endif
    }
}
