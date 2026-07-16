using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TinfTools
{
    public class CubeRegionScatter : EditorWindow
    {
        // ── Region ──
        Vector3 regionCenter = Vector3.zero;
        Vector3 regionHalfExtents = new Vector3(5f, 5f, 5f);

        // ── Density ──
        int count = 20;
        float minDistance = 0f;
        int seed = 0;

        // ── Source ──
        List<GameObject> sourcePrefabs = new List<GameObject>();
        bool foldoutSources = true;

        // ── Rotation ──
        bool randomRotation = true;
        Vector3 rotMin = Vector3.zero;
        Vector3 rotMax = new Vector3(360f, 360f, 360f);

        // ── Scale ──
        bool randomScale = false;
        float scaleMin = 0.8f;
        float scaleMax = 1.2f;
        bool uniformScale = true;

        // ── Options ──
        bool parentToContainer = true;
        bool useInstantiate = true; // true = prefab instance, false = deep copy
        bool showBoundsGizmo = true;

        // ── Internal ──
        Vector2 scrollPos;
        GameObject containerGO;
        static CubeRegionScatter window;

        [MenuItem("Tools/Cube Region Scatter")]
        static void Open()
        {
            window = GetWindow<CubeRegionScatter>("Cube Scatter");
            window.minSize = new Vector2(320, 500);
            window.Show();
        }

        // ────────────────────────────────
        //  GUI
        // ────────────────────────────────
        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // ── Region ──
            EditorGUILayout.LabelField("Region", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                regionCenter = EditorGUILayout.Vector3Field("Center", regionCenter);
                regionHalfExtents = EditorGUILayout.Vector3Field("Half Extents", regionHalfExtents);
                regionHalfExtents = Vector3.Max(regionHalfExtents, Vector3.one * 0.01f);
                showBoundsGizmo = EditorGUILayout.Toggle("Show Bounds Gizmo", showBoundsGizmo);
            }

            EditorGUILayout.Space(6);

            // ── Sources ──
            foldoutSources = EditorGUILayout.Foldout(foldoutSources, "Source Prefabs / Objects", true, EditorStyles.foldoutHeader);
            if (foldoutSources)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    int removeIdx = -1;
                    for (int i = 0; i < sourcePrefabs.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        sourcePrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                            sourcePrefabs[i], typeof(GameObject), true);
                        if (GUILayout.Button("×", GUILayout.Width(22)))
                            removeIdx = i;
                        EditorGUILayout.EndHorizontal();
                    }
                    if (removeIdx >= 0) sourcePrefabs.RemoveAt(removeIdx);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+ Add Slot", GUILayout.Width(100)))
                        sourcePrefabs.Add(null);
                    if (GUILayout.Button("+ From Selection", GUILayout.Width(120)))
                    {
                        foreach (var go in Selection.gameObjects)
                        {
                            if (!sourcePrefabs.Contains(go))
                                sourcePrefabs.Add(go);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(6);

            // ── Density ──
            EditorGUILayout.LabelField("Density", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                count = Mathf.Max(1, EditorGUILayout.IntField("Count", count));
                minDistance = Mathf.Max(0f, EditorGUILayout.FloatField("Min Distance", minDistance));
                seed = EditorGUILayout.IntField("Seed (0 = random)", seed);
            }

            EditorGUILayout.Space(6);

            // ── Rotation ──
            EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                randomRotation = EditorGUILayout.Toggle("Random Rotation", randomRotation);
                if (randomRotation)
                {
                    rotMin = EditorGUILayout.Vector3Field("Min (degrees)", rotMin);
                    rotMax = EditorGUILayout.Vector3Field("Max (degrees)", rotMax);
                }
            }

            EditorGUILayout.Space(6);

            // ── Scale ──
            EditorGUILayout.LabelField("Scale", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                randomScale = EditorGUILayout.Toggle("Random Scale", randomScale);
                if (randomScale)
                {
                    uniformScale = EditorGUILayout.Toggle("Uniform", uniformScale);
                    scaleMin = EditorGUILayout.FloatField("Min", scaleMin);
                    scaleMax = EditorGUILayout.FloatField("Max", scaleMax);
                    scaleMin = Mathf.Max(0.001f, scaleMin);
                    scaleMax = Mathf.Max(scaleMin, scaleMax);
                }
            }

            EditorGUILayout.Space(6);

            // ── Options ──
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                parentToContainer = EditorGUILayout.Toggle("Parent to Container", parentToContainer);
                useInstantiate = EditorGUILayout.Toggle("Prefab Instance (vs Copy)", useInstantiate);
            }

            EditorGUILayout.Space(10);

            // ── Buttons ──
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.4f, 0.85f, 0.55f);
            if (GUILayout.Button("Scatter", GUILayout.Height(32)))
                DoScatter();
            GUI.backgroundColor = new Color(1f, 0.45f, 0.4f);
            if (GUILayout.Button("Clear", GUILayout.Height(32)))
                DoClear();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            // Force scene view repaint so gizmo updates live
            if (showBoundsGizmo)
                SceneView.RepaintAll();
        }

        // ────────────────────────────────
        //  Scatter Logic
        // ────────────────────────────────
        void DoScatter()
        {
            // Collect valid sources
            var validSources = new List<GameObject>();
            foreach (var s in sourcePrefabs)
            {
                if (s != null) validSources.Add(s);
            }
            if (validSources.Count == 0)
            {
                EditorUtility.DisplayDialog("Cube Scatter", "No source prefabs/objects assigned.", "OK");
                return;
            }

            System.Random rng = seed != 0 ? new System.Random(seed) : new System.Random();

            // Container
            Transform parent = null;
            if (parentToContainer)
            {
                containerGO = new GameObject("Scatter_Instances");
                Undo.RegisterCreatedObjectUndo(containerGO, "Scatter");
                parent = containerGO.transform;
            }

            var placed = new List<Vector3>();
            int maxAttempts = count * 20;
            int created = 0;
            int attempts = 0;

            while (created < count && attempts < maxAttempts)
            {
                attempts++;

                // Random position inside box
                float px = regionCenter.x + Lerp(rng, -regionHalfExtents.x, regionHalfExtents.x);
                float py = regionCenter.y + Lerp(rng, -regionHalfExtents.y, regionHalfExtents.y);
                float pz = regionCenter.z + Lerp(rng, -regionHalfExtents.z, regionHalfExtents.z);
                Vector3 pos = new Vector3(px, py, pz);

                // Min distance rejection
                if (minDistance > 0f)
                {
                    bool tooClose = false;
                    for (int i = 0; i < placed.Count; i++)
                    {
                        if (Vector3.Distance(pos, placed[i]) < minDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;
                }
                placed.Add(pos);

                // Pick random source
                GameObject src = validSources[rng.Next(validSources.Count)];

                // Instantiate or copy
                GameObject inst;
                if (useInstantiate)
                {
                    inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
                    // fallback if src isn't a prefab asset
                    if (inst == null)
                        inst = Instantiate(src);
                }
                else
                {
                    inst = Instantiate(src);
                }

                inst.name = src.name + "_" + created;
                inst.transform.position = pos;

                // Rotation
                if (randomRotation)
                {
                    float rx = Lerp(rng, rotMin.x, rotMax.x);
                    float ry = Lerp(rng, rotMin.y, rotMax.y);
                    float rz = Lerp(rng, rotMin.z, rotMax.z);
                    inst.transform.rotation = Quaternion.Euler(rx, ry, rz);
                }

                // Scale
                if (randomScale)
                {
                    if (uniformScale)
                    {
                        float s = Lerp(rng, scaleMin, scaleMax);
                        inst.transform.localScale = Vector3.one * s;
                    }
                    else
                    {
                        float sx = Lerp(rng, scaleMin, scaleMax);
                        float sy = Lerp(rng, scaleMin, scaleMax);
                        float sz = Lerp(rng, scaleMin, scaleMax);
                        inst.transform.localScale = new Vector3(sx, sy, sz);
                    }
                }

                if (parent != null)
                    inst.transform.SetParent(parent, true);

                Undo.RegisterCreatedObjectUndo(inst, "Scatter");
                created++;
            }

            Debug.Log($"[CubeScatter] Scattered {created} instances ({attempts} attempts)");
        }

        void DoClear()
        {
            // Find and destroy all Scatter_Instances roots
            var roots = new List<GameObject>();
            foreach (var go in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("Scatter_Instances") && go.parent == null)
                    roots.Add(go.gameObject);
            }

            if (roots.Count == 0)
            {
                EditorUtility.DisplayDialog("Cube Scatter", "No Scatter_Instances found.", "OK");
                return;
            }

            foreach (var go in roots)
                Undo.DestroyObjectImmediate(go);

            Debug.Log($"[CubeScatter] Cleared {roots.Count} scatter containers");
        }

        // ────────────────────────────────
        //  Gizmo
        // ────────────────────────────────
        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void OnSceneGUI(SceneView sv)
        {
            if (!showBoundsGizmo) return;

            Handles.color = new Color(0.2f, 0.9f, 1f, 0.7f);
            Handles.DrawWireCube(regionCenter, regionHalfExtents * 2f);

            // Draggable center handle
            EditorGUI.BeginChangeCheck();
            Vector3 newCenter = Handles.PositionHandle(regionCenter, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                regionCenter = newCenter;
                Repaint();
            }
        }

        // ────────────────────────────────
        //  Utility
        // ────────────────────────────────
        static float Lerp(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
