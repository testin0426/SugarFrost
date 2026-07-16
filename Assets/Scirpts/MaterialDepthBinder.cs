using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class MaterialDepthBinder : MonoBehaviour
{
    [Tooltip("Camera whose depth texture is sampled.")]
    public Camera sourceCamera;

    [Tooltip("Renderer that uses RTfilter with depth-edge outline. If empty, uses this GameObject's renderer.")]
    public Renderer targetRenderer;

    [Tooltip("Shader property name to receive the depth texture.")]
    public string propertyName = "_BgDepthTex";

    [SerializeField] bool debugLog = false;

    RenderTexture rt;
    MaterialPropertyBlock mpb;
    int propId;
    bool loggedOnce;

    void OnEnable()
    {
        loggedOnce = false;
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        propId = Shader.PropertyToID(propertyName);
        mpb = new MaterialPropertyBlock();
        if (sourceCamera != null)
            sourceCamera.depthTextureMode |= DepthTextureMode.Depth;
        RenderPipelineManager.endCameraRendering += OnEnd;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEnd;
        if (rt != null) { rt.Release(); DestroyImmediate(rt); rt = null; }
    }

    void EnsureRT(int w, int h)
    {
        if (rt != null && rt.width == w && rt.height == h) return;
        if (rt != null) { rt.Release(); DestroyImmediate(rt); }
        rt = new RenderTexture(w, h, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
        {
            name = "DepthCopy_" + name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        rt.Create();
    }

    void OnEnd(ScriptableRenderContext ctx, Camera c)
    {
        if (c != sourceCamera || sourceCamera == null || targetRenderer == null) return;
        var src = Shader.GetGlobalTexture("_CameraDepthTexture");
        if (src == null) return;

        EnsureRT(src.width, src.height);
        Graphics.Blit(src, rt);

        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetTexture(propId, rt);
        targetRenderer.SetPropertyBlock(mpb);

        // NOTE: OnEnd runs once per camera every frame. Logging here — even once per
        // frame — floods the console/logfile (each editor Debug.Log captures a full stack
        // trace) and can lock the main thread solid. Log exactly once per enable instead.
        if (debugLog && !loggedOnce)
        {
            loggedOnce = true;
            Debug.Log($"[MaterialDepthBinder] {name}: src='{src.name}' {src.width}x{src.height} -> renderer='{targetRenderer.name}' prop='{propertyName}'");
        }
    }
}
