using System;
using UnityEditor;

namespace Naninovel
{
    /// <summary>
    /// Allows updating resource paths of the <see cref="Script"/> assets.
    /// </summary>
    public static class ScriptPathRefresher
    {
        private static ScriptPathResolver resolver;
        private static string[] guids;

        /// <summary>
        /// Updates resource paths of all the <see cref="Script"/> assets in the project;
        /// also removes stale (non-existent) script resource records.
        /// </summary>
        public static void RefreshAll ()
        {
            Initialize();
            try { RefreshAssets(); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static void Initialize ()
        {
            PackagePath.Refresh();
            resolver = new() { RootUri = PackagePath.ScenarioRoot };
        }

        private static void RefreshAssets ()
        {
            Assets.UnregisterWithPrefix(ScriptsConfiguration.DefaultPathPrefix);
            var guids = AssetDatabase.FindAssets("t:Naninovel.Script", new[] { PackagePath.ScenarioRoot });
            for (var i = 0; i < guids.Length; i++)
                RefreshAsset(guids[i], i, guids.Length);
        }

        private static void RefreshAsset (string guid, int idx, int total)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath)) return;
            if (EditorUtility.DisplayCancelableProgressBar("Refreshing Script Paths", assetPath, idx / (float)total))
                throw new OperationCanceledException("Script paths refresh cancelled by the user.");
            var scriptPath = resolver.Resolve(assetPath);
            Assets.Register(new(guid, ScriptsConfiguration.DefaultPathPrefix, scriptPath));
        }
    }
}
