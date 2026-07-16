using System.Collections.Generic;
using System.IO;
using System.Linq;
using Naninovel;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Interactive replacement for the old auto-running CharacterVariantPostprocessor.
/// You drop in a character prefab, pick the armature / face mesh from dropdowns, then
/// drive each setup stage from buttons:
///   1. Util scripts  — root gets Animator + GenericCharacterBehaviour + the appearance/
///                       visibility bridges + the requested controllers; the face mesh
///                       gets BlendShapePoseBaker. Root transform + armature offset applied.
///   2. Materials     — assigns matching .mat assets (+ alpha maps) onto the model's slots.
///   3. Layer/Camera  — creates a per-character layer, moves all the model's meshes onto it,
///                       parents the camera/plane rig, builds a 2048² RenderTexture named after
///                       the character, clones the RTfilter material, and culls the camera to
///                       only this character's layer.
/// Naninovel registration is intentionally NOT done here — you register the saved prefab
/// in Naninovel's character config yourself.
/// </summary>
public class CharacterSetupTool : EditorWindow
{
    // ---- Source asset GUIDs (carried over from CharacterVariantPostprocessor) ----
    const string DefaultRigGuid = "703ebea5f57256a4aaf7c89c2e8271c5";    // characterutils.prefab (camera + plane)
    const string DefaultMatGuid = "0eebcc3f541193046ad87de23a6f2ec9";    // RTfilter material template

    // ---- User input ----
    GameObject sourcePrefab;
    string characterName = "";

    bool addMouseLook = true;
    bool addBlendShapeBaker = true;     // the "blendshape (mouth)" component — goes on the face mesh
    bool addMaterialEffect = true;

    DefaultAsset materialFolder;        // optional: folder containing the model's .mat assets
    DefaultAsset texturesFolder;        // optional: folder for alpha-map fallback / manifest

    GameObject rigPrefab;               // camera + plane rig instantiated under the root
    Material sourceMaterial;            // RTfilter material template (cloned per character)
    int rtSize = 2048;

    // Root / armature transform setup (defaults match the legacy pipeline)
    float rootRotationY = 180f;
    float rootScale = 7f;
    Vector2 armatureXZ = new Vector2(10f, 20f);

    string savePath = "";

    // ---- Working state ----
    GameObject workingInstance;         // the scene instance we operate on

    readonly List<Transform> hierTransforms = new();
    string[] hierNames = System.Array.Empty<string>();
    int armatureIdx, eyeLIdx, eyeRIdx;

    readonly List<SkinnedMeshRenderer> faceCandidates = new();
    string[] faceNames = System.Array.Empty<string>();
    int faceIdx;

    Vector2 scroll;

    // ---- Material-assignment config (ported verbatim from the postprocessor) ----
    static readonly Dictionary<string, string> KeywordOverride = new()
    {
        { "EyeHighlight", "eyeline" },
    };
    static readonly string[] AlphaSuffixes = { "_alpha", "_Alpha", "_a", "_opacity", "_Opacity", "_mask", "_Mask" };
    static readonly string[] TextureExtensions = { ".png", ".tga", ".jpg", ".jpeg", ".exr", ".tif", ".tiff", ".psd" };
    const string AlphaManifestName = "_alpha_map.json";

    [System.Serializable] class AlphaManifestEntry { public string material; public string alpha; }
    [System.Serializable] class AlphaManifest { public AlphaManifestEntry[] entries; }

    [MenuItem("Tools/Character Setup")]
    static void Open ()
    {
        var w = GetWindow<CharacterSetupTool>("Character Setup");
        w.minSize = new Vector2(380, 560);
        w.ResolveDefaults();
    }

    void OnEnable () => ResolveDefaults();

