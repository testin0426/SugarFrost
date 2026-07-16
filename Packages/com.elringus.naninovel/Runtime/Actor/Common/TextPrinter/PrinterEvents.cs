using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Routes essential <see cref="ITextPrinterManager"/> APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Printer Events")]
    public class PrinterEvents : UnityEvents
    {
        [ActorPopup(TextPrintersConfiguration.DefaultPathPrefix), CanBeNull]
        [Tooltip("The identifier of the printer actor for the events and actions. Leave empty for default.")]
        public string PrinterId;

        [Space]
        [Tooltip("Occurs when availability of the printer manager engine service changes.")]
        public BoolUnityEvent ServiceAvailable;
        [Tooltip("Occurs when a printer with the specified ID starts printing the message.")]
        public StringUnityEvent PrintStarted;
        [Tooltip("Occurs when a printer with the specified ID finishes printing the message.")]
        public StringUnityEvent PrintFinished;

        public void Print (string message)
        {
            if (!Engine.TryGetService<ITextPrinterManager>(out var manager)) return;
            manager.Print(GetActorIdOrDefault(), new(message));
        }

        public void HidePrinter ()
        {
            if (!Engine.TryGetService<ITextPrinterManager>(out var manager)) return;
            if (!manager.ActorExists(GetActorIdOrDefault())) return;
            manager.GetActorOrErr(GetActorIdOrDefault()).ChangeVisibility(false, new(manager.Configuration.DefaultDuration));
        }

        public void HideAllPrinters ()
        {
            if (!Engine.TryGetService<ITextPrinterManager>(out var manager)) return;
            using var _ = manager.RentActors(out var printers);
            foreach (var printer in printers)
                printer.ChangeVisibility(false, new(manager.Configuration.DefaultDuration)).Forget();
        }

        protected override void HandleEngineInitialized ()
        {
            if (Engine.TryGetService<ITextPrinterManager>(out var manager))
            {
                ServiceAvailable?.Invoke(true);

                manager.OnPrintStarted -= HandlePrintStarted;
                manager.OnPrintStarted += HandlePrintStarted;

                manager.OnPrintFinished -= HandlePrintFinished;
                manager.OnPrintFinished += HandlePrintFinished;
            }
            else ServiceAvailable?.Invoke(false);
        }

        protected override void HandleEngineDestroyed ()
        {
            ServiceAvailable?.Invoke(false);
        }

        protected virtual void HandlePrintStarted (PrintMessageArgs args)
        {
            if (args.Printer.Id == GetActorIdOrDefault())
                PrintStarted?.Invoke(args.Message.Text);
        }

        protected virtual void HandlePrintFinished (PrintMessageArgs args)
        {
            if (args.Printer.Id == GetActorIdOrDefault())
                PrintFinished?.Invoke(args.Message.Text);
        }

        private string GetActorIdOrDefault ()
        {
            if (!string.IsNullOrEmpty(PrinterId)) return PrinterId;
            return Engine.GetServiceOrErr<ITextPrinterManager>().Configuration.DefaultPrinterId;
        }
    }
}
