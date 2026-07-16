using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Serializable state of a <see cref="ITextPrinterActor"/>.
    /// </summary>
    [System.Serializable]
    public class TextPrinterState : ActorState<ITextPrinterActor>
    {
        /// <inheritdoc cref="ITextPrinterActor.Messages"/>
        public List<PrintedMessage> Messages => messages;
        /// <inheritdoc cref="ITextPrinterActor.Templates"/>
        public List<MessageTemplate> Templates => templates;
        /// <inheritdoc cref="ITextPrinterActor.RevealProgress"/>
        public float RevealProgress => revealProgress;

        [SerializeField] private List<PrintedMessage> messages = new();
        [SerializeField] private List<MessageTemplate> templates = new();
        [SerializeField] private float revealProgress;
        [SerializeField] private bool anchoringAllowed = true;

        public override void OverwriteFromActor (ITextPrinterActor actor)
        {
            base.OverwriteFromActor(actor);

            using var _ = actor.RentTemplates(out var newTemplates);
            using var __ = actor.RentMessages(out var newMessages);
            templates.ReplaceWith(newTemplates);
            messages.ReplaceWith(newMessages);
            revealProgress = actor.RevealProgress;
            anchoringAllowed = actor.AnchoringAllowed;
        }

        public override async Awaitable ApplyToActor (ITextPrinterActor actor)
        {
            await base.ApplyToActor(actor);

            using var _ = Async.Rent(out var tasks);
            foreach (var message in messages)
            {
                tasks.Add(message.Text.Load(actor));
                if (message.Author is { Label: { IsEmpty: false } label })
                    tasks.Add(label.Load(actor));
            }
            await Async.All(tasks);

            actor.SetTemplates(templates);
            actor.SetMessages(messages);
            actor.RevealProgress = revealProgress;
            actor.AnchoringAllowed = anchoringAllowed;
        }
    }
}
