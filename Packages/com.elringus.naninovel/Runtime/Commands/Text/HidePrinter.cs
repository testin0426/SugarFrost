using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Hides a text printer.",
        null,
        @"
; Hide a default printer.
@hidePrinter",
        @"
; Hide printer with ID 'Wide'.
@hidePrinter Wide"
    )]
    [Serializable, TextGroup, Icon("CommentSlashDuo")]
    public class HidePrinter : PrinterCommand
    {
        [Doc("ID of the printer actor to use. Will use a default one when not specified.")]
        [Alias(NamelessParameterAlias), ActorContext(TextPrintersConfiguration.DefaultPathPrefix)]
        public StringParameter PrinterId;
        [Doc(SharedDocs.DurationParameter)]
        [Alias("time")]
        public DecimalParameter Duration;
        [Doc(SharedDocs.WaitParameter)]
        public BooleanParameter Wait;

        protected override string AssignedPrinterId => PrinterId;

        public override Awaitable Execute (ExecutionContext ctx)
        {
            return WaitOrForget(Hide, Wait, ctx);
        }

        protected virtual async Awaitable Hide (ExecutionContext ctx)
        {
            var printer = await GetOrAddPrinter(ctx.Token);
            var printerMeta = Printers.Configuration.GetMetadataOrDefault(printer.Id);
            var hideDuration = Assigned(Duration) ? Duration.Value : printerMeta.ChangeVisibilityDuration;
            if (ctx.Token.Completed) printer.Visible = false;
            else await printer.ChangeVisibility(false, new(hideDuration), token: ctx.Token);
        }
    }
}
