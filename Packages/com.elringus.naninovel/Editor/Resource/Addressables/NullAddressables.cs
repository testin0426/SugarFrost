using System.Collections.Generic;

namespace Naninovel
{
    /// <summary>
    /// A noop implementation of the <see cref="IAddressables"/>
    /// used when the Addressables package is not available in the project.
    /// </summary>
    public class NullAddressables : IAddressables
    {
        public void Register (string guid, string fullPath) { }
        public void UnregisterResource (string fullPath) { }
        public void UnregisterAsset (string guid) { }
        public void CollectResources (string guid, ICollection<string> fullPaths) { }
        public void CollectAssets (ICollection<string> guids) { }
        public void Label () { }
        public void Build (IEnumerable<string> excludeGuids = null) { }
    }
}
