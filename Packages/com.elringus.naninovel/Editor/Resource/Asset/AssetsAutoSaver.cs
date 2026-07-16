using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Auto-saves <see cref="Assets"/> in editor.
    /// </summary>
    public static class AssetsAutoSaver
    {
        private static readonly Assets.Serialized buffer = new() { Assets = new() };
        private static double lastSaveTime;
        private static bool dirty;

        public static void Initialize ()
        {
            Assets.OnModified -= HandleAssetsModified;
            Assets.OnModified += HandleAssetsModified;
            EditorApplication.update -= HandleEditorUpdate;
            EditorApplication.update += HandleEditorUpdate;
            AssemblyReloadEvents.beforeAssemblyReload -= HandleBeforeReload;
            AssemblyReloadEvents.beforeAssemblyReload += HandleBeforeReload;
            EditorApplication.quitting -= HandleEditorQuitting;
            EditorApplication.quitting += HandleEditorQuitting;
        }

        private static void HandleAssetsModified ()
        {
            dirty = true;
        }

        private static void HandleEditorUpdate ()
        {
            if (!dirty || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var timeSinceSave = EditorApplication.timeSinceStartup - lastSaveTime;
            if (timeSinceSave > 1.0) SaveAssets();
        }

        private static void HandleBeforeReload ()
        {
            if (dirty) SaveAssets();
        }

        private static void HandleEditorQuitting ()
        {
            if (dirty) SaveAssets(false);
        }

        private static void SaveAssets (bool import = true)
        {
            buffer.Assets.Clear();
            Assets.Collect(buffer.Assets);
            buffer.Assets = buffer.Assets.OrderBy(a => a.FullPath).ToList();
            var path = PathUtils.Combine(PackagePath.GeneratedResourcesPath, "Assets.json");
            var json = JsonUtility.ToJson(buffer, true);
            IOUtils.WriteTextAtomic(path, json);
            if (import) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            lastSaveTime = EditorApplication.timeSinceStartup;
            dirty = false;
        }
    }
}
