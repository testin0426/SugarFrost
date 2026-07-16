using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="IChoiceHandlerManager"/> and <see cref="IChoiceHandlerActor"/>.
    /// </summary>
    public static class ChoiceHandlerExtensions
    {
        /// <summary>
        /// Rents a pooled list and collects currently available options, in the added order.
        /// </summary>
        public static IDisposable RentChoices (this IChoiceHandlerActor actor, out List<Choice> choices)
        {
            var rent = ListPool<Choice>.Rent(out choices);
            actor.CollectChoices(choices);
            return rent;
        }

        /// <summary>
        /// Whether any of the currently available options satisfy the specified filter;
        /// when filter is not specified returns whether any choices are available at all.
        /// </summary>
        public static bool AnyChoice (this IChoiceHandlerActor actor, [CanBeNull] Predicate<Choice> filter = null)
        {
            if (filter == null) return actor.FindChoice(_ => true) != null;
            return actor.FindChoice(filter) != null;
        }
    }
}
