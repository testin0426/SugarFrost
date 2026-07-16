using UnityEngine;

/// 
/// 在 Start 时（或每帧）用 Compute Shader 生成 / 更新 3D 云噪声贴图。
///
/// Inspector 设置：
///   noiseCompute  —— 指向 CloudNoiseGen.compute
///   volumeTexture —— 预先在 Project 里创建的 RenderTexture
///                    Width=Height=Depth=64（或 128），
///                    Dimension=3D，Enable Random Write=true，
///                    Format 推荐 ARGB Float 或 ARGBHalf
///   animateNoise  —— 勾选后每帧重新 Dispatch，让噪声随 time 变化；
///                    不勾选则只在 Start 生成一次（性能友好）
/// 
public class CloudNoiseGenerator : MonoBehaviour
{
    [Header("References")]
    public ComputeShader noiseCompute;
    public RenderTexture volumeTexture;

    [Header("Animation")]
    [Tooltip("每帧重新生成噪声（更动态，但有 GPU 开销）。\n" +
             "若只需 Shader 层面的滚动动画，关闭此选项即可。")]
    public bool animateNoise = false;

    private int   _kernelIndex = -1;
    private int   _resolution  = 0;

    // ---- Unity 消息 ----

    void Start()
    {
        if (!Validate()) return;

        _kernelIndex = noiseCompute.FindKernel("CSMain");
        _resolution  = volumeTexture.width;

        noiseCompute.SetTexture(_kernelIndex, "Result",     volumeTexture);
        noiseCompute.SetInt    ("resolution", _resolution);

        Dispatch(0f);
        Debug.Log($"[CloudNoiseGenerator] 已生成 {_resolution}³ 噪声贴图。");
    }

    void Update()
    {
        if (!animateNoise || _kernelIndex < 0) return;
        Dispatch(Time.time);
    }

    // ---- 内部方法 ----

    /// 派发 Compute Shader，传入当前时间。
    void Dispatch(float t)
    {
        noiseCompute.SetFloat("time", t);

        int groups = Mathf.CeilToInt(_resolution / 8.0f);
        noiseCompute.Dispatch(_kernelIndex, groups, groups, groups);
    }

    /// 检查所有必要条件，出错时打印详细信息。
    bool Validate()
    {
        if (noiseCompute == null)
        {
            Debug.LogError("[CloudNoiseGenerator] 未指定 Compute Shader。", this);
            return false;
        }
        if (volumeTexture == null)
        {
            Debug.LogError("[CloudNoiseGenerator] 未指定 RenderTexture（volumeTexture）。", this);
            return false;
        }
        if (!volumeTexture.enableRandomWrite)
        {
            Debug.LogError("[CloudNoiseGenerator] RenderTexture 未开启 Enable Random Write。", this);
            return false;
        }
        if (volumeTexture.dimension != UnityEngine.Rendering.TextureDimension.Tex3D)
        {
            Debug.LogError("[CloudNoiseGenerator] RenderTexture 的 Dimension 必须设为 3D。", this);
            return false;
        }
        return true;
    }

    // ---- 公共 API ----

    /// 
    /// 可在外部调用，强制立即重新生成噪声（例如修改参数后刷新）。
    /// 
    public void ForceRegenerate()
    {
        if (_kernelIndex >= 0)
            Dispatch(Time.time);
    }
}
