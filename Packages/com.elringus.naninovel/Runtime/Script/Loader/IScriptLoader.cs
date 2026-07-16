using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Handles pre-/loading and unloading resources associated with scenario scripts.
    /// </summary>
    public interface IScriptLoader : IEngineService
    {
        /// <summary>
        /// Occurs when script load progress is changed, in the 0.0 to 1.0 range.
        /// </summary>
        event Action<float> OnLoadProgress;

        /// <summary>
        /// Loads resources associated with the script that has the specified local resource path
        /// and unloads resources associated with previously loaded scripts in accordance with
        /// <see cref="ResourcePolicy"/>, all scoped under a <see cref="IScriptTrack"/> with
        /// the specified identifier.
        /// </summary>
        /// <param name="trackId">Identifier of a script track playing the loaded script.</param>
        /// <param name="scriptPath">Local resource path of the script to load.</param>
        /// <param name="startIndex">Index in the playlist from which to start loading the script.</param>
        Awaitable Load (string trackId, string scriptPath, int startIndex = 0);
        /// <summary>
        /// Releases script resources loaded under the scope of a script track with the specified identifier. 
        /// </summary>
        void Release (string trackId);
    }
}
