using System.Collections.Generic;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage <see cref="IResourceProvider"/> objects.
    /// </summary>
    public interface IResourceProviderManager : IEngineService<ResourceProviderConfiguration>, IHoldersTracker
    {
        /// <summary>
        /// Checks whether a resource provider of specified type (assembly-qualified name) is available.
        /// </summary>
        bool IsProviderInitialized (string type);
        /// <summary>
        /// Returns a resource provider of the requested type (assembly-qualified name).
        /// </summary>
        IResourceProvider GetProvider (string type);
        /// <summary>
        /// Collects resource providers of the requested types (assembly-qualified names), in the requested order.
        /// </summary>
        void CollectProviders (IList<IResourceProvider> providers, IReadOnlyList<string> types);

        /// <summary>
        /// Creates a list of resource providers of the requested types (assembly-qualified names), in the requested order.
        /// </summary>
        public List<IResourceProvider> CollectProviders (IReadOnlyList<string> types)
        {
            var list = new List<IResourceProvider>();
            CollectProviders(list, types);
            return list;
        }
    }
}
