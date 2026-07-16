using JetBrains.Annotations;

namespace Naninovel.UI
{
    /// <summary>
    /// A message recorded by <see cref="IBacklogUI"/>.
    /// </summary>
    public struct BacklogMessage
    {
        /// <summary>
        /// Text of the message.
        /// </summary>
        public LocalizableText Text;
        /// <summary>
        /// Actor ID associated with the message or null.
        /// </summary>
        [CanBeNull] public string AuthorId;
        /// <summary>
        /// Author label associated with the message or null.
        /// </summary>
        public LocalizableText? AuthorLabel;
        /// <summary>
        /// Playback spot associated with the message or null.
        /// </summary>
        public PlaybackSpot? Spot;
        /// <summary>
        /// Voice local resource paths associated with the message or null.
        /// </summary>
        [CanBeNull] public string Voice;
    }
}
