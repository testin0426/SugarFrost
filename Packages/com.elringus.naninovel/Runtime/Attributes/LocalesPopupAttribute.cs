using System.Diagnostics;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Draws a selectable dropdown list (popup) of available locales (language tags) based on <see cref="Languages"/>.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public class LocalesPopupAttribute : PropertyAttribute
    {
        public readonly bool IncludeEmpty;

        /// <param name="includeEmpty">Whether to include an empty ('None') option to the list.</param>
        public LocalesPopupAttribute (bool includeEmpty)
        {
            IncludeEmpty = includeEmpty;
        }
    }
}
