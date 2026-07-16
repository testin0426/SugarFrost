using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Enters the dialogue mode by enabling Naninovel activities, such as rendering and input processing.
Intended to switch into the dialogue or visual novel mode when Naninovel is used as a drop-in dialogue/cutscene system."
    )]
    [Serializable, PlaybackGroup]
    public class EnterDialogue : Command
    {
        public override Awaitable Execute (ExecutionContext ctx) => Dialogue.Enter();
    }
}
