using UnityEngine;

namespace Naninovel
{
    [EditInProjectSettings]
    public class ResourceProviderConfiguration : Configuration
    {
        /// <summary>
        /// Assembly-qualified type name of the built-in project resource provider.
        /// </summary>
        public const string ProjectTypeName = "Naninovel.ProjectResourceProvider, Elringus.Naninovel.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        /// <summary>
        /// Assembly-qualified type name of the built-in local resource provider.
        /// </summary>
        public const string LocalTypeName = "Naninovel.LocalResourceProvider, Elringus.Naninovel.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        /// <summary>
        /// Assembly-qualified type name of the built-in virtual resource provider.
        /// </summary>
        public const string VirtualTypeName = "Naninovel.VirtualResourceProvider, Elringus.Naninovel.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        /// <summary>
        /// Assembly-qualified type name of the built-in addressable resource provider.
        /// </summary>
        public const string AddressableTypeName = "Naninovel.AddressableResourceProvider, Elringus.Naninovel.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        /// <summary>
        /// Assigned from the editor assembly when the application is running under Unity editor.
        /// </summary>
        public static IResourceProvider EditorProvider = default;

        /// <summary>
        /// Used by the <see cref="IResourceProviderManager"/> before all the other providers.
        /// </summary>
        public virtual IResourceProvider MasterProvider => EditorProvider;

        [Header("Resources Management")]
        [Tooltip("Dictates when the resources are loaded and unloaded during script execution:" +
                 "\n\n • Conservative — The default mode with balanced memory utilization. All the resources required for script execution are preloaded when starting playback and unloaded when the script has finished playing. Scripts referenced in [@gosub] commands are preloaded as well. Additional scripts can be preloaded by using the `hold` parameter of the [@goto] command." +
                 "\n\n • Optimistic — All the resources required by the played script, as well as all resources of the scripts specified in [@goto] and [@gosub] commands, are preloaded and not unloaded unless the `release` parameter is specified in the [@goto] command. This minimizes loading screens and allows smooth rollback, but requires manually specifying when the resources should be unloaded, increasing the risk of out-of-memory exceptions." +
                 "\n\n • Lazy — No resources are preloaded for executed scripts when starting playback, and no loading screens are automatically shown. Instead, only the resources required for the next few commands are loaded \"on the fly\" while the script is playing, and resources used by executed commands are immediately released. This policy requires no scenario planning or manual control and consumes the least memory, but it may result in stutters during gameplay due to resources being loaded in the background—especially when fast-forwarding (skip mode) or performing rollback.")]
        public ResourcePolicy ResourcePolicy = ResourcePolicy.Conservative;
        [Tooltip("When lazy resource policy is enabled, controls the size of the preload buffer, that is the maximum number of script commands to preload.")]
        public int LazyBuffer = 25;
        [Tooltip("When lazy resource policy is enabled, controls the background thread priority where the resources are loaded. Decrease to minimize stutters at the cost of longer load times.")]
        public ThreadPriority LazyPriority = ThreadPriority.BelowNormal;
        [Tooltip("Whether to automatically remove unused actors (characters, backgrounds, text printers and choice handlers) when unloading script resources. Note that even when enabled, it's still possible to remove actors manually with `@remove` commands at any time.")]
        public bool RemoveActors = true;

        [Header("Build Processing")]
        [Tooltip("Whether to register a custom build player handle to process the assets assigned as Naninovel resources.\n\nWarning: In order for this setting to take effect, it's required to restart the Unity editor.")]
        public bool EnableBuildProcessing = true;
        [Tooltip("When the Addressable Asset System is installed, enabling this property will optimize asset processing step improving the build time.")]
        public bool UseAddressables = true;
        [Tooltip("Whether to automatically build the addressable asset bundles when building the player. Has no effect when `Use Addressables` is disabled.")]
        public bool AutoBuildBundles = true;
        [Tooltip("Whether to label all the Naninovel addressable assets by the scenario script path they're used in. When `Bundle Mode` is set to `Pack Together By Label` in the addressable group settings, will result in a more efficient bundle packing.\n\nNote that script labels will be assigned to all the assets with 'Naninovel' label, which includes assets manually exposed to the addressable resource provider (w/o using the resource editor menus).")]
        public bool LabelByScripts = true;

        [Header("Local Provider")]
        [Tooltip("Path root to use for the local resource provider. Can be an absolute path to the folder where the resources are located, or a relative path with one of the available origins:" +
                 "\n • %DATA% — Game data folder on the target device (UnityEngine.Application.dataPath)." +
                 "\n • %PDATA% — Persistent data directory on the target device (UnityEngine.Application.persistentDataPath)." +
                 "\n • %STREAM% — `StreamingAssets` folder (UnityEngine.Application.streamingAssetsPath)." +
                 "\n • %SPECIAL{F}% — An OS special folder (where F is value from System.Environment.SpecialFolder).")]
        public string LocalRootPath = "%DATA%/Resources";
        [Tooltip("When streaming videos under WebGL (movies, video backgrounds), specify the extension of the video files.")]
        public string VideoStreamExtension = ".mp4";
        [Tooltip("Whether to watch and hot reload modified scenario scripts stored under the local provider directory.")]
        public bool ReloadScripts = true;
    }
}
