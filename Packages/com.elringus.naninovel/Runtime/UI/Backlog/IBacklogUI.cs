using System.Collections.Generic;

namespace Naninovel.UI
{
    /// <summary>
    /// Represents a set of UI elements used for managing backlog messages.
    /// </summary>
    public interface IBacklogUI : IManagedUI
    {
        /// <summary>
        /// Adds specified message to the log.
        /// </summary>
        void AddMessage (BacklogMessage message);
        /// <summary>
        /// Appends text to the last message of the log (if exists).
        /// </summary>
        /// <param name="text">Text to append to the last message.</param>
        /// <param name="voicePath">Associated voice local resource path or null.</param>
        void AppendMessage (LocalizableText text, string voicePath = null);
        /// <summary>
        /// Adds choice options to the log.
        /// </summary>
        /// <param name="choices">Options to add, in order.</param>
        void AddChoice (IReadOnlyList<BacklogChoice> choices);
        /// <summary>
        /// Removes all messages from the backlog.
        /// </summary>
        void Clear ();
    }
}
