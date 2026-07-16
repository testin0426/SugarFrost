using System;
using System.Threading;
using Naninovel.Metadata;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Holds scenario playback until either the specified async task or all nested lines have finished executing.",
        @"
The nested block is expected to always finish; don't nest any commands that could
navigate outside the block, as this may cause undefined behaviour.",
        @"
; Run nested lines in parallel and wait until they all are finished.
@await
    @back RainyScene
    @bgm RainAmbient
    @camera zoom:0.5 time:3
    It starts Raining...[>]
; Following line will execute after all the above is finished.
...",
        @"
; Pan the camera slowly across the two points.
@async CameraPan
    @camera offset:4,1 zoom:0.5 time:3 wait!
    @camera offset:,-2 zoom:0.4 time:2 wait!
...
; Before modifying the camera again, make sure the animation has finished.
@await CameraPan complete!
@camera zoom:0"
    )]
    [Serializable, PlaybackGroup, Branch(BranchTraits.Nest | BranchTraits.Return)]
    public class Await : Command, Command.INestedHost
    {
        [Doc("The identifier of an async script track to await. " +
             "Can be used to await the completion of a track spawned with the [@async] command.")]
        [Alias(NamelessParameterAlias)]
        public StringParameter TrackId;
        [Doc("Whether to force-complete the awaited track as soon as possible. Has no effect when awaiting nested lines.")]
        [ParameterDefaultValue("false")]
        public BooleanParameter Complete;

        private bool nesting;
        private CancellationTokenRegistration ctr;

        public override int GetNextPlaybackIndex (ScriptPlaylist playlist, int playedIndex)
        {
            // this method is only invoked when @await has nested lines

            if (playlist.IsEnteringNestedAt(playedIndex))
                return nesting
                    ? playedIndex + 1
                    : playlist.SkipNestedAt(playedIndex, Indent);

            if (playlist.IsExitingNestedAt(playedIndex, Indent))
                return nesting
                    ? playlist.IndexOf(this)
                    : playlist.ExitNestedAt(playedIndex, Indent);

            nesting = true;
            return playedIndex + 1;
        }

        public override Awaitable Execute (ExecutionContext ctx)
        {
            if (Assigned(TrackId)) return AwaitTrack(ctx, TrackId);
            return AwaitNested(ctx);
        }

        protected virtual async Awaitable AwaitTrack (ExecutionContext ctx, string trackId)
        {
            var player = Engine.GetServiceOrErr<IScriptPlayer>();
            if (player.GetTrack(trackId) is not { } track) return;

            var complete = GetAssignedOrDefault(Complete, false);
            while (ctx.Token.EnsureNotCanceled() && track.Executing)
                if ((complete || ctx.Token.Completed) && !track.Completing) await track.Complete();
                else await Async.NextFrame(ctx.Token);

            if (track.AwaitingInput) // as it causes a deadlock when requesting ASAP completion, eg when saving
                Engine.Err("Awaiting completion of tracks that await input is not supported.");
        }

        protected virtual async Awaitable AwaitNested (ExecutionContext ctx)
        {
            if (!nesting)
            {
                nesting = true;
                ctr.Dispose();
                ctr = ctx.Token.CancellationToken.Register(() => nesting = false);
                return;
            }

            using var _ = ctx.Track.RentExecutedCommands(out var executed);
            using var __ = SetPool<Command>.Rent(out var awaited);
            foreach (var cmd in executed)
                if (ctx.Track.Playlist.IsNestedUnder(cmd, this))
                    awaited.Add(cmd);

            try
            {
                while (ctx.Token.EnsureNotCanceledOrCompleted())
                {
                    if (!awaited.Overlaps(executed)) break;
                    await Async.NextFrame(ctx.Token);
                    executed.Clear();
                    ctx.Track.CollectExecutedCommands(executed);
                }
            }
            finally
            {
                ctr.Dispose();
                nesting = false;
                if (!ctx.Token.Canceled && ctx.Token.Completed)
                    ctx.Track.Complete(awaited.Contains).Forget();
            }
        }
    }
}
