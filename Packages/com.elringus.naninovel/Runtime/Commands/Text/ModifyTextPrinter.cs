using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Modifies a [text printer actor](/guide/text-printers).",
        null,
        @"
; Will make 'Wide' printer default and hide any other visible printers.
@printer Wide",
        @"
; Will assign 'Right' appearance to 'Bubble' printer, make it default,
; position at the center of the scene and won't hide other printers.
@printer Bubble.Right pos:50,50 !hideOther"
    )]
    [Serializable, Alias("printer"), TextGroup, Icon("CommentDuo")]
    [ActorContext(TextPrintersConfiguration.DefaultPathPrefix, paramId: "Id")]
    public class ModifyTextPrinter : ModifyOrthoActor<ITextPrinterActor, TextPrinterState, TextPrinterMetadata, TextPrintersConfiguration, ITextPrinterManager>
    {
        [Doc("ID of the printer to modify and the appearance to set. When ID or appearance are not specified, will use default ones.")]
        [Alias(NamelessParameterAlias), ActorContext(TextPrintersConfiguration.DefaultPathPrefix, 0), AppearanceContext(1)]
        public NamedStringParameter IdAndAppearance;
        [Doc("Whether to make the printer the default one. Default printer will be subject of all the printer-related commands when `printer` parameter is not specified.")]
        [Alias("default"), ParameterDefaultValue("true")]
        public BooleanParameter MakeDefault;
        [Doc("Whether to hide all the other printers.")]
        [ParameterDefaultValue("true")]
        public BooleanParameter HideOther;
        [Doc("Whether to allow auto printer positioning via actor anchors. Enable for supported printers after manually positioning a printer to resume automatic positioning. Note that anchoring is disabled automatically when an explicit position is assigned with this command.")]
        [Alias("anchor")]
        public BooleanParameter AllowAnchoring;

        protected override bool AllowPreload => !Assigned(IdAndAppearance) || !IdAndAppearance.DynamicValue;
        protected override string AssignedId => !string.IsNullOrEmpty(IdAndAppearance?.Name) ? IdAndAppearance.Name : ActorManager.DefaultPrinterId;
        protected override string AlternativeAppearance => IdAndAppearance?.NamedValue;

        protected override async Awaitable Modify (ExecutionContext ctx)
        {
            await base.Modify(ctx);

            if (Assigned(AllowAnchoring))
                ActorManager.GetActorOrErr(AssignedId).AnchoringAllowed = AllowAnchoring;
            else if (Assigned(Position) || Assigned(ScenePosition))
                ActorManager.GetActorOrErr(AssignedId).AnchoringAllowed = false;

            if (GetAssignedOrDefault(MakeDefault, true) && !string.IsNullOrEmpty(AssignedId))
                ActorManager.DefaultPrinterId = AssignedId;

            if (GetAssignedOrDefault(HideOther, true))
            {
                var tween = new Tween(AssignedDuration, complete: !AssignedLazy);
                using var _ = ActorManager.RentActors(out var printers);
                foreach (var printer in printers)
                    if (printer.Id != AssignedId && printer.Visible)
                        printer.ChangeVisibility(false, tween, token: ctx.Token).Forget();
            }
        }
    }
}
