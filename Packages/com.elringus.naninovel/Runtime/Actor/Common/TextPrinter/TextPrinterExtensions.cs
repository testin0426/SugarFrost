using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="ITextPrinterManager"/> and <see cref="ITextPrinterActor"/>.
    /// </summary>
    public static class TextPrinterExtensions
    {
        /// <summary>
        /// Rents a pooled list and collects currently assigned messages.
        /// </summary>
        public static IDisposable RentMessages (this ITextPrinterActor actor, out List<PrintedMessage> messages)
        {
            var rent = ListPool<PrintedMessage>.Rent(out messages);
            actor.CollectMessages(messages);
            return rent;
        }

        /// <summary>
        /// Whether any of the currently assigned messages satisfy the specified filter;
        /// when filter is not specified returns whether any messages are assigned at all.
        /// </summary>
        public static bool AnyMessage (this ITextPrinterActor actor, [CanBeNull] Predicate<PrintedMessage> filter = null)
        {
            if (filter == null) return actor.FindMessage(_ => true) != null;
            return actor.FindMessage(filter) != null;
        }

        /// <summary>
        /// Removes all assigned messages.
        /// </summary>
        public static void ClearMessages (this ITextPrinterActor actor)
        {
            actor.SetMessages(Array.Empty<PrintedMessage>());
        }

        /// <summary>
        /// Rents a pooled list and collects currently applied formatting templates.
        /// </summary>
        public static IDisposable RentTemplates (this ITextPrinterActor actor, out List<MessageTemplate> templates)
        {
            var rent = ListPool<MessageTemplate>.Rent(out templates);
            actor.CollectTemplates(templates);
            return rent;
        }

        /// <summary>
        /// Whether any of the currently applied formatting templates satisfy the specified filter;
        /// when filter is not specified returns whether any templates are applied at all.
        /// </summary>
        public static bool AnyTemplate (this ITextPrinterActor actor, [CanBeNull] Predicate<MessageTemplate> filter = null)
        {
            if (filter == null) return actor.FindTemplate(_ => true) != null;
            return actor.FindTemplate(filter) != null;
        }

        /// <summary>
        /// Removes all applied formatting templates.
        /// </summary>
        public static void ClearTemplates (this ITextPrinterActor actor)
        {
            actor.SetTemplates(Array.Empty<MessageTemplate>());
        }
    }
}
