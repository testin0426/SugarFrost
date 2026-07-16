using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes a <see cref="ITextPrinterActor"/> with specified ID or an active one.
    /// </summary>
    public class ShakePrinter : ShakeTransform
    {
        private Canvas canvas;

        protected override Transform GetShakenTransform ()
        {
            var manager = Engine.GetServiceOrErr<ITextPrinterManager>();
            var id = string.IsNullOrEmpty(ObjectName) ? manager.DefaultPrinterId : ObjectName;
            if (id is null || !manager.ActorExists(id))
                throw new Error($"Failed to shake printer with '{id}' ID: actor not found.");
            var content = (manager.GetActorOrErr(id) as UITextPrinter)?.PrinterPanel.Content ??
                          (manager.GetActorOrErr(id) as TransientUITextPrinter)?.PrinterPanel.Content;
            if (!content) return null;
            canvas = content.FindTopmostComponent<Canvas>();
            return content;
        }

        protected override Vector3 GetShakeDelta ()
        {
            return base.GetShakeDelta() / canvas.scaleFactor;
        }
    }
}
