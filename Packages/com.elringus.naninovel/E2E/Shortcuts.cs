using System;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel.E2E
{
    /// <summary>
    /// Static methods to help authoring concise test suits.
    /// </summary>
    public static class Shortcuts
    {
        /// <summary>
        /// Whether <see cref="ITitleUI"/> is visible.
        /// </summary>
        public static Func<bool> InTitle => () => UI<ITitleUI>().Visible;
        /// <summary>
        /// Whether script player is playing.
        /// </summary>
        public static Func<bool> Playing => () => MainTrack.Playing;
        /// <summary>
        /// Whether at least one choice is available and script player is stopped (player is expected to choose to continue).
        /// </summary>
        public static Func<bool> Choosing => () => !Playing() && Choices.AnyActor(a => a.Visible);

        /// <summary>
        /// Main script track.
        /// </summary>
        public static IScriptTrack MainTrack => Service<IScriptPlayer>().MainTrack;
        /// <summary>
        /// The character manager service.
        /// </summary>
        public static ICharacterManager Chars => Service<ICharacterManager>();
        /// <summary>
        /// The background manager service.
        /// </summary>
        public static IBackgroundManager Backs => Service<IBackgroundManager>();
        /// <summary>
        /// Main background actor.
        /// </summary>
        public static IBackgroundActor MainBack => Service<IBackgroundManager>().GetActor(BackgroundsConfiguration.MainActorId);
        /// <summary>
        /// The choice handler manager service.
        /// </summary>
        public static IChoiceHandlerManager Choices => Service<IChoiceHandlerManager>();
        /// <summary>
        /// The text printer manager service.
        /// </summary>
        public static ITextPrinterManager Printers => Service<ITextPrinterManager>();

        /// <summary>
        /// Attempts to get engine service with the specified type; throws when not found.
        /// </summary>
        public static TService Service<TService> () where TService : class, IEngineService => Engine.GetServiceOrErr<TService>();
        /// <summary>
        /// Attempts to get managed UI with the specified type; throws when not found.
        /// </summary>
        public static TUI UI<TUI> () where TUI : class, IManagedUI => Service<IUIManager>().GetUIOrErr<TUI>();
        /// <summary>
        /// Attempts to get managed UI with the specified name; throws when not found.
        /// </summary>
        public static IManagedUI UI (string name) => Service<IUIManager>().GetUIOrErr(name);
        /// <summary>
        /// Attempts to get input sampler with the specified name; throws when not found.
        /// </summary>
        public static IInputHandle Input (string name) => Service<IInputManager>().GetInputOrErr(name);
        /// <summary>
        /// Whether choice with the specified summary text ID (or any if not specified) is available.
        /// </summary>
        public static Condition Choice (string id = null) => new(() =>
            Choices.AnyActor(a => a.Visible && a.AnyChoice(c => c.Summary.Parts.Any(p => id is null || p.Id == id)))
                ? (true, "") : (false, $"Expected choice '{id}', but it was not found."));
        /// <summary>
        /// Selects choice with the specified summary text ID or first available when not specified.
        /// Will fail in case handler with the ID is not found or not visible.
        /// </summary>
        /// <remarks>
        /// Example of assigning custom text ID to choice summary:
        /// <code>@choice "Choice summary|#choice-id|"</code>
        /// </remarks>
        public static ISequence Choose (string id = null,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) => new Sequence().Choose(id, file, line);
        /// <summary>
        /// Whether custom variable with the specified name and type is equal to the specified value.
        /// </summary>
        public static Condition Var<TValue> (string name, TValue value) => new(() =>
            Service<ICustomVariableManager>().TryGetVariableValue<TValue>(name, out var val) && val.Equals(value)
                ? (true, "") : (false, $"Expected variable '{name}' to equal '{value}', but it was '{val}'."));
        /// <summary>
        /// Whether specified path equals currently played script path.
        /// </summary>
        public static Condition Script (string path) => new(() => MainTrack.PlayedScript?.Path == path
            ? (true, "") : (false, $"Expected played script '{path}', but it was '{MainTrack.PlayedScript?.Path}'."));
        /// <inheritdoc cref="Extensions.Play(Naninovel.E2E.ISequence,Naninovel.E2E.ISequence)"/>
        public static ISequence Play (ISequence sequence) => new Sequence().Play(sequence);
        /// <inheritdoc cref="Extensions.Play(Naninovel.E2E.ISequence,Naninovel.E2E.ISequence[])"/>
        public static ISequence Play (params ISequence[] sequences) => new Sequence().Play(sequences);
        /// <inheritdoc cref="Extensions.Once"/>
        public static ISequence Once (Func<bool> condition, float timeout = 10, [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) => new Sequence().Once(condition, timeout, file, line);
        /// <inheritdoc cref="Extensions.On"/>
        public static ISequence On (Func<bool> condition, ISequence @do, Func<bool> @continue = null, float timeout = 10,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) => new Sequence().On(condition, @do, @continue, timeout, file, line);

        /// <summary>
        /// Fails currently running test suite.
        /// </summary>
        [ContractAnnotation("=> halt")]
        public static void Fail (string message, string file = null, int line = 0)
        {
            if (!string.IsNullOrEmpty(file))
                message += $" At: {StringUtils.BuildAssetLink(PathUtils.AbsoluteToAssetPath(file), line)}.";
            if (Service<IScriptPlayer>()?.MainTrack.PlayedScript is { } script && script)
                message += $" Played: {ObjectUtils.BuildAssetLink(script, MainTrack.PlaybackSpot.LineIndex)}.";
            Debug.LogAssertion(message, Service<IScriptPlayer>()?.MainTrack.PlayedScript);
            Application.Quit();
        }
    }
}
