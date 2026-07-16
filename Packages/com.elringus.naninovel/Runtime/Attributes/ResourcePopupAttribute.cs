using System.Diagnostics;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Draws a dropdown selection list of the resource paths, which are added via editor managers (aka 'EditorResources').
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public class ResourcePopupAttribute : PropertyAttribute
    {
        public const string EmptyValue = "None (disabled)";

        public readonly string Prefix;

        /// <param name="prefix">Type discriminator of the resources.</param>
        /// <param name="emptyOption">When specified, will include an additional option with the specified name and <see cref="string.Empty"/> value to the list.</param>
        public ResourcePopupAttribute (string prefix)
        {
            Prefix = prefix;
        }
    }

    /// <summary>
    /// Draws a dropdown selection list of the actors, which are added via editor managers (aka `EditorResources`).
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public class ActorPopupAttribute : ResourcePopupAttribute
    {
        /// <param name="prefix">Type discriminator of the actors.</param>
        public ActorPopupAttribute (string prefix)
            : base(prefix) { }
    }
}
