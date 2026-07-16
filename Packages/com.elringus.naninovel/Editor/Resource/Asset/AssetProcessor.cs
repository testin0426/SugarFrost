using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Naninovel
{
    public class AssetProcessor : AssetPostprocessor
    {
        public static event Action OnConfigurationModified;

        private static ScriptsConfiguration cfg;

        private static void OnPostprocessAllAssets (string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (BuildProcessor.Building) return;
            // Delayed call is required to prevent running when re-importing all assets,
            // at which point editor resources asset is not available.
            EditorApplication.delayCall += () => PostprocessDelayed(imported, moved, deleted);
        }

        private static void PostprocessDelayed (string[] imported, string[] moved, string[] deleted)
        {
            if (!cfg) cfg = Configuration.GetOrDefault<ScriptsConfiguration>();

            var importedDirs = new HashSet<string>();

            foreach (var assetPath in deleted)
                if (assetPath.EndsWithOrdinal(".nani"))
                    EnsureScriptRemoved(assetPath);

            foreach (string assetPath in imported)
            {
                if (assetPath.EndsWithOrdinal(".nani"))
                {
                    HandleAutoAdd(assetPath);
                    UpdateScriptPath(assetPath);
                    importedDirs.Add(Path.GetDirectoryName(Path.GetFullPath(assetPath)));
                    continue;
                }

                var type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (typeof(Configuration).IsAssignableFrom(type)) OnConfigurationModified?.Invoke();
            }

            foreach (string assetPath in moved)
            {
                if (!assetPath.EndsWithOrdinal(".nani")) continue;
                UpdateScriptPath(assetPath);
                AssetDatabase.ImportAsset(assetPath); // re-import required to actualize script path in serialized lines
            }

            if (importedDirs.Count > 0)
                ScriptFileWatcher.AddWatchedDirectories(importedDirs);
        }

        private static void UpdateScriptPath (string assetPath)
        {
            if (!cfg.AutoResolvePath) return;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (guid is null) return;

            var newPath = ResolveScriptPath(assetPath);
            var newFullPath = Resource.BuildFullPath(ScriptsConfiguration.DefaultPathPrefix, newPath);
            var oldFullPath = ScriptAssets.GetPath(guid);
            if (newFullPath == oldFullPath) return;

            Assets.Register(new(guid, ScriptsConfiguration.DefaultPathPrefix, newPath));
        }

        private static void HandleAutoAdd (string assetPath)
        {
            if (!cfg.AutoAddScripts) return;

            var path = ResolveScriptPath(assetPath);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (guid != null) Assets.Register(new(guid, ScriptsConfiguration.DefaultPathPrefix, path));
        }

        private static void EnsureScriptRemoved (string assetPath)
        {
            // Even though we have ScriptAssetProcessor.OnWillDeleteAsset taking care of
            // deleted script assets, it only calls back when an asset is deleted via Unity UI
            // or with the AssetDatabase APIs; files deleted via C# File.Delete or via external
            // tools are not detected. Hence, we handle these cases here.
            var path = ResolveScriptPath(assetPath);
            var fullPath = Resource.BuildFullPath(ScriptsConfiguration.DefaultPathPrefix, path);
            Assets.Unregister(fullPath);
        }

        private static string ResolveScriptPath (string assetPath)
        {
            var resolver = new ScriptPathResolver { RootUri = PackagePath.ScenarioRoot };
            return resolver.Resolve(assetPath);
        }
    }

    public class ScriptAssetProcessor : AssetModificationProcessor
    {
        private static EditorResources editorResources;

        private static AssetDeleteResult OnWillDeleteAsset (string assetPath, RemoveAssetOptions options)
        {
            if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(Script))
                return AssetDeleteResult.DidNotDelete;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (guid is null) return AssetDeleteResult.DidNotDelete;

            Assets.UnregisterWithGuid(guid);
            return AssetDeleteResult.DidNotDelete;
        }
    }
}
