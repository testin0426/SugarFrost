using System;
using Naninovel.Metadata;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Marks the beginning of a conditional execution block.
Nested lines are considered body of the block and will be executed only in case
the conditional nameless parameter is evaluated to `true`.
See [conditional execution](/guide/scenario-scripting#conditional-execution) guide for more info.",
        @"
This command is inverse and complementary to [@unless].",
        @"
; Print text line(s) depending on ""score"" variable:
;   ""You've failed. Try again!"" - when score is below 6.
;   ""You've passed the test."" and ""Brilliant!"" - when score is above 8.
;   ""You've passed the test."" and ""Impressive!"" - when score is above 7.
;   ""You've passed the test."" and ""Good job!"" - otherwise.
@if score>6
    You've passed the test.
    @if score>8
        Brilliant!
    @else if:score>7
        Impressive!
    @else
        Good job!
@else
    You've failed. Try again!",
        @"
; Print text line depending on ""score"" variable:
;   ""Test result: Failed."" - when score is below 6.
;   ""Test result: Perfect!"" - when score is above 8.
;   ""Test result: Passed."" - otherwise.
Test result:[if score>8] Perfect![else if:score>6] Passed.[else] Failed.[endif]"
    )]
    [Serializable, Alias("if"), BranchingGroup, Branch(BranchTraits.Nest | BranchTraits.Return | BranchTraits.Switch)]
    [Ignore(nameof(ConditionalExpression)), Ignore(nameof(InvertedConditionalExpression))]
    public class BeginIf : Command, Command.INestedHost
    {
        [Doc("A [script expression](/guide/script-expressions), which should return a boolean value " +
             "determining whether the associated nested block will be executed.")]
        [Alias(NamelessParameterAlias), RequiredParameter, ConditionContext]
        public StringParameter Expression;

        public override bool ShouldExecute => true;

        public override Awaitable Execute (ExecutionContext ctx)
        {
            if (Assigned(ConditionalExpression) || Assigned(InvertedConditionalExpression))
                Warn("Parameters 'if' and 'unless' in conditional commands are ignored; use nameless parameter for the condition instead.");
            // truthy @if -> enter the branch
            if (EvaluateExpression()) return Async.Completed;
            // falsy @if -> enter the branch under the first truthy @else
            if (FindTruthyElse(ctx.Track) is { } idx) ctx.Track.Resume(idx + 1);
            // no truthy @else -> exit the conditional block
            else ctx.Track.Resume(ExitConditionalBlock(ctx.Track.Playlist, ctx.Track.PlayedIndex));
            return Async.Completed;
        }

        protected virtual bool EvaluateExpression ()
        {
            return ExpressionEvaluator.Evaluate<bool>(Expression, new() { OnError = Err, Parameter = new(Expression, 0) });
        }

        /// <summary>
        /// Returns playback index of the first truthy @else command in the current conditional block
        /// or null in case all the @else branches are falsy.
        /// </summary>
        protected virtual int? FindTruthyElse (IScriptTrack track)
        {
            return track.Playlist.IsEnteringNestedAt(track.PlayedIndex)
                ? FindNestedTruthyElse(track.Playlist, track.PlayedIndex)
                : FindFlatTruthyElse(track.Playlist, track.PlayedIndex);
        }

        protected virtual int? FindNestedTruthyElse (ScriptPlaylist list, int startIdx)
        {
            var host = list[startIdx];
            var idx = list.SkipNestedAt(startIdx + 1, host.Indent);
            while (list.GetCommandByIndex(idx) is Else el && el.Indent == host.Indent)
                if (IsTruthyElse(el)) return idx;
                else
                    idx = list.IsEnteringNestedAt(idx)
                        ? list.SkipNestedAt(idx + 1, host.Indent)
                        : idx + 1;
            return null;
        }

        protected virtual int? FindFlatTruthyElse (ScriptPlaylist list, int startIdx)
        {
            var depth = 0;
            for (var idx = startIdx + 1; idx < list.Count; idx++)
                if (depth == 0 && IsTruthyElse(list[idx])) return idx;
                else if (list[idx] is BeginIf or Unless) depth++;
                else if (list[idx] is EndIf)
                    if (depth > 0) depth--;
                    else return null;
            return null;
        }

        protected virtual bool IsTruthyElse (Command cmd)
        {
            if (cmd is not Else el) return false;
            if (!Assigned(el.ConditionalExpression) && !Assigned(el.InvertedConditionalExpression)) return true;
            if (Assigned(el.ConditionalExpression))
                return ExpressionEvaluator.Evaluate<bool>(el.ConditionalExpression,
                    new() { OnError = el.Err, Parameter = new(el.ConditionalExpression, 0) });
            return !ExpressionEvaluator.Evaluate<bool>(el.InvertedConditionalExpression,
                new() { OnError = el.Err, Parameter = new(el.InvertedConditionalExpression, 0) });
        }

        /// <summary>
        /// Given a specified playback index is pointing to a conditional command (@if/unless or @else)
        /// inside the specified playlist, returns the playback index that should be executed after
        /// exiting the conditional block. Returns -1 if there are no commands following the conditional block.
        /// </summary>
        public static int ExitConditionalBlock (ScriptPlaylist list, int index)
        {
            var cmd = list[index];
            if (cmd is not (BeginIf or Unless or Else))
                throw Engine.Fail(
                    $"Failed to exit conditional block from '{cmd.GetType().Name}' command: " +
                    $"only @if, @unless and @else commands are supported.", cmd.PlaybackSpot);
            var idx = list.IsEnteringNestedAt(index)
                ? ExitNestedConditionalBlock(list, index)
                : ExitFlatConditionalBlock(list, index);
            return list.IsIndexValid(idx) ? idx : -1;
        }

        private static int ExitNestedConditionalBlock (ScriptPlaylist list, int startIdx)
        {
            var host = list[startIdx];
            var idx = list.SkipNestedAt(startIdx + 1, host.Indent);
            while (list.GetCommandByIndex(idx) is Else el && el.Indent == host.Indent)
                idx = list.IsEnteringNestedAt(idx)
                    ? list.SkipNestedAt(idx + 1, host.Indent)
                    : idx + 1;
            return idx;
        }

        private static int ExitFlatConditionalBlock (ScriptPlaylist list, int startIdx)
        {
            var depth = 0;
            for (var idx = startIdx + 1; idx < list.Count; idx++)
                if (list[idx] is BeginIf or Unless) depth++;
                else if (list[idx] is EndIf)
                    if (depth > 0) depth--;
                    else return idx + 1;
            return -1;
        }
    }
}
