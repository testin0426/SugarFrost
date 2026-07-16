using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Makes [UI elements](/guide/gui) with the specified resource names visible.
When no names are specified, will reveal the entire UI (in case it was hidden with [@hideUI]).",
        null,
        @"
; Given you've added a custom UI with 'Calendar' name,
; the following will make it visible on the scene.
@showUI Calendar",
        @"
; Given you've hidden the entire UI with @hideUI, show it back.
@showUI",
        @"
; Simultaneously reveal built-in 'TipsUI' and custom 'Calendar' UIs.
@showUI TipsUI,Calendar"
    )]
    [Serializable, UIGroup, Icon("Subtitles")]
    public class ShowUI : Command
    {
        [Doc("Name of the UI resource to make visible.")]
        [Alias(NamelessParameterAlias), ResourceContext(UIConfiguration.DefaultUIPathPrefix)]
        public StringListParameter UINames;
        [Doc("Duration (in seconds) of the show animation. When not specified, will use UI-specific duration.")]
        [Alias("time")]
        public DecimalParameter Duration;
        [Doc("Whether to wait for the UI fade-in animation before playing next command.")]
        public BooleanParameter Wait;

        public override Awaitable Execute (ExecutionContext ctx)
        {
            return WaitOrForget(Show, Wait, ctx);
        }

        protected virtual async Awaitable Show (ExecutionContext ctx)
        {
            var uis = Engine.GetServiceOrErr<IUIManager>();

            if (!Assigned(UINames))
            {
                uis.SetUIVisibleWithToggle(true);
                return;
            }

            using var _ = Async.Rent(out var tasks);
            foreach (var name in UINames)
            {
                var ui = uis.GetUI(name);
                if (ui is null)
                {
                    Warn($"Failed to show '{name}' UI: managed UI with the specified resource name not found.");
                    continue;
                }
                tasks.Add(ui.ChangeVisibility(true, Assigned(Duration) ? Duration : null, ctx.Token));
            }

            await Async.All(tasks);
        }
    }
}
