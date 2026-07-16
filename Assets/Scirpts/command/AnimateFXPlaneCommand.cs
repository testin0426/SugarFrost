using Naninovel;
using Naninovel.Commands;
using UnityEngine;

[Alias("playFX")]
public class AnimateFXPlaneCommand : Command
{
    private static readonly string ShaderProp = "_Threshold";
    private static readonly string ColorProp  = "_BaseColor";

    [RequiredParameter] public DecimalParameter Threshold;
    public DecimalParameter Duration = 1f;
    // Optional tint: "white", "black", "#RRGGBB"... If omitted, the material's color is left as-is.
    public StringParameter Color;
    // wait:true (default) blocks the script until the fade finishes; wait:false plays it
    // in the background and lets the following commands run immediately.
    [Alias("wait")] public BooleanParameter Wait = true;

    public override async Awaitable Execute(ExecutionContext ctx)
    {
        GameObject fxPlane = GameObject.Find("FX");
        if (fxPlane == null)
        {
            Debug.LogError("[playFX] Cannot find 'FX'.");
            return;
        }

        if (!fxPlane.TryGetComponent<Renderer>(out var renderer))
        {
            Debug.LogError("[playFX] 'FX' has no Renderer.");
            return;
        }

        Material mat = renderer.material;

        if (!mat.HasProperty(ShaderProp))
        {
            Debug.LogError($"[playFX] Material has no property '{ShaderProp}'.");
            return;
        }

        // Apply tint only if a color was supplied; otherwise keep the material's current color.
        if (Assigned(Color) && mat.HasProperty(ColorProp))
        {
            if (ColorUtility.TryParseHtmlString(Color, out var tint))
            {
                tint.a = mat.GetColor(ColorProp).a; // preserve color alpha; _Threshold drives blend
                mat.SetColor(ColorProp, tint);
            }
            else Debug.LogWarning($"[playFX] Could not parse color '{Color}', keeping current.");
        }

        float startVal = mat.GetFloat(ShaderProp);
        float endVal   = Threshold;
        float duration = Mathf.Max(Duration, 0.0001f);

        // Record the target so Naninovel can roll the FX back instead of leaving it
        // residual over the text. Recorded up-front so it's captured even with wait:false.
        Engine.GetService<FXStateService>()?.RecordFx(endVal);

        Debug.Log($"[playFX] {startVal} → {endVal} over {duration}s (wait={Wait.Value})");

        Awaitable animation = Animate(mat, startVal, endVal, duration, ctx.Token);
        if (Wait) await animation;
        else animation.Forget();
    }

    private static async Awaitable Animate(Material mat, float startVal, float endVal, float duration, AsyncToken asyncToken)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (asyncToken.Canceled) break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            mat.SetFloat(ShaderProp, Mathf.Lerp(startVal, endVal, t));

            await Async.NextFrame(asyncToken);
        }

        mat.SetFloat(ShaderProp, endVal);
    }
}