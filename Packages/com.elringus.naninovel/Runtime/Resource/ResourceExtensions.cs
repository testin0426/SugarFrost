using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="Resource"/> and associated types.
    /// </summary>
    public static class ResourceExtensions
    {
        /// <summary>
        /// Rents a pooled list and collects full paths of all the available resources
        /// optionally filtered by the specified path prefix.
        /// </summary>
        public static IDisposable RentPaths (this IResourceProvider provider, out List<string> fullPaths,
            [CanBeNull] string prefix = null)
        {
            var rent = ListPool<string>.Rent(out fullPaths);
            provider.CollectPaths(fullPaths, prefix);
            return rent;
        }

        /// <summary>
        /// Rents a pooled list and collects local paths of all the available resources
        /// optionally filtered by the specified prefix.
        /// </summary>
        public static IDisposable RentPaths (this IResourceLoader loader, out List<string> fullPaths,
            [CanBeNull] string prefix = null)
        {
            var rent = ListPool<string>.Rent(out fullPaths);
            loader.CollectPaths(fullPaths, prefix);
            return rent;
        }

        /// <summary>
        /// Whether a resource with the specified full path is currently loaded and valid.
        /// </summary>
        public static bool IsLoaded (this IResourceProvider provider, string fullPath)
        {
            return provider.GetLoaded(fullPath) is { Valid: true };
        }

        /// <inheritdoc cref="IResourceProvider.Load"/>
        public static async Awaitable<Resource<TResource>> Load<TResource> (this IResourceProvider provider,
            string fullPath) where TResource : Object
        {
            var resource = await provider.Load(fullPath);
            return Resource<TResource>.From(resource);
        }

        /// <summary>
        /// Rents a pooled hash set and collects unique holders currently holding any resources in the loader.
        /// When the path is specified, collects only the holders associated with the local resource path.
        /// </summary>
        public static IDisposable RentHolders (this IResourceLoader loader, out HashSet<object> holders,
            [CanBeNull] string path = null)
        {
            var rent = SetPool<object>.Rent(out holders);
            loader.CollectHolders(holders, path);
            return rent;
        }

        /// <summary>
        /// Given the specified resource is loaded by the loader, hold it.
        /// </summary>
        public static void Hold (this IResourceLoader loader, Resource resource, object holder)
        {
            var localPath = loader.GetLocalPath(resource.FullPath);
            if (!string.IsNullOrEmpty(localPath))
                loader.Hold(localPath, holder);
        }

        /// <summary>
        /// Given specified resource is loaded by the loader, release it.
        /// </summary>
        public static void Release (this IResourceLoader loader, Resource resource, object holder, bool unload = true)
        {
            var localPath = loader.GetLocalPath(resource.FullPath);
            if (!string.IsNullOrEmpty(localPath))
                loader.Release(localPath, holder, unload);
        }

        /// <summary>
        /// Attempts to retrieve an object of a resource with the specified local path; returns false when
        /// it's not loaded by this loader or is not a valid (destroyed) Unity object.
        /// </summary>
        public static bool TryGetLoaded (this IResourceLoader loader, string path, out Object obj)
        {
            return obj = loader.GetLoaded(path);
        }

        /// <inheritdoc cref="TryGetLoaded"/>
        public static bool TryGetLoaded<T> (this IResourceLoader<T> loader, string path, out T obj) where T : Object
        {
            return obj = loader.GetLoaded(path);
        }

        /// <summary>
        /// Whether a resource with the specified path is currently loaded and valid.
        /// </summary>
        public static bool IsLoaded (this IResourceLoader loader, string path)
        {
            return loader.GetLoaded(path) is { Valid: true };
        }

        /// <summary>
        /// Returns an object of a resource with the specified local path in case it's loaded by this loader
        /// and is a valid (not destroyed) Unity object, throws otherwise.
        /// </summary>
        public static Object GetLoadedOrErr (this IResourceLoader loader, string path)
        {
            var resource = loader.GetLoaded(path);
            if (resource is not { Valid: true }) throw new Error($"Failed to get '{path}' resource: not loaded.");
            return resource;
        }

        /// <inheritdoc cref="GetLoadedOrErr"/>
        public static TResource GetLoadedOrErr<TResource> (this IResourceLoader<TResource> loader, string path)
            where TResource : Object
        {
            var resource = loader.GetLoaded(path);
            if (resource is not { Valid: true }) throw new Error($"Failed to get '{path}' resource: not loaded.");
            return resource;
        }

        /// <summary>
        /// Loads a resource with the specified full path. When a holder is specified also holds the resource.
        /// Throws when loading failed or the resource doesn't exist.
        /// </summary>
        public static async Awaitable<Resource> LoadOrErr (this IResourceLoader loader, string path,
            [CanBeNull] object holder = null)
        {
            var resource = await loader.Load(path, holder);
            if (!resource.Valid) throw new Error($"Failed to load '{path}' resource: make sure the resource is registered with a resource provider.");
            return resource;
        }

        /// <summary>
        /// Loads a resource with the specified type and local path. When a holder is specified, will as well hold
        /// the resource. Throws when loading failed or the resource doesn't exist.
        /// </summary>
        public static async Awaitable<Resource<TResource>> LoadOrErr<TResource> (this IResourceLoader<TResource> loader,
            string path, [CanBeNull] object holder = null) where TResource : Object
        {
            var resource = await loader.Load(path, holder);
            if (!resource.Valid)
                throw new Error($"Failed to load '{path}' resource of type '{typeof(TResource).FullName}': " +
                                "make sure the resource is registered with a resource provider.");
            return resource;
        }

        /// <summary>
        /// Given a full path of the specified resource starts with a path prefix managed by this loader,
        /// returns the local (to the loader) path of the resource, null otherwise.
        /// </summary>
        [CanBeNull]
        public static string GetLocalPath (this IResourceLoader loader, Resource resource)
        {
            return loader.GetLocalPath(resource.FullPath);
        }
    }
}
