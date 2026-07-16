using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Editor window for bulk-optimizing model (FBX / blend) import settings.
// Open via:  Tools > Mesh Optimizer
//
// Add folders, choose targets, Scan to see current source size, then Optimize
// to reimport models with mesh compression and Read/Write disabled.
public class MeshOptimizerWindow : EditorWindow
{
    private readonly List<string> _folders = new List<string>();
    private Vector2 _scroll;

    private ModelImporterMeshCompression _compression = ModelImporterMeshCompression.High;
    private bool _setCompression = true;
    private bool _disableReadWrite = true;
    private bool _optimizeMesh = true;
    private bool _stripBlendShapes = false;   // off by default: VN faces often use these
    private bool _stripRig = false;           // off by default: animated chars need the rig
    private bool _stripCamerasLights = true;

    private string _status = "Add folders, then Scan or Optimize.";

    [MenuItem("Tools/Mesh Optimizer")]
    private static void Open()
    {
        var w = GetWindow<MeshOptimizerWindow>("Mesh Optimizer");
        w.minSize = new Vector2(460, 380);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Folders to process", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(110));
        int removeAt = -1;
        for (int i = 0; i < _folders.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(_folders[i], EditorStyles.textField, GUILayout.Height(18));
            if (GUILayout.Button("X", GUILayout.Width(22))) removeAt = i;
            EditorGUILayout.EndHorizontal();
        }
        if (removeAt >= 0) _folders.RemoveAt(removeAt);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add folder…")) AddFolderViaDialog();
        if (GUILayout.Button("Add selected in Project")) AddSelectedFolders();
        if (GUILayout.Button("Clear")) _folders.Clear();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Target settings", EditorStyles.boldLabel);

        _setCompression = EditorGUILayout.BeginToggleGroup("Set mesh compression", _setCompression);
        _compression = (ModelImporterMeshCompression)EditorGUILayout.EnumPopup("Compression", _compression);
        EditorGUILayout.EndToggleGroup();

        _disableReadWrite = EditorGUILayout.Toggle("Disable Read/Write", _disableReadWrite);
        _optimizeMesh = EditorGUILayout.Toggle("Optimize Mesh (reorder)", _optimizeMesh);
        _stripCamerasLights = EditorGUILayout.Toggle("Don't import Cameras/Lights", _stripCamerasLights);
        _stripBlendShapes = EditorGUILayout.Toggle("Strip Blend Shapes (careful!)", _stripBlendShapes);
        _stripRig = EditorGUILayout.Toggle("Strip rig / set Animation None (careful!)", _stripRig);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan (no changes)", GUILayout.Height(28))) Run(dryRun: true);
        GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
        if (GUILayout.Button("Optimize", GUILayout.Height(28))) Run(dryRun: false);
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(_status, MessageType.Info);
    }

    private void AddFolderViaDialog()
    {
        string abs = EditorUtility.OpenFolderPanel("Pick a folder under Assets/", Application.dataPath, "");
        if (string.IsNullOrEmpty(abs)) return;
        string rel = ToAssetsRelative(abs);
        if (rel == null) _status = "Folder must be inside this project's Assets/ folder.";
        else if (!_folders.Contains(rel)) _folders.Add(rel);
    }

    private void AddSelectedFolders()
    {
        foreach (var guid in Selection.assetGUIDs)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(p) && !_folders.Contains(p)) _folders.Add(p);
        }
    }

    private static string ToAssetsRelative(string absolute)
    {
        absolute = absolute.Replace('\\', '/');
        string data = Application.dataPath.Replace('\\', '/');
        if (absolute == data) return "Assets";
        if (absolute.StartsWith(data + "/")) return "Assets" + absolute.Substring(data.Length);
        return null;
    }

    private void Run(bool dryRun)
    {
        if (_folders.Count == 0) { _status = "Add at least one folder first."; return; }

        string[] guids = AssetDatabase.FindAssets("t:Model", _folders.ToArray());
        long beforeBytes = 0;
        int changed = 0;

        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar(dryRun ? "Scanning" : "Optimizing",
                    path, (float)i / guids.Length);

                var fi = new FileInfo(path);
                if (fi.Exists) beforeBytes += fi.Length;

                if (dryRun) continue;

                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp == null) continue;

                bool dirty = false;
                if (_setCompression && imp.meshCompression != _compression)
                { imp.meshCompression = _compression; dirty = true; }

                if (_disableReadWrite && imp.isReadable)
                { imp.isReadable = false; dirty = true; }

                if (_optimizeMesh && imp.optimizeMeshPolygons == false)
                { imp.optimizeMeshPolygons = true; imp.optimizeMeshVertices = true; dirty = true; }

                if (_stripCamerasLights && (imp.importCameras || imp.importLights))
                { imp.importCameras = false; imp.importLights = false; dirty = true; }

                if (_stripBlendShapes && imp.importBlendShapes)
                { imp.importBlendShapes = false; dirty = true; }

                if (_stripRig && imp.animationType != ModelImporterAnimationType.None)
                { imp.animationType = ModelImporterAnimationType.None; dirty = true; }

                if (dirty) { EditorUtility.SetDirty(imp); imp.SaveAndReimport(); changed++; }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        if (dryRun)
            _status = $"Scanned {guids.Length} models.\nCurrent source size: {Mb(beforeBytes)} MB.\n" +
                      "(In-build size is usually smaller than source FBX.)";
        else
            _status = $"Done. Reimported {changed} of {guids.Length} models.\n" +
                      $"Source bytes scanned: {Mb(beforeBytes)} MB.\nRebuild to see the real change.";
    }

    private static string Mb(long bytes) => (bytes / 1048576f).ToString("0.0");
}
