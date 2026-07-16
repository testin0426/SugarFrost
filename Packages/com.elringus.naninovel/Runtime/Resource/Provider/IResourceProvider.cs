using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Loads <see cref="Resource"/> objects from a specific provision source, such as the local file system,
    /// virtual in-memory storage, Unity's Addressables or 'Resources' systems.
    /// </summary>
    public interface IResourceProvider
    {
        /// <summary>
        /// Loads a resource at the specified full path; throws in case the path doesn't <see cref="Exists"/>.
        /// </summary>
        Awaitable<Resource> Load (string fullPath);
        /// <summary>
        /// Loads all available resources optionally filtered by the specified path prefix.
        /// </summary>
        Awaitable<IReadOnlyCollection<Resource>> LoadAll ([CanBeNull] string prefix = null);
        /// <summary>
        /// Collects full paths of all the available resources optionally filtered by the specified path prefix.
        /// </summary>
        void CollectPaths (ICollection<string> fullPaths, [CanBeNull] string prefix = null);
        /// <summary>
        /// Whether a resource with the specified full path is available (is or can be loaded).
        /// </summary>
        bool Exists (string fullPath);
        /// <summary>
        /// Unloads a resource at the specified full path; noop in case not loaded.
        /// </summary>
        void Unload (string fullPath);
        /// <summary>
        /// Unloads all resources currently loaded by the provider.
        /// </summary>
        void UnloadAll ();
        /// <summary>
        /// Returns a loaded resource with the specified full path or null in case not loaded.
        /// </summary>
        [CanBeNull] Resource GetLoaded (string fullPath);
        /// <summary>
        /// Collects all resources currently loaded by this provider to the specified collection.
        /// </summary>
        void CollectLoaded (ICollection<Resource> resources);
    }
}
