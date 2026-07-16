using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Navigates the player track with the specified identifier to the current line and disposes the host track.
Use to join (synchronize) the asynchronously executed tracks with each other or the main track.
Consult the [concurrent playback](/guide/scenario-scripting#concurrent-playback) guide for more info.",
        null,
        @"
You'll have 60 seconds to defuse the bomb!

@async Boom
    @wait 60
    ; After 60 seconds, if the 'Boom' task is not stopped,
    ; the @sync command below will forcefully move the main
    ; track here, which will then navigate to the 'BadEnd' script.
    @sync
    @goto BadEnd

; Simulating a series of bomb-defuse puzzles.
The defuse puzzle 1.
The defuse puzzle 2.
The defuse puzzle 3.

; The 'Boom' async task is stopped, so the main track
; will continue executing without interruption.
@stop Boom
The bomb is defused!
"
    )]
    [Serializable, PlaybackGroup]
    public class Sync : Command
    {
        [Doc("Unique identifier of the player track to join with. Uses main track when not specified.")]
        [Alias(NamelessParameterAlias)]
        public StringParameter TrackId;

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            var player = Engine.GetServiceOrErr<IScriptPlayer>();
            var track = player.GetTrackOrErr(GetAssignedOrDefault(TrackId, player.MainTrack.Id));
            if (ctx.Track == track) return;

            var scriptPath = PlaybackSpot.ScriptPath;
            var playbackIdx = ctx.Track.PlayedIndex;
            await DisposeTrack(ctx.Track);
            await JoinTrack(track, scriptPath, playbackIdx);
        }

        protected async Awaitable DisposeTrack (IScriptTrack track)
        {
            var player = Engine.GetServiceOrErr<IScriptPlayer>();
            var loader = Engine.GetServiceOrErr<IScriptLoader>();
            track.Stop();
            await track.Complete(cmd => cmd != this);
            loader.Release(track.Id);
            if (player.HasTrack(track.Id))
                player.RemoveTrack(track.Id);
        }

        protected async Awaitable JoinTrack (IScriptTrack track, string scriptPath, int playbackIdx)
        {
            track.Stop();
            if (track.AwaitingInput) track.SetAwaitInput(false);
            await track.LoadAndPlay(scriptPath, playbackIdx);
        }
    }
}
