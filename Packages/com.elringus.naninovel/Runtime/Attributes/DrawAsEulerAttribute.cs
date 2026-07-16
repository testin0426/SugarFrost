using System.Diagnostics;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// When applied to a <see cref="Quaternion"/> field, draws it as <see cref="Vector3"/> euler.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public class DrawAsEulerAttribute : PropertyAttribute { }
}
