using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Removes all the messages from [printer backlog](/guide/text-printers#printer-backlog).",
        null,
        @"
; Printed text will be removed from the backlog.
Lorem ipsum dolor sit amet, consectetur adipiscing elit.
@clearBacklog"
    )]
    [Serializable, TextGroup, Icon("AlignSlashDuo")]
    public class ClearBacklog : Command
    {
        public override Awaitable Execute (ExecutionContext ctx)
        {
            Engine.GetService<IUIManager>()?.GetUI<UI.IBacklogUI>()?.Clear();
            return Async.Completed;
        }
    }
}
