using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A transient <see cref="IResourceProvider"/> keeping the resources in-memory.
    /// </summary>
    public class VirtualResourceProvider : IResourceProvider
    {
        protected virtual Dictionary<string, Resource> ResourceByPath { get; } = new();

        public virtual void SetResource (string fullPath, Object obj)
        {
            ResourceByPath[fullPath] = new(fullPath, obj);
        }

        public virtual void RemoveResource (string fullPath)
        {
            ResourceByPath.Remove(fullPath);
        }

        public virtual void RemoveAllResources ()
        {
            ResourceByPath.Clear();
        }

        public virtual Awaitable<Resource> Load (string fullPath)
        {
            return Async.Result(ResourceByPath[fullPath]);
        }

        public virtual Awaitable<IReadOnlyCollection<Resource>> LoadAll (string prefix = null)
        {
            using var _ = this.RentPaths(out var fullPaths, prefix);
            var resources = new List<Resource>();
            foreach (var fullPath in fullPaths)
                resources.Add(ResourceByPath[fullPath]);
            return Async.Result<IReadOnlyCollection<Resource>>(resources);
        }

        public virtual void CollectPaths (ICollection<string> fullPaths, string prefix = null)
        {
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith('/')) prefix += '/';
            foreach (var fullPath in ResourceByPath.Keys)
                if (string.IsNullOrEmpty(prefix) || fullPath.StartsWithOrdinal(prefix))
                    fullPaths.Add(fullPath);
        }

        public virtual bool Exists (string fullPath)
        {
            return ResourceByPath.ContainsKey(fullPath);
        }

        public virtual void Unload (string fullPath) { }

        public virtual void UnloadAll () { }

        public virtual Resource GetLoaded (string fullPath)
        {
            return ResourceByPath[fullPath];
        }

        public virtual void CollectLoaded (ICollection<Resource> resources)
        {
            foreach (var resource in ResourceByPath.Values)
                resources.Add(resource);
        }
    }
}
