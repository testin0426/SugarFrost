using System;
using Naninovel.Metadata;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Executes the nested lines asynchronously on a dedicated script track in parallel with the main scenario playback routine.
Use to run composite animations or arbitrary command chains concurrently with the subsequent scenario.
Consult the [concurrent playback](/guide/scenario-scripting#concurrent-playback) guide for more info.",
        null,
        @"
; Pan the camera slowly across three points.
@async CameraPan
    @camera offset:4,1 zoom:0.5 time:3 wait!
    @camera offset:,-2 zoom:0.4 time:2 wait!
    @camera offset:0,0 zoom:0 time:3 wait!
; The text below prints while the animation above runs independently.
...
; Before modifying the camera again, make sure the pan animation has finished.
@await CameraPan
@camera zoom:0.7",
        @"
; Run the 'Quake' async task in a loop.
@async Quake loop!
    @spawn Pebbles
    @shake Camera
    @wait { random(3,10) }
...
; Stop the task.
@stop Quake"
    )]
    [Serializable, Alias("async"), RequireNested, PlaybackGroup, Branch(BranchTraits.Nest | BranchTraits.Return)]
    public class ExecuteAsync : Command, Command.INestedHost
    {
        [Doc("Unique identifier of the player track responsible for executing the nested lines. " +
             "When specified, the ID can be used to [@await] or [@stop] the async track playback.")]
        [Alias(NamelessParameterAlias)]
        public StringParameter TrackId;
        [Doc("Whether to play the nested lines in a loop, until stopped with [@stop].")]
        [ParameterDefaultValue("false")]
        public BooleanParameter Loop;

        public override int GetNextPlaybackIndex (ScriptPlaylist playlist, int playedIndex)
        {
            if (playlist.IsEnteringNestedAt(playedIndex)) // host track entering the async block...
                return playlist.SkipNestedAt(playedIndex, Indent); // -> async track is already spawned in execute — skip the block
            if (playlist.IsExitingNestedAt(playedIndex, Indent)) // async track exiting the host async block...
                return -1; // -> let the async track self-dispose or loop when enabled
            return playedIndex + 1; // async track moving inside the async block -> let it be
        }

        public override Awaitable Execute (ExecutionContext ctx)
        {
            var id = GetTrackId();
            var startIdx = ctx.Track.PlayedIndex + 1;
            var track = GetAsyncTrack(id, startIdx);
            return track.LoadAndPlay(PlaybackSpot.ScriptPath, startIdx);
        }

        protected virtual string GetTrackId ()
        {
            if (Assigned(TrackId)) return TrackId;
            return $"@async-{PlaybackSpot.ScriptPath}-{PlaybackSpot.LineIndex}";
        }

        protected virtual IScriptTrack GetAsyncTrack (string id, int startIdx)
        {
            var player = Engine.GetServiceOrErr<IScriptPlayer>();
            if (player.GetTrack(id) is { } track) track.Stop();
            else track = player.AddTrack(id, CreateOptions(startIdx));
            return track;
        }

        protected virtual PlaybackOptions CreateOptions (int startIdx) => new() {
            CompleteOnContinue = DefaultSwitch.Disable,
            LoopAt = GetAssignedOrDefault(Loop, false) ? startIdx : null,
            Dispose = true
        };
    }
}
