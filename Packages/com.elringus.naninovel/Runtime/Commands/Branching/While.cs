using System;
using Naninovel.Metadata;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Executes nested lines in a loop, as long as specified conditional expression resolves to `true`.",
        null,
        @"
; Guess the number game.
@set number=random(1,100);answer=0
@while answer!=number
    @input answer summary:""Guess a number between 1 and 100""
    @if answer<number
        Wrong, too low.
    @else if:answer>number
        Wrong, too high.
    @else
        Correct!"
    )]
    [Serializable, RequireNested, BranchingGroup, Icon("ArrowsRepeat"), Branch(BranchTraits.Nest | BranchTraits.Return),
     Ignore(nameof(ConditionalExpression)), Ignore(nameof(InvertedConditionalExpression))]
    public class While : Command, Command.INestedHost
    {
        [Doc("A [script expression](/guide/script-expressions), which should return a boolean value " +
             "determining whether the associated nested block should continue executing in loop.")]
        [Alias(NamelessParameterAlias), RequiredParameter, ConditionContext]
        public StringParameter Expression;

        public override bool ShouldExecute => true;

        public override int GetNextPlaybackIndex (ScriptPlaylist playlist, int playedIndex)
        {
            if (playlist.IsEnteringNestedAt(playedIndex))
                return ExpressionEvaluator.Evaluate<bool>(Expression, new() { OnError = Err, Parameter = new(Expression, 0) })
                    ? playedIndex + 1
                    : playlist.SkipNestedAt(playedIndex, Indent);
            if (playlist.IsExitingNestedAt(playedIndex, Indent))
                return playlist.IndexOf(this);
            return playedIndex + 1;
        }

        public override Awaitable Execute (ExecutionContext ctx)
        {
            if (Assigned(ConditionalExpression) || Assigned(InvertedConditionalExpression))
                Warn("Parameters 'if' and 'unless' in '@while' command are ignored; use nameless parameter for the condition instead.");
            return Async.Completed;
        }
    }
}
