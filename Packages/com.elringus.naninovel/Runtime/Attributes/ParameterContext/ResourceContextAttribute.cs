using System;
using System.Diagnostics;
using Naninovel.Metadata;

namespace Naninovel
{
    /// <summary>
    /// Can be applied to a command or expression function parameter to associate resources with a specific path prefix.
    /// Used by the bridging service to provide the context for external tools (IDE extension, web editor, etc).
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = true)]
    public sealed class ResourceContextAttribute : ParameterContextAttribute
    {
        /// <param name="prefix">Resource path prefix to associate with the parameter.</param>
        public ResourceContextAttribute (string prefix, int index = -1, string paramId = null)
            : base(ValueContextType.Resource, prefix, index, paramId) { }
    }
}
