using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace Naninovel
{
    public class ResourcesBuilder
    {
        public const string TempResourcesPath = "Assets/TEMP_NANINOVEL/Resources";
        public const string TempStreamingPath = "Assets/StreamingAssets";

        private readonly ResourceProviderConfiguration cfg;
        private readonly HashSet<string> projectPaths = new();

        public ResourcesBuilder (ResourceProviderConfiguration cfg)
        {
            this.cfg = cfg;
        }

        public void Build (BuildPlayerOptions options)
        {
            PrepareForBuild();

            var progress = 0f;
            using var _ = Assets.Rent(out var assets);
            foreach (var asset in assets)
            {
                DisplayProgressBar($"Processing '{asset.FullPath}'", progress++ / assets.Count);
                ProcessRecord(asset.Guid, asset.FullPath, options.target == BuildTarget.WebGL);
            }

            using var __ = ListPool<string>.Rent(out var addressableGuids);
            using var ___ = SetPool<string>.Rent(out var excludeGuids);
            Addressables.CollectAssets(addressableGuids);
            foreach (var guid in addressableGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath != null && TryGetAssetType(assetPath, out var assetType) &&
                    assetType == typeof(VideoClip) && options.target == BuildTarget.WebGL && excludeGuids.Add(guid))
                {
                    using var ____ = ListPool<string>.Rent(out var paths);
                    Addressables.CollectResources(guid, paths);
                    if (paths.Count > 1)
                        Engine.Warn($"Movie resource '{guid}' is registered multiple times: " +
                                    $"{string.Join(", ", paths)}. On WebGL this will cause asset duplication.");
                    foreach (var path in paths)
                        ProcessVideoForWebGL(path, assetPath);
                }
            }

            if (cfg.LabelByScripts)
                Addressables.Label();

            AssetDatabase.SaveAssets();

            if (cfg.AutoBuildBundles)
                Addressables.Build(excludeGuids);
        }

        public void Cleanup ()
        {
            AssetDatabase.DeleteAsset(TempResourcesPath.GetBeforeLast("/"));
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }

        private void PrepareForBuild ()
        {
            EditorUtils.CreateFolderAsset(TempResourcesPath);
            ProjectResourcesBuildProcessor.TempFolderPath = TempResourcesPath;
            projectPaths.Clear();
            projectPaths.UnionWith(ProjectResources.Collect().Paths);
        }

        private void ProcessRecord (string assetGuid, string resourcePath, bool webgl)
        {
            if (!TryGetAssetPath(assetGuid, resourcePath, out var assetPath)) return;
            if (!TryGetAssetType(assetPath, out var assetType)) return;
            if (assetType == typeof(SceneAsset)) ProcessSceneResource(assetPath);
            else if (assetType == typeof(VideoClip) && webgl) ProcessVideoForWebGL(resourcePath, assetPath);
            else ProcessResourceAsset(assetGuid, resourcePath, assetPath);
        }

        private bool TryGetAssetPath (string assetGuid, string resourcePath, out string assetPath)
        {
            assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (!string.IsNullOrEmpty(assetPath)) return true;
            Engine.Warn($"Failed to resolve '{resourcePath}' resource GUID. The resource won't be included to the build.");
            return false;
        }

        private bool TryGetAssetType (string assetPath, out Type assetType)
        {
            assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (assetType != null) return true;
            Engine.Warn($"Failed to evaluate type of '{assetPath}' asset. The asset won't be included to the build.");
            return false;
        }

        private static void ProcessSceneResource (string assetPath)
        {
            var currentScenes = EditorBuildSettings.scenes.ToList();
            if (string.IsNullOrEmpty(assetPath) || currentScenes.Exists(s => s.path == assetPath)) return;
            currentScenes.Add(new(assetPath, true));
            EditorBuildSettings.scenes = currentScenes.ToArray();
        }

        private void ProcessVideoForWebGL (string resourcePath, string assetPath)
        {
            // Stub with an empty video to satisfy resource loaders.
            var nullVideoAssetPath = $"{PackagePath.RuntimeResourcesPath}/NullVideo";
            CopyTemporaryAsset(nullVideoAssetPath, resourcePath, "mp4");
            // Copy the actual video to streaming assets' folder.
            var streamingPath = PathUtils.Combine(TempStreamingPath, resourcePath);
            if (assetPath.Contains(".")) streamingPath += $".{assetPath.GetAfter(".")}";
            if (assetPath.EndsWithOrdinal(streamingPath)) return; // Already in the streaming folder.
            EditorUtils.CreateFolderAsset(streamingPath.GetBeforeLast("/"));
            AssetDatabase.CopyAsset(assetPath, streamingPath);
        }

        private void ProcessResourceAsset (string assetGuid, string resourcePath, string assetPath)
        {
            if (IsProjectResource(assetGuid, resourcePath)) return;
            if (Addressables.Available) Addressables.Register(assetGuid, resourcePath);
            else CopyTemporaryAsset(assetPath, resourcePath);
        }

        private bool IsProjectResource (string assetGuid, string resourcePath)
        {
            if (!projectPaths.Contains(resourcePath)) return false;
            CheckProjectResourceConflict(assetGuid, resourcePath);
            return true;
        }

        private void CheckProjectResourceConflict (string assetGuid, string resourcePath)
        {
            var otherResourcePath = $"Naninovel/{resourcePath}";
            var otherAsset = Resources.Load(otherResourcePath);
            if (!otherAsset) return;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(otherAsset, out var otherGuid, out long _);
            if (otherGuid == assetGuid) return;
            var otherPath = AssetDatabase.GetAssetPath(otherAsset);
            throw new InvalidOperationException(
                $"Resource conflict detected: asset stored at '{otherPath}' conflicts with " +
                $"'{resourcePath}' Naninovel resource; rename or move the conflicting asset.");
        }

        private void CopyTemporaryAsset (string assetPath, string resourcePath, string ext = null)
        {
            var tempPath = PathUtils.Combine(TempResourcesPath, "Naninovel", resourcePath);
            if (ext != null) tempPath += $".{ext}";
            else if (assetPath.Contains(".")) tempPath += $".{assetPath.GetAfter(".")}";
            EditorUtils.CreateFolderAsset(tempPath.GetBeforeLast("/"));
            AssetDatabase.CopyAsset(assetPath, tempPath);
        }

        private static void DisplayProgressBar (string job, float progress)
        {
            if (EditorUtility.DisplayCancelableProgressBar("Processing Naninovel Resources", $"{job}...", progress))
                throw new OperationCanceledException("Build was cancelled by the user.");
        }
    }
}
