using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Exits the dialogue mode by resetting the engine state and disabling most Naninovel activities, such as rendering and input processing.
Intended to switch out of the dialogue or visual novel mode when Naninovel is used as a drop-in dialogue/cutscene system."
    )]
    [Serializable, PlaybackGroup]
    public class ExitDialogue : Command
    {
        [Doc("Whether to also destroy (deinitialize) the engine after exiting the dialogue mode.")]
        [ParameterDefaultValue("false")]
        public BooleanParameter Destroy;

        public override Awaitable Execute (ExecutionContext ctx) => Dialogue.Exit(GetAssignedOrDefault(Destroy, false));
    }
}
