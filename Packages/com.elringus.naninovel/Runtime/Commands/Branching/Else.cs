using System;
using Naninovel.Metadata;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Marks a branch of a conditional execution block,
which is executed in case condition of the opening [@if] or [@unless] and preceding [@else] (if any) commands are not met.
For usage examples see [conditional execution](/guide/scenario-scripting#conditional-execution) guide."
    )]
    [Serializable, BranchingGroup, Branch(BranchTraits.Nest | BranchTraits.Return, new[] { nameof(BeginIf), nameof(Unless) })]
    public class Else : Command, Command.INestedHost
    {
        public override bool ShouldExecute => true;

        public override int GetNextPlaybackIndex (ScriptPlaylist playlist, int playedIndex)
        {
            // @if/unless command opening the conditional block decides which one of the associated else branches
            // is executed and navigates to the first command under the branch (not this command); so always skip
            // the @else conditional block, as it's only executed when exiting from a previous @else branch or
            // entering a malformed conditional block.
            if (playlist.IsEnteringNestedAt(playedIndex))
                return BeginIf.ExitConditionalBlock(playlist, playedIndex);
            return base.GetNextPlaybackIndex(playlist, playedIndex);
        }

        public override Awaitable Execute (ExecutionContext ctx)
        {
            // Mirror the nested behaviour for flat conditional blocks, such as inlined.
            ctx.Track.Resume(BeginIf.ExitConditionalBlock(ctx.Track.Playlist, ctx.Track.PlayedIndex));
            return Async.Completed;
        }
    }
}
