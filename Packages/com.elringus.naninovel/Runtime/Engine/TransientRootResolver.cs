using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    public static class TransientRootResolver
    {
        [CanBeNull] public static Func<string> Resolver { get; set; }

        /// <summary>
        /// Resolves path (relative to the Unity project root) to the transient data directory; throws in build.
        /// </summary>
        public static string Resolve ()
        {
            if (!Application.isEditor) throw new Error("Failed to resolve transient root: only available in editor.");
            return Resolver?.Invoke() ?? throw new Error("Failed to resolve transient root: resolver is not assigned.");
        }
    }
}
