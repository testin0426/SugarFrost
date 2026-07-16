#if ADDRESSABLES_AVAILABLE

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Naninovel
{
    public sealed class AddressableResourceProvider : ResourceProvider
    {
        private readonly Dictionary<string, string> addressByFullPath = new();

        public AddressableResourceProvider ()
        {
            foreach (var locator in Addressables.ResourceLocators)
            foreach (var key in locator.Keys)
                if (key is string address)
                    using (AddressableUtils.RentPaths(address, out var fullPaths))
                        foreach (var fullPath in fullPaths)
                        {
                            Paths.Add(fullPath);
                            addressByFullPath[fullPath] = address;
                        }
        }

        protected override async Awaitable<Object> LoadObject (string fullPath)
        {
            if (!addressByFullPath.TryGetValue(fullPath, out var address))
                throw new Error($"Failed to load '{fullPath}' resource: address not found.");
            return await Addressables.LoadAssetAsync<Object>(address);
        }

        protected override void DisposeResource (Resource resource)
        {
            if (!resource.Valid) return;
            Addressables.Release(resource.Object);
        }
    }
}

#endif
