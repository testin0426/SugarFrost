using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Unity asset registered as a Naninovel resource.
    /// </summary>
    [Serializable]
    public class Asset
    {
        /// <summary>
        /// Stable unique (across other Unity assets) identifier of the registered Unity asset.
        /// </summary>
        public string Guid => guid;
        /// <summary>
        /// Resource's type discriminator; prefixed to the local path to build the full path.
        /// </summary>
        public string Prefix => prefix;
        /// <summary>
        /// Local path of the registered Naninovel resource.
        /// </summary>
        public string Path => path;
        /// <summary>
        /// Full unique (across other Naninovel resources) identifier of the registered resource.
        /// </summary>
        public string FullPath => GetFullPathCached();
        /// <summary>
        /// Optional identifier of a group assigned to the resource or null when none.
        /// </summary>
        [CanBeNull] public string Group => string.IsNullOrEmpty(group) ? null : group;

        [SerializeField] private string guid;
        [SerializeField] private string prefix;
        [SerializeField] private string path;
        [SerializeField, CanBeNull] private string group;

        [CanBeNull] private string fullPathCache;

        public Asset (string guid, string prefix, string path, [CanBeNull] string group = null)
        {
            this.guid = guid;
            this.prefix = prefix;
            this.path = path;
            this.group = group;
        }

        private string GetFullPathCached ()
        {
            // Not caching in ctor, as Unity's serde accesses the property before the ctor is invoked.
            if (!string.IsNullOrEmpty(fullPathCache)) return fullPathCache;
            if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(path)) return null;
            return fullPathCache = Resource.BuildFullPath(prefix, path);
        }
    }
}
