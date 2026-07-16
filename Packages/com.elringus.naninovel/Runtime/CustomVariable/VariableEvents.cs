using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;

namespace Naninovel
{
    /// <summary>
    /// Allows to listen for events when value of a custom state variable with specific name is changed.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Variable Events")]
    public class VariableEvents : UnityEvents
    {
        [Serializable] private class StringChangedEvent : UnityEvent<string> { }
        [Serializable] private class NumericChangedEvent : UnityEvent<float> { }
        [Serializable] private class IntegerChangedEvent : UnityEvent<int> { }
        [Serializable] private class BooleanChangedEvent : UnityEvent<bool> { }
        [Serializable] private class VariableRemovedEvent : UnityEvent { }

        /// <summary>
        /// Name of a custom state variable to listen for.
        /// </summary>
        public virtual string CustomVariableName { get => customVariableName; set => customVariableName = value; }

        [Tooltip("Name of a custom state variable to listen for.")]
        [SerializeField] private string customVariableName;
        [Tooltip("Occurs when availability of the variable manager engine service changes.")]
        [SerializeField] private BoolUnityEvent serviceAvailable;
        [Tooltip("Invoked when value of a custom variable with specified name is changed. Invoked even when the value type is not string, in which case the value is converted to string.")]
        [SerializeField] private StringChangedEvent onStringChanged;
        [Tooltip("Invoked when value of a custom variable with specified name is changed and the value is a number (both float and integer).")]
        [SerializeField] private NumericChangedEvent onNumericChanged;
        [Tooltip("Invoked when value of a custom variable with specified name is changed and the value is an integer number.")]
        [SerializeField] private IntegerChangedEvent onIntegerChanged;
        [Tooltip("Invoked when value of a custom variable with specified name is changed and the value is a boolean.")]
        [SerializeField] private BooleanChangedEvent onBooleanChanged;
        [Tooltip("Invoked when value of a custom variable with specified name is changed and the value is a truthy boolean.")]
        [SerializeField] private UnityEvent onBooleanTruthy;
        [Tooltip("Invoked when value of a custom variable with specified name is changed and the value is a falsy boolean.")]
        [SerializeField] private UnityEvent onBooleanFalsy;
        [Tooltip("Invoked when variable with specified name is removed.")]
        [SerializeField] private VariableRemovedEvent onRemoved;

        public void SetStringVariable (string value)
        {
            if (Engine.TryGetService<ICustomVariableManager>(out var manager))
                manager.SetVariableValue(CustomVariableName, new(value));
        }

        public void SetNumberVariable (float value)
        {
            if (Engine.TryGetService<ICustomVariableManager>(out var manager))
                manager.SetVariableValue(CustomVariableName, new(value));
        }

        public void SetBooleanVariable (bool value)
        {
            if (Engine.TryGetService<ICustomVariableManager>(out var manager))
                manager.SetVariableValue(CustomVariableName, new(value));
        }

        public void ResetVariable ()
        {
            if (Engine.TryGetService<ICustomVariableManager>(out var manager))
                manager.ResetVariable(CustomVariableName);
        }

        public void ResetAllVariables ()
        {
            if (Engine.TryGetService<ICustomVariableManager>(out var manager))
                manager.ResetAllVariables();
        }

        protected override void HandleEngineInitialized ()
        {
            if (Engine.TryGetService<ICustomVariableManager>(out var vars))
            {
                serviceAvailable?.Invoke(true);

                vars.OnVariableUpdated -= HandleVariableUpdated;
                vars.OnVariableUpdated += HandleVariableUpdated;

                if (vars.VariableExists(CustomVariableName))
                    NotifyValueChanged(vars.GetVariableValue(CustomVariableName));
            }
            else serviceAvailable?.Invoke(false);
        }

        protected override void HandleEngineDestroyed ()
        {
            serviceAvailable?.Invoke(false);
        }

        protected virtual void HandleVariableUpdated (CustomVariableUpdatedArgs args)
        {
            if (args.Name.EqualsIgnoreCase(CustomVariableName))
                NotifyValueChanged(args.Value);
        }

        protected virtual void NotifyValueChanged (CustomVariableValue? value)
        {
            if (value == null)
            {
                onRemoved?.Invoke();
                return;
            }

            var v = value.Value;
            if (v.Type == CustomVariableValueType.String)
            {
                onStringChanged?.Invoke(v.String);
            }
            else if (v.Type == CustomVariableValueType.Numeric)
            {
                onNumericChanged?.Invoke(v.Number);
                onStringChanged?.Invoke(v.Number.ToString(CultureInfo.InvariantCulture));
                if (IsInteger(v.Number)) onIntegerChanged?.Invoke((int)v.Number);
            }
            else
            {
                onBooleanChanged?.Invoke(v.Boolean);
                if (v.Boolean) onBooleanTruthy?.Invoke();
                else onBooleanFalsy?.Invoke();
                onStringChanged?.Invoke(v.Boolean.ToString(CultureInfo.InvariantCulture));
            }
        }

        protected virtual bool IsInteger (float value)
        {
            const float tolerance = 1e-6f;
            return Mathf.Abs(value % 1) < tolerance && value > int.MinValue && value < int.MaxValue;
        }
    }
}
