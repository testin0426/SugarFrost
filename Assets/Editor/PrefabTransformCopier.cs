using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Matches GameObjects between two prefabs by name and copies the SOURCE
/// Transform values (local position / rotation / scale) onto the matching
/// GameObject in the TARGET prefab.
/// Companion to <see cref="PrefabComponentMigrator"/>, which copies everything
/// EXCEPT Transforms and matches by hierarchy path instead of name.
/// </summary>
public class PrefabTransformCopier : EditorWindow
{
    private GameObject sourcePrefab;
    private GameObject targetPrefab;
    private bool directChildrenOnly = true;
    private bool includeRoot = false;
    private bool copyPosition = true;
    private bool copyRotation = true;
    private bool copyScale = true;
    private bool dryRun = false;
    private Vector2 scroll;
    private string lastReport = "";

    [MenuItem("Tools/Prefab/Copy Transforms By Name")]
    public static void Open()
    {
        GetWindow<PrefabTransformCopier>("Prefab Transform Copier");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "For each GameObject in the SOURCE prefab, finds the GameObject with the SAME NAME " +
            "in the TARGET prefab and copies the selected Transform values onto it.\n" +
            "- 'Direct Children Only' processes just the root's first-level children " +
            "(so deep bone transforms are NOT touched).\n" +
            "- Duplicate names in the target are skipped (ambiguous) and reported.\n" +
            "- Names present only in source or only in target are reported.",
            MessageType.Info);

        sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Source Prefab", sourcePrefab, typeof(GameObject), false);
        targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Prefab", targetPrefab, typeof(GameObject), false);

        directChildrenOnly = EditorGUILayout.Toggle("Direct Children Only", directChildrenOnly);
        includeRoot = EditorGUILayout.Toggle("Include Root GameObject", includeRoot);
        copyPosition = EditorGUILayout.Toggle("Copy Local Position", copyPosition);
        copyRotation = EditorGUILayout.Toggle("Copy Local Rotation", copyRotation);
        copyScale = EditorGUILayout.Toggle("Copy Local Scale", copyScale);
        dryRun = EditorGUILayout.Toggle("Dry Run (no save)", dryRun);

        bool nothingToCopy = !copyPosition && !copyRotation && !copyScale;
        using (new EditorGUI.DisabledScope(sourcePrefab == null || targetPrefab == null || sourcePrefab == targetPrefab || nothingToCopy))
        {
            if (GUILayout.Button("Copy Transforms"))
            {
                Copy();
            }
        }

        if (!string.IsNullOrEmpty(lastReport))
        {
            EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(lastReport, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }

    private void Copy()
    {
        var srcPath = AssetDatabase.GetAssetPath(sourcePrefab);
        var dstPath = AssetDatabase.GetAssetPath(targetPrefab);

        if (string.IsNullOrEmpty(srcPath) || string.IsNullOrEmpty(dstPath))
        {
            EditorUtility.DisplayDialog("Copy Transforms", "Both objects must be prefab assets in the project.", "OK");
            return;
        }

        var srcRoot = PrefabUtility.LoadPrefabContents(srcPath);
        var dstRoot = PrefabUtility.LoadPrefabContents(dstPath);
        var log = new StringBuilder();
        var copied = 0;
        var noMatch = 0;
        var ambiguous = 0;

        try
        {
            var dstMap = BuildNameMap(dstRoot, includeRoot, directChildrenOnly, out var dstDuplicateNames);

            foreach (var srcT in EnumerateTransforms(srcRoot, includeRoot, directChildrenOnly))
            {
                var name = srcT.name;

                if (dstDuplicateNames.Contains(name))
                {
                    ambiguous++;
                    log.AppendLine($"[ambiguous] '{name}' appears multiple times in target — skipped");
                    continue;
                }

                if (!dstMap.TryGetValue(name, out var dstT))
                {
                    noMatch++;
                    log.AppendLine($"[no match]  '{name}' not found in target");
                    continue;
                }

                var changes = new List<string>();
                if (copyPosition && dstT.localPosition != srcT.localPosition)
                {
                    dstT.localPosition = srcT.localPosition;
                    changes.Add("pos");
                }
                if (copyRotation && dstT.localRotation != srcT.localRotation)
                {
                    dstT.localRotation = srcT.localRotation;
                    changes.Add("rot");
                }
                if (copyScale && dstT.localScale != srcT.localScale)
                {
                    dstT.localScale = srcT.localScale;
                    changes.Add("scale");
                }

                if (changes.Count > 0)
                {
                    copied++;
                    log.AppendLine($"[copy]      '{name}' :: {string.Join(",", changes)}");
                }
            }

            if (!dryRun && copied > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(dstRoot, dstPath);
                AssetDatabase.SaveAssets();
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(srcRoot);
            PrefabUtility.UnloadPrefabContents(dstRoot);
        }

        var header = $"Source: {srcPath}\nTarget: {dstPath}\n" +
                     $"Copied: {copied}   Unmatched names: {noMatch}   Ambiguous (skipped): {ambiguous}\n" +
                     (dryRun ? "(dry run — no save)\n" : "") +
                     "----\n";
        lastReport = header + log.ToString();
        Debug.Log("[PrefabTransformCopier] " + header.Replace("\n", " | "));
    }

    private static IEnumerable<Transform> EnumerateTransforms(GameObject root, bool includeRootNode, bool directChildrenOnly)
    {
        if (includeRootNode) yield return root.transform;

        if (directChildrenOnly)
        {
            foreach (Transform child in root.transform)
                yield return child;
        }
        else
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == root.transform) continue;
                yield return t;
            }
        }
    }

    private static Dictionary<string, Transform> BuildNameMap(GameObject root, bool includeRootNode, bool directChildrenOnly, out HashSet<string> duplicateNames)
    {
        var map = new Dictionary<string, Transform>();
        duplicateNames = new HashSet<string>();
        foreach (var t in EnumerateTransforms(root, includeRootNode, directChildrenOnly))
        {
            if (map.ContainsKey(t.name))
            {
                duplicateNames.Add(t.name);
            }
            else
            {
                map[t.name] = t;
            }
        }
        return map;
    }
}
