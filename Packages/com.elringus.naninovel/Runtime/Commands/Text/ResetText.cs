using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Resets (clears) the contents of a text printer.",
        null,
        @"
; Print and then clear contents of the default printer.
This line will disappear.
@resetText",
        @"
; Same as above, but with 'Wide' printer.
@print ""This line will disappear."" printer:Wide
@resetText Wide"
    )]
    [Serializable, TextGroup, Icon("TextSlashDuo")]
    public class ResetText : PrinterCommand
    {
        [Doc("ID of the printer actor to use. Will use a default one when not specified.")]
        [Alias(NamelessParameterAlias), ActorContext(TextPrintersConfiguration.DefaultPathPrefix)]
        public StringParameter PrinterId;

        protected override string AssignedPrinterId => PrinterId;

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            var printer = await GetOrAddPrinter(ctx.Token);
            printer.RevealProgress = 0f;
            printer.ClearMessages();
        }
    }
}
