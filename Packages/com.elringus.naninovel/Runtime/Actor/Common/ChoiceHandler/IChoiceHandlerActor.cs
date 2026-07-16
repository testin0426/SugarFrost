using System;
using System.Collections.Generic;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to represent a choice handler actor on scene.
    /// </summary>
    public interface IChoiceHandlerActor : IActor
    {
        /// <summary>
        /// Occurs when a choice is added.
        /// </summary>
        event Action<Choice> OnChoiceAdded;
        /// <summary>
        /// Occurs when a choice is removed.
        /// </summary>
        event Action<Choice> OnChoiceRemoved;
        /// <summary>
        /// Occurs when a choice is handled (selected).
        /// </summary>
        event Action<Choice> OnChoiceHandled;

        /// <summary>
        /// Adds an option to choose from.
        /// </summary>
        void AddChoice (Choice choice);
        /// <summary>
        /// Removes a choice option with the specified ID.
        /// </summary>
        void RemoveChoice (string id);
        /// <summary>
        /// Selects a choice option with the specified ID.
        /// </summary>
        void HandleChoice (string id);
        /// <summary>
        /// Collects currently available options to choose from to specified list, in the added order.
        /// </summary>
        void CollectChoices (IList<Choice> choices);
        /// <summary>
        /// Returns the first options that satisfy the specified filter or null.
        /// </summary>
        Choice? FindChoice (Predicate<Choice> filter);
    }
}
