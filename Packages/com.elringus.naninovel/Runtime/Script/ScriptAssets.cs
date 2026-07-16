using System.Collections.Generic;
using JetBrains.Annotations;

namespace Naninovel
{
    /// <summary>
    /// An <see cref="Assets"/> registry view over the <see cref="Script"/> assets with efficient lookups.
    /// </summary>
    public static class ScriptAssets
    {
        private static readonly Dictionary<string, string> pathByGuid = new();
        private static readonly Dictionary<string, string> guidByPath = new();

        /// <summary>
        /// Resolves local script path from the specified unique asset GUID;
        /// returns null in case script asset with specified GUID is not registered.
        /// </summary>
        [CanBeNull]
        public static string GetPath (string guid)
        {
            EnsureCached();
            return pathByGuid.GetValueOrDefault(guid);
        }

        /// <summary>
        /// Resolves local script path from the specified unique asset GUID;
        /// throws in case script asset with the specified GUID is not registered.
        /// </summary>
        public static string GetPathOrErr (string guid)
        {
            return GetPath(guid) ?? throw new Error(
                $"Failed to resolve script resource path for an asset with '{guid}' GUID." +
                " Make sure script asset reference is valid.");
        }

        /// <summary>
        /// Resolves GUID of a script asset associated with the specified local script path;
        /// returns null in case script asset with specified path is not registered.
        /// </summary>
        [CanBeNull]
        public static string GetGuid (string path)
        {
            EnsureCached();
            return guidByPath.GetValueOrDefault(path);
        }

        /// <summary>
        /// Resolves GUID of a script asset associated with the specified local script path;
        /// throws in case script asset with the specified path is not registered.
        /// </summary>
        public static string GetGuidOrErr (string path)
        {
            return GetGuid(path) ?? throw new Error(
                $"Failed to resolve script asset GUID for a resource with '{path}' path." +
                " Make sure script asset reference is valid.");
        }

        /// <summary>
        /// Returns all the registered script asset GUIDs.
        /// </summary>
        public static IReadOnlyCollection<string> GetAllGuids ()
        {
            EnsureCached();
            return pathByGuid.Keys;
        }

        /// <summary>
        /// Returns all the registered local script paths.
        /// </summary>
        public static IReadOnlyCollection<string> GetAllPaths ()
        {
            EnsureCached();
            return pathByGuid.Values;
        }

        private static void EnsureCached ()
        {
            if (pathByGuid.Count > 0) return;

            Assets.OnModified -= ClearCache;
            Assets.OnModified += ClearCache;

            using var _ = Assets.RentWithPrefix(ScriptsConfiguration.DefaultPathPrefix, out var scripts);
            foreach (var script in scripts)
            {
                pathByGuid[script.Guid] = script.Path;
                guidByPath[script.Path] = script.Guid;
            }
        }

        private static void ClearCache ()
        {
            pathByGuid.Clear();
            guidByPath.Clear();
        }
    }
}
