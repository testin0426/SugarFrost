using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="ICustomVariableManager"/>.
    /// </summary>
    public static class CustomVariableManagerExtensions
    {
        /// <summary>
        /// Attempts to retrieve value of a variable with the specified name. Variable names are case-insensitive. 
        /// Returns false when no variables of the specified name are found.
        /// </summary>
        public static bool TryGetVariableValue (this ICustomVariableManager manager, string name, out CustomVariableValue value)
        {
            value = default;
            if (manager.VariableExists(name)) value = manager.GetVariableValue(name);
            else return false;
            return true;
        }

        /// <summary>
        /// Attempts to retrieve value of a variable with the specified name and type. Variable names are case-insensitive. 
        /// Returns false when no variables of the specified name are found or when the value is not of the requested type.
        /// </summary>
        public static bool TryGetVariableValue<TValue> (this ICustomVariableManager manager, string name, out TValue value)
        {
            value = default;
            if (!TryGetVariableValue(manager, name, out var customValue)) return false;

            if (typeof(TValue) == typeof(string))
            {
                if (customValue.Type != CustomVariableValueType.String) return false;
                var str = customValue.String;
                value = UnsafeUtility.As<string, TValue>(ref str);
                return true;
            }

            if (typeof(TValue) == typeof(float))
            {
                if (customValue.Type != CustomVariableValueType.Numeric) return false;
                var num = customValue.Number;
                value = UnsafeUtility.As<float, TValue>(ref num);
                return true;
            }

            if (typeof(TValue) == typeof(int))
            {
                if (customValue.Type != CustomVariableValueType.Numeric) return false;
                var num = Mathf.RoundToInt(customValue.Number);
                value = UnsafeUtility.As<int, TValue>(ref num);
                return true;
            }

            if (typeof(TValue) == typeof(bool))
            {
                if (customValue.Type != CustomVariableValueType.Boolean) return false;
                var bl = customValue.Boolean;
                value = UnsafeUtility.As<bool, TValue>(ref bl);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to set value of a variable with the specified name and type. Variable names are case-insensitive. 
        /// When no variables of the specified name are found, will add a new one and assign the value.
        /// In case the name is starting with <see cref="CustomVariablesConfiguration.GlobalPrefix"/>, the variable will have global scope.
        /// Returns false when specified value type is not supported, ie is not a string, number or boolean.
        /// </summary>
        public static bool TrySetVariableValue<TValue> (this ICustomVariableManager manager, string name, TValue value)
        {
            if (typeof(TValue) == typeof(string))
            {
                manager.SetVariableValue(name, new(Convert.ToString(value)));
                return true;
            }

            if (typeof(TValue) == typeof(int) || typeof(TValue) == typeof(float) ||
                typeof(TValue) == typeof(decimal) || typeof(TValue) == typeof(double))
            {
                manager.SetVariableValue(name, new(Convert.ToSingle(value)));
                return true;
            }

            if (typeof(TValue) == typeof(bool))
            {
                manager.SetVariableValue(name, new(Convert.ToBoolean(value)));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether a variable with the specified name is of a global lifetime scope.
        /// </summary>
        public static bool IsGlobal (this ICustomVariableManager manager, string name)
        {
            return manager.GetScope(name) == CustomVariableScope.Global;
        }
    }
}
