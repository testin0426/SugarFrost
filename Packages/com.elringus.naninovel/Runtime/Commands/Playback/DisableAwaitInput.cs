using System;

namespace Naninovel.Commands
{
    [Doc(
        @"
Append to generic lines to disable awaiting input (CTC) after the text is revealed. Shortcut for `[< skip!]`.",
        null,
        @"
; The player won't have to activate a 'continue' input after the line is revealed.
Lorem ipsum dolor sit amet.[>]"
    )]
    [Serializable, Alias(">"), InlineOnly, PlaybackGroup, Icon("ForwardFast")]
    public class DisableAwaitInput : Command { }
}
