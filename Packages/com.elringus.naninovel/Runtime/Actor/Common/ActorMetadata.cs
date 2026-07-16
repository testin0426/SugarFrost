using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents serializable data required to construct and initialize a <see cref="IActor"/>.
    /// </summary>
    [System.Serializable]
    public abstract class ActorMetadata
    {
        [System.Serializable]
        public struct Anchor
        {
            [Tooltip("Identifier of the anchor.")]
            public string Id;
            [Tooltip("Local anchor position inside the actor's game object.")]
            public Vector3 Position;
        }

        /// <summary>
        /// Globally-unique identifier of the medata instance.
        /// </summary>
        public string Guid => guid;

        [Tooltip("Assembly-qualified type name of the actor implementation.")]
        public string Implementation;
        [Tooltip("Data describing how to load actor's resources.")]
        public ResourceLoaderConfiguration Loader;

        [HideInInspector]
        [SerializeField] private string guid = System.Guid.NewGuid().ToString();
        [SerializeReference] private CustomMetadata customData;

        /// <summary>
        /// Attempts to retrieve an actor pose associated with the specified name;
        /// returns null when not found.
        /// </summary>
        public abstract ActorPose<TState> GetPose<TState> (string poseName) where TState : ActorState;

        /// <summary>
        /// Attempts to retrieve a custom data of type <typeparamref name="TData"/>.
        /// </summary>
        /// <typeparam name="TData">Type of the custom data to retrieve.</typeparam>
        public virtual TData GetCustomData<TData> () where TData : CustomMetadata
        {
            return customData as TData;
        }

        /// <summary>
        /// Assigns a custom data of type <typeparamref name="TData"/>.
        /// </summary>
        public void SetCustomMetadata<TData> (TData data) where TData : CustomMetadata
        {
            customData = data;
        }

        /// <summary>
        /// Returns ID of the resource group associated with the metadata.
        /// </summary>
        public string GetResourceGroup () => $"{Loader.PathPrefix}/{Guid}";

        #if UNITY_EDITOR
        public void RegenerateGuid () => guid = System.Guid.NewGuid().ToString();
        #endif
    }
}
