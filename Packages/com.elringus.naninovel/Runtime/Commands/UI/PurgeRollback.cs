using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Prevents player from rolling back to the previous state snapshots.",
        null,
        @"
; Prevent player from rolling back to try selecting another choice.

Select a choice. You won't be able to rollback.
@choice One goto:#One
@choice Two goto:#Two

# One
@purgeRollback
You've picked one.
@stop

# Two
@purgeRollback
You've picked two.
@stop"
    )]
    [Serializable, UIGroup, Icon("AlignSlashDuo")]
    public class PurgeRollback : Command
    {
        public override Awaitable Execute (ExecutionContext ctx)
        {
            Engine.GetService<IStateManager>()?.PurgeRollbackData();
            return Async.Completed;
        }
    }
}
