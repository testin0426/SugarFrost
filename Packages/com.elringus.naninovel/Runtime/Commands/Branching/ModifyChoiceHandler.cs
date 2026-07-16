using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Modifies a [choice handler actor](/guide/choices).",
        null,
        @"
; Will make the 'ButtonArea' choice handler default.
@choiceHandler ButtonArea default!"
    )]
    [Serializable, Alias("choiceHandler"), BranchingGroup]
    [ActorContext(ChoiceHandlersConfiguration.DefaultPathPrefix, paramId: "Id")]
    public class ModifyChoiceHandler : ModifyActor<IChoiceHandlerActor, ChoiceHandlerState, ChoiceHandlerMetadata, ChoiceHandlersConfiguration, IChoiceHandlerManager>
    {
        [Doc("ID of the choice handler actor to modify. When not specified, will use the default ones.")]
        [Alias(NamelessParameterAlias), ActorContext(ChoiceHandlersConfiguration.DefaultPathPrefix)]
        public StringParameter HandlerId;
        [Doc("Whether to make the choice handler default. Default handler will be subject of all the choice-related commands when `handler` parameter is not specified.")]
        [Alias("default")]
        public BooleanParameter MakeDefault;

        protected override bool AllowPreload => !Assigned(HandlerId) || !HandlerId.DynamicValue;
        protected override string AssignedId => !string.IsNullOrEmpty(HandlerId) ? HandlerId : ActorManager.DefaultHandlerId;

        protected override async Awaitable Modify (ExecutionContext ctx)
        {
            await base.Modify(ctx);

            if (GetAssignedOrDefault(MakeDefault, false) && !string.IsNullOrEmpty(AssignedId))
                ActorManager.DefaultHandlerId = AssignedId;
        }
    }
}
