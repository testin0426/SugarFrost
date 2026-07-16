using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Provides resources registered in the Unity editor via <see cref="Assets"/>.
    /// </summary>
    public sealed class EditorResourceProvider : ResourceProvider
    {
        public EditorResourceProvider ()
        {
            using var _ = Assets.Rent(out var assets);
            foreach (var asset in assets)
                Paths.Add(asset.FullPath);
        }

        protected override Awaitable<Object> LoadObject (string fullPath)
        {
            if (Assets.Get(fullPath)?.Guid is not { } guid)
                throw new Error($"Failed to load '{fullPath}' resource with editor provider: not registered.");
            if (AssetDatabase.GUIDToAssetPath(guid) is not { Length: > 0 } assetPath)
                throw new Error($"Failed to load '{fullPath}' resource with editor provider: invalid asset.");
            return Async.Result(AssetDatabase.LoadAssetAtPath<Object>(assetPath));
        }

        protected override void DisposeResource (Resource resource) { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InjectSelf ()
        {
            ResourceProviderConfiguration.EditorProvider = new EditorResourceProvider();
        }
    }
}
