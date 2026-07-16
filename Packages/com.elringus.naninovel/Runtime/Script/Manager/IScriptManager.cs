namespace Naninovel
{
    /// <summary>
    /// Manages scenario scripts.
    /// </summary>
    public interface IScriptManager : IEngineService<ScriptsConfiguration>
    {
        /// <summary>
        /// Manages scenario script resources.
        /// </summary>
        IResourceLoader<Script> ScriptLoader { get; }
        /// <summary>
        /// Manages external scenario script resources (community modding feature),
        /// when <see cref="ScriptsConfiguration.EnableCommunityModding"/> is enabled.
        /// </summary>
        IResourceLoader<Script> ExternalScriptLoader { get; }
        /// <summary>
        /// Total number of commands existing in all the available scenario scripts.
        /// </summary>
        /// <remarks>
        /// Updated on build and when invoking 'Naninovel/Show Project Stats' via editor menu.
        /// </remarks>
        int TotalCommandsCount { get; }

        /// <summary>
        /// Registers a new transient script with the specified local resource path and source text.
        /// Once registered, the script can be loaded with the <see cref="ScriptLoader"/> until
        /// unregistered with <see cref="RemoveTransientScript"/>.
        /// </summary>
        /// <remarks>
        /// Use this method when generating scripts at runtime to allow them to function
        /// the same way as regular <see cref="Script"/> assets. Registrations persist across
        /// save-load cycles; be sure to unregister them when no longer needed, as the source
        /// text is serialized with game save files.
        /// </remarks>
        /// <param name="path">A unique local resource path that identifies the script.</param>
        /// <param name="text">The source scenario text to compile into a <see cref="Script"/>.</param>
        /// <exception cref="Error">Transient script with specified path is already registered.</exception>
        void AddTransientScript (string path, string text);
        /// <summary>
        /// Unregisters a transient script with the specified local resource path that was previously
        /// registered using <see cref="AddTransientScript"/>.
        /// </summary>
        /// <exception cref="Error">Transient script with specified path is not registered.</exception>
        void RemoveTransientScript (string path);
        /// <summary>
        /// Whether a transient script with the specified local resource path is currently registered.
        /// </summary>
        bool HasTransientScript (string path);
        /// <summary>
        /// Attempts to resolve specified endpoint syntax; returns null when fails.
        /// </summary>
        /// <param name="syntax">The endpoint syntax to resolve.</param>
        /// <param name="host">The local resource path of the script that hosts the endpoint.</param>
        /// <returns>Resolved endpoint or null when fails.</returns>
        Endpoint? ResolveEndpoint (string syntax, string host);
        /// <inheritdoc cref="EndpointSerializer.Serialize"/>
        string SerializeEndpoint (Endpoint endpoint, string host, EndpointSyntaxTraits traits = default);
    }
}
