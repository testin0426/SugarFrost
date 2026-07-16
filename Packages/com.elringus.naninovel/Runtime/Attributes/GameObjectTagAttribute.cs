using System.Diagnostics;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Draws Unity's game object tag dropdown.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public class GameObjectTagAttribute : PropertyAttribute { }
}
