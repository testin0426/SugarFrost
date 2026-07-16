// TODO: Remove in 1.22 (superseded by Assets)

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    [Serializable]
    public class EditorResources : ScriptableObject
    {
        [Serializable]
        private class ResourceCategory
        {
            public string Id;
            public List<EditorResource> Resources;
        }

        [SerializeField] private List<ResourceCategory> resourceCategories = new();
        [SerializeField] private bool migrated;

        [CanBeNull]
        public static List<Asset> GetAssetToMigrate ()
        {
            if (LoadExisting() is not { } resources || resources.migrated) return null;

            var assets = new List<Asset>();
            foreach (var cat in resources.resourceCategories)
            foreach (var res in cat.Resources)
                if (EditorUtils.AssetExistsByGuid(res.Guid))
                    assets.Add(new(res.Guid, res.Prefix, res.Path, cat.Id != res.Prefix ? cat.Id : null));
            return assets;
        }

        public static void SetMigrated (bool migrated)
        {
            if (LoadExisting() is not { } resources) return;

            resources.migrated = migrated;
            EditorUtility.SetDirty(resources);
            AssetDatabase.SaveAssets();
        }

        [CanBeNull]
        private static EditorResources LoadExisting ()
        {
            var resPath = PathUtils.Combine(PackagePath.GeneratedDataPath, $"{nameof(EditorResources)}.asset");
            var resources = AssetDatabase.LoadAssetAtPath<EditorResources>(resPath);
            return resources ? resources : null;
        }
    }
}
