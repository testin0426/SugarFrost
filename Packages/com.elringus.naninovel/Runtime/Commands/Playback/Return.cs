using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Attempts to navigate scenario script playback to a command after the last used [@gosub].
See [@gosub] command summary for more info and usage examples."
    )]
    [Serializable, PlaybackGroup, Icon("Reply")]
    public class Return : Command
    {
        [Doc("When specified, will reset the engine services state before returning to the initial script " +
             "from which the gosub was entered (in case it's not the currently played script). " +
             "Specify `*` to reset all the services, or specify service names to exclude from reset. " +
             "By default, the state does not reset.")]
        [Alias("reset")]
        public StringListParameter ResetState;

        protected virtual IScriptPlayer Player => Engine.GetServiceOrErr<IScriptPlayer>();

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            if (ctx.Track.GosubReturnSpots.Count == 0)
            {
                Warn("Failed to return to the last gosub: state data is missing or invalid.");
                return;
            }
            await Reset();
            await Navigate(ctx.Track);
        }

        protected virtual async Awaitable Reset ()
        {
            var state = Engine.GetServiceOrErr<IStateManager>();
            if (Assigned(ResetState) && ResetState.Length == 1 && ResetState[0] == "*") await state.ResetState();
            else if (Assigned(ResetState) && ResetState.Length > 0) await state.ResetState(ResetState.ToReadOnlyList());
        }

        protected virtual async Awaitable Navigate (IScriptTrack track)
        {
            var spot = track.GosubReturnSpots.Pop();
            if (!spot.Valid) // gosub was called from an end of a script — signal playback finished
            {
                track.Resume(-1);
                return;
            }
            if (track.PlayedScript && track.PlayedScript.Path.EqualsIgnoreCase(spot.ScriptPath))
            {
                track.ResumeAtLine(spot.LineIndex);
                return;
            }
            await track.LoadAndPlayAtLine(spot.ScriptPath, spot.LineIndex, spot.InlineIndex);
        }
    }
}
