using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Holds script execution until the specified wait condition.",
        null,
        @"
; Thunder SFX will play 0.5 seconds after shake background effect finishes.
@spawn ShakeBackground
@wait 0.5
@sfx Thunder",
        @"
; Print first 2 words, then wait for input before printing the rest.
Lorem ipsum[wait i] dolor sit amet.
; You can also use the following shortcut for this wait mode.
Lorem ipsum[-] dolor sit amet.",
        @"
; Start looped SFX, print message and wait for a skippable 5 seconds delay,
; then stop the SFX.
@sfx Noise loop!
Jeez, what a disgusting Noise. Shut it down![wait i5][>]
@stopSfx Noise"
    )]
    [Serializable, PlaybackGroup, Icon("Pause")]
    public class Wait : Command
    {
        /// <summary>
        /// Literal used to indicate "wait-for-input" mode.
        /// </summary>
        public const string InputLiteral = "i";

        [Doc("Wait conditions:<br/>" +
             " - `i` user press continue or skip input key;<br/>" +
             " - `0.0` timer (seconds);<br/>" +
             " - `i0.0` timer, that is skip-able by continue or skip input keys.")]
        [Alias(NamelessParameterAlias), RequiredParameter]
        public StringParameter WaitMode;

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            // Don't just return here if skip is enabled; state snapshot is marked as allowed
            // for player rollback when setting waiting for input.

            // Always wait for at least a frame; otherwise skip-able timer (eg, @wait i3) may not behave correctly
            // when used before/after a generic text line: https://forum.naninovel.com/viewtopic.php?p=156#p156
            await Async.NextFrame(ctx.Token);

            if (!Assigned(WaitMode))
            {
                Warn($"'{nameof(WaitMode)}' parameter is not specified, the wait command will do nothing.");
                return;
            }

            var waitMode = WaitMode.Value;
            if (waitMode.EqualsIgnoreCase(InputLiteral))
                await WaitForInput(ctx);
            else if (waitMode.StartsWithOrdinal(InputLiteral) && ParseUtils.TryInvariantFloat(waitMode.GetAfterFirst(InputLiteral), out var waitTime))
                await WaitForInputOrTimer(waitTime, ctx);
            else if (ParseUtils.TryInvariantFloat(waitMode, out waitTime))
                await WaitForTimer(waitTime, ctx);
            else Warn($"Failed to resolve value of the '{nameof(WaitMode)}' parameter for the wait command. Check the API reference for list of supported values.");
        }

        protected virtual async Awaitable WaitForInput (ExecutionContext ctx)
        {
            var player = Engine.GetServiceOrErr<IScriptPlayer>();
            ctx.Track.SetAwaitInput(true);
            while (Application.isPlaying && ctx.Token.EnsureNotCanceledOrCompleted())
            {
                await Async.NextFrame(ctx.Token);
                if (!ctx.Track.AwaitingInput || player.AutoPlaying) break;
            }
        }

        protected virtual async Awaitable WaitForInputOrTimer (float waitTime, ExecutionContext ctx)
        {
            using var _ = CompleteOnContinue(ref ctx);
            await WaitForTimer(waitTime, ctx);
        }

        protected virtual async Awaitable WaitForTimer (float waitTime, ExecutionContext ctx)
        {
            var startTime = Engine.Time.Time;
            while (ctx.Token.EnsureNotCanceledOrCompleted())
            {
                await Async.NextFrame(ctx.Token);
                var waitedEnough = Engine.Time.Time - startTime >= waitTime;
                if (waitedEnough) break;
            }
        }
    }
}
