using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Shows a text printer.",
        null,
        @"
; Show a default printer.
@showPrinter",
        @"
; Show printer with ID 'Wide'.
@showPrinter Wide"
    )]
    [Serializable, TextGroup, Icon("CommentDuo")]
    public class ShowPrinter : PrinterCommand
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
            var showDuration = Assigned(Duration) ? Duration.Value : printerMeta.ChangeVisibilityDuration;
            if (ctx.Token.Completed) printer.Visible = true;
            else await printer.ChangeVisibility(true, new(showDuration), token: ctx.Token);
        }
    }
}
