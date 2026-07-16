using System.Collections.Generic;

namespace Naninovel
{
    /// <summary>
    /// Provides efficient lookup over the <see cref="Asset"/> keys.
    /// </summary>
    public class AssetRegistry
    {
        public Dictionary<string, Asset> ByFullPath { get; } = new();
        public Dictionary<string, List<Asset>> ByGuid { get; } = new();
        public Dictionary<string, List<Asset>> ByPrefix { get; } = new();
        public Dictionary<string, List<Asset>> ByGroup { get; } = new();

        public void Register (Asset asset)
        {
            ByFullPath[asset.FullPath] = asset;
            if (ByGuid.TryGetValue(asset.Guid, out var withGuid)) withGuid.Add(asset);
            else ByGuid[asset.Guid] = new() { asset };
            if (ByPrefix.TryGetValue(asset.Prefix, out var withPrefix)) withPrefix.Add(asset);
            else ByPrefix[asset.Prefix] = new() { asset };
            if (asset.Group != null)
                if (ByGroup.TryGetValue(asset.Group, out var withGroup)) withGroup.Add(asset);
                else ByGroup[asset.Group] = new() { asset };
        }

        public bool Unregister (string fullPath)
        {
            if (!ByFullPath.Remove(fullPath, out var asset)) return false;
            if (ByGuid.TryGetValue(asset.Guid, out var withGuid))
                for (var i = withGuid.Count - 1; i >= 0; i--)
                    if (withGuid[i].FullPath == fullPath)
                    {
                        withGuid.RemoveAt(i);
                        break;
                    }
            if (ByPrefix.TryGetValue(asset.Prefix, out var withPrefix))
                for (var i = withPrefix.Count - 1; i >= 0; i--)
                    if (withPrefix[i].FullPath == fullPath)
                    {
                        withPrefix.RemoveAt(i);
                        break;
                    }
            if (asset.Group != null)
                if (ByGroup.TryGetValue(asset.Group, out var withGroup))
                    for (var i = withGroup.Count - 1; i >= 0; i--)
                        if (withGroup[i].FullPath == fullPath)
                        {
                            withGroup.RemoveAt(i);
                            break;
                        }
            return true;
        }
    }
}
