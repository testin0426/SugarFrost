using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage <see cref="IChoiceHandlerActor"/> actors.
    /// </summary>
    public interface IChoiceHandlerManager : IActorManager<IChoiceHandlerActor, ChoiceHandlerState, ChoiceHandlerMetadata, ChoiceHandlersConfiguration>
    {
        /// <summary>
        /// Used by the service to load custom choice button prefabs.
        /// </summary>
        IResourceLoader<GameObject> ChoiceButtonLoader { get; }
        /// <summary>
        /// ID of the choice handler actor to use by default when a specific one is not specified.
        /// </summary>
        string DefaultHandlerId { get; set; }

        /// <summary>
        /// Remembers the spot which was (going to be) played when choice at the specified spot was selected.
        /// In case the spot was already played (player is stopped), next played spot should be remembered instead.
        /// </summary>
        /// <param name="hostedAt">Location of the '@choice' command which hosts the selected choice.</param>
        /// <param name="continueAt">Location to continue the playback after the choice is selected.</param>
        void PushSelectedChoice (PlaybackSpot hostedAt, PlaybackSpot continueAt);
        /// <summary>
        /// Retrieves previously remembered selected spot of the choice with specified spot.
        /// Will as well 'forget' the choice, so it can only be accessed once. Throws when not found.
        /// </summary>
        /// <param name="hostedAt">Location of the '@choice' command which hosts the selected choice.</param>
        /// <returns>Location where the choice was selected.</returns>
        PlaybackSpot PopSelectedChoice (PlaybackSpot hostedAt);
    }
}