    void ResolveDefaults ()
    {
        if (rigPrefab == null)
        {
            var p = AssetDatabase.GUIDToAssetPath(DefaultRigGuid);
            if (!string.IsNullOrEmpty(p)) rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
        }
        if (sourceMaterial == null)
        {
            var p = AssetDatabase.GUIDToAssetPath(DefaultMatGuid);
            if (!string.IsNullOrEmpty(p)) sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(p);
        }
        if (backPrefab == null)
            backPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BackPrefabPath);
    }

    // ===================================================================================
    //  GUI
    // ===================================================================================
    static readonly string[] TabLabels = { "Character", "CG / Background" };
    int tab;

    void OnGUI ()
    {
        tab = GUILayout.Toolbar(tab, TabLabels);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (tab == 0) DrawCharacterTab();
        else DrawCgBackgroundTab();
        EditorGUILayout.EndScrollView();
    }

    void DrawCharacterTab ()
    {
        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Character Prefab", sourcePrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck() && sourcePrefab != null && string.IsNullOrEmpty(characterName))
            characterName = sourcePrefab.name;

        characterName = EditorGUILayout.TextField("Character Name", characterName);
        EditorGUILayout.HelpBox("Name drives the layer, RenderTexture and material asset names.", MessageType.None);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(sourcePrefab == null))
            if (GUILayout.Button("① Instantiate Prefab in Scene"))
                InstantiateWorkingInstance();

        if (workingInstance != null)
            EditorGUILayout.HelpBox($"Working on scene instance: {workingInstance.name}", MessageType.Info);
        else
        {
            EditorGUILayout.HelpBox("Instantiate the prefab to begin. All steps operate on the scene instance, then you save it back as a prefab.", MessageType.Warning);
            return;
        }

        // ---- Hierarchy pickers ----
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hierarchy", EditorStyles.boldLabel);
        if (GUILayout.Button("Refresh dropdowns")) RefreshHierarchyLists();
        if (hierNames.Length == 0) RefreshHierarchyLists();

        armatureIdx = EditorGUILayout.Popup("Armature", armatureIdx, hierNames);
        eyeLIdx = EditorGUILayout.Popup("Eye Bone L (opt)", eyeLIdx, WithNone(hierNames));
        eyeRIdx = EditorGUILayout.Popup("Eye Bone R (opt)", eyeRIdx, WithNone(hierNames));
        faceIdx = EditorGUILayout.Popup("Face Mesh (blendshapes)", faceIdx, faceNames);

        // ---- Util scripts ----
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Util Scripts", EditorStyles.boldLabel);
        addMouseLook = EditorGUILayout.ToggleLeft("MouseLookController (root)", addMouseLook);
        addBlendShapeBaker = EditorGUILayout.ToggleLeft("BlendShapePoseBaker (face mesh)", addBlendShapeBaker);
        addMaterialEffect = EditorGUILayout.ToggleLeft("CharacterMaterialEffectBridge (root)", addMaterialEffect);

        EditorGUILayout.LabelField("Root transform", EditorStyles.miniBoldLabel);
        rootRotationY = EditorGUILayout.FloatField("Rotation Y", rootRotationY);
        rootScale = EditorGUILayout.FloatField("Uniform Scale", rootScale);
        armatureXZ = EditorGUILayout.Vector2Field("Armature local X / Z", armatureXZ);

        if (GUILayout.Button("② Setup Util Scripts"))
            SetupUtilScripts();

        // ---- Materials ----
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
        materialFolder = (DefaultAsset)EditorGUILayout.ObjectField("Material Folder", materialFolder, typeof(DefaultAsset), false);
        texturesFolder = (DefaultAsset)EditorGUILayout.ObjectField("Textures Folder (opt)", texturesFolder, typeof(DefaultAsset), false);
        using (new EditorGUI.DisabledScope(materialFolder == null))
            if (GUILayout.Button("③ Assign Materials"))
                AssignMaterialsStep();

        // ---- Layer / Camera / RT ----
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Layer · Camera · RenderTexture", EditorStyles.boldLabel);
        rigPrefab = (GameObject)EditorGUILayout.ObjectField("Camera/Plane Rig", rigPrefab, typeof(GameObject), false);
        sourceMaterial = (Material)EditorGUILayout.ObjectField("RTfilter Material Template", sourceMaterial, typeof(Material), false);
        rtSize = EditorGUILayout.IntField("Output Texture Size", rtSize);
        if (GUILayout.Button("④ Setup Layer + Camera + RT"))
            SetupLayerCameraRT();

        // ---- Run all + save ----
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);
        if (GUILayout.Button("▶ Run Steps ②–④"))
        {
            SetupUtilScripts();
            if (materialFolder != null) AssignMaterialsStep();
            SetupLayerCameraRT();
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        savePath = EditorGUILayout.TextField("Save Path", savePath);
        if (GUILayout.Button("…", GUILayout.Width(28)))
        {
            var picked = EditorUtility.SaveFilePanelInProject("Save Character Prefab", characterName, "prefab", "Choose where to save the character prefab");
            if (!string.IsNullOrEmpty(picked)) savePath = picked;
        }
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("💾 Save as Prefab"))
            SaveAsPrefab();
    }

    // ===================================================================================
    //  CG / Background tab
    // ===================================================================================
    enum AssetMode { CG, Background }
    AssetMode assetMode = AssetMode.CG;
    const string BackPrefabPath = "Assets/Backgrounds/back.prefab";
    const string CgLayerName = "CG";

    GameObject cgSourcePrefab;     // source character prefab carrying the correct materials
    GameObject cgTargetPrefab;     // target CG prefab to receive them
    GameObject backPrefab;         // Assets/Backgrounds/back.prefab
    GameObject cgInstance;         // working scene instance (CG target, or back.prefab)
    string cgSavePath = "";

    void DrawCgBackgroundTab ()
    {
        assetMode = (AssetMode)GUILayout.Toolbar((int)assetMode, new[] { "CG", "Background" });
        EditorGUILayout.Space();

        if (assetMode == AssetMode.CG) DrawCgMode();
        else DrawBackgroundMode();
    }

    void DrawCgMode ()
    {
        EditorGUILayout.LabelField("CG — copy materials from a character", EditorStyles.boldLabel);
        cgSourcePrefab = (GameObject)EditorGUILayout.ObjectField("Source Character", cgSourcePrefab, typeof(GameObject), false);
        cgTargetPrefab = (GameObject)EditorGUILayout.ObjectField("Target CG Prefab", cgTargetPrefab, typeof(GameObject), false);
        EditorGUILayout.HelpBox("Materials are copied from the source onto the target by matching mesh GameObject name + slot index.", MessageType.None);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(cgTargetPrefab == null))
            if (GUILayout.Button("① Instantiate Target in Scene"))
                InstantiateCgInstance();

        if (cgInstance == null)
        {
            EditorGUILayout.HelpBox("Instantiate the target CG prefab to begin.", MessageType.Warning);
            return;
        }
        EditorGUILayout.HelpBox($"Working on: {cgInstance.name}", MessageType.Info);

        using (new EditorGUI.DisabledScope(cgSourcePrefab == null))
            if (GUILayout.Button("② Copy Materials from Source"))
                CopyMaterialsByMeshName();
        if (GUILayout.Button($"③ Move Meshes to '{CgLayerName}' Layer"))
            MoveCgMeshesToLayer();
        using (new EditorGUI.DisabledScope(cgSourcePrefab == null))
            if (GUILayout.Button("▶ Run ②–③"))
            {
                CopyMaterialsByMeshName();
                MoveCgMeshesToLayer();
            }

        EditorGUILayout.Space();
        DrawSaveRow(ref cgSavePath, cgInstance);
    }

    void DrawBackgroundMode ()
    {
        EditorGUILayout.LabelField("Background — add back.prefab", EditorStyles.boldLabel);
        if (backPrefab == null) ResolveDefaults();
        backPrefab = (GameObject)EditorGUILayout.ObjectField("Background Prefab", backPrefab, typeof(GameObject), false);
        EditorGUILayout.HelpBox($"Adds {BackPrefabPath} (a textured background quad) to the scene. Nothing else.", MessageType.None);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(backPrefab == null))
            if (GUILayout.Button("Add back.prefab to Scene"))
            {
                if (cgInstance != null) DestroyImmediate(cgInstance);
                cgInstance = (GameObject)PrefabUtility.InstantiatePrefab(backPrefab);
                Undo.RegisterCreatedObjectUndo(cgInstance, "Add Background");
                Selection.activeGameObject = cgInstance;
                SceneView.FrameLastActiveSceneView();
            }

        if (cgInstance != null)
        {
            EditorGUILayout.Space();
            DrawSaveRow(ref cgSavePath, cgInstance);
        }
    }

    void DrawSaveRow (ref string path, GameObject instance)
    {
        EditorGUILayout.BeginHorizontal();
        path = EditorGUILayout.TextField("Save Path", path);
        if (GUILayout.Button("…", GUILayout.Width(28)))
        {
            var picked = EditorUtility.SaveFilePanelInProject("Save Prefab", instance.name, "prefab", "Choose where to save");
            if (!string.IsNullOrEmpty(picked)) path = picked;
        }
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("💾 Save as Prefab"))
        {
            var p = path;
            if (string.IsNullOrEmpty(p))
                p = EditorUtility.SaveFilePanelInProject("Save Prefab", instance.name, "prefab", "Choose where to save");
            if (string.IsNullOrEmpty(p)) return;
            path = p;
            var saved = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, p, InteractionMode.UserAction);
            Debug.Log(saved != null ? $"[CharacterSetup] Saved {p}." : "[CharacterSetup] Save failed.");
        }
    }

    void InstantiateCgInstance ()
    {
        if (cgInstance != null) DestroyImmediate(cgInstance);
        cgInstance = (GameObject)PrefabUtility.InstantiatePrefab(cgTargetPrefab);
        Undo.RegisterCreatedObjectUndo(cgInstance, "Instantiate CG");
        Selection.activeGameObject = cgInstance;
        SceneView.FrameLastActiveSceneView();
    }

    /// Copies sharedMaterials from the source character prefab onto the target instance,
    /// matching renderers by GameObject name and assigning per matching slot index.
    void CopyMaterialsByMeshName ()
    {
        if (cgInstance == null || cgSourcePrefab == null) return;
        Undo.RegisterFullObjectHierarchyUndo(cgInstance, "Copy CG Materials");

        // Map source renderers by mesh GameObject name. Duplicate names are a real risk in
        // rigged meshes, so warn and keep the first — the user can rename if it matters.
        var srcByName = new Dictionary<string, Renderer>();
        foreach (var r in cgSourcePrefab.GetComponentsInChildren<Renderer>(true))
        {
            if (!srcByName.ContainsKey(r.gameObject.name)) srcByName[r.gameObject.name] = r;
            else Debug.LogWarning($"[CharacterSetup] Source has duplicate mesh name '{r.gameObject.name}'; using the first.");
        }

        int matched = 0, missed = 0;
        foreach (var tr in cgInstance.GetComponentsInChildren<Renderer>(true))
        {
            if (!srcByName.TryGetValue(tr.gameObject.name, out var sr))
            {
                missed++;
                Debug.LogWarning($"[CharacterSetup] No source mesh named '{tr.gameObject.name}'; skipped.");
                continue;
            }
            var sm = sr.sharedMaterials;
            var tm = tr.sharedMaterials;
            int n = Mathf.Min(sm.Length, tm.Length);
            for (int i = 0; i < n; i++) tm[i] = sm[i];
            tr.sharedMaterials = tm;
            matched++;
            if (sm.Length != tm.Length)
                Debug.LogWarning($"[CharacterSetup] '{tr.gameObject.name}' slot count differs (src {sm.Length}, dst {tm.Length}); copied first {n}.");
        }
        Debug.Log($"[CharacterSetup] CG materials copied: {matched} meshes matched, {missed} unmatched.");
    }

    void MoveCgMeshesToLayer ()
    {
        if (cgInstance == null) return;
        Undo.RegisterFullObjectHierarchyUndo(cgInstance, "Move CG Meshes to Layer");
        int layer = FindOrCreateLayer(CgLayerName);
        if (layer < 0) { Debug.LogError("[CharacterSetup] No free layer for 'CG'."); return; }
        int count = 0;
        foreach (var r in cgInstance.GetComponentsInChildren<Renderer>(true))
            if (r is MeshRenderer || r is SkinnedMeshRenderer) { r.gameObject.layer = layer; count++; }
        Debug.Log($"[CharacterSetup] Moved {count} meshes to layer '{CgLayerName}' (#{layer}).");
    }

    static string[] WithNone (string[] names)
    {
        var arr = new string[names.Length + 1];
        arr[0] = "(none)";
        System.Array.Copy(names, 0, arr, 1, names.Length);
        return arr;
    }

    // ===================================================================================
    //  Working instance + hierarchy
    // ===================================================================================
    void InstantiateWorkingInstance ()
    {
        if (workingInstance != null) DestroyImmediate(workingInstance);
        workingInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
        workingInstance.name = string.IsNullOrEmpty(characterName) ? sourcePrefab.name : characterName;
        Undo.RegisterCreatedObjectUndo(workingInstance, "Instantiate Character");
        Selection.activeGameObject = workingInstance;
        SceneView.FrameLastActiveSceneView();
        RefreshHierarchyLists();
    }

    void RefreshHierarchyLists ()
    {
        hierTransforms.Clear();
        faceCandidates.Clear();
        if (workingInstance == null)
        {
            hierNames = System.Array.Empty<string>();
            faceNames = System.Array.Empty<string>();
            return;
        }

        foreach (var t in workingInstance.GetComponentsInChildren<Transform>(true))
            hierTransforms.Add(t);
        hierNames = hierTransforms.Select(t => PathOf(t)).ToArray();

        foreach (var smr in workingInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            faceCandidates.Add(smr);
        // Surface blendshape-bearing meshes first so the likely face mesh is the default.
        faceCandidates.Sort((a, b) => BlendCount(b).CompareTo(BlendCount(a)));
        faceNames = faceCandidates.Select(s => $"{PathOf(s.transform)} ({BlendCount(s)} shapes)").ToArray();

        // Best-effort default selections.
        armatureIdx = Mathf.Max(0, hierTransforms.FindIndex(t => t.name.IndexOf("armature", System.StringComparison.OrdinalIgnoreCase) >= 0));
        faceIdx = 0;
    }

    static int BlendCount (SkinnedMeshRenderer s) => s != null && s.sharedMesh != null ? s.sharedMesh.blendShapeCount : 0;

    string PathOf (Transform t) => workingInstance != null ? AnimationUtility.CalculateTransformPath(t, workingInstance.transform) : t.name;

    Transform SelectedArmature => Valid(armatureIdx, hierTransforms) ? hierTransforms[armatureIdx] : null;
    Transform SelectedEyeL => eyeLIdx > 0 && Valid(eyeLIdx - 1, hierTransforms) ? hierTransforms[eyeLIdx - 1] : null;
    Transform SelectedEyeR => eyeRIdx > 0 && Valid(eyeRIdx - 1, hierTransforms) ? hierTransforms[eyeRIdx - 1] : null;
    SkinnedMeshRenderer SelectedFace => Valid(faceIdx, faceCandidates) ? faceCandidates[faceIdx] : null;

    static bool Valid<T> (int i, List<T> list) => i >= 0 && i < list.Count;

    // ===================================================================================
    //  Step ②  — util scripts + root transform
    // ===================================================================================
    void SetupUtilScripts ()
    {
        if (workingInstance == null) return;
        Undo.RegisterFullObjectHierarchyUndo(workingInstance, "Setup Util Scripts");

        var root = workingInstance;

        // Always-on Naninovel-side components.
        AddIfMissing<Animator>(root);
        AddIfMissing<GenericCharacterBehaviour>(root);
        AddIfMissing<CharacterAppearanceBridge>(root);
        AddIfMissing<VisibilityFadeBridge>(root);

        if (addMaterialEffect) AddIfMissing<CharacterMaterialEffectBridge>(root);

        if (addMouseLook)
        {
            var look = AddIfMissing<MouseLookController>(root);
            var so = new SerializedObject(look);
            SetObjRef(so, "armature", SelectedArmature);
            if (SelectedEyeL != null) SetObjRef(so, "eyeBoneL", SelectedEyeL);
            if (SelectedEyeR != null) SetObjRef(so, "eyeBoneR", SelectedEyeR);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        if (addBlendShapeBaker)
        {
            var face = SelectedFace;
            if (face != null)
            {
                var baker = AddIfMissing<BlendShapePoseBaker>(face.gameObject);
                var so = new SerializedObject(baker);
                var animProp = so.FindProperty("rootAnimator");
                if (animProp != null) animProp.objectReferenceValue = root.GetComponent<Animator>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[CharacterSetup] No face mesh selected; skipping BlendShapePoseBaker.");
        }

        // Root transform + armature offset.
        var rt = root.transform;
        rt.localScale = Vector3.one * rootScale;
        var e = rt.localEulerAngles; e.y = rootRotationY; rt.localEulerAngles = e;

        var arm = SelectedArmature;
        if (arm != null)
        {
            var p = arm.localPosition;
            arm.localPosition = new Vector3(armatureXZ.x, p.y, armatureXZ.y);
        }

        Debug.Log("[CharacterSetup] Util scripts + root transform applied.");
    }

    static void SetObjRef (SerializedObject so, string field, Object value)
    {
        var p = so.FindProperty(field);
        if (p != null) p.objectReferenceValue = value;
        else Debug.LogWarning($"[CharacterSetup] Field '{field}' not found on {so.targetObject.GetType().Name}.");
    }

    // ===================================================================================
    //  Step ③  — material assignment (ported from the postprocessor)
    // ===================================================================================
    void AssignMaterialsStep ()
    {
        if (workingInstance == null || materialFolder == null) return;
        Undo.RegisterFullObjectHierarchyUndo(workingInstance, "Assign Materials");

        string matDir = AssetDatabase.GetAssetPath(materialFolder);
        if (!AssetDatabase.IsValidFolder(matDir)) { Debug.LogError("[CharacterSetup] Material Folder is not a valid folder."); return; }
        string texDir = texturesFolder != null ? AssetDatabase.GetAssetPath(texturesFolder) : null;
        if (texDir != null && !AssetDatabase.IsValidFolder(texDir)) texDir = null;

        var nameToMat = LoadMaterials(matDir);
        var manifest = LoadAlphaManifest(texDir);
        AssignMaterials(workingInstance, nameToMat, texDir, manifest);
        Debug.Log($"[CharacterSetup] Materials assigned from {matDir}.");
    }

    // ===================================================================================
    //  Step ④  — layer + camera + RenderTexture
    // ===================================================================================
    void SetupLayerCameraRT ()
    {
        if (workingInstance == null) return;
        if (string.IsNullOrEmpty(characterName)) { Debug.LogError("[CharacterSetup] Set a Character Name first."); return; }
        Undo.RegisterFullObjectHierarchyUndo(workingInstance, "Setup Layer/Camera/RT");

        // 1) Per-character layer.
        int layer = FindOrCreateLayer(characterName);
        if (layer < 0)
        {
            Debug.LogError("[CharacterSetup] No free user layer slots (max 32 reached). Free a layer and retry.");
            return;
        }

        // 2) Move all the model's meshes onto that layer. (Done BEFORE the rig is parented so the
        //    rig's display plane stays on the default layer and remains visible to the main camera.)
        foreach (var r in workingInstance.GetComponentsInChildren<Renderer>(true))
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
                r.gameObject.layer = layer;

        // 3) Create the output RenderTexture and clone the RTfilter material.
        string dir = !string.IsNullOrEmpty(savePath) ? Path.GetDirectoryName(savePath).Replace('\\', '/')
                   : PrefabStageDir() ?? "Assets";
        string rtPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{characterName}.renderTexture");
        var rt = new RenderTexture(rtSize, rtSize, 24) { name = characterName };
        AssetDatabase.CreateAsset(rt, rtPath);

        Material mat = null;
        if (sourceMaterial != null)
        {
            string srcMatPath = AssetDatabase.GetAssetPath(sourceMaterial);
            string matPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{characterName}.mat");
            if (AssetDatabase.CopyAsset(srcMatPath, matPath))
                mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        }
        if (mat == null) Debug.LogWarning("[CharacterSetup] No RTfilter material template; plane material left unset.");
        else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", rt);
        if (mat != null) EditorUtility.SetDirty(mat);

        // 4) Parent the camera/plane rig under the root.
        if (rigPrefab == null) { Debug.LogError("[CharacterSetup] No Camera/Plane rig assigned."); return; }
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, workingInstance.transform);
        rig.transform.localPosition = Vector3.zero;

        // Camera: render only this character's layer into the RT.
        var cam = rig.GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            cam.targetTexture = rt;
            cam.cullingMask = 1 << layer;
        }
        else Debug.LogWarning("[CharacterSetup] Rig has no Camera.");

        // Plane: show the RT via the cloned RTfilter material.
        var plane = FindChildRecursive(rig.transform, "plane");
        Renderer planeRenderer = null;
        if (plane != null)
        {
            planeRenderer = plane.GetComponent<MeshRenderer>();
            if (planeRenderer != null && mat != null)
            {
                var mats = planeRenderer.sharedMaterials;
                if (mats.Length > 0) mats[0] = mat; else mats = new[] { mat };
                planeRenderer.sharedMaterials = mats;
            }
        }
        else Debug.LogWarning("[CharacterSetup] Rig has no 'plane' child.");

        // 5) Wire VisibilityFadeBridge to the plane (it must start inactive).
        var fade = workingInstance.GetComponent<VisibilityFadeBridge>();
        if (fade != null && plane != null)
        {
            var so = new SerializedObject(fade);
            SetObjRef(so, "plane", plane.gameObject);
            if (planeRenderer != null) SetObjRef(so, "planeRenderer", planeRenderer);
            so.ApplyModifiedPropertiesWithoutUndo();
            plane.gameObject.SetActive(false);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[CharacterSetup] Layer '{characterName}' (#{layer}) + camera + {rtSize}² RT created.");
    }

    static string PrefabStageDir ()
    {
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        return stage != null ? Path.GetDirectoryName(stage.assetPath).Replace('\\', '/') : null;
    }

    /// Returns the index of an existing user layer with this name, or creates one in the first
    /// free slot (8..31). Returns -1 if all user layers are taken.
    static int FindOrCreateLayer (string layerName)
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        var tagManager = new SerializedObject(asset);
        var layers = tagManager.FindProperty("layers");

        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                return i;

        for (int i = 8; i < layers.arraySize; i++)
        {
            var el = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(el.stringValue))
            {
                el.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }
        return -1;
    }

    // ===================================================================================
    //  Save
    // ===================================================================================
    void SaveAsPrefab ()
    {
        if (workingInstance == null) return;
        if (string.IsNullOrEmpty(savePath))
        {
            savePath = EditorUtility.SaveFilePanelInProject("Save Character Prefab", characterName, "prefab", "Choose where to save the character prefab");
            if (string.IsNullOrEmpty(savePath)) return;
        }
        var saved = PrefabUtility.SaveAsPrefabAssetAndConnect(workingInstance, savePath, InteractionMode.UserAction);
        if (saved != null) Debug.Log($"[CharacterSetup] Saved prefab to {savePath}. Register it in Naninovel's character config to finish.");
        else Debug.LogError("[CharacterSetup] Failed to save prefab.");
    }

    // ===================================================================================
    //  Material assignment helpers (ported from CharacterVariantPostprocessor)
    // ===================================================================================
    static Dictionary<string, Material> LoadMaterials (string materialDir)
    {
        var dict = new Dictionary<string, Material>();
        foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { materialDir }))
        {
            string p = AssetDatabase.GUIDToAssetPath(g).Replace('\\', '/');
            if (Path.GetDirectoryName(p).Replace('\\', '/') != materialDir) continue;
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m != null) dict[Path.GetFileNameWithoutExtension(p)] = m;
        }
        return dict;
    }

    static void AssignMaterials (GameObject root, Dictionary<string, Material> nameToMat,
        string texturesDir, Dictionary<string, string> alphaManifest)
    {
        var alphaPassDone = new HashSet<(Material, string)>();

        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                Material chosen = null;
                string slotName = mats[i].name;

                if (nameToMat.TryGetValue(slotName, out var exact)) chosen = exact;

                foreach (var kv in KeywordOverride)
                {
                    if (slotName.IndexOf(kv.Key, System.StringComparison.OrdinalIgnoreCase) >= 0
                        && nameToMat.TryGetValue(kv.Value, out var kw))
                    {
                        chosen = kw;
                        break;
                    }
                }

                if (chosen != null && chosen != mats[i]) { mats[i] = chosen; changed = true; }

                if (chosen != null && alphaPassDone.Add((chosen, slotName)))
                    TryAssignAlphaMap(chosen, slotName, texturesDir, alphaManifest);
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    static void TryAssignAlphaMap (Material mat, string slotName, string texturesDir, Dictionary<string, string> manifest)
    {
        if (mat == null || !mat.HasProperty("_AlphaMap")) return;

        Texture2D alphaTex = null;
        string source = null;

        if (manifest != null && !string.IsNullOrEmpty(slotName) && !string.IsNullOrEmpty(texturesDir)
            && manifest.TryGetValue(slotName, out var fname) && !string.IsNullOrEmpty(fname))
        {
            string candidate = $"{texturesDir}/{fname}";
            alphaTex = AssetDatabase.LoadAssetAtPath<Texture2D>(candidate);
            if (alphaTex != null) source = candidate;
            else Debug.LogWarning($"[CharacterSetup] Alpha manifest references missing file '{candidate}' for slot '{slotName}'.");
        }

        if (alphaTex == null)
        {
            if (!mat.HasProperty("_BaseMap")) return;
            var baseTex = mat.GetTexture("_BaseMap") as Texture2D;
            if (baseTex == null) return;
            string basePath = AssetDatabase.GetAssetPath(baseTex);
            if (string.IsNullOrEmpty(basePath)) return;
            string baseDir = Path.GetDirectoryName(basePath).Replace('\\', '/');
            string baseStem = Path.GetFileNameWithoutExtension(basePath);
            alphaTex = FindAlphaTextureBySuffix(baseDir, baseStem) ?? FindAlphaTextureBySuffix(texturesDir, baseStem);
            if (alphaTex != null) source = AssetDatabase.GetAssetPath(alphaTex);
        }

        if (alphaTex == null) return;

        var baseRef = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
        if (alphaTex == baseRef) return;

        bool changed = false;
        if (mat.GetTexture("_AlphaMap") != alphaTex) { mat.SetTexture("_AlphaMap", alphaTex); changed = true; }
        if (mat.HasProperty("_UseAlphaMap") && mat.GetFloat("_UseAlphaMap") < 0.5f)
        {
            mat.SetFloat("_UseAlphaMap", 1f);
            mat.EnableKeyword("_USEALPHAMAP");
            changed = true;
        }
        if (changed)
        {
            EditorUtility.SetDirty(mat);
            Debug.Log($"[CharacterSetup] Alpha map -> {mat.name} (slot '{slotName}'): {source}");
        }
    }

    static Texture2D FindAlphaTextureBySuffix (string dir, string stem)
    {
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(stem)) return null;
        foreach (var suffix in AlphaSuffixes)
            foreach (var ext in TextureExtensions)
            {
                string candidate = $"{dir}/{stem}{suffix}{ext}";
                if (File.Exists(candidate))
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(candidate);
                    if (tex != null) return tex;
                }
            }
        return null;
    }

    static Dictionary<string, string> LoadAlphaManifest (string texturesDir)
    {
        if (string.IsNullOrEmpty(texturesDir)) return null;
        string path = $"{texturesDir}/{AlphaManifestName}";
        if (!File.Exists(path)) return null;
        try
        {
            var parsed = JsonUtility.FromJson<AlphaManifest>(File.ReadAllText(path));
            if (parsed?.entries == null) return null;
            var dict = new Dictionary<string, string>(parsed.entries.Length);
            foreach (var e in parsed.entries)
            {
                if (e == null || string.IsNullOrEmpty(e.material) || string.IsNullOrEmpty(e.alpha)) continue;
                dict[e.material] = e.alpha;
            }
            return dict;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[CharacterSetup] Failed to parse alpha manifest at {path}: {ex.Message}");
            return null;
        }
    }

    // ===================================================================================
    //  Small helpers
    // ===================================================================================
    static T AddIfMissing<T> (GameObject go) where T : Component
    {
        var existing = go.GetComponent<T>();
        return existing != null ? existing : go.AddComponent<T>();
    }

    static Transform FindChildRecursive (Transform t, string name)
    {
        if (string.Equals(t.name, name, System.StringComparison.OrdinalIgnoreCase)) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var found = FindChildRecursive(t.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
