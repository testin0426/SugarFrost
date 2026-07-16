using JetBrains.Annotations;
using static Naninovel.Inputs;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="IInputManager"/>.
    /// </summary>
    public static class InputManagerExtensions
    {
        /// <summary>
        /// Attempts to get an input handle with the specified identifier; returns null when not available.
        /// Find available pre-defined input identifiers in <see cref="Inputs"/>.
        /// </summary>
        public static bool TryGetInput (this IInputManager m, string id, out IInputHandle input) => (input = m?.GetInput(id)) != null;
        /// <summary>
        /// Attempts to get an input handle with the specified identifier; throws when not available.
        /// Find available pre-defined input identifiers in <see cref="Inputs"/>.
        /// </summary>
        public static IInputHandle GetInputOrErr (this IInputManager m, string id) => m.GetInput(id) ?? throw new Error($"Input with '{id}' ID is not available.");

        /// <summary>
        /// Returns the <see cref="Inputs.Submit"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetSubmit (this IInputManager m) => m.GetInput(Submit);
        /// <summary>
        /// Returns the <see cref="Inputs.Submit"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetSubmitOrErr (this IInputManager m) => m.GetInputOrErr(Submit);
        /// <summary>
        /// Returns the <see cref="Inputs.Cancel"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetCancel (this IInputManager m) => m.GetInput(Cancel);
        /// <summary>
        /// Returns the <see cref="Inputs.Cancel"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetCancelOrErr (this IInputManager m) => m.GetInputOrErr(Cancel);
        /// <summary>
        /// Returns the <see cref="Inputs.Delete"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetDelete (this IInputManager m) => m.GetInput(Delete);
        /// <summary>
        /// Returns the <see cref="Inputs.Delete"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetDeleteOrErr (this IInputManager m) => m.GetInputOrErr(Delete);
        /// <summary>
        /// Returns the <see cref="Inputs.Navigate"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetNavigate (this IInputManager m) => m.GetInput(Navigate);
        /// <summary>
        /// Returns the <see cref="Inputs.Navigate"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetNavigateOrErr (this IInputManager m) => m.GetInputOrErr(Navigate);
        /// <summary>
        /// Returns the <see cref="Inputs.Scroll"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetScroll (this IInputManager m) => m.GetInput(Scroll);
        /// <summary>
        /// Returns the <see cref="Inputs.Scroll"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetScrollOrErr (this IInputManager m) => m.GetInputOrErr(Scroll);
        /// <summary>
        /// Returns the <see cref="Inputs.Page"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetPage (this IInputManager m) => m.GetInput(Page);
        /// <summary>
        /// Returns the <see cref="Inputs.Page"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetPageOrErr (this IInputManager m) => m.GetInputOrErr(Page);
        /// <summary>
        /// Returns the <see cref="Inputs.Tab"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetTab (this IInputManager m) => m.GetInput(Tab);
        /// <summary>
        /// Returns the <see cref="Inputs.Tab"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetTabOrErr (this IInputManager m) => m.GetInputOrErr(Tab);
        /// <summary>
        /// Returns the <see cref="Inputs.Continue"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetContinue (this IInputManager m) => m.GetInput(Continue);
        /// <summary>
        /// Returns the <see cref="Inputs.Continue"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetContinueOrErr (this IInputManager m) => m.GetInputOrErr(Continue);
        /// <summary>
        /// Returns the <see cref="Inputs.Skip"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetSkip (this IInputManager m) => m.GetInput(Skip);
        /// <summary>
        /// Returns the <see cref="Inputs.Skip"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetSkipOrErr (this IInputManager m) => m.GetInputOrErr(Skip);
        /// <summary>
        /// Returns the <see cref="Inputs.ToggleSkip"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetToggleSkip (this IInputManager m) => m.GetInput(ToggleSkip);
        /// <summary>
        /// Returns the <see cref="Inputs.ToggleSkip"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetToggleSkipOrErr (this IInputManager m) => m.GetInputOrErr(ToggleSkip);
        /// <summary>
        /// Returns the <see cref="Inputs.SkipMovie"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetSkipMovie (this IInputManager m) => m.GetInput(SkipMovie);
        /// <summary>
        /// Returns the <see cref="Inputs.SkipMovie"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetSkipMovieOrErr (this IInputManager m) => m.GetInputOrErr(SkipMovie);
        /// <summary>
        /// Returns the <see cref="Inputs.AutoPlay"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetAutoPlay (this IInputManager m) => m.GetInput(AutoPlay);
        /// <summary>
        /// Returns the <see cref="Inputs.AutoPlay"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetAutoPlayOrErr (this IInputManager m) => m.GetInputOrErr(AutoPlay);
        /// <summary>
        /// Returns the <see cref="Inputs.ToggleUI"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetToggleUI (this IInputManager m) => m.GetInput(ToggleUI);
        /// <summary>
        /// Returns the <see cref="Inputs.ToggleUI"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetToggleUIOrErr (this IInputManager m) => m.GetInputOrErr(ToggleUI);
        /// <summary>
        /// Returns the <see cref="Inputs.ShowBacklog"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetShowBacklog (this IInputManager m) => m.GetInput(ShowBacklog);
        /// <summary>
        /// Returns the <see cref="Inputs.ShowBacklog"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetShowBacklogOrErr (this IInputManager m) => m.GetInputOrErr(ShowBacklog);
        /// <summary>
        /// Returns the <see cref="Inputs.Rollback"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetRollback (this IInputManager m) => m.GetInput(Rollback);
        /// <summary>
        /// Returns the <see cref="Inputs.Rollback"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetRollbackOrErr (this IInputManager m) => m.GetInputOrErr(Rollback);
        /// <summary>
        /// Returns the <see cref="Inputs.CameraLook"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetCameraLook (this IInputManager m) => m.GetInput(CameraLook);
        /// <summary>
        /// Returns the <see cref="Inputs.CameraLook"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetCameraLookOrErr (this IInputManager m) => m.GetInputOrErr(CameraLook);
        /// <summary>
        /// Returns the <see cref="Inputs.Pause"/> input handle or null when not available.
        /// </summary>
        [CanBeNull] public static IInputHandle GetPause (this IInputManager m) => m.GetInput(Pause);
        /// <summary>
        /// Returns the <see cref="Inputs.Pause"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetPauseOrErr (this IInputManager m) => m.GetInputOrErr(Pause);
        /// <summary>
        /// Returns the <see cref="Inputs.ToggleConsole"/> input handle or null when not available. 
        /// </summary>
        [CanBeNull] public static IInputHandle GetToggleConsole (this IInputManager m) => m.GetInput(ToggleConsole);
        /// <summary>
        /// Returns the <see cref="Inputs.ToggleConsole"/> input handle or throws when not available.
        /// </summary>
        public static IInputHandle GetToggleConsoleOrErr (this IInputManager m) => m.GetInputOrErr(ToggleConsole);
    }
}
