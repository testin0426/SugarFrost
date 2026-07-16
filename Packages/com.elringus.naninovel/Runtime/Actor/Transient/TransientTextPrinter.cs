using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A transient <see cref="ITextPrinterActor"/> implementation with lifecycle managed outside Naninovel.
    /// </summary>
    [ActorResources(null, false)]
    [AddComponentMenu("Naninovel/ Actors/Transient Text Printer")]
    public class TransientTextPrinter : TransientActor<ITextPrinterManager, TextPrinterMetadata>, ITextPrinterActor
    {
        public virtual event Action OnMessagesChanged;
        public virtual event Action OnTemplatesChanged;

        public virtual float RevealProgress { get; set; }
        public virtual bool AnchoringAllowed { get; set; }
        public virtual PrintedMessage? FinalMessage => Messages.Count > 0 ? Messages[^1] : null;

        protected virtual List<PrintedMessage> Messages { get; } = new();
        protected virtual List<MessageTemplate> Templates { get; } = new();

        public virtual void AddMessage (PrintedMessage message)
        {
            Messages.Add(message);
            OnMessagesChanged?.Invoke();
        }

        public virtual void AppendText (LocalizableText text)
        {
            if (Messages.Count == 0)
            {
                AddMessage(new(text));
                return;
            }
            Messages[^1] = new(Messages[^1].Text + text, Messages[^1].Author ?? default);
            OnMessagesChanged?.Invoke();
        }

        public virtual void SetMessages (IReadOnlyList<PrintedMessage> messages)
        {
            Messages.Clear();
            Messages.AddRange(messages);
            OnMessagesChanged?.Invoke();
        }

        public virtual void CollectMessages (ICollection<PrintedMessage> messages)
        {
            foreach (var message in Messages)
                messages.Add(message);
        }

        public virtual PrintedMessage? FindMessage (Predicate<PrintedMessage> filter)
        {
            foreach (var message in Messages)
                if (filter(message))
                    return message;
            return null;
        }

        public virtual void SetTemplates (IReadOnlyList<MessageTemplate> templates)
        {
            Templates.Clear();
            Templates.AddRange(templates);
            OnTemplatesChanged?.Invoke();
        }

        public virtual void CollectTemplates (ICollection<MessageTemplate> templates)
        {
            foreach (var template in Templates)
                templates.Add(template);
        }

        public virtual MessageTemplate? FindTemplate (Predicate<MessageTemplate> filter)
        {
            foreach (var template in Templates)
                if (filter(template))
                    return template;
            return null;
        }

        public virtual Awaitable Reveal (float delay, AsyncToken token = default)
        {
            return Async.Completed;
        }
    }
}
