using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Allows halting and resuming user input processing (eg, reacting to pressing keyboard keys).
The effect of the action is persistent and saved with the game.",
        null,
        @"
; Halt input processing of all the samplers.
@processInput false",
        @"
; Resume input processing of all the samplers.
@processInput true",
        @"
; Mute 'Rollback' and 'Pause' inputs and un-mute 'Continue' input.
@processInput set:Rollback.false,Pause.false,Continue.true"
    )]
    [Serializable, UIGroup, Icon("GamepadModern")]
    public class ProcessInput : Command
    {
        [Doc("Whether to enable input processing of all the samplers.")]
        [Alias(NamelessParameterAlias)]
        public BooleanParameter InputEnabled;
        [Doc("Allows muting and un-muting individual input samplers.")]
        [Alias("set")]
        public NamedBooleanListParameter SetEnabled;

        public override Awaitable Execute (ExecutionContext ctx)
        {
            if (!Assigned(InputEnabled) && !Assigned(SetEnabled))
            {
                Warn("No parameters were specified in '@processInput'; command won't have any effect.");
                return Async.Completed;
            }

            var input = Engine.GetServiceOrErr<IInputManager>();

            if (Assigned(InputEnabled))
                input.Muted = !InputEnabled;

            if (Assigned(SetEnabled))
            {
                foreach (var kv in SetEnabled)
                {
                    if (!kv.HasValue || !kv.NamedValue.HasValue)
                    {
                        Err("An invalid item in 'set' parameter detected in '@processInput' command. Make sure all items have both name and value specified.");
                        continue;
                    }

                    if (input.GetInput(kv.Name) is not { } handle)
                    {
                        Err($"'{kv.Name}' input sampler wasn't found while executing '@processInput' command. Make sure a binding with that name exist in the input configuration.");
                        continue;
                    }
                    handle.Muted = !kv.NamedValue;
                }
            }

            return Async.Completed;
        }
    }
}
