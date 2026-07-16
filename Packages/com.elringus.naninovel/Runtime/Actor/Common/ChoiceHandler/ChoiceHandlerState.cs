using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Serializable state of a <see cref="IChoiceHandlerActor"/>.
    /// </summary>
    [Serializable]
    public class ChoiceHandlerState : ActorState<IChoiceHandlerActor>
    {
        /// <inheritdoc cref="IChoiceHandlerActor.Choices"/>
        public List<Choice> Choices => new(choices);

        [SerializeField] private List<Choice> choices = new();

        public override void OverwriteFromActor (IChoiceHandlerActor actor)
        {
            base.OverwriteFromActor(actor);

            using var _ = actor.RentChoices(out var newChoices);
            choices.ReplaceWith(newChoices);
        }

        public override async Awaitable ApplyToActor (IChoiceHandlerActor actor)
        {
            await base.ApplyToActor(actor);

            var buttonLoader = Engine.GetServiceOrErr<IChoiceHandlerManager>().ChoiceButtonLoader;
            using var _ = actor.RentChoices(out var existingChoices);
            foreach (var existingChoice in existingChoices)
                if (!choices.Contains(existingChoice))
                {
                    actor.RemoveChoice(existingChoice.Id);
                    existingChoice.Summary.Release(actor);
                }

            foreach (var choice in choices)
                if (!actor.AnyChoice(c => c == choice))
                {
                    if (!string.IsNullOrEmpty(choice.ButtonPath))
                        await buttonLoader.LoadOrErr(choice.ButtonPath);
                    await choice.Summary.Load(actor);
                    actor.AddChoice(choice);
                }
        }
    }
}
