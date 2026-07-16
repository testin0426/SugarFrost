using System;
using System.Diagnostics;

namespace Naninovel
{
    /// <summary>
    /// When applied to <see cref="Command.INestedHost"/>, notifies external tools (IDE, web editor)
    /// that the host always expects nested commands and error should be shown when it's not the case.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RequireNestedAttribute : Attribute { }
}
