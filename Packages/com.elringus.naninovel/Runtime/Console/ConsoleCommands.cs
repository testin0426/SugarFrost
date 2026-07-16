using System;
using System.Collections.Generic;
using System.Reflection;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Provides implementations of the built-in debug console commands.
    /// </summary>
    public static class ConsoleCommands
    {
        public static void InitializeConsole ()
        {
            var input = Engine.GetService<IInputManager>()?.GetToggleConsole();
            if (input != null) input.OnEnd += ConsoleGUI.Toggle;

            ConsoleGUI.OnShow = () => Engine.GetService<IUIManager>()?.GetUI<IClickThroughPanel>()?.Show(false, null, Inputs.ToggleConsole);
            ConsoleGUI.OnHide = () => Engine.GetService<IUIManager>()?.GetUI<IClickThroughPanel>()?.Hide();
            ConsoleGUI.ToggleKey = KeyCode.None;
            ConsoleGUI.Initialize(FindCommands());
            InputPreprocessor.AddPreprocessor(ProcessCommandInput);

            Engine.OnDestroyed -= ConsoleGUI.Destroy;
            Engine.OnDestroyed += ConsoleGUI.Destroy;

            static Dictionary<string, MethodInfo> FindCommands ()
            {
                var commands = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (var type in Engine.Types.ConsoleCommandHosts)
                {
                    var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        var method = methods[i];
                        var attr = method.GetCustomAttribute<ConsoleCommandAttribute>();
                        if (attr is null) continue;
                        commands[attr.Alias ?? method.Name] = method;
                    }
                }
                return commands;
            }
        }

        [ConsoleCommand("nav")]
        public static void ToggleScriptNavigator ()
        {
            if (!Engine.GetServiceOrErr<IUIManager>().TryGetUI<IScriptNavigatorUI>(out var nav)) return;
            if (nav.Visible) nav.Hide();
            else nav.Show();
        }

        [ConsoleCommand("debug")]
        public static void ToggleDebugInfoGUI () => DebugInfoGUI.Toggle();

        [ConsoleCommand("var")]
        public static void ToggleCustomVariableGUI () => CustomVariableGUI.Toggle();

        [ConsoleCommand]
        public static void Play () => Engine.GetService<IScriptPlayer>()?.MainTrack.Resume();

        [ConsoleCommand]
        public static void PlayScript (string name) => Engine.GetService<IScriptPlayer>()?.MainTrack.LoadAndPlay(name);

        [ConsoleCommand]
        public static void Stop () => Engine.GetService<IScriptPlayer>()?.MainTrack.Stop();

        [ConsoleCommand]
        public static void Reload () => Engine.GetServiceOrErr<IScriptPlayer>().Reload().Forget();

        [ConsoleCommand]
        public static async void Rewind (int line)
        {
            line = Mathf.Clamp(line, 1, int.MaxValue);
            var player = Engine.GetServiceOrErr<IScriptPlayer>();
            var ok = await player.MainTrack.Rewind(line - 1);
            if (!ok) Engine.Warn($"Failed to rewind to line #{line} of script '{player.MainTrack.PlayedScript.Path}'. Make sure the line exists in the script and it's playable (either a command or a generic text line). When rewinding forward, '@stop' commands can prevent reaching the target line. When rewinding backward the target line should've been previously played and be kept in the rollback stack (capacity controlled by '{nameof(StateConfiguration.StateRollbackSteps)}' property in state configuration).");
        }

        private static string ProcessCommandInput (string input)
        {
            if (input is null || !input.StartsWithOrdinal(Compiler.Symbols.CommandLine)) return input;
            var body = input.GetAfterFirst(Compiler.Symbols.CommandLine);
            Engine.GetServiceOrErr<IScriptPlayer>().MainTrack.ExecuteTransientCommand(body, "Console").Forget();
            return null;
        }
    }
}
