using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Appends specified text to a text printer.",
        @"
The entire text is appended instantly, without triggering the reveal effect.",
        @"
; Print first part of the sentence as usual (with gradual reveal),
; then append the end of the sentence at once.
Lorem ipsum
@append "" dolor sit amet."""
    )]
    [Serializable, Alias("append"), Icon("Plus"), TextGroup]
    public class AppendText : PrinterCommand, Command.IPreloadable, Command.ILocalizable
    {
        [Doc("The text to append.")]
        [Alias(NamelessParameterAlias), RequiredParameter]
        public LocalizableTextParameter Text;
        [Doc("ID of the printer actor to use. Will use a default one when not specified.")]
        [Alias("printer"), ActorContext(TextPrintersConfiguration.DefaultPathPrefix)]
        public StringParameter PrinterId;
        [Doc("ID of the actor, which should be associated with the appended text.")]
        [Alias("author"), ActorContext(CharactersConfiguration.DefaultPathPrefix)]
        public StringParameter AuthorId;

        protected override string AssignedPrinterId => PrinterId;
        protected override string AssignedAuthorId => AuthorId;
        protected virtual IUIManager UIManager => Engine.GetServiceOrErr<IUIManager>();

        public override async Awaitable PreloadResources ()
        {
            await base.PreloadResources();
            await PreloadStaticTextResources(Text);
        }

        public override void ReleaseResources ()
        {
            base.ReleaseResources();
            ReleaseStaticTextResources(Text);
        }

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            using var _ = await LoadDynamicTextResources(Text);
            var printer = await GetOrAddPrinter(ctx.Token);
            printer.AppendText(Text);
            printer.RevealProgress = 1f;
            UIManager.GetUI<UI.IBacklogUI>()?.AppendMessage(Text);
        }
    }
}
