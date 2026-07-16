using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Allows to enable or disable script player ""skip"" mode.",
        null,
        @"
; Enable skip mode.
@skip",
        @"
; Disable skip mode.
@skip false"
    )]
    [Serializable, PlaybackGroup, Icon("ForwardFast")]
    public class Skip : Command
    {
        [Doc("Whether to enable (default) or disable the skip mode.")]
        [Alias(NamelessParameterAlias), ParameterDefaultValue("true")]
        public BooleanParameter Enable;

        public override Awaitable Execute (ExecutionContext ctx)
        {
            var scriptPlayer = Engine.GetServiceOrErr<IScriptPlayer>();
            scriptPlayer.SetSkip(GetAssignedOrDefault(Enable, true));
            return Async.Completed;
        }
    }
}
