using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Controls the dialogue mode when Naninovel is used as a drop-in dialogue/cutscene system.
    /// </summary>
    public static class Dialogue
    {
        /// <summary>
        /// Occurs when the dialogue mode is activated.
        /// </summary>
        public static event Action OnEntered;
        /// <summary>
        /// Occurs when the dialogue mode is deactivated.
        /// </summary>
        public static event Action OnExited;

        /// <summary>
        /// Whether the dialogue mode is currently active.
        /// </summary>
        public static bool Active { get; private set; }

        /// <summary>
        /// Activates the dialogue mode by initializing the engine (in case it's not initialized)
        /// and enabling essential activities, such as rendering and input processing.
        /// Has no effect when the dialogue mode is already active.
        /// </summary>
        public static async Awaitable Enter ()
        {
            if (Active) return;
            Active = true;
            if (!Engine.Initialized) await RuntimeInitializer.Initialize();
            else Engine.GetService<IStateManager>()?.ResetService(); // clear rollback stack, otherwise hot-reload fails when editing first generic line
            if (Engine.TryGetService<IInputManager>(out var input)) input.Enabled = true;
            if (Engine.TryGetService<ICameraManager>(out var camera)) camera.Enabled = true;
            OnEntered?.Invoke();
        }

        /// <summary>
        /// Activates the dialogue mode and plays a scenario script with the specified path.
        /// In case the dialogue mode is already active, will just play the script.
        /// </summary>
        /// <param name="scriptPath">Local resource path of the scenario script to play.</param>
        /// <param name="label">Optional label inside the scenario script to start playing from.</param>
        public static async Awaitable EnterAndPlay (string scriptPath, [CanBeNull] string label = null)
        {
            if (!Active) await Enter();
            var track = Engine.GetServiceOrErr<IScriptPlayer>().MainTrack;
            if (string.IsNullOrWhiteSpace(label)) await track.LoadAndPlay(scriptPath);
            else await track.LoadAndPlayAtLabel(scriptPath, label);
        }

        /// <summary>
        /// Activates the dialogue mode and plays a scenario script with the specified asset GUID
        /// usually retrieved from a serialized field with <see cref="ScriptAssetRefAttribute"/>.
        /// In case the dialogue mode is already active, will just play the script.
        /// </summary>
        /// <param name="scriptGuid">GUID of the script asset to play.</param>
        /// <param name="label">Optional label inside the scenario script to start playing from.</param>
        public static Awaitable EnterAndPlayAsset (string scriptGuid, [CanBeNull] string label = null)
        {
            if (ScriptAssets.GetPath(scriptGuid) is not { } scriptPath)
                throw new Error($"Failed to enter dialogue mode and play scenario script with '{scriptGuid}' asset GUID: asset not found.");
            return EnterAndPlay(scriptPath, label);
        }

        /// <summary>
        /// Deactivates the dialogue mode by resetting the engine state and disabling essential activities,
        /// such as rendering and input processing. Has not effect when the dialogue mode is not active.
        /// </summary>
        /// <param name="destroy">Whether to also destroy (deinitialize) the engine.</param>
        public static async Awaitable Exit (bool destroy = false)
        {
            if (!Active) return;
            Active = false;
            if (!Engine.Initialized) return;
            using (new InteractionBlocker())
                await Engine.GetServiceOrErr<IStateManager>().ResetState();
            if (Engine.TryGetService<IInputManager>(out var input)) input.Enabled = false;
            if (Engine.TryGetService<ICameraManager>(out var camera)) camera.Enabled = false;
            if (destroy) Engine.Destroy();
            OnExited?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset ()
        {
            OnEntered = null;
            OnExited = null;
            Active = false;
        }
    }
}
