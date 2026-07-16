using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    [EditInProjectSettings]
    public class CustomVariablesConfiguration : Configuration
    {
        [Tooltip("When set to 'Global' all variables will be treated as globals, even when their name doesn't start with the 'g_' prefix. Global variables are not reset when starting a new game and are auto-saved on change. Useful for the dialogue mode when the engine is reset constantly and game state is handled externally.")]
        public CustomVariableScope DefaultScope = CustomVariableScope.Local;
        [Tooltip("The list of variables to initialize by default. Global variables (names starting with `g_`) are initialized on first application start, and others on each state reset.")]
        public List<CustomVariablePredefine> PredefinedVariables = new();

        /// <summary>
        /// Checks whether specified custom variable name has <see cref="Compiler.GlobalVariablePrefix"/> prefix.
        /// </summary>
        public static bool HasGlobalPrefix (string name)
        {
            return name.StartsWith(Compiler.GlobalVariablePrefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
