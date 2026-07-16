using UnityEditor;

namespace Naninovel
{
    /// <summary>
    /// Initializes Naninovel editor services on application start and domain reload.
    /// </summary>
    [InitializeOnLoad]
    public static class InitializeOnLoad
    {
        static InitializeOnLoad ()
        {
            // Remember that the Unity's asset serialization is not finished here
            // and any serialized data (eg, fields in a ScriptableAsset) may be null
            // at this time, EVEN WHEN A DEFAULT VALUE IS ASSIGNED.
            // https://docs.unity3d.com/ScriptReference/InitializeOnLoadAttribute

            // Delay the initializers, so that Naninovel code is never
            // invoked on a path where Unity serialization is not finished.
            EditorApplication.delayCall += Initialize;

            // Double-check the execution paths below are not touching any code that
            // uses serialized assets. Remember, that it's not safe to even use the
            // Engine type here, as it'll chain into accessing configuration assets due
            // to static properties initialization. Basically, only assign lazy callbacks
            // here, which may be requested before the delayed Initialize() is invoked,
            // but after the Unity serialization is finished (eg, OnEnable of the script editors).
            TypeCache.EditorResolver = TypesResolver.Resolve;
            TransientRootResolver.Resolver = () => PackagePath.TransientDataPath;
        }

        private static void Initialize ()
        {
            AssetsAutoSaver.Initialize();
            StandardAssets.Initialize();
            ResourceHeaderEditor.Initialize();
            ScriptFileWatcher.Initialize();
            HotReloadService.Initialize();
            BuildProcessor.Initialize();
            PackageInstaller.Initialize();
            BridgingService.Restart();
            AboutWindow.FirstTimeSetup();

            #if ADDRESSABLES_AVAILABLE // ensure settings when addressable is installed; throws on play otherwise
            UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.GetSettings(true);
            #endif
        }
    }
}
