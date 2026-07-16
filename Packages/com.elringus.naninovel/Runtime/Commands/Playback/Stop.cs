using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Stops the scenario script playback.",
        null,
        @"
@gosub #Label
...
@stop

; Stop command above prevents script playback from proceeding
; under the label below.
# Label
This line is only executed when navigated directly with a @gosub.
@return",
        @"
; Loop the 'Quake' async task until stopped.
@async Quake loop!
    @spawn Pebbles
    @shake Camera
    @wait { random(3,10) }
...
; Stop the 'Quake' async task.
@stop Quake"
    )]
    [Serializable, PlaybackGroup, Icon("Stop")]
    public class Stop : Command
    {
        [Doc("The identifier of the script track to stop; stops the main track when not specified. " +
             "Can be used to stop the playback of an async track spawned with the [@async] command.")]
        [Alias(NamelessParameterAlias)]
        public StringParameter TrackId;

        public override Awaitable Execute (ExecutionContext ctx)
        {
            if (Assigned(TrackId)) DisposeAsyncTrack(TrackId);
            else ctx.Track.Stop();
            return Async.Completed;
        }

        protected virtual void DisposeAsyncTrack (string trackId)
        {
            var player = Engine.GetServiceOrErr<IScriptPlayer>();
            if (player.GetTrack(trackId) is not { } track) return;
            track.Stop();
            player.RemoveTrack(track.Id);
        }
    }
}
