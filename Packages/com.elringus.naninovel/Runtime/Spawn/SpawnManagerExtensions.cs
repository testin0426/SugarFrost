using System;
using System.Collections.Generic;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="ISpawnManager"/>.
    /// </summary>
    public static class SpawnManagerExtensions
    {
        /// <summary>
        /// Returns a spawned object with the specified path; throws when not spawned.
        /// </summary>
        public static SpawnedObject GetSpawnedOrErr (this ISpawnManager manager, string path)
        {
            return manager.GetSpawned(path) ??
                   throw new Error($"Failed to get '{path}' spawned object: not spawned.");
        }

        /// <summary>
        /// Rents a pooled list and collects all the spawned objects.
        /// </summary>
        public static IDisposable RentSpawned (this ISpawnManager manager, out List<SpawnedObject> spawned)
        {
            var rent = ListPool<SpawnedObject>.Rent(out spawned);
            manager.CollectSpawned(spawned);
            return rent;
        }
    }
}
