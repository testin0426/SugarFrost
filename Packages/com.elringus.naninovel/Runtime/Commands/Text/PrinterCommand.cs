using UnityEngine;

namespace Naninovel.Commands
{
    public abstract class PrinterCommand : Command
    {
        protected abstract string AssignedPrinterId { get; }
        protected virtual string AssignedAuthorId => null;

        protected virtual ITextPrinterManager Printers => Engine.GetServiceOrErr<ITextPrinterManager>();
        protected virtual ICharacterManager Characters => Engine.GetServiceOrErr<ICharacterManager>();
        protected virtual TextPrintersConfiguration Configuration => Printers.Configuration;

        public virtual async Awaitable PreloadResources ()
        {
            await GetOrAddPrinter();
        }

        public virtual void ReleaseResources () { }

        protected virtual async Awaitable<ITextPrinterActor> GetOrAddPrinter (AsyncToken token = default)
        {
            var printerId = default(string);

            if (string.IsNullOrEmpty(AssignedPrinterId) && !string.IsNullOrEmpty(AssignedAuthorId))
                printerId = Characters.Configuration.GetMetadataOrDefault(AssignedAuthorId).LinkedPrinter;

            if (string.IsNullOrEmpty(printerId))
                printerId = AssignedPrinterId;

            var printer = await Printers.GetOrAddActor(printerId ?? Printers.DefaultPrinterId);
            token.ThrowIfCanceled();
            return printer;
        }
    }
}
