using System;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Allows entering the title screen (main menu) and invoking associated callbacks.
    /// </summary>
    public static class TitleScreen
    {
        public const string NewGameCallbackLabel = "OnNewGame";
        public const string LoadCallbackLabel = "OnLoad";
        public const string ExitCallbackLabel = "OnExit";

        /// <summary>
        /// Whether the title screen is enabled in the configuration (a title script is assigned) and can be entered.
        /// </summary>
        public static bool Enabled => !string.IsNullOrEmpty(Engine.GetConfiguration<ScriptsConfiguration>().TitleScript);

        /// <summary>
        /// Enters the title screen.
        /// </summary>
        public static async Awaitable Enter ()
        {
            var scripts = Engine.GetServiceOrErr<IScriptManager>();
            var path = scripts.Configuration.TitleScript;
            if (string.IsNullOrEmpty(path))
            {
                Engine.Err("Failed to enter title screen: 'Title Script' is not assigned in the configuration.");
                return;
            }

            if (!scripts.ScriptLoader.Exists(path))
            {
                // Title script is assigned, but missing. May happen if the user manually deleted the default
                // title script, or it failed to scaffold in a new project. Warn and attempt to show the title UI.
                Engine.Warn($"Assigned title script '{path}' is missing. Will show the title UI instead. " +
                            "Check the scripts configuration and make sure the assigned 'Title Script' exist.");
                if (Engine.GetService<IUIManager>()?.GetUI<ITitleUI>() is { } ui) ui.Show();
                return;
            }

            await Engine.GetServiceOrErr<IScriptPlayer>().MainTrack.LoadAndPlay(path);
        }

        /// <summary>
        /// When <see cref="ScriptsConfiguration.TitleScript"/> is assigned and has the specified callback label,
        /// plays it; a noop otherwise.
        /// </summary>
        /// <param name="label">The callback label inside the title script to play.</param>
        public static async Awaitable PlayCallback (string label, AsyncToken token = default)
        {
            var scripts = Engine.GetServiceOrErr<IScriptManager>();
            var path = scripts.Configuration.TitleScript;
            if (string.IsNullOrEmpty(path) || !scripts.ScriptLoader.Exists(path)) return;

            var script = (Script)await scripts.ScriptLoader.LoadOrErr(path);
            if (!script.LabelExists(label)) return;

            var player = Engine.GetServiceOrErr<IScriptPlayer>();
            var track = player.MainTrack;
            player.ResetService();

            await track.LoadAndPlayAtLabel(path, label: label);
            try
            {
                while (token.EnsureNotCanceled() && (track.Executing || track.AwaitingInput))
                    if (token.Completed && !track.Completing && track.Executing) await track.Complete();
                    else await Async.NextFrame(token);
            }
            catch (OperationCanceledException) { return; }
        }
    }
}
