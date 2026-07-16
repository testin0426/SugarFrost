using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraDebug : MonoBehaviour
{
    void Start()
    {
        var all = Camera.allCameras;
        Debug.Log($"=== {all.Length} cameras in scene ===");
        foreach (var cam in all)
        {
            var urp = cam.GetUniversalAdditionalCameraData();
            Debug.Log($"  [{cam.name}] enabled={cam.enabled} depth={cam.depth} " +
                      $"targetTex={cam.targetTexture?.name ?? "screen"} " +
                      $"renderType={urp?.renderType} " +
                      $"baseType={urp?.renderType == CameraRenderType.Base}");
        }
    }
}
