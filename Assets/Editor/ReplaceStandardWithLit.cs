using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReplaceStandardWithLit
{
    [MenuItem("Tools/Shaders/Replace Standard With URP Lit (Folder)")]
    public static void Run()
    {
        string abs = EditorUtility.OpenFolderPanel("Pick a folder under Assets/", Application.dataPath, "");
        if (string.IsNullOrEmpty(abs)) return;

        string folder = ToAssetsRelative(abs);
        if (folder == null)
        {
            Debug.LogWarning("Folder must be inside this project's Assets/ directory.");
            return;
        }

        RunForFolder(folder);
    }

    public static void RunForFolder(string folder)
    {
        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null)
        {
            Debug.LogError("URP Lit shader not found. Make sure URP package is installed.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        if (guids.Length == 0)
        {
            Debug.LogWarning($"No prefabs found under {folder}.");
            return;
        }

        int totalReplaced = 0;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)) continue;
            totalReplaced += RunForPrefab(path, litShader);
        }

        Debug.Log($"Done. Total materials replaced: {totalReplaced}.");
    }

    public static int RunForPrefab(string prefabPath, Shader litShader = null)
    {
        if (!File.Exists(prefabPath))
        {
            Debug.LogWarning($"Prefab not found: {prefabPath}");
            return 0;
        }

        if (litShader == null)
            litShader = Shader.Find("Universal Render Pipeline/Lit");

        if (litShader == null)
        {
            Debug.LogError("URP Lit shader not found. Make sure URP package is installed.");
            return 0;
        }

        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            int replaced = 0;
            var visited = new HashSet<Material>();

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                bool dirty = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null || mat.shader == null) continue;
                    if (!visited.Add(mat)) continue;
                    if (!IsStandardShader(mat.shader)) continue;

                    ConvertToLit(mat, litShader);
                    EditorUtility.SetDirty(mat);
                    replaced++;
                    dirty = true;
                }

                if (dirty) EditorUtility.SetDirty(renderer);
            }

            if (replaced > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[Prefab] Replaced {replaced} materials in {prefabPath}.");
            return replaced;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    public static void RunForScenes(IEnumerable<string> scenePaths)
    {
        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null)
        {
            Debug.LogError("URP Lit shader not found. Make sure URP package is installed.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        int totalReplaced = 0;

        foreach (var scenePath in scenePaths)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"Scene not found: {scenePath}");
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int sceneReplaced = 0;
            var visited = new HashSet<Material>();

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = renderer.sharedMaterials;
                    bool dirty = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var mat = mats[i];
                        if (mat == null || mat.shader == null) continue;
                        if (!visited.Add(mat)) continue;
                        if (!IsStandardShader(mat.shader)) continue;

                        ConvertToLit(mat, litShader);
                        EditorUtility.SetDirty(mat);
                        sceneReplaced++;
                        dirty = true;
                    }
                    if (dirty) EditorUtility.SetDirty(renderer);
                }
            }

            if (sceneReplaced > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"[{System.IO.Path.GetFileName(scenePath)}] Replaced {sceneReplaced} materials.");
            totalReplaced += sceneReplaced;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Done. Total materials replaced: {totalReplaced}.");
    }

    private static string ToAssetsRelative(string absolute)
    {
        absolute = absolute.Replace('\\', '/');
        string data = Application.dataPath.Replace('\\', '/');
        if (absolute == data) return "Assets";
        if (absolute.StartsWith(data + "/")) return "Assets" + absolute.Substring(data.Length);
        return null;
    }

    private static bool IsStandardShader(Shader shader)
    {
        var n = shader.name;
        return n == "Standard" || n == "Standard (Specular setup)";
    }

    private static void ConvertToLit(Material mat, Shader litShader)
    {
        bool isSpecular = mat.shader.name == "Standard (Specular setup)";

        Texture albedo = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
        Vector2 albedoScale = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") : Vector2.one;
        Vector2 albedoOffset = mat.HasProperty("_MainTex") ? mat.GetTextureOffset("_MainTex") : Vector2.zero;
        Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

        Texture metallicGloss = mat.HasProperty("_MetallicGlossMap") ? mat.GetTexture("_MetallicGlossMap") : null;
        float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
        float glossiness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;

        Texture specGloss = mat.HasProperty("_SpecGlossMap") ? mat.GetTexture("_SpecGlossMap") : null;
        Color specColor = mat.HasProperty("_SpecColor") ? mat.GetColor("_SpecColor") : Color.gray;

        Texture bump = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
        float bumpScale = mat.HasProperty("_BumpScale") ? mat.GetFloat("_BumpScale") : 1f;

        Texture occlusion = mat.HasProperty("_OcclusionMap") ? mat.GetTexture("_OcclusionMap") : null;
        float occlusionStrength = mat.HasProperty("_OcclusionStrength") ? mat.GetFloat("_OcclusionStrength") : 1f;

        Texture emission = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
        Color emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;

        float mode = mat.HasProperty("_Mode") ? mat.GetFloat("_Mode") : 0f;
        float cutoff = mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.5f;

        mat.shader = litShader;

        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);
        if (mat.HasProperty("_BaseMap")) { mat.SetTextureScale("_BaseMap", albedoScale); mat.SetTextureOffset("_BaseMap", albedoOffset); }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

        if (isSpecular)
        {
            if (mat.HasProperty("_WorkflowMode")) mat.SetFloat("_WorkflowMode", 0f);
            if (mat.HasProperty("_SpecGlossMap")) mat.SetTexture("_SpecGlossMap", specGloss);
            if (mat.HasProperty("_SpecColor")) mat.SetColor("_SpecColor", specColor);
            mat.EnableKeyword("_SPECULAR_SETUP");
        }
        else
        {
            if (mat.HasProperty("_WorkflowMode")) mat.SetFloat("_WorkflowMode", 1f);
            if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", metallicGloss);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            mat.DisableKeyword("_SPECULAR_SETUP");
        }

        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", glossiness);

        if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", bump);
        if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", bumpScale);
        if (bump != null) mat.EnableKeyword("_NORMALMAP");

        if (mat.HasProperty("_OcclusionMap")) mat.SetTexture("_OcclusionMap", occlusion);
        if (mat.HasProperty("_OcclusionStrength")) mat.SetFloat("_OcclusionStrength", occlusionStrength);
        if (occlusion != null) mat.EnableKeyword("_OCCLUSIONMAP");

        if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", emission);
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
        if (emissionColor.maxColorComponent > 0f || emission != null)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        SetupSurface(mat, mode, cutoff);
    }

    private static void SetupSurface(Material mat, float standardMode, float cutoff)
    {
        // 0=Opaque, 1=Cutout, 2=Fade, 3=Transparent
        if (standardMode <= 0.5f)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }
        else if (standardMode <= 1.5f)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1f);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", cutoff);
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
        else
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
