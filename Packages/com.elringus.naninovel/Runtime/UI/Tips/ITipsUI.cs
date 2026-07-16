namespace Naninovel.UI
{
    /// <summary>
    /// Represents a set of UI elements used for browsing unlockable tips.
    /// </summary>
    public interface ITipsUI : IManagedUI
    {
        /// <summary>
        /// Selects tip record with the specified ID.
        /// </summary>
        /// <param name="tipId">ID of the tip to select.</param>
        void SelectTipRecord (string tipId);
    }
}
