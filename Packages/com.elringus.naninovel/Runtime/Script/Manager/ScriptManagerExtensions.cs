using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="IScriptManager"/>.
    /// </summary>
    public static class ScriptManagerExtensions
    {
        /// <summary>
        /// Attempts to resolve specified endpoint syntax; throws when fails.
        /// </summary>
        /// <param name="syntax">The endpoint syntax to resolve.</param>
        /// <param name="hostScriptPath">The local resource path of the script that hosts the endpoint.</param>
        /// <returns>Resolved endpoint.</returns>
        public static Endpoint ResolveEndpointOrErr (this IScriptManager self, string syntax, string hostScriptPath)
        {
            return self.ResolveEndpoint(syntax, hostScriptPath) ??
                   throw new Error($"Failed to resolve endpoint syntax: '{syntax}'. Host script: '{hostScriptPath}'.");
        }

        /// <summary>
        /// Attempts to resolve specified endpoint syntax; returns false when fails.
        /// </summary>
        /// <param name="syntax">The endpoint syntax to resolve.</param>
        /// <param name="hostScriptPath">The local resource path of the script that hosts the endpoint.</param>
        /// <param name="endpoint">Resolved endpoint.</param>
        /// <returns>Whether endpoint was resolved.</returns>
        public static bool TryResolveEndpoint (this IScriptManager self, string syntax, string hostScriptPath,
            out Endpoint endpoint)
        {
            endpoint = default;
            var result = self.ResolveEndpoint(syntax, hostScriptPath);
            if (!result.HasValue) return false;
            endpoint = result.Value;
            return true;
        }

        /// <summary>
        /// Given both 'from' and 'to' scripts are loaded by the <see cref="IScriptManager.ScriptLoader"/>,
        /// holds the 'to' script while releasing the 'from' script, but only in case they are not the same
        /// script and returns the 'to' script.
        /// </summary>
        /// <remarks>
        /// This is intended to be used as a shortcut when re-assigning script resources, encapsulating the
        /// invocations of <see cref="IResourceLoader.Hold"/> and <see cref="IResourceLoader.Release"/>.
        /// </remarks>
        [CanBeNull] [return: NotNullIfNotNull("to")]
        public static Script Juggle (this IScriptManager self, [CanBeNull] Script from, [CanBeNull] Script to,
            object holder)
        {
            var toPath = to ? to.Path : null;
            var fromPath = from ? from.Path : null;
            if (toPath != null) self.ScriptLoader.Hold(toPath, holder);
            if (fromPath != null && fromPath != toPath) self.ScriptLoader.Release(fromPath, holder);
            return to;
        }
    }
}
