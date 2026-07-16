using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A transient <see cref="IChoiceHandlerActor"/> implementation with lifecycle managed outside of Naninovel.
    /// </summary>
    [ActorResources(null, false)]
    [AddComponentMenu("Naninovel/ Actors/Transient Choice Handler")]
    public class TransientChoiceHandler : TransientActor<IChoiceHandlerManager, ChoiceHandlerMetadata>, IChoiceHandlerActor
    {
        public virtual event Action<Choice> OnChoiceAdded;
        public virtual event Action<Choice> OnChoiceRemoved;
        public virtual event Action<Choice> OnChoiceHandled;

        protected virtual List<Choice> Choices { get; } = new();

        public virtual void AddChoice (Choice choice)
        {
            Choices.Add(choice);
            OnChoiceAdded?.Invoke(choice);
        }

        public virtual void RemoveChoice (string id)
        {
            for (var i = Choices.Count - 1; i >= 0; i--)
            {
                var choice = Choices[i];
                if (choice.Id != id) continue;
                Choices.RemoveAt(i);
                OnChoiceRemoved?.Invoke(choice);
            }
        }

        public virtual void HandleChoice (string id)
        {
            foreach (var choice in Choices)
                if (choice.Id == id)
                {
                    OnChoiceHandled?.Invoke(choice);
                    break;
                }
        }

        public virtual void CollectChoices (IList<Choice> choices)
        {
            foreach (var choice in Choices)
                choices.Add(choice);
        }

        public virtual Choice? FindChoice (Predicate<Choice> filter)
        {
            foreach (var choice in Choices)
                if (filter(choice))
                    return choice;
            return null;
        }
    }
}
