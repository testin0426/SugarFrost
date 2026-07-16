using Naninovel;
using UnityEngine;
using UnityEngine.UI;

// @playUIFX threshold:0.8 color:black duration:1
// Animates a full-screen UI Image ("UIFX") that tints the UI beneath it.
// The Image must live in a Canvas whose Sorting Order is HIGHER than the UI
// it should tint, so it composites over that UI (a world mesh cannot do this
// when the UI canvas is Screen Space - Camera).
//   threshold = tint alpha (0 = no influence, 1 = fully covers the UI)
//   color     = "black" (default) or "white"
[Alias("playUIFX")]
public class AnimateUIFXCommand : Command
{
    [RequiredParameter] public DecimalParameter Threshold;
    public DecimalParameter Duration = 1f;
    [ParameterDefaultValue("black")] public StringParameter Color = "black";
    // wait:true (default) blocks the script until the tint finishes; wait:false plays it
    // in the background and lets the following commands run immediately.
    [Alias("wait")] public BooleanParameter Wait = true;

    public override async Awaitable Execute(ExecutionContext ctx)
    {
        GameObject uiFx = GameObject.Find("UIFX");
        if (uiFx == null)
        {
            Debug.LogError("[playUIFX] Cannot find 'UIFX'.");
            return;
        }

        // Accept the Image whether 'UIFX' is the Image itself or its parent Canvas;
        // also search inactive children.
        if (!uiFx.TryGetComponent<Graphic>(out var graphic))
            graphic = uiFx.GetComponentInChildren<Graphic>(true);

        if (graphic == null)
        {
            Debug.LogError("[playUIFX] No UI Graphic (Image) found on 'UIFX' or its children.");
            return;
        }

        // Never let the full-screen overlay intercept input meant for the UI below it.
        if (graphic.raycastTarget) graphic.raycastTarget = false;

        // Resolve tint color (default black); preserve current alpha as the start.
        Color rgb = graphic.color;
        if (ColorUtility.TryParseHtmlString(Color, out var parsed))
            rgb = parsed;
        else
            Debug.LogWarning($"[playUIFX] Could not parse color '{Color}', keeping current.");

        float startVal = graphic.color.a;
        float endVal   = Mathf.Clamp01(Threshold);
        float duration = Mathf.Max(Duration, 0.0001f);

        // Record the target so rollback restores the overlay alpha instead of leaving it
        // residual. Recorded up-front so it's captured even with wait:false.
        Engine.GetService<FXStateService>()?.RecordUiFx(endVal);

        Debug.Log($"[playUIFX] alpha {startVal} → {endVal} over {duration}s, color={Color} (wait={Wait.Value})");

        Awaitable animation = Animate(graphic, rgb, startVal, endVal, duration, ctx.Token);
        if (Wait) await animation;
        else animation.Forget();
    }

    private static async Awaitable Animate(Graphic graphic, Color rgb, float startVal, float endVal, float duration, AsyncToken asyncToken)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (asyncToken.Canceled) break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            graphic.color = new Color(rgb.r, rgb.g, rgb.b, Mathf.Lerp(startVal, endVal, t));

            await Async.NextFrame(asyncToken);
        }

        graphic.color = new Color(rgb.r, rgb.g, rgb.b, endVal);
    }
}
