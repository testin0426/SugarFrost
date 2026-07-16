using System.Collections.Generic;
using JetBrains.Annotations;

namespace Naninovel
{
    /// <summary>
    /// Abstraction over the Unity's Addressable Asset System APIs,
    /// which may or may not be available in the project.
    /// </summary>
    public interface IAddressables
    {
        /// <summary>
        /// Adds an asset with the specified GUID as a Naninovel resource with the specified full path.
        /// </summary>
        void Register (string guid, string fullPath);
        /// <summary>
        /// Removes previously registered addressable resource with the specified full path.
        /// </summary>
        void UnregisterResource (string fullPath);
        /// <summary>
        /// Removes all previously registered addressable resources associated with the specified asset GUID.
        /// </summary>
        void UnregisterAsset (string guid);
        /// <summary>
        /// In case an asset with the specified GUID is registered as an addressable Naninovel resource,
        /// collects the full resource paths by which the asset is registered to the specified collection.
        /// </summary>
        void CollectResources (string guid, ICollection<string> fullPaths);
        /// <summary>
        /// Collects the GUIDs of all the registered assets to the specified collection.
        /// </summary>
        void CollectAssets (ICollection<string> guids);
        /// <summary>
        /// Labels the assets of the registered Naninovel addressable resources based
        /// on the scenario scrips the resources are used in.
        /// </summary>
        void Label ();
        /// <summary>
        /// Starts the Addressable Asset System content build procedure.
        /// Will exclude the assets with the specified GUIDs from the build.
        /// </summary>
        void Build ([CanBeNull] IEnumerable<string> excludeGuids = null);
    }
}
