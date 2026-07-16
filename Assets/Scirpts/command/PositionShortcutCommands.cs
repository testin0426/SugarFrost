using System.Globalization;
using Naninovel;
using Naninovel.Commands;
using UnityEngine;

// Hotkey aliases that wrap Naninovel's built-in @animate.
// Scene posX is 0..100 (left..right); 50 is center.
// Tweak these three constants to taste — every shortcut uses them.
internal static class PosShortcut
{
    public const float Center = 50f;
    public const float Left   = 20f;
    public const float Right  = 80f;

    public const float DefaultTime = 1f;

    public static Awaitable Run (StringListParameter actorIds, float fromX, float toX, DecimalParameter duration, BooleanParameter wait, ExecutionContext ctx)
    {
        var inv = CultureInfo.InvariantCulture;
        float dur = duration != null && duration.HasValue ? duration.Value : DefaultTime;
        var animate = new AnimateActor
        {
            ActorIds       = actorIds,
            ScenePositionX = $"{fromX.ToString(inv)},{toX.ToString(inv)}",
            Duration       = $"0,{dur.ToString(inv)}",
            Wait           = wait
        };
        return animate.Execute(ctx);
    }
}

public abstract class PosShortcutBase : Command
{
    [RequiredParameter, Alias(NamelessParameterAlias)]
    public StringListParameter ActorIds;

    [Alias("t"), ParameterDefaultValue("1")]
    public DecimalParameter Duration = 1f;

    public BooleanParameter Wait;

    protected abstract float FromX { get; }
    protected abstract float ToX { get; }

    public override Awaitable Execute (ExecutionContext ctx)
        => PosShortcut.Run(ActorIds, FromX, ToX, Duration, Wait, ctx);
}

[Alias("c2r")]
public class CenterToRightCommand : PosShortcutBase
{
    protected override float FromX => PosShortcut.Center;
    protected override float ToX   => PosShortcut.Right;
}

[Alias("c2l")]
public class CenterToLeftCommand : PosShortcutBase
{
    protected override float FromX => PosShortcut.Center;
    protected override float ToX   => PosShortcut.Left;
}

[Alias("l2c")]
public class LeftToCenterCommand : PosShortcutBase
{
    protected override float FromX => PosShortcut.Left;
    protected override float ToX   => PosShortcut.Center;
}

[Alias("r2c")]
public class RightToCenterCommand : PosShortcutBase
{
    protected override float FromX => PosShortcut.Right;
    protected override float ToX   => PosShortcut.Center;
}