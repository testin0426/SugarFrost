using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Naninovel
{
    /// <summary>
    /// Manages <see cref="Resource"/> objects of a specific type over multiple <see cref="IResourceProvider"/>.
    /// </summary>
    public interface IResourceLoader
    {
        /// <summary>
        /// Resolves full path by prepending path prefix of this loader to the specified local path.
        /// </summary>
        string GetFullPath (string localPath);
        /// <summary>
        /// Given specified full path starts with path prefix managed by this loader,
        /// returns local (to the loader) path of the resource by removing the prefix, null otherwise.
        /// </summary>
        [CanBeNull] string GetLocalPath (string fullPath);
        /// <summary>
        /// Checks whether a resource with the specified local path is available (is or can be loaded).
        /// </summary>
        bool Exists (string path);
        /// <summary>
        /// Collects local paths of all the available resources optionally filtered by the specified prefix.
        /// </summary>
        void CollectPaths (ICollection<string> paths, [CanBeNull] string prefix = null);
        /// <summary>
        /// Returns a resource with the specified local path in case it's loaded by this loader, null otherwise.
        /// </summary>
        [CanBeNull] Resource GetLoaded (string path);
        /// <summary>
        /// Collects all the resources currently loaded by this loader to the specified collection.
        /// </summary>
        void CollectLoaded (ICollection<Resource> resources);
        /// <summary>
        /// Loads a resource at the specified local path.
        /// When <see cref="holder"/> is specified, will as well <see cref="Hold"/> the resource.
        /// </summary>
        Awaitable<Resource> Load (string path, [CanBeNull] object holder = null);
        /// <summary>
        /// Loads all available resources optionally filtered by the specified local path prefix.
        /// When <see cref="holder"/> is specified, will as well <see cref="Hold"/> the resources.
        /// </summary>
        Awaitable<IReadOnlyCollection<Resource>> LoadAll ([CanBeNull] string prefix = null, [CanBeNull] object holder = null);
        /// <summary>
        /// Given resource with specified path is loaded by this loader (throws otherwise),
        /// registers specified object as holder of the resource.
        /// The resource won't be unloaded while it's held by at least one object.
        /// </summary>
        void Hold (string path, object holder);
        /// <summary>
        /// Given resource with the specified local path is loaded by this loader,
        /// removes specified object from holder list of the resource.
        /// Will (optionally) unload the resource in case no other objects are holding it.
        /// </summary>
        void Release (string path, object holder, bool unload = true);
        /// <summary>
        /// Removes specified holder object from holder list of all the resources loaded by this loader.
        /// Will (optionally) unload the affected resources in case no other objects are holding them.
        /// </summary>
        void ReleaseAll (object holder, bool unload = true);
        /// <summary>
        /// Given resource with specified local path is loaded by this loader,
        /// checks whether specified holder object is in holder list of the resource.
        /// </summary>
        bool IsHeldBy (string path, object holder);
        /// <summary>
        /// Returns number of unique holders currently holding any resources in the loader.
        /// When path is specified, will only count holders associated with the local resource path.
        /// </summary>
        int CountHolders ([CanBeNull] string path = null);
        /// <summary>
        /// Collects unique holders currently holding any resources in the loader to the specified set.
        /// When prefix is specified, will only collect holders associated with the local resource path.
        /// </summary>
        void CollectHolders (ISet<object> holders, [CanBeNull] string path = null);
        /// <summary>
        /// Unloads a loaded resource with the specified local path and loads it back,
        /// while keeping the existing holders. Use to refresh resources modified at runtime.
        /// </summary>
        Awaitable<Resource> Reload (string path);
        /// <summary>
        /// Updates a loaded resource at the specified local path with the specified object, keeping the existing holders.
        /// Use to update resources at runtime without changing the underlying resource served by the provider.
        /// Note that the resource will revert back to one served by the associated provider on reload.
        /// </summary>
        void Update (string path, Object obj);
    }

    /// <inheritdoc/>
    public interface IResourceLoader<TResource> : IResourceLoader
        where TResource : Object
    {
        /// <summary>
        /// Occurs when a resource managed by this loader is loaded.
        /// </summary>
        public event Action<Resource<TResource>> OnLoaded;
        /// <summary>
        /// When invoked when a resource managed by this loader is unloaded.
        /// </summary>
        public event Action<Resource<TResource>> OnUnloaded;

        /// <inheritdoc cref="IResourceLoader.GetLoaded"/>
        [CanBeNull] new Resource<TResource> GetLoaded (string path);
        /// <inheritdoc cref="IResourceLoader.CollectLoaded"/>
        void CollectLoaded (ICollection<Resource<TResource>> resources);
        /// <inheritdoc cref="IResourceLoader.Load"/>
        new Awaitable<Resource<TResource>> Load (string path, [CanBeNull] object holder = null);
        /// <inheritdoc cref="IResourceLoader.LoadAll"/>
        new Awaitable<IReadOnlyCollection<Resource<TResource>>> LoadAll (string prefix = null, [CanBeNull] object holder = null);
        /// <inheritdoc cref="IResourceLoader.Reload"/>
        new Awaitable<Resource<TResource>> Reload (string path);
        /// <inheritdoc cref="IResourceLoader.Reload"/>
        void Update (string path, TResource obj);
    }
}
