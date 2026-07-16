using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class ExposeBackgroundDepth : MonoBehaviour
{
    [SerializeField] string globalName = "_BgDepthTex";
    [SerializeField] bool debugLog = false;

    Camera cam;
    RenderTexture ownDepth;
    int frameLogged = -1;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;
        RenderPipelineManager.endCameraRendering += OnEnd;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEnd;
        if (ownDepth != null) { ownDepth.Release(); Destroy(ownDepth); ownDepth = null; }
    }

    void EnsureRT(int w, int h)
    {
        if (ownDepth != null && ownDepth.width == w && ownDepth.height == h) return;
        if (ownDepth != null) { ownDepth.Release(); Destroy(ownDepth); }
        ownDepth = new RenderTexture(w, h, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        ownDepth.name = "BgDepthCopy";
        ownDepth.filterMode = FilterMode.Point;
        ownDepth.wrapMode = TextureWrapMode.Clamp;
        ownDepth.Create();
    }

    void OnEnd(ScriptableRenderContext _, Camera c)
    {
        if (c != cam) return;
        var src = Shader.GetGlobalTexture("_CameraDepthTexture") as Texture;
        if (src == null) return;

        EnsureRT(src.width, src.height);
        Graphics.Blit(src, ownDepth);
        Shader.SetGlobalTexture(globalName, ownDepth);

        if (debugLog && Time.frameCount != frameLogged)
        {
            frameLogged = Time.frameCount;
            Debug.Log($"[ExposeBackgroundDepth] cam={cam.name} src={src.name} {src.width}x{src.height} -> ownDepth={ownDepth.width}x{ownDepth.height}");
        }
    }
}
