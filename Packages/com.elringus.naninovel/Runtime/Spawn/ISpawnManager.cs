using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage objects spawned with <see cref="Commands.Spawn"/> commands.
    /// </summary>
    public interface ISpawnManager : IEngineService<SpawnConfiguration>
    {
        /// <summary>
        /// Spawns an object with the specified path.
        /// </summary>
        Awaitable<SpawnedObject> Spawn (string path, InstantiateOptions options = default, AsyncToken token = default);
        /// <summary>
        /// Checks whether an object with the specified path is currently spawned.
        /// </summary>
        bool IsSpawned (string path);
        /// <summary>
        /// Returns a spawned object with the specified path or null when not spawned.
        /// </summary>
        [CanBeNull] SpawnedObject GetSpawned (string path);
        /// <summary>
        /// Collects currently spawned objects into the specified collection.
        /// </summary>
        void CollectSpawned (ICollection<SpawnedObject> spawned);
        /// <summary>
        /// Destroys a spawned object with the specified path.
        /// </summary>
        /// <param name="dispose">Whether to also dispose (destroy) the spawned game object.</param>
        void DestroySpawned (string path, bool dispose = true);

        /// <summary>
        /// Preloads and holds resources required to spawn an object with the specified path.
        /// </summary>
        Awaitable HoldResources (string path, object holder);
        /// <summary>
        /// Releases resources required to spawn an object with the specified path.
        /// </summary>
        void ReleaseResources (string path, object holder);
    }
}
