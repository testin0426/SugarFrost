using UnityEditor;

namespace Naninovel
{
    public class TextPrintersSettings : OrthoActorManagerSettings<TextPrintersConfiguration, ITextPrinterActor, TextPrinterMetadata>
    {
        protected override string HelpUri => "guide/text-printers";
        protected override string ResourcesSelectionTooltip => GetTooltip(EditedActorId, Configuration);
        protected override MetadataEditor<ITextPrinterActor, TextPrinterMetadata> MetadataEditor { get; } = new TextPrinterMetadataEditor();

        public static string GetTooltip (string actorId, TextPrintersConfiguration cfg)
        {
            if (actorId == cfg.DefaultPrinterId)
                return "This printer will be active by default: all the generic text and `@print` commands will use it to output the text. Use `@printer PrinterID` action to change active printer.";
            return $"Use `@printer {actorId}` in scenario scripts to set this printer active; all the subsequent generic text and `@print` commands will then use it to output the text.";
        }

        [MenuItem(MenuPath.Root + "/Resources/Text Printers")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}
