namespace Naninovel
{
    /// <summary>
    /// Manages <see cref="IHotReload"/> in the editor.
    /// </summary>
    public static class HotReloadService
    {
        private static ScriptPathResolver path;
        private static IScriptPlayer player;

        /// <summary>
        /// Starts listening for file changes to perform hot reload when scenario changes.
        /// </summary>
        public static void Initialize ()
        {
            Engine.OnInitializationFinished -= HandleEngineInitialized;
            Engine.OnInitializationFinished += HandleEngineInitialized;
        }

        /// <summary>
        /// Stops listening for file changes and de-inits the hot reload service.
        /// </summary>
        public static void Deinitialize ()
        {
            ScriptFileWatcher.OnModified -= HandleScriptModified;
            Engine.OnInitializationFinished -= HandleEngineInitialized;
        }

        private static void HandleEngineInitialized ()
        {
            if (Engine.Behaviour is not RuntimeBehaviour) return;
            path = new() { RootUri = PackagePath.ScenarioRoot };
            player = Engine.GetServiceOrErr<IScriptPlayer>();
            player.ReloadEnabled = Configuration.GetOrDefault<ScriptsConfiguration>().HotReloadScripts;
            ScriptFileWatcher.OnModified -= HandleScriptModified;
            ScriptFileWatcher.OnModified += HandleScriptModified;
        }

        private static void HandleScriptModified (string assetPath)
        {
            if (!Engine.Initialized || Engine.Behaviour is not RuntimeBehaviour ||
                !player.ReloadEnabled || !player.MainTrack.PlayedScript ||
                player.MainTrack.PlayedScript.Path != path.Resolve(assetPath)) return;
            player.Reload().Forget();
        }
    }
}
