using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Provides access to cached <see cref="EngineTypes"/>.
    /// </summary>
    public static class TypeCache
    {
        /// <summary>
        /// Default resource path of the serialized type cache asset.
        /// </summary>
        public const string ResourcePath = "Naninovel/TypeCache";

        /// <summary>
        /// Editor-only delegate to resolve the types (assigned on domain reload).
        /// </summary>
        [CanBeNull] public static Func<EngineTypes> EditorResolver { get; set; }

        /// <summary>
        /// Loads the cached types from resources; throws when cache asset is missing.
        /// </summary>
        public static EngineTypes Load ()
        {
            if (Application.isEditor) return EditorResolver?.Invoke() ?? throw new Error("Missing engine types editor resolver.");
            var json = Resources.Load<TextAsset>(ResourcePath);
            if (!json) throw new Error("Failed to load type cache: missing cache asset.");
            if (string.IsNullOrWhiteSpace(json.text)) throw new Error("Failed to load type cache: cache asset is empty.");
            return JsonUtility.FromJson<EngineTypes>(json.text) ?? throw new Error("Failed to load type cache: invalid JSON.");
        }
    }
}
