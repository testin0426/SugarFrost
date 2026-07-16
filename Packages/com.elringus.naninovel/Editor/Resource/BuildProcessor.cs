using UnityEditor;

namespace Naninovel
{
    /// <remarks>
    /// On build pre-process:
    ///   - Update projects stats, such as total command count required to report player read progress.
    ///   - When addressable provider is used: assign an addressable address and label to the assets registered in <see cref="EditorResources"/>;
    ///   - Otherwise: copy the <see cref="EditorResources"/> assets to a temp 'Resources' folder (except the assets already stored in 'Resources' folders).
    /// On build post-process or build fail: 
    ///   - restore any affected assets and delete the created temporary 'Resources' folder.
    /// </remarks>
    public static class BuildProcessor
    {
        /// <summary>
        /// Whether the build processor is currently running.
        /// </summary>
        public static bool Building { get; private set; }

        private static ResourcesBuilder builder;

        public static void PreprocessBuild (BuildPlayerOptions options)
        {
            TypesResolver.WriteCacheAsset(TypesResolver.Resolve());
            var cfg = Configuration.GetOrDefault<ResourceProviderConfiguration>();
            CheckAddressables(cfg);
            builder = new ResourcesBuilder(cfg);
            _ = ProjectStatsResolver.Resolve();
            builder.Build(options);
        }

        public static void PostprocessBuild () => builder?.Cleanup();

        public static void Initialize ()
        {
            var config = Configuration.GetOrDefault<ResourceProviderConfiguration>();
            if (config.EnableBuildProcessing)
                BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayer);
        }

        [MenuItem(MenuPath.Root + "/Build Resources", priority = 4)]
        private static void BuildResourcesMenu ()
        {
            try
            {
                Building = true;
                PreprocessBuild(new());
            }
            finally
            {
                PostprocessBuild();
                Building = false;
            }
        }

        private static void BuildPlayer (BuildPlayerOptions options)
        {
            try
            {
                Building = true;
                PreprocessBuild(options);
                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
            }
            finally
            {
                PostprocessBuild();
                Building = false;
            }
        }

        private static void CheckAddressables (ResourceProviderConfiguration cfg)
        {
            if (!Addressables.Available || !cfg.UseAddressables)
                Engine.Log("Consider installing Unity's Addressable Asset System and enabling 'Use Addressables' in Naninovel's 'Resource Provider' configuration menu. When the system is not available, all the assets assigned as Naninovel resources are copied and re-imported on build, which significantly increases build time. Check https://naninovel.com/guide/resource-providers#addressable for more info.");
        }
    }
}
