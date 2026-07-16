using Naninovel;
using UnityEngine;

// Generic animator for the RT plane shader family (RTfilter, Drawing_Toon_Plane).
// Animates any float or color material property on a named scene object, toggles
// renderer visibility, and drives the shared screen-space mask via the special
// target "global" (Shader.SetGlobal*, read by LayerFX.hlsl on materials with
// "Receive Global Mask" enabled).
//
//   @rtfx obj:CharPlane prop:_Alpha to:0 duration:0.5
//   @rtfx obj:CharPlane prop:_TintColor color:#804040 duration:1 wait:false
//   @rtfx obj:CharPlane prop:_BlurLod to:3
//   @rtfx obj:CharPlane visible:false
//   @rtfx obj:global prop:_GMaskMode to:3 duration:0
//   @rtfx obj:global prop:_GMaskCenterX to:1.5 duration:2
[Alias("rtfx")]
public class RTFXCommand : Command
{
    [RequiredParameter, Alias("obj")] public StringParameter ObjectName;
    [Alias("prop")] public StringParameter Property;
    [Alias("to")] public DecimalParameter FloatValue;
    // "white", "black", "#RRGGBB", "#RRGGBBAA"...
    [Alias("color")] public StringParameter ColorValue;
    // Instant renderer on/off (not animated, ignores duration).
    [Alias("visible")] public BooleanParameter Visible;
    public DecimalParameter Duration = 0.5f;
    // wait:true (default) blocks the script until the animation finishes.
    [Alias("wait")] public BooleanParameter Wait = true;

    public override async Awaitable Execute (ExecutionContext ctx)
    {
        string obj = ObjectName;
        bool isGlobal = obj == FXStateService.GlobalTarget;
        var fxState = Engine.GetService<FXStateService>();

        if (Assigned(Visible))
        {
            if (isGlobal) { Debug.LogError("[rtfx] visible: cannot target 'global'."); return; }
            if (!FXStateService.ApplyVisibility(obj, Visible))
            {
                Debug.LogError($"[rtfx] Cannot find '{obj}' (or it has no renderers).");
                return;
            }
            fxState?.RecordVisibility(obj, Visible);
            if (!Assigned(Property)) return;
        }

        if (!Assigned(Property))
        {
            Debug.LogError("[rtfx] Nothing to do: specify prop: (with to:/color:) or visible:.");
            return;
        }
        if (Assigned(FloatValue) == Assigned(ColorValue))
        {
            Debug.LogError("[rtfx] Specify exactly one of to: (float) or color:.");
            return;
        }

        string prop = Property;
        Material mat = null;
        if (!isGlobal)
        {
            // Resolves through the rig's VisibilityFadeBridge plane, or any child
            // renderer whose material has the property (see FXStateService).
            var renderer = FXStateService.FindRenderer(obj, prop);
            if (renderer == null)
            {
                Debug.LogError($"[rtfx] No renderer with material property '{prop}' on or under '{obj}'.");
                return;
            }
            mat = renderer.material;
        }

        float duration = Mathf.Max(Duration, 0f);

        if (Assigned(FloatValue))
        {
            float start = isGlobal ? Shader.GetGlobalFloat(prop) : mat.GetFloat(prop);
            float end = FloatValue;
            // Recorded up-front so rollback captures the target even with wait:false.
            fxState?.RecordPropFloat(obj, prop, end);

            Awaitable animation = Animate(duration, ctx.Token, t => {
                float v = Mathf.Lerp(start, end, t);
                if (isGlobal) Shader.SetGlobalFloat(prop, v);
                else mat.SetFloat(prop, v);
            });
            if (Wait) await animation;
            else animation.Forget();
        }
        else
        {
            if (!ColorUtility.TryParseHtmlString(ColorValue, out var end))
            {
                Debug.LogError($"[rtfx] Could not parse color '{ColorValue}'.");
                return;
            }
            Color start = isGlobal ? Shader.GetGlobalColor(prop) : mat.GetColor(prop);
            fxState?.RecordPropColor(obj, prop, end);

            Awaitable animation = Animate(duration, ctx.Token, t => {
                Color v = Color.Lerp(start, end, t);
                if (isGlobal) Shader.SetGlobalColor(prop, v);
                else mat.SetColor(prop, v);
            });
            if (Wait) await animation;
            else animation.Forget();
        }
    }

    private static async Awaitable Animate (float duration, AsyncToken asyncToken, System.Action<float> apply)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (asyncToken.Canceled) break;

            elapsed += Time.deltaTime;
            apply(Mathf.Clamp01(elapsed / duration));

            await Async.NextFrame(asyncToken);
        }

        apply(1f);
    }
}
