using System.Diagnostics;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Draws script asset field which is serialized as asset reference string,
    /// that can be turned into script path at runtime via <see cref="ScriptAssets"/>.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public class ScriptAssetRefAttribute : PropertyAttribute { }
}
