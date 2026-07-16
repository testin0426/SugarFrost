using System;

namespace Naninovel.Commands
{
    [Doc(
        @"
Alternative to using indentation in conditional blocks: marks end of the block
opened with previous [@if] command, no matter the indentation.
For usage examples see [conditional execution](/guide/scenario-scripting#conditional-execution) guide."
    )]
    [Serializable, BranchingGroup]
    public class EndIf : Command { }
}
