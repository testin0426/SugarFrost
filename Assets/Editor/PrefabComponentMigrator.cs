using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class PrefabComponentMigrator : EditorWindow
{
    private GameObject sourcePrefab;
    private GameObject targetPrefab;
    private bool includeRoot = true;
    private bool dryRun = false;
    private Vector2 scroll;
    private string lastReport = "";

    [MenuItem("Tools/Prefab/Migrate Components Between Prefabs")]
    public static void Open()
    {
        GetWindow<PrefabComponentMigrator>("Prefab Component Migrator");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Copies components from the SOURCE prefab to the matching child (by hierarchy path) " +
            "in the TARGET prefab.\n" +
            "- Missing component types are added.\n" +
            "- Existing same-type components have their values overwritten.\n" +
            "- Transforms, MeshRenderers and SkinnedMeshRenderers are skipped.",
            MessageType.Info);

        sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Source Prefab", sourcePrefab, typeof(GameObject), false);
        targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Prefab", targetPrefab, typeof(GameObject), false);
        includeRoot = EditorGUILayout.Toggle("Include Root GameObject", includeRoot);
        dryRun = EditorGUILayout.Toggle("Dry Run (no save)", dryRun);

        using (new EditorGUI.DisabledScope(sourcePrefab == null || targetPrefab == null || sourcePrefab == targetPrefab))
        {
            if (GUILayout.Button("Migrate"))
            {
                Migrate();
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

    private void Migrate()
    {
        var srcPath = AssetDatabase.GetAssetPath(sourcePrefab);
        var dstPath = AssetDatabase.GetAssetPath(targetPrefab);

        if (string.IsNullOrEmpty(srcPath) || string.IsNullOrEmpty(dstPath))
        {
            EditorUtility.DisplayDialog("Migrate Components", "Both objects must be prefab assets in the project.", "OK");
            return;
        }

        var srcRoot = PrefabUtility.LoadPrefabContents(srcPath);
        var dstRoot = PrefabUtility.LoadPrefabContents(dstPath);
        var log = new StringBuilder();
        var addedCount = 0;
        var overwrittenCount = 0;
        var skippedNoMatch = 0;

        try
        {
            var srcMap = BuildPathMap(srcRoot, includeRoot);
            var dstMap = BuildPathMap(dstRoot, includeRoot);

            foreach (var kvp in srcMap)
            {
                var path = kvp.Key;
                var srcGo = kvp.Value;

                if (!dstMap.TryGetValue(path, out var dstGo))
                {
                    skippedNoMatch++;
                    log.AppendLine($"[no match] {path}");
                    continue;
                }

                foreach (var srcComp in srcGo.GetComponents<Component>())
                {
                    if (srcComp == null) continue; // missing script
                    if (srcComp is Transform) continue;
                    if (srcComp is MeshRenderer) continue;
                    if (srcComp is SkinnedMeshRenderer) continue;

                    var compType = srcComp.GetType();
                    var existing = dstGo.GetComponent(compType);

                    if (!ComponentUtility.CopyComponent(srcComp))
                    {
                        log.AppendLine($"[copy failed] {path} :: {compType.Name}");
                        continue;
                    }

                    if (existing == null)
                    {
                        if (ComponentUtility.PasteComponentAsNew(dstGo))
                        {
                            addedCount++;
                            log.AppendLine($"[add]      {path} :: {compType.Name}");
                        }
                        else
                        {
                            log.AppendLine($"[add failed] {path} :: {compType.Name}");
                        }
                    }
                    else
                    {
                        if (ComponentUtility.PasteComponentValues(existing))
                        {
                            overwrittenCount++;
                            log.AppendLine($"[overwrite] {path} :: {compType.Name}");
                        }
                        else
                        {
                            log.AppendLine($"[overwrite failed] {path} :: {compType.Name}");
                        }
                    }
                }
            }

            if (!dryRun && (addedCount > 0 || overwrittenCount > 0))
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
                     $"Added: {addedCount}   Overwritten: {overwrittenCount}   Unmatched paths: {skippedNoMatch}\n" +
                     (dryRun ? "(dry run — no save)\n" : "") +
                     "----\n";
        lastReport = header + log.ToString();
        Debug.Log("[PrefabComponentMigrator] " + header.Replace("\n", " | "));
    }

    private static Dictionary<string, GameObject> BuildPathMap(GameObject root, bool includeRootNode)
    {
        var map = new Dictionary<string, GameObject>();
        if (includeRootNode)
        {
            map[""] = root;
        }
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root.transform) continue;
            var path = GetPath(root.transform, t);
            // Last writer wins on duplicate sibling names; warn via Debug.
            if (map.ContainsKey(path))
            {
                Debug.LogWarning($"[PrefabComponentMigrator] Duplicate child path '{path}' in '{root.name}'. Using last occurrence.");
            }
            map[path] = t.gameObject;
        }
        return map;
    }

    private static string GetPath(Transform root, Transform t)
    {
        var stack = new Stack<string>();
        while (t != null && t != root)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        return string.Join("/", stack.ToArray());
    }
}
