using Naninovel;
using Naninovel.Commands;
using UnityEngine;
using DG.Tweening;

[Alias("animFilter")]
public class AnimateFilterCommand : Command
{
    // These parameters can be set in your .nani script
    [RequiredParameter] public DecimalParameter TargetValue;
    public DecimalParameter Duration = 1f;

    public override async Awaitable Execute (ExecutionContext ctx)
    {
        // 1. Get a reference to your material. 
        // Note: It is best practice to have a singleton manager class that holds references to your URP materials.
        Material targetMat = GraphicsManager.Instance.FullScreenFilterMaterial;

        // 2. Use DOTween to animate the material property. 
        // Replace "_Intensity" with the actual shader property reference name.
        targetMat.DOFloat(TargetValue, "_Intensity", Duration);

        // 3. Make Naninovel wait for the animation to finish before proceeding to the next text line.
        await Async.Delay(System.TimeSpan.FromSeconds(Duration.Value), ctx.Token);
    }
}