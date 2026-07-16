using Naninovel.UI;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"Toggles wave animation on a text printer's revealed characters.",
        null,
        @"; Enable wave on default printer
@wave",
        @"; Disable
@wave !on",
        @"; Stronger, slower wave on a specific printer
@wave amp:8 freq:1 printer:Wide"
    )]
    [Alias("wave")]
    public class ToggleWave : PrinterCommand
    {
        [Alias("on"), ParameterDefaultValue("true")]
        public BooleanParameter Enable = true;
        [Alias("amp")]
        public DecimalParameter Amplitude;
        [Alias("freq")]
        public DecimalParameter Frequency;
        [Alias("phase")]
        public DecimalParameter PhasePerChar;
        [Alias("printer"), ActorContext(TextPrintersConfiguration.DefaultPathPrefix)]
        public StringParameter PrinterId;

        protected override string AssignedPrinterId => PrinterId;
        protected override string AssignedAuthorId => null;

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            var printer = await GetOrAddPrinter(ctx.Token);
            if (printer is not UITextPrinter ui) return;

            var wave = ui.PrinterPanel.GetComponentInChildren<RevealWave>(includeInactive: true);
            if (!wave) { Engine.Warn("@wave: no RevealWave component found on printer."); return; }

            if (Assigned(Amplitude)) wave.Amplitude = Amplitude;
            if (Assigned(Frequency)) wave.Frequency = Frequency;
            if (Assigned(PhasePerChar)) wave.PhasePerChar = PhasePerChar;

            wave.enabled = Enable;
        }
    }
}
