using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Holds further scenario playback until user activates a `continue` input aka CTC. Shortcut for `@wait i`.",
        null,
        @"
; User will have to activate a 'continue' input after the first sentence
; for the printer to continue printing out the following text.
Lorem ipsum dolor sit amet.[-] Consectetur adipiscing elit."
    )]
    [Serializable, Alias("-"), InlineOnly, PlaybackGroup, Icon("CirclePause")]
    public class I : Command // keep the "I" class name for backward compatibility with [i]
    {
        public override Awaitable Execute (ExecutionContext ctx) => new Wait {
            Indent = Indent,
            PlaybackSpot = PlaybackSpot,
            WaitMode = Wait.InputLiteral
        }.Execute(ctx);
    }
}
