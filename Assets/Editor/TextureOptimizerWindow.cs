using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Editor window for bulk-optimizing texture import settings.
// Open via:  Tools > Texture Optimizer
//
// Add one or more folders, tweak the target settings, hit "Scan" to see how
// much disk each folder currently uses, then "Optimize" to reimport them with
// GPU block compression (BC7/DXT) instead of uncompressed RGBA32.
public class TextureOptimizerWindow : EditorWindow
{
    private readonly List<string> _folders = new List<string>();   // Assets-relative
    private Vector2 _scroll;

    // Target settings
    private int _maxSize = 2048;
    private int _compQuality = 50;          // crunch quality 0..100
    private bool _useCrunch = true;
    private bool _capSize = true;
    private bool _skipSprites = false;      // leave Sprite-type textures untouched
    private bool _skipNormalCrunch = true;  // don't crunch normal maps

    private static readonly int[] SizeOptions = { 256, 512, 1024, 2048, 4096 };

    // Last scan / run results
    private string _status = "Add folders, then Scan or Optimize.";

    [MenuItem("Tools/Texture Optimizer")]
    private static void Open()
    {
        var w = GetWindow<TextureOptimizerWindow>("Texture Optimizer");
        w.minSize = new Vector2(460, 380);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Folders to process", EditorStyles.boldLabel);

        // --- folder list ---
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(120));
        int removeAt = -1;
        for (int i = 0; i < _folders.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(_folders[i],
                EditorStyles.textField, GUILayout.Height(18));
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

        _capSize = EditorGUILayout.BeginToggleGroup("Cap max texture size", _capSize);
        int idx = System.Array.IndexOf(SizeOptions, _maxSize);
        idx = EditorGUILayout.Popup("Max size", Mathf.Max(0, idx),
            SizeOptions.Select(s => s.ToString()).ToArray());
        _maxSize = SizeOptions[idx];
        EditorGUILayout.EndToggleGroup();

        _useCrunch = EditorGUILayout.Toggle("Crunch compression", _useCrunch);
        using (new EditorGUI.DisabledScope(!_useCrunch))
            _compQuality = EditorGUILayout.IntSlider("Crunch quality", _compQuality, 0, 100);

        _skipNormalCrunch = EditorGUILayout.Toggle("Don't crunch normal maps", _skipNormalCrunch);
        _skipSprites = EditorGUILayout.Toggle("Skip Sprite-type textures", _skipSprites);

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
        if (rel == null)
            _status = "Folder must be inside this project's Assets/ folder.";
        else if (!_folders.Contains(rel))
            _folders.Add(rel);
    }

    private void AddSelectedFolders()
    {
        foreach (var guid in Selection.assetGUIDs)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(p) && !_folders.Contains(p))
                _folders.Add(p);
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

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", _folders.ToArray());
        long beforeBytes = 0, afterEstimate = 0;
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

                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;

                bool isSprite = imp.textureType == TextureImporterType.Sprite;
                if (_skipSprites && isSprite) continue;

                bool isNormal = imp.textureType == TextureImporterType.NormalMap;

                if (dryRun) { afterEstimate += fi.Exists ? fi.Length / 6 : 0; continue; }

                bool dirty = false;
                if (imp.textureCompression != TextureImporterCompression.Compressed)
                { imp.textureCompression = TextureImporterCompression.Compressed; dirty = true; }

                if (_capSize && imp.maxTextureSize > _maxSize)
                { imp.maxTextureSize = _maxSize; dirty = true; }

                bool wantCrunch = _useCrunch && !(isNormal && _skipNormalCrunch);
                if (imp.crunchedCompression != wantCrunch)
                { imp.crunchedCompression = wantCrunch; dirty = true; }
                if (wantCrunch && imp.compressionQuality != _compQuality)
                { imp.compressionQuality = _compQuality; dirty = true; }

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
            _status = $"Scanned {guids.Length} textures.\n" +
                      $"Current source size: {Mb(beforeBytes)} MB\n" +
                      $"Rough optimized estimate: ~{Mb(afterEstimate)} MB (very approximate).";
        else
            _status = $"Done. Reimported {changed} of {guids.Length} textures.\n" +
                      $"Source bytes scanned: {Mb(beforeBytes)} MB.\n" +
                      "Rebuild to see the real build-size change.";
    }

    private static string Mb(long bytes) => (bytes / 1048576f).ToString("0.0");
}
