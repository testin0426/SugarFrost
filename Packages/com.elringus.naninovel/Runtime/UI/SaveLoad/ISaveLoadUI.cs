namespace Naninovel.UI
{
    /// <summary>
    /// Represents a set of UI elements for managing <see cref="GameStateMap"/> slots.
    /// </summary>
    public interface ISaveLoadUI : IManagedUI
    {
        /// <summary>
        /// Shows the UI in the load presentation mode, where user can load by clicking
        /// the slots and all the tabs are available.
        /// </summary>
        void ShowLoad ();
        /// <summary>
        /// Shows the UI in save presentation mode, where user can save by clicking
        /// the slots and only the tab for manual save slots is available.
        /// </summary>
        void ShowSave ();
    }
}
