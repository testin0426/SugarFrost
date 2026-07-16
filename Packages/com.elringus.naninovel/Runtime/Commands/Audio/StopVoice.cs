using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Stops playback of the currently played voice clip.",
        null,
        @"
; Given a voice is being played, stop it.
@stopVoice"
    )]
    [Serializable, AudioGroup, Icon("MicrophoneSlashDuo")]
    public class StopVoice : AudioCommand
    {
        public override Awaitable Execute (ExecutionContext ctx)
        {
            AudioManager.StopVoice();
            return Async.Completed;
        }
    }
}
