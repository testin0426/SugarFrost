namespace Naninovel
{
    /// <summary>
    /// Type of a save game operation context.
    /// </summary>
    public enum SaveType
    {
        /// <summary>
        /// Default save usually performed manually to a specific save slot.
        /// </summary>
        Normal,
        /// <summary>
        /// Quick save usually performed manually to the first quick-save slot.
        /// </summary>
        Quick,
        /// <summary>
        /// Auto save usually performed automatically to the first auto-save slot.
        /// </summary>
        Auto
    }
}
