using System;
using System.Collections.Generic;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A transient <see cref="ITextPrinterActor"/> implementation backed by
    /// <see cref="UITextPrinterPanel"/> with lifecycle managed outside Naninovel.
    /// </summary>
    [ActorResources(null, false)]
    [DefaultExecutionOrder(-1)] // Otherwise the PrinterPanel's Awakes are invoked before this one's and require Naninovel's APIs which is not yet initialized.
    [AddComponentMenu("Naninovel/ Actors/Transient UI Text Printer")]
    public class TransientUITextPrinter : TransientTextPrinter
    {
        public override event Action<string> OnAppearanceChanged { add => UI.OnAppearanceChanged += value; remove => UI.OnAppearanceChanged -= value; }
        public override event Action<bool> OnVisibilityChanged { add => UI.OnVisibilityChanged += value; remove => UI.OnVisibilityChanged -= value; }
        public override event Action<Vector3> OnPositionChanged { add => UI.OnPositionChanged += value; remove => UI.OnPositionChanged -= value; }
        public override event Action<Quaternion> OnRotationChanged { add => UI.OnRotationChanged += value; remove => UI.OnRotationChanged -= value; }
        public override event Action<Vector3> OnScaleChanged { add => UI.OnScaleChanged += value; remove => UI.OnScaleChanged -= value; }
        public override event Action<Color> OnTintColorChanged { add => UI.OnTintColorChanged += value; remove => UI.OnTintColorChanged -= value; }
        public override event Action OnMessagesChanged { add => UI.OnMessagesChanged += value; remove => UI.OnMessagesChanged -= value; }
        public override event Action OnTemplatesChanged { add => UI.OnTemplatesChanged += value; remove => UI.OnTemplatesChanged -= value; }

        [field: SerializeField, Tooltip("The UI panel backing the printer's implementation.")]
        public virtual UITextPrinterPanel PrinterPanel { get; private set; }

        public override string Appearance { get => UI.Appearance; set => UI.Appearance = value; }
        public override bool Visible { get => UI.Visible; set => UI.Visible = value; }
        public override Vector3 Position { get => UI.Position; set => UI.Position = value; }
        public override Quaternion Rotation { get => UI.Rotation; set => UI.Rotation = value; }
        public override Vector3 Scale { get => UI.Scale; set => UI.Scale = value; }
        public override Color TintColor { get => UI.TintColor; set => UI.TintColor = value; }
        public override float RevealProgress { get => UI.RevealProgress; set => UI.RevealProgress = value; }
        public override bool AnchoringAllowed { get => UI.AnchoringAllowed; set => UI.AnchoringAllowed = value; }

        protected virtual UIPrinter UI { get; private set; }

        public override Awaitable ChangeAppearance (string appearance, Tween tween, Transition? transition = default, AsyncToken token = default) => UI.ChangeAppearance(appearance, tween, transition, token);
        public override Awaitable ChangeVisibility (bool visible, Tween tween, AsyncToken token = default) => UI.ChangeVisibility(visible, tween, token);
        public override Awaitable ChangePosition (Vector3 position, Tween tween, AsyncToken token = default) => UI.ChangePosition(position, tween, token);
        public override Awaitable ChangeRotation (Quaternion rotation, Tween tween, AsyncToken token = default) => UI.ChangeRotation(rotation, tween, token);
        public override Awaitable ChangeScale (Vector3 scale, Tween tween, AsyncToken token = default) => UI.ChangeScale(scale, tween, token);
        public override Awaitable ChangeTintColor (Color tintColor, Tween tween, AsyncToken token = default) => UI.ChangeTintColor(tintColor, tween, token);
        public override void AddMessage (PrintedMessage message) => UI.AddMessage(message);
        public override void AppendText (LocalizableText text) => UI.AppendText(text);
        public override void SetMessages (IReadOnlyList<PrintedMessage> messages) => UI.SetMessages(messages);
        public override void CollectMessages (ICollection<PrintedMessage> messages) => UI.CollectMessages(messages);
        public override PrintedMessage? FindMessage (Predicate<PrintedMessage> filter) => UI.FindMessage(filter);
        public override void SetTemplates (IReadOnlyList<MessageTemplate> templates) => UI.SetTemplates(templates);
        public override void CollectTemplates (ICollection<MessageTemplate> templates) => UI.CollectTemplates(templates);
        public override MessageTemplate? FindTemplate (Predicate<MessageTemplate> filter) => UI.FindTemplate(filter);
        public override Awaitable Reveal (float delay, AsyncToken token = default) => UI.Reveal(delay, token);

        protected override void Awake ()
        {
            base.Awake();
            ObjectUtils.AssertRequiredObjects(PrinterPanel);
            PrinterPanel.gameObject.SetActive(false);
        }

        public override void InitializeTransientActor ()
        {
            UI = new(ActorId, Metadata, PrinterPanel);
            base.InitializeTransientActor();
            PrinterPanel.gameObject.SetActive(true);
            PrinterPanel.Initialize().Forget();
            UI.Initialize().Forget();
        }

        protected class UIPrinter : UITextPrinter
        {
            public override UITextPrinterPanel PrinterPanel => panel;

            private readonly UITextPrinterPanel panel;

            public UIPrinter (string id, TextPrinterMetadata meta, UITextPrinterPanel panel) : base(id, meta)
            {
                this.panel = panel;
            }

            public override Awaitable Initialize ()
            {
                if (Engine.TryGetService<ILocalizationManager>(out var l10n))
                    l10n.OnLocaleChanged += HandleLocaleChanged;
                SetTemplates(Array.Empty<MessageTemplate>());
                SetMessages(Array.Empty<PrintedMessage>());
                RevealProgress = 0;
                Visible = false;
                return Async.Completed;
            }

            protected override Vector3 GetBehaviourPosition () => PrinterPanel.Content.localPosition;
            protected override void SetBehaviourPosition (Vector3 position) => PrinterPanel.Content.localPosition = (Vector2)position;
            protected override Quaternion GetBehaviourRotation () => PrinterPanel.Content.localRotation;
            protected override void SetBehaviourRotation (Quaternion rotation) => PrinterPanel.Content.localRotation = rotation;
        }
    }
}
