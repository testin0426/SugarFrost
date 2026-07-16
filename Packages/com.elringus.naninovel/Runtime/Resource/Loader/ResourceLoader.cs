// #define NANINOVEL_RESOURCES_DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Naninovel
{
    /// <summary>
    /// Allows to load and unload <see cref="Resource{TResource}"/> objects via a prioritized <see cref="ProvisionSource"/> list.
    /// </summary>
    public class ResourceLoader<TResource> : IResourceLoader<TResource>
        where TResource : Object
    {
        public class LoadedResource
        {
            public Resource<TResource> Resource { get; private set; }
            public ProvisionSource Source { get; }
            public string LocalPath { get; }
            public Object Object => Resource.Object;
            public string FullPath => Resource.FullPath;
            public bool Valid => Resource.Valid;
            public IReadOnlyCollection<object> Holders => holders;

            private readonly HashSet<object> holders = new();

            public LoadedResource (Resource<TResource> resource, ProvisionSource source,
                IReadOnlyCollection<object> holders = null)
            {
                Resource = resource;
                Source = source;
                LocalPath = source.BuildLocalPath(resource.FullPath);

                if (holders != null)
                    this.holders.UnionWith(holders);
            }

            public void AddHolder (object holder) => holders.Add(holder);
            public void RemoveHolder (object holder) => holders.Remove(holder);
            public bool IsHeldBy (object holder) => holders.Contains(holder);
            public void AddHoldersFrom (LoadedResource resource) => holders.UnionWith(resource.holders);
            public void UpdateObject (TResource obj) => Resource = new(Resource.FullPath, obj);
        }

        public event Action<Resource<TResource>> OnLoaded;
        public event Action<Resource<TResource>> OnUnloaded;

        /// <summary>
        /// Prioritized provision sources list used by the loader.
        /// </summary>
        protected readonly List<ProvisionSource> ProvisionSources = new();
        /// <summary>
        /// Resources loaded by the loader mapped by their full path.
        /// </summary>
        protected readonly Dictionary<string, LoadedResource> LoadedByFullPath = new();
        /// <summary>
        /// Resources loaded by the loader mapped by their local path.
        /// </summary>
        protected readonly Dictionary<string, LoadedResource> LoadedByLocalPath = new();
        /// <summary>
        /// Resources currently being loaded mapped by their local path.
        /// </summary>
        protected virtual Dictionary<string, AsyncSource<Resource<TResource>>> LoadingByLocalPath { get; } = new();
        /// <summary>
        /// Tracks active resource holders.
        /// </summary>
        protected readonly IHoldersTracker HoldersTracker;
        /// <summary>
        /// Optional prefix prepended to local paths managed by this loader;
        /// full path is resolved by prepending the prefix to local path.
        /// </summary>
        protected readonly string PathPrefix;

        private readonly string localPrefix;

        public ResourceLoader (IEnumerable<IResourceProvider> providers, IHoldersTracker holdersTracker, string pathPrefix = "")
        {
            PathPrefix = pathPrefix;
            localPrefix = string.IsNullOrEmpty(pathPrefix) ? null : pathPrefix + "/";
            HoldersTracker = holdersTracker;
            foreach (var provider in providers)
                ProvisionSources.Add(new(provider, pathPrefix));
        }

        public string GetFullPath (string localPath)
        {
            if (string.IsNullOrEmpty(PathPrefix)) return localPath;
            return $"{PathPrefix}/{localPath}";
        }

        public virtual string GetLocalPath (string fullPath)
        {
            if (localPrefix == null) return fullPath;
            return fullPath.GetAfterFirst(localPrefix);
        }

        public virtual void Hold (string path, object holder)
        {
            var resource = GetLoadedResourceOrNull(path);
            if (resource is null || !resource.Valid)
                throw new Error($"Failed to hold '{GetFullPath(path)}' by '{holder}': resource is not loaded.");

            resource.AddHolder(holder);
            if (resource.Holders.Count == 1)
                HoldersTracker.Hold(resource.Object, this);
            LogDebug($"Held '{resource.FullPath}' by '{holder}' via '{resource.Source.Provider}'.");
        }

        public virtual void Release (string path, object holder, bool unload = true)
        {
            var resource = GetLoadedResourceOrNull(path);
            if (resource == null) return;

            Release(resource, holder, unload);
            LogDebug($"Released '{resource.FullPath}' by '{holder}' via '{resource.Source.Provider}'.");
        }

        public virtual void ReleaseAll (object holder, bool unload = true)
        {
            foreach (var res in LoadedByLocalPath.Values.ToArray())
                Release(res, holder, unload);
        }

        public virtual void Unload (string path)
        {
            if (LoadedByLocalPath.GetValueOrDefault(path) is { } res)
                Unload(res);
        }

        public virtual void UnloadAll ()
        {
            foreach (var res in LoadedByLocalPath.Values.ToArray())
                Unload(res);
        }

        public virtual async Awaitable<Resource<TResource>> Reload (string path)
        {
            var loaded = LoadedByLocalPath.GetValueOrDefault(path);
            if (Application.isEditor && loaded?.Source.Provider.GetType().Name == "EditorResourceProvider")
                return GetLoaded(path); // assets in editor are updated in-memory on import and should not be reloaded
            var holders = loaded?.Holders ?? Array.Empty<object>();
            Unload(path);
            var resource = await Load(path);
            foreach (var holder in holders)
                Hold(path, holder);
            return resource;
        }

        public virtual void Update (string path, TResource obj)
        {
            if (!LoadedByLocalPath.TryGetValue(path, out var res))
                throw new Error($"Failed to update '{path}' resource: not loaded.");
            res.UpdateObject(obj);
        }

        public virtual bool IsHeldBy (string path, object holder)
        {
            return GetLoadedResourceOrNull(path)?.IsHeldBy(holder) ?? false;
        }

        public virtual int CountHolders (string path = null)
        {
            if (!string.IsNullOrEmpty(path))
                return GetLoadedResourceOrNull(path)?.Holders.Count ?? 0;
            using var _ = SetPool<object>.Rent(out var holders);
            foreach (var res in LoadedByFullPath.Values)
                holders.UnionWith(res.Holders);
            return holders.Count;
        }

        public virtual void CollectHolders (ISet<object> holders, string path = null)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (GetLoadedResourceOrNull(path)?.Holders is { } result)
                    holders.UnionWith(result);
                return;
            }

            foreach (var res in LoadedByFullPath.Values)
                holders.UnionWith(res.Holders);
        }

        public virtual bool IsLoaded (string path)
        {
            return LoadedByLocalPath.TryGetValue(path, out var res) && res.Valid;
        }

        public virtual Resource<TResource> GetLoaded (string path)
        {
            return GetLoadedResourceOrNull(path)?.Resource;
        }

        public virtual async Awaitable<Resource<TResource>> Load (string path, object holder = null)
        {
            if (IsLoaded(path))
            {
                if (holder != null) Hold(path, holder);
                return GetLoaded(path);
            }

            if (LoadingByLocalPath.TryGetValue(path, out var loading))
            {
                var result = await loading.WaitResult();
                if (holder != null) Hold(path, holder);
                return result;
            }

            foreach (var source in ProvisionSources)
            {
                var fullPath = source.BuildFullPath(path);
                if (!source.Provider.Exists(fullPath)) continue;

                LoadingByLocalPath[path] = new();
                var resource = await source.Provider.Load<TResource>(fullPath);
                AddLoadedResource(new(resource, source));
                if (holder != null) Hold(path, holder);
                if (LoadingByLocalPath.Remove(path, out var src)) src.Complete(resource);
                return resource;
            }

            return Resource<TResource>.Invalid;
        }

        public virtual async Awaitable<IReadOnlyCollection<Resource<TResource>>> LoadAll (string prefix = null,
            object holder = null)
        {
            var result = new List<Resource<TResource>>();
            var addedPaths = new HashSet<string>();
            using var _ = Async.Rent<Resource<TResource>>(out var loadTasks);
            var loadData = new Dictionary<string, (ProvisionSource, string)>();

            foreach (var src in ProvisionSources)
            {
                var fullPathPrefix = src.BuildFullPath(prefix);
                using var __ = ListPool<string>.Rent(out var fullPaths);
                src.Provider.CollectPaths(fullPaths, fullPathPrefix);
                foreach (var fullPath in fullPaths)
                {
                    var localPath = src.BuildLocalPath(fullPath);
                    if (!addedPaths.Add(localPath)) continue;

                    if (IsLoaded(localPath))
                    {
                        result.Add(GetLoaded(localPath));
                        continue;
                    }

                    loadTasks.Add(src.Provider.Load<TResource>(fullPath));
                    loadData[fullPath] = (src, localPath);
                }
            }

            var resources = await Async.All(loadTasks);

            foreach (var resource in resources)
            {
                var (source, _) = loadData[resource.FullPath];
                AddLoadedResource(new(resource, source));
                if (holder != null) Hold(GetLocalPath(resource.FullPath), holder);
                result.Add(resource);
            }

            return result;
        }

        public virtual void CollectLoaded (ICollection<Resource<TResource>> resources)
        {
            if (LoadedByFullPath.Count == 0) return;
            foreach (var res in LoadedByFullPath.Values)
                if (res.Valid)
                    resources.Add(res.Resource);
        }

        public virtual void CollectPaths (ICollection<string> paths, string prefix = null)
        {
            using var _ = SetPool<string>.Rent(out var localPaths);
            foreach (var src in ProvisionSources)
            {
                using var __ = SetPool<string>.Rent(out var fullPaths);
                src.Provider.CollectPaths(fullPaths, src.BuildFullPath(prefix));
                foreach (var fullPath in fullPaths)
                    localPaths.Add(src.BuildLocalPath(fullPath));
            }
            foreach (var path in localPaths)
                paths.Add(path);
        }

        public virtual bool Exists (string path)
        {
            if (IsLoaded(path)) return true;
            foreach (var source in ProvisionSources)
                if (source.Provider.Exists(source.BuildFullPath(path)))
                    return true;
            return false;
        }

        [CanBeNull]
        protected virtual LoadedResource GetLoadedResourceOrNull (string localPath)
        {
            return LoadedByLocalPath.TryGetValue(localPath, out var res) && res.Valid ? res : null;
        }

        protected virtual void Release (LoadedResource resource, object holder, bool unload = true)
        {
            resource.RemoveHolder(holder);
            if (resource.Valid && (resource.Holders.Count > 0 || HoldersTracker.Release(resource.Object, this) > 0 || !unload)) return;
            Unload(resource);
        }

        protected virtual void Unload (LoadedResource resource)
        {
            resource.Source.Provider.Unload(resource.FullPath);
            RemoveLoadedResource(resource);
        }

        protected virtual void AddLoadedResource (LoadedResource resource)
        {
            LoadedByFullPath[resource.FullPath] = resource;
            LoadedByLocalPath[resource.LocalPath] = resource;
            OnLoaded?.Invoke(resource.Resource);
            LogDebug($"<color=green>Loaded '{resource.FullPath}' via '{resource.Source.Provider}'.</color>");
        }

        protected virtual void RemoveLoadedResource (LoadedResource resource)
        {
            // Notify before removing, as listener may need to resolve local path of the resource.
            OnUnloaded?.Invoke(resource.Resource);
            LoadedByFullPath.Remove(resource.FullPath);
            LoadedByLocalPath.Remove(resource.LocalPath);
            LogDebug($"<color=red>Unloaded '{resource.FullPath}' via '{resource.Source.Provider}'.</color>");
        }

        Resource IResourceLoader.GetLoaded (string path) => GetLoaded(path);
        async Awaitable<Resource> IResourceLoader.Load (string path, object holder) => await Load(path, holder);
        async Awaitable<IReadOnlyCollection<Resource>> IResourceLoader.LoadAll (string prefix, object holder) => await LoadAll(prefix, holder);
        async Awaitable<Resource> IResourceLoader.Reload (string path) => await Reload(path);
        void IResourceLoader.Update (string path, Object obj) => Update(path, (TResource)obj);

        void IResourceLoader.CollectLoaded (ICollection<Resource> resources)
        {
            if (LoadedByFullPath.Count == 0) return;
            foreach (var res in LoadedByFullPath.Values)
                if (res.Valid)
                    resources.Add(res.Resource);
        }

        [Conditional("NANINOVEL_RESOURCES_DEBUG")]
        private static void LogDebug (string message) => Engine.Log(message);
    }
}
