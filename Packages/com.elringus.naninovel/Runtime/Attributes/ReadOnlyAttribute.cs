using System.Diagnostics;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// The field won't be editable in the editor.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public class ReadOnlyAttribute : PropertyAttribute { }
}
