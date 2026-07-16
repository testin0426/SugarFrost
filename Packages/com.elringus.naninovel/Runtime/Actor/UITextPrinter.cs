using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ITextPrinterActor"/> implementation using <see cref="UITextPrinterPanel"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(UITextPrinterPanel), false)]
    public class UITextPrinter : MonoBehaviourActor<TextPrinterMetadata>, ITextPrinterActor
    {
        public virtual event Action OnMessagesChanged;
        public virtual event Action OnTemplatesChanged;

        public override GameObject GameObject => PrinterPanel.gameObject;
        public override string Appearance { get => PrinterPanel.Appearance; set => SetAppearance(value); }
        public override bool Visible { get => PrinterPanel.Visible; set => SetVisible(value); }
        public virtual float RevealProgress { get => PrinterPanel.RevealProgress; set => SetRevealProgress(value); }
        public virtual bool AnchoringAllowed { get => PrinterPanel.AnchoringAllowed; set => PrinterPanel.AnchoringAllowed = value; }
        public virtual PrintedMessage? FinalMessage => Messages.Count > 0 ? Messages[^1] : null;
        public virtual UITextPrinterPanel PrinterPanel { get; private set; }

        protected virtual List<PrintedMessage> Messages { get; } = new();
        protected virtual List<MessageTemplate> Templates { get; } = new();
        protected virtual AspectMonitor AspectMonitor { get; } = new();

        private readonly IUIManager uis;
        private readonly ILocalizationManager l10n;
        private readonly IResourceProviderManager resources;

        private CancellationTokenSource revealTextCTS;

        public UITextPrinter (string id, TextPrinterMetadata meta)
            : base(id, meta)
        {
            uis = Engine.GetServiceOrErr<IUIManager>();
            l10n = Engine.GetServiceOrErr<ILocalizationManager>();
            resources = Engine.GetServiceOrErr<IResourceProviderManager>();
        }

        public override async Awaitable Initialize ()
        {
            await base.Initialize();
            var prefab = await LoadUIPrefab();
            PrinterPanel = await uis.AddUI(prefab, group: BuildActorGroup()) as UITextPrinterPanel;
            if (!PrinterPanel) throw new Error($"Failed to initialize '{Id}' printer actor: printer panel UI instantiation failed.");
            AspectMonitor.OnChanged += HandleAspectChanged;
            AspectMonitor.Start(target: PrinterPanel);
            l10n.OnLocaleChanged += HandleLocaleChanged;
            SetTemplates(Array.Empty<MessageTemplate>());
            SetMessages(Array.Empty<PrintedMessage>());
            RevealProgress = 0;
            Visible = false;
        }

        public override Awaitable ChangeAppearance (string appearance, Tween tween,
            Transition? transition = default, AsyncToken token = default)
        {
            Appearance = appearance;
            return Async.Completed;
        }

        public override async Awaitable ChangeVisibility (bool visible, Tween tween, AsyncToken token = default)
        {
            base.Visible = visible;
            await PrinterPanel.ChangeVisibility(visible, tween.Duration, token);
        }

        public virtual void AddMessage (PrintedMessage message)
        {
            message.Text.Hold(this);
            message.Author?.Label.Hold(this);
            Messages.Add(message);
            PrinterPanel.AddMessage(message);
            OnMessagesChanged?.Invoke();
        }

        public virtual void AppendText (LocalizableText text)
        {
            if (Messages.Count == 0)
            {
                AddMessage(new(text));
                return;
            }
            text.Hold(this);
            Messages[^1] = new(Messages[^1].Text + text, Messages[^1].Author ?? default);
            PrinterPanel.AppendText(text);
            OnMessagesChanged?.Invoke();
        }

        public virtual void SetMessages (IReadOnlyList<PrintedMessage> messages)
        {
            var count = Mathf.Max(Messages.Count, messages.Count);
            for (int i = 0; i < count; i++)
            {
                var from = Messages.ElementAtOrDefault(i);
                var to = messages.ElementAtOrDefault(i);
                from.Text.Juggle(to.Text, this);
                if (from.Author is { Label: { IsEmpty: false } label })
                    label.Juggle(to.Author?.Label ?? default, this);
                else to.Author?.Label.Hold(this);
            }
            Messages.ReplaceWith(messages);
            PrinterPanel.SetMessages(messages);
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
            Templates.ReplaceWith(templates);
            PrinterPanel.SetTemplates(templates);
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

        public virtual async Awaitable Reveal (float delay, AsyncToken token = default)
        {
            CancelRevealTextRoutine();
            revealTextCTS = CancellationTokenSource.CreateLinkedTokenSource(token.CancellationToken);
            var revealTextToken = new AsyncToken(revealTextCTS.Token, token.CompletionToken);
            await PrinterPanel.RevealMessages(delay, revealTextToken);
        }

        public override void Dispose ()
        {
            base.Dispose();

            AspectMonitor?.Stop();
            CancelRevealTextRoutine();

            if (PrinterPanel)
            {
                uis.RemoveUI(PrinterPanel);
                ObjectUtils.DestroyOrImmediate(PrinterPanel.gameObject);
                PrinterPanel = null;
            }

            if (l10n != null)
                l10n.OnLocaleChanged -= HandleLocaleChanged;
        }

        protected virtual async Awaitable<GameObject> LoadUIPrefab ()
        {
            return await ActorMeta.Loader.CreateLocalizableFor<GameObject>(resources, l10n).LoadOrErr(Id);
        }

        protected override GameObject CreateHostObject () => null;

        protected virtual void SetRevealProgress (float value)
        {
            CancelRevealTextRoutine();
            PrinterPanel.RevealProgress = value;
        }

        protected virtual void SetAppearance (string appearance)
        {
            base.Appearance = appearance;
            PrinterPanel.Appearance = appearance;
        }

        protected virtual void SetVisible (bool visible)
        {
            base.Visible = visible;
            PrinterPanel.Visible = visible;
        }

        protected override Vector3 GetBehaviourPosition ()
        {
            if (!PrinterPanel || !PrinterPanel.Content) return Vector3.zero;
            return PrinterPanel.Content.position;
        }

        protected override void SetBehaviourPosition (Vector3 position)
        {
            if (!PrinterPanel || !PrinterPanel.Content) return;
            PrinterPanel.Content.position = (Vector2)position; // don't change z-pos, as it'll break UI ordering
        }

        protected override Quaternion GetBehaviourRotation ()
        {
            if (!PrinterPanel || !PrinterPanel.Content) return Quaternion.identity;
            return PrinterPanel.Content.rotation;
        }

        protected override void SetBehaviourRotation (Quaternion rotation)
        {
            if (!PrinterPanel || !PrinterPanel.Content) return;
            PrinterPanel.Content.rotation = rotation;
        }

        protected override Vector3 GetBehaviourScale ()
        {
            if (!PrinterPanel || !PrinterPanel.Content) return Vector3.one;
            return PrinterPanel.Content.localScale;
        }

        protected override void SetBehaviourScale (Vector3 scale)
        {
            if (!PrinterPanel || !PrinterPanel.Content) return;
            PrinterPanel.Content.localScale = scale;
        }

        protected override Color GetBehaviourTintColor () => PrinterPanel.TintColor;

        protected override void SetBehaviourTintColor (Color value) => PrinterPanel.TintColor = value;

        protected virtual void HandleAspectChanged (AspectMonitor monitor)
        {
            // UI printers anchored to canvas borders are moved on aspect change;
            // re-set position here to return them to correct relative positions.
            SetBehaviourPosition(GetBehaviourPosition());
        }

        protected virtual void HandleLocaleChanged (LocaleChangedArgs _)
        {
            PrinterPanel.SetMessages(Messages);
        }

        protected virtual void CancelRevealTextRoutine ()
        {
            revealTextCTS?.Cancel();
            revealTextCTS?.Dispose();
            revealTextCTS = null;
        }
    }
}
