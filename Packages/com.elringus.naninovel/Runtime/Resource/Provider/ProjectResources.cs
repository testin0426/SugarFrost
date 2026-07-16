using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Stores resources placed under 'Resources/Naninovel' project folders.
    /// </summary>
    public class ProjectResources : ScriptableObject
    {
        /// <summary>
        /// The Unity's <see cref="Resources"/> path of this asset serialized at build time.
        /// </summary>
        public const string AssetPath = "Naninovel/ProjectResources";

        /// <summary>
        /// Full paths of the project resources.
        /// </summary>
        public IReadOnlyList<string> Paths => paths;

        [SerializeField] private List<string> paths = new();

        /// <summary>
        /// Collects all the project resource paths in case invoked in editor
        /// or loads the cached paths when invoked in build.
        /// </summary>
        public static ProjectResources Collect ()
        {
            var asset = Application.isEditor
                ? CreateInstance<ProjectResources>()
                : Resources.Load<ProjectResources>(AssetPath);
            if (Application.isEditor) asset.CollectPaths();
            return asset;
        }

        private void CollectPaths ()
        {
            #if UNITY_EDITOR
            paths.Clear();

            foreach (var folderGuid in UnityEditor.AssetDatabase.FindAssets("Naninovel t:folder"))
            {
                var folderPath = UnityEditor.AssetDatabase.GUIDToAssetPath(folderGuid);
                if (folderPath == null || !folderPath.EndsWith("/Resources/Naninovel")) continue;
                foreach (var assetGuid in UnityEditor.AssetDatabase.FindAssets("", new[] { folderPath }))
                {
                    var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
                    if (!UnityEditor.AssetDatabase.IsValidFolder(assetPath))
                        paths.Add(ToResourcePath(assetPath));
                }
            }

            static string ToResourcePath (string assetPath)
            {
                assetPath = assetPath.GetAfterFirst("/Resources/Naninovel/");
                return assetPath.Contains('.') ? assetPath.GetBeforeLast(".") : assetPath;
            }
            #endif
        }
    }
}
