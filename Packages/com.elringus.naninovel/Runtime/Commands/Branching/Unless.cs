using System;
using Naninovel.Metadata;

namespace Naninovel.Commands
{
    [Doc(
        @"
Marks the beginning of an inverted conditional execution block.
Nested lines are considered body of the block and will be executed only in case
the conditional nameless parameter is evaluated to `false`.
See [conditional execution](/guide/scenario-scripting#conditional-execution) guide for more info.",
        @"
This command is inverse and complementary to [@if].",
        @"
; Prints ""You're still alive!"" in case ""dead"" variable is false,
; otherwise prints ""You're done."".
@unless dead
    You're still alive!
@else
    You're done.",
        @"
; Print text line depending on ""score"" variable:
;   ""Test result: Passed."" - when score is 10 or above.
;   ""Test result: Failed."" - when score is below 10.
Test result:[unless score<10] Passed.[else] Failed.[endif]"
    )]
    [Serializable, BranchingGroup, Branch(BranchTraits.Nest | BranchTraits.Return | BranchTraits.Switch)]
    [Ignore(nameof(ConditionalExpression)), Ignore(nameof(InvertedConditionalExpression))]
    public class Unless : BeginIf
    {
        protected override bool EvaluateExpression ()
        {
            return !base.EvaluateExpression();
        }
    }
}
