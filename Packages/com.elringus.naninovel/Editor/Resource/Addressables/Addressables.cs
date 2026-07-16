using System.Collections.Generic;
using JetBrains.Annotations;

namespace Naninovel
{
    /// <summary>
    /// Naninovel editor APIs involving the Unity's Addressable Asset System.
    /// </summary>
    public static class Addressables
    {
        /// <summary>
        /// Whether the Addressable Asset System is available in the project.
        /// When not available, all the operations are noop.
        /// </summary>
        public static bool Available => impl is not NullAddressables;

        private static IAddressables impl => implCache ?? Initialize();
        [CanBeNull] private static IAddressables implCache;

        /// <summary>
        /// Adds an asset with the specified GUID as a Naninovel resource with the specified prefix and local path.
        /// </summary>
        public static void Register (string guid, string prefix, string path) => impl.Register(guid, Resource.BuildFullPath(prefix, path));
        /// <inheritdoc cref="IAddressables.Register"/>
        public static void Register (string guid, string fullPath) => impl.Register(guid, fullPath);
        /// <inheritdoc cref="IAddressables.UnregisterResource"/>
        public static void UnregisterResource (string fullPath) => impl.UnregisterResource(fullPath);
        /// <inheritdoc cref="IAddressables.UnregisterAsset"/>
        public static void UnregisterAsset (string guid) => impl.UnregisterAsset(guid);
        /// <inheritdoc cref="IAddressables.CollectResources"/>
        public static void CollectResources (string guid, ICollection<string> fullPaths) => impl.CollectResources(guid, fullPaths);
        /// <inheritdoc cref="IAddressables.CollectAssets"/>
        public static void CollectAssets (ICollection<string> guids) => impl.CollectAssets(guids);
        /// <inheritdoc cref="IAddressables.Label"/>
        public static void Label () => impl.Label();
        /// <inheritdoc cref="IAddressables.Build"/>
        public static void Build ([CanBeNull] IEnumerable<string> excludeGuids = null) => impl.Build(excludeGuids);

        private static IAddressables Initialize ()
        {
            #if ADDRESSABLES_AVAILABLE
            return implCache = new UnityAddressables();
            #else
            return implCache = new NullAddressables();
            #endif
        }
    }
}
