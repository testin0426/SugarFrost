using System;
using System.Diagnostics;
using Naninovel.Metadata;

namespace Naninovel
{
    /// <summary>
    /// Can be applied to a command or expression function parameter to associate actor records with a specific path prefix.
    /// Used by the bridging service to provide the context for external tools (IDE extension, web editor, etc).
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = true)]
    public sealed class ActorContextAttribute : ParameterContextAttribute
    {
        /// <param name="prefix">Actor path prefix to associate with the parameter. When *, will associate with all the available actors.</param>
        public ActorContextAttribute (string prefix = "*", int index = -1, string paramId = null)
            : base(ValueContextType.Actor, prefix, index, paramId) { }
    }
}
