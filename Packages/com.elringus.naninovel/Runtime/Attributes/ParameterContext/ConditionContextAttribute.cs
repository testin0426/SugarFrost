using System;
using System.Diagnostics;
using Naninovel.Metadata;

namespace Naninovel
{
    /// <summary>
    /// Can be applied to a string command parameter to indicate that the value should be treated as expression,
    /// even when it's not wrapped in curly braces. Additionally, indicates that the parameter value should be
    /// treated as a boolean expression, which evaluation result indicates whether the command should execute.
    /// Used by bridging service to provide the context for external tools (IDE extension, web editor, etc).
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ConditionContextAttribute : ParameterContextAttribute
    {
        /// <param name="inverted">Whether the condition's result is inverted, ie the command executes when the result is negative</param>
        public ConditionContextAttribute (bool inverted = false, int index = -1, string paramId = null)
            : base(ValueContextType.Expression, inverted ? Constants.InvertedCondition : Constants.Condition, index, paramId) { }
    }
}
