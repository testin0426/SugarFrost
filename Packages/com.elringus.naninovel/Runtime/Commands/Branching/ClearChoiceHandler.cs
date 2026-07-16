using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Removes current choices in the choice handler with the specified ID (or in default one, when ID is not specified;
or in all the existing handlers, when `*` is specified as ID) and (optionally) hides it (them).",
        null,
        @"
; Give the player 2 seconds to select a choice.
You have 2 seconds to respond![>]
@addChoice ""Cats"" set:response=""Cats""
@addChoice ""Dogs"" set:response=""Dogs""
@set response=""None""
@wait 2
@clearChoice
@unless response=""None""
    {response}, huh?
@else
    Time's out!"
    )]
    [Serializable, Alias("clearChoice"), BranchingGroup, Icon("CircleDashed")]
    public class ClearChoiceHandler : Command
    {
        [Doc("ID of the choice handler to clear. Will use a default handler if not specified. " +
             "Specify `*` to clear all the existing handlers.")]
        [Alias(NamelessParameterAlias), ActorContext(ChoiceHandlersConfiguration.DefaultPathPrefix)]
        public StringParameter HandlerId;
        [Doc("Identifier of a specific choice to remove. Will remove all choices when not specified.")]
        [Alias("id")]
        public StringParameter ChoiceId;
        [Doc("Whether to also hide the affected choice handlers.")]
        [ParameterDefaultValue("true")]
        public BooleanParameter Hide;

        public override Awaitable Execute (ExecutionContext ctx)
        {
            var hide = GetAssignedOrDefault(Hide, true);
            var choices = Engine.GetServiceOrErr<IChoiceHandlerManager>();

            if (Assigned(HandlerId) && HandlerId == "*")
            {
                using var _ = choices.RentActors(out var actors);
                foreach (var handler in actors)
                {
                    RemoveAllChoices(handler);
                    if (hide) handler.Visible = false;
                }
                return Async.Completed;
            }

            var handlerId = Assigned(HandlerId) ? HandlerId.Value : choices.DefaultHandlerId;
            if (!choices.ActorExists(handlerId))
            {
                Warn($"Failed to clear `{handlerId}` choice handler: handler actor with the specified ID doesn't exist.");
                return Async.Completed;
            }

            var choiceHandler = choices.GetActorOrErr(handlerId);
            RemoveAllChoices(choiceHandler);
            if (hide) choiceHandler.Visible = false;
            return Async.Completed;
        }

        protected virtual void RemoveAllChoices (IChoiceHandlerActor handler)
        {
            var choiceId = GetAssignedOrDefault(ChoiceId, default(string));
            using var _ = handler.RentChoices(out var choices);
            foreach (var choice in choices)
                if (choiceId == null || choice.Id.EqualsOrdinal(ChoiceId))
                    handler.RemoveChoice(choice.Id);
        }
    }
}
