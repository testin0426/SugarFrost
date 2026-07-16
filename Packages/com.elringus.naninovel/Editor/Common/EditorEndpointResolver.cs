using JetBrains.Annotations;

namespace Naninovel
{
    /// <summary>
    /// Allows resolving <see cref="Endpoint"/> in the editor.
    /// </summary>
    public static class EditorEndpointResolver
    {
        [CanBeNull] private static EndpointResolver cachedResolver;

        /// <inheritdoc cref="IScriptManager.ResolveEndpoint"/>
        public static Endpoint? Resolve (string syntax, string hostScriptPath)
        {
            var resolver = GetResolverCached();
            return resolver.Resolve(syntax, hostScriptPath, ScriptAssets.GetAllPaths());
        }

        private static EndpointResolver GetResolverCached ()
        {
            if (cachedResolver != null) return cachedResolver;
            return cachedResolver = new(Compiler.Symbols);
        }
    }
}
