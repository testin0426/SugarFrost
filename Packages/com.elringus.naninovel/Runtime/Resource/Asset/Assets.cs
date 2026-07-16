using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A persistent <see cref="Asset"/> registry.
    /// </summary>
    public static class Assets
    {
        /// <summary>
        /// Occurs when the registry is modified.
        /// </summary>
        public static event Action OnModified;

        private static AssetRegistry registry => loaded ?? Load();
        [CanBeNull] private static AssetRegistry loaded;

        /// <summary>
        /// Returns an asset registered with the specified full path or null when none.
        /// </summary>
        [CanBeNull]
        public static Asset Get (string fullPath)
        {
            return registry.ByFullPath.GetValueOrDefault(fullPath);
        }

        /// <summary>
        /// Returns first registered asset that satisfied the specified filter; returns null when not found.
        /// </summary>
        [CanBeNull]
        public static Asset Find (Predicate<Asset> filter)
        {
            foreach (var asset in registry.ByFullPath.Values)
                if (filter(asset))
                    return asset;
            return null;
        }

        /// <summary>
        /// Collects all registered assets to the specified collection, optionally filtered by the predicate.
        /// </summary>
        public static void Collect (ICollection<Asset> assets, Predicate<Asset> filter = null)
        {
            foreach (var asset in registry.ByFullPath.Values)
                if (filter == null || filter(asset))
                    assets.Add(asset);
        }

        /// <summary>
        /// Rents a pooled list and collects all registered assets, optionally filtered by the predicate.
        /// </summary>
        public static IDisposable Rent (out List<Asset> assets, Predicate<Asset> filter = null)
        {
            var rent = ListPool<Asset>.Rent(out assets);
            Collect(assets, filter);
            return rent;
        }

        /// <summary>
        /// Returns the number of registered assets with the specified GUID.
        /// </summary>
        public static int CountWithGuid (string guid)
        {
            if (registry.ByGuid.TryGetValue(guid, out var assetsWithGuid))
                return assetsWithGuid.Count;
            return 0;
        }

        /// <summary>
        /// Collects all registered assets with the specified GUID to the specified collection.
        /// </summary>
        public static void CollectWithGuid (string guid, ICollection<Asset> assets)
        {
            if (registry.ByGuid.TryGetValue(guid, out var withGuid))
                foreach (var asset in withGuid)
                    assets.Add(asset);
        }

        /// <summary>
        /// Rents a pooled list and collects all registered assets with the specified GUID.
        /// </summary>
        public static IDisposable RentWithGuid (string guid, out List<Asset> assets)
        {
            var rent = ListPool<Asset>.Rent(out assets);
            CollectWithGuid(guid, assets);
            return rent;
        }

        /// <summary>
        /// Returns the number of registered assets with the specified path prefix.
        /// </summary>
        public static int CountWithPrefix (string prefix)
        {
            if (registry.ByPrefix.TryGetValue(prefix, out var withPrefix))
                return withPrefix.Count;
            return 0;
        }

        /// <summary>
        /// Collects all registered assets with the specified path prefix to the specified collection.
        /// </summary>
        public static void CollectWithPrefix (string prefix, ICollection<Asset> assets)
        {
            if (registry.ByPrefix.TryGetValue(prefix, out var withPrefix))
                foreach (var asset in withPrefix)
                    assets.Add(asset);
        }

        /// <summary>
        /// Rents a pooled list and collects all registered assets with the specified path prefix.
        /// </summary>
        public static IDisposable RentWithPrefix (string prefix, out List<Asset> assets)
        {
            var rent = ListPool<Asset>.Rent(out assets);
            CollectWithPrefix(prefix, assets);
            return rent;
        }

        /// <summary>
        /// Returns the number of registered assets with the specified group.
        /// </summary>
        public static int CountWithGroup (string group)
        {
            if (registry.ByGroup.TryGetValue(group, out var withGroup))
                return withGroup.Count;
            return 0;
        }

        /// <summary>
        /// Collects all registered assets with the specified group to the specified collection.
        /// </summary>
        public static void CollectWithGroup (string group, ICollection<Asset> assets)
        {
            if (registry.ByGroup.TryGetValue(group, out var withGroup))
                foreach (var asset in withGroup)
                    assets.Add(asset);
        }

        /// <summary>
        /// Rents a pooled list and collects all registered assets with the specified group.
        /// </summary>
        public static IDisposable RentWithGroup (string group, out List<Asset> assets)
        {
            var rent = ListPool<Asset>.Rent(out assets);
            CollectWithGroup(group, assets);
            return rent;
        }

        /// <summary>
        /// Registers specified assets, unless already registered.
        /// </summary>
        public static void Register (Asset asset)
        {
            if (Get(asset.FullPath) is { } ex && ex.Guid == asset.Guid && ex.Group == asset.Group) return;
            registry.Register(asset);
            OnModified?.Invoke();
        }

        /// <summary>
        /// Registers specified assets in a batch.
        /// </summary>
        public static void RegisterMany (IEnumerable<Asset> assets)
        {
            foreach (var asset in assets)
                registry.Register(asset);
            OnModified?.Invoke();
        }

        /// <summary>
        /// Removes registered asset with the specified full path, if any.
        /// </summary>
        public static void Unregister (string fullPath)
        {
            if (registry.Unregister(fullPath))
                OnModified?.Invoke();
        }

        /// <summary>
        /// Removes registered assets that satisfy specified filter.
        /// </summary>
        public static void Unregister (Predicate<Asset> filter)
        {
            var any = false;
            foreach (var asset in registry.ByFullPath.Values.ToArray())
                if (filter(asset))
                    any = registry.Unregister(asset.FullPath);
            if (any) OnModified?.Invoke();
        }

        /// <summary>
        /// Removes registered assets with the specified asset GUID.
        /// </summary>
        public static void UnregisterWithGuid (string guid)
        {
            if (registry.ByGuid.Remove(guid, out var removed))
                foreach (var asset in removed)
                    registry.Unregister(asset.FullPath);
            if (removed is { Count: > 0 }) OnModified?.Invoke();
        }

        /// <summary>
        /// Removes registered assets with the specified path prefix.
        /// </summary>
        public static void UnregisterWithPrefix (string prefix)
        {
            if (registry.ByPrefix.Remove(prefix, out var removed) && removed.Count > 0)
                foreach (var asset in removed)
                    registry.Unregister(asset.FullPath);
            if (removed is { Count: > 0 }) OnModified?.Invoke();
        }

        /// <summary>
        /// Removes registered assets with the specified group.
        /// </summary>
        public static void UnregisterWithGroup (string group)
        {
            if (registry.ByGroup.Remove(group, out var removed) && removed.Count > 0)
                foreach (var asset in removed)
                    registry.Unregister(asset.FullPath);
            if (removed is { Count: > 0 }) OnModified?.Invoke();
        }

        private static AssetRegistry Load ()
        {
            loaded = new(); // UnityEngine APIs in static ctor are not safe, hence the lazy-loading
            if (Resources.Load<TextAsset>("Naninovel/Assets")?.text is { Length: > 0 } json &&
                JsonUtility.FromJson<Serialized>(json)?.Assets is { } assets)
                foreach (var asset in assets)
                    loaded.Register(asset);
            return loaded;
        }
        
        // @formatter:off (JsonUtility doesn't support top-level lists, hence the wrapper)
        [Serializable] public class Serialized { public List<Asset> Assets; } // @formatter:on
    }
}
