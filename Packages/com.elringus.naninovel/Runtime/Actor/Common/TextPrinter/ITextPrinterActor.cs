using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// An actor which is able to present, format and gradually reveal text messages.
    /// </summary>
    /// <remarks>
    /// While most printers use a single text block and concatenate multiple <see cref="PrintedMessage"/>,
    /// some may choose to distinguish the messages, for example chat printer.
    /// Additionally, <see cref="MessageTemplate"/> are expected to be applied to each message individually,
    /// which allows visually distinguishing even the concatenated messages, for example via line breaks.
    /// The way <see cref="RevealProgress"/> applies to the messages is up to the printer implementation.
    /// </remarks>
    public interface ITextPrinterActor : IActor
    {
        /// <summary>
        /// Occurs when the assigned text messages are changed.
        /// </summary>
        event Action OnMessagesChanged;
        /// <summary>
        /// Occurs when the applied formatting templates are changed.
        /// </summary>
        event Action OnTemplatesChanged;

        /// <summary>
        /// The reveal ratio of the assigned messages, in 0.0 to 1.0 range.
        /// </summary>
        float RevealProgress { get; set; }
        /// <summary>
        /// Whether auto-positioning via <see cref="IActorAnchor"/> is allowed for this printer.
        /// </summary>
        bool AnchoringAllowed { get; set; }
        /// <summary>
        /// The last (trailing) assigned message or null when no messages currently assigned.
        /// </summary>
        PrintedMessage? FinalMessage { get; }

        /// <summary>
        /// Adds specified message to the assigned messages.
        /// </summary>
        void AddMessage (PrintedMessage message);
        /// <summary>
        /// Appends specified text to the last assigned message, or assigns a new message
        /// with the specified text when no messages currently assigned.
        /// </summary>
        void AppendText (LocalizableText text);
        /// <summary>
        /// Assigns specified text messages; specify an empty collection to clear the printer.
        /// </summary>
        void SetMessages (IReadOnlyList<PrintedMessage> messages);
        /// <summary>
        /// Collects currently assigned text messages to the specified collection.
        /// </summary>
        void CollectMessages (ICollection<PrintedMessage> messages);
        /// <summary>
        /// Returns the first assigned message that satisfies the specified filter or null.
        /// </summary>
        PrintedMessage? FindMessage (Predicate<PrintedMessage> filter);
        /// <summary>
        /// Assigns specified formatting templates to be applied to the assigned messages.
        /// </summary>
        void SetTemplates (IReadOnlyList<MessageTemplate> templates);
        /// <summary>
        /// Collects current formatting templates to the specified collection.
        /// </summary>
        void CollectTemplates (ICollection<MessageTemplate> templates);
        /// <summary>
        /// Returns the first assigned formatting template that satisfies the specified filter or null.
        /// </summary>
        MessageTemplate? FindTemplate (Predicate<MessageTemplate> filter);
        /// <summary>
        /// Reveals the assigned messages over time.
        /// </summary>
        /// <param name="delay">Delay (in seconds) to wait after revealing each text character.</param>
        Awaitable Reveal (float delay, AsyncToken token = default);
    }
}
