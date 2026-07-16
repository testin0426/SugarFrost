using System.Collections.Generic;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Used by <see cref="UITextPrinter"/> to control the printed text.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UITextPrinterPanel : CustomUI, IManagedUI
    {
        /// <summary>
        /// Contents of the printer to be used for transformations.
        /// </summary>
        public virtual RectTransform Content => content;
        /// <summary>
        /// The reveal ratio of the assigned messages, in 0.0 to 1.0 range.
        /// </summary>
        public abstract float RevealProgress { get; set; }
        /// <summary>
        /// Appearance of the printer.
        /// </summary>
        public abstract string Appearance { get; set; }
        /// <summary>
        /// Tint color of the printer.
        /// </summary>
        public virtual Color TintColor { get => tintColor; set => SetTintColor(value); }
        /// <summary>
        /// Formatting templates to apply by default (before any templates assigned via '@format' command) for the messages printed by the printer actor.
        /// </summary>
        public virtual IReadOnlyList<MessageTemplate> DefaultTemplates => defaultTemplates;
        /// <summary>
        /// Whether auto-positioning via <see cref="IActorAnchor"/> is allowed for this printer.
        /// </summary>
        public virtual bool AnchoringAllowed { get; set; } = true;

        protected virtual ICharacterManager Characters { get; private set; }
        protected virtual List<PrintedMessage> Messages { get; } = new();
        protected virtual List<MessageTemplate> Templates { get; } = new();

        [Header("Base Printer Setup")]
        [Tooltip("Transform used for printer position, scale and rotation external manipulations.")]
        [SerializeField] private RectTransform content;
        [Tooltip("Formatting templates to apply by default (before any templates assigned via '@format' command) for the messages printed by the printer actor." +
                 "\n\n%TEXT% is replaced with the message text, %AUTHOR% — with the author name." +
                 "\n\nThe templates are applied in order and filtered by the author: '+' applies for any authored message, '-' — for un-authored messages and '*' for all messages, authored or not.")]
        [SerializeField] private List<MessageTemplate> defaultTemplates = new();
        [Header("Base Printer Behaviour")]
        [Tooltip("Occurs when tint color of the printer actor is changed.")]
        [SerializeField] private ColorUnityEvent onTintChanged;

        private IScriptPlayer player;
        private Color tintColor = Color.white;

        public override async Awaitable Initialize ()
        {
            await base.Initialize();
            if (this) player.OnAwaitInput += HandleAwaitInput;
        }

        Awaitable IManagedUI.ChangeVisibility (bool visible, float? duration, AsyncToken token)
        {
            Engine.Err("@showUI and @hideUI commands can't be used with text printers; use @show/hide or @show/hidePrinter commands instead");
            return Async.Completed;
        }

        /// <summary>
        /// Assigns text messages to print.
        /// </summary>
        public abstract void SetMessages (IReadOnlyList<PrintedMessage> messages);
        /// <summary>
        /// Adds specified text message to print.
        /// </summary>
        public abstract void AddMessage (PrintedMessage message);
        /// <summary>
        /// Appends specified text to the last printed message, or adds new message with the text when no messages printed.
        /// </summary>
        public abstract void AppendText (LocalizableText text);
        /// <summary>
        /// Assigns templates to format subsequent printed messages (doesn't affect current messages).
        /// </summary>
        public virtual void SetTemplates (IReadOnlyList<MessageTemplate> templates) => Templates.ReplaceWith(templates);
        /// <summary>
        /// Reveals the <see cref="Messages"/>'s text char by char over time.
        /// </summary>
        /// <param name="delay">Delay (in seconds) between revealing consecutive characters.</param>
        /// <param name="token">The reveal should be canceled when requested by the specified token.</param>
        public abstract Awaitable RevealMessages (float delay, AsyncToken token);
        /// <summary>
        /// Controls visibility of the wait for input indicator.
        /// </summary>
        public abstract void SetAwaitInputIndicatorVisible (bool visible);

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(content);

            player = Engine.GetServiceOrErr<IScriptPlayer>();
            Characters = Engine.GetServiceOrErr<ICharacterManager>();
        }

        protected override void OnDestroy ()
        {
            if (player != null) player.OnAwaitInput -= HandleAwaitInput;
            base.OnDestroy();
        }

        protected virtual void SetTintColor (Color color)
        {
            tintColor = color;
            onTintChanged?.Invoke(color);
        }

        protected virtual string FormatMessage (PrintedMessage message)
        {
            var text = (string)message.Text;
            if (DefaultTemplates.Count + Templates.Count == 0) return text;

            var authorLabel = message.Author is { Id: { Length: > 0 } authorId } author
                ? (author.Label.IsEmpty ? Characters.GetAuthorName(authorId) : author.Label)
                : "";
            foreach (var template in DefaultTemplates)
                if (template.Applicable(message.Author?.Id))
                    ApplyTemplate(template.Template);
            foreach (var template in Templates)
                if (template.Applicable(message.Author?.Id))
                    ApplyTemplate(template.Template);

            return text;

            void ApplyTemplate (string template)
            {
                text = template.Replace("%AUTHOR%", authorLabel).Replace("%TEXT%", text);
            }
        }

        protected virtual void HandleAwaitInput (IScriptTrack track)
        {
            if (this) SetAwaitInputIndicatorVisible(track.AwaitingInput);
        }
    }
}
