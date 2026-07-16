using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Provides resources stored under the 'Resources/Naninovel' folders of the Unity project.
    /// </summary>
    public sealed class ProjectResourceProvider : ResourceProvider
    {
        public ProjectResourceProvider ()
        {
            Paths.UnionWith(ProjectResources.Collect().Paths);
        }

        protected override async Awaitable<Object> LoadObject (string fullPath)
        {
            var assetPath = $"Naninovel/{fullPath}";
            return await Resources.LoadAsync(assetPath);
        }

        protected override void DisposeResource (Resource resource)
        {
            if (!resource.Valid) return;
            // Can't unload prefabs: https://forum.unity.com/threads/393385.
            if (resource.Object is GameObject || resource.Object is Component) return;
            Resources.UnloadAsset(resource.Object);
        }
    }
}
