using Naninovel;
using Naninovel.Commands;
using UnityEngine;

// @bt = blackout transition. Runs:
//   @hideAll
//   @playFX threshold:1.0 duration:1
//   @playFX threshold:-0.32 duration:1
[Alias("bt")]
public class BlackoutTransitionCommand : Command
{
    private const float FxOn  = 1.0f;
    private const float FxOff = -0.32f;

    [Alias("time"), ParameterDefaultValue("1")]
    public DecimalParameter Duration = 1f;

    public override async Awaitable Execute (ExecutionContext ctx)
    {
        await new AnimateFXPlaneCommand { Threshold = FxOn,  Duration = Duration }.Execute(ctx);
        await new AnimateFXPlaneCommand { Threshold = FxOff, Duration = Duration }.Execute(ctx);
        await new HideAllActors { Duration = Duration, Wait = true }.Execute(ctx);
    }
}