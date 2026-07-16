using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A base <see cref="IResourceProvider"/> implementation.
    /// </summary>
    public abstract class ResourceProvider : IResourceProvider
    {
        protected virtual HashSet<string> Paths { get; } = new();
        protected virtual Dictionary<string, Resource> LoadedByPath { get; } = new();
        protected virtual Dictionary<string, AsyncSource<Resource>> LoadingByPath { get; } = new();

        public virtual async Awaitable<Resource> Load (string fullPath)
        {
            if (!Paths.Contains(fullPath)) throw new Error($"Resource '{fullPath}' failed to load: not found.");
            if (LoadedByPath.TryGetValue(fullPath, out var loaded)) return loaded;
            if (LoadingByPath.TryGetValue(fullPath, out var loading)) return await loading.WaitResult();
            LoadingByPath[fullPath] = new();
            var obj = await LoadObject(fullPath);
            if (!obj) throw new Error($"Resource '{fullPath}' failed to load: invalid object.");
            var resource = LoadedByPath[fullPath] = new(fullPath, obj);
            if (LoadingByPath.Remove(fullPath, out loading)) loading.Complete(resource);
            return resource;
        }

        public virtual async Awaitable<IReadOnlyCollection<Resource>> LoadAll (string prefix = null)
        {
            using var _ = Async.Rent<Resource>(out var tasks);
            using var __ = ListPool<string>.Rent(out var fullPaths);
            CollectPaths(fullPaths, prefix);
            foreach (var fullPath in fullPaths)
                tasks.Add(Load(fullPath));
            return await Async.All(tasks);
        }

        public virtual void CollectPaths (ICollection<string> fullPaths, string prefix = null)
        {
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith('/')) prefix += '/';
            foreach (var fullPath in Paths)
                if (string.IsNullOrEmpty(prefix) || fullPath.StartsWithOrdinal(prefix))
                    fullPaths.Add(fullPath);
        }

        public virtual bool Exists (string fullPath)
        {
            return Paths.Contains(fullPath);
        }

        public virtual void Unload (string fullPath)
        {
            if (LoadingByPath.Remove(fullPath, out var loading)) loading.Reset();
            if (!LoadedByPath.Remove(fullPath, out var loaded)) return;
            // Make sure no other resources use the same object before disposing it.
            foreach (var otherLoaded in LoadedByPath.Values)
                if (otherLoaded.Object == loaded.Object)
                    return;
            DisposeResource(loaded);
        }

        public virtual void UnloadAll ()
        {
            foreach (var loading in LoadingByPath.Values)
                loading.Reset();
            LoadingByPath.Clear();
            foreach (var loaded in LoadedByPath.Values)
                if (loaded.Valid)
                    DisposeResource(loaded);
            LoadedByPath.Clear();
        }

        public virtual Resource GetLoaded (string fullPath)
        {
            return LoadedByPath.GetValueOrDefault(fullPath);
        }

        public virtual void CollectLoaded (ICollection<Resource> resources)
        {
            foreach (var resource in LoadedByPath.Values)
                resources.Add(resource);
        }

        protected abstract Awaitable<Object> LoadObject (string fullPath);
        protected abstract void DisposeResource (Resource resource);
    }
}
