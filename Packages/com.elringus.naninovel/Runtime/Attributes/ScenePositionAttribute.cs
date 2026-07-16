using System.Diagnostics;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Adds a selection box to the decorated <see cref="Vector3"/> field, allowing to select between
    /// world-space and Naninovel scene-space (relative to <see cref="CameraConfiguration.ReferenceResolution"/>) position modes.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public class ScenePositionAttribute : PropertyAttribute { }
}
