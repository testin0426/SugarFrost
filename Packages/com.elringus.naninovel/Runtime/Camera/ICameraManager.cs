using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage cameras and other systems required for scene rendering.
    /// </summary>
    public interface ICameraManager : IEngineService<CameraConfiguration>
    {
        /// <summary>
        /// Occurs when the <see cref="RenderUI"/> changes.
        /// </summary>
        event Action<bool> OnRenderUIChanged;
        /// <summary>
        /// Occurs when the <see cref="Offset"/> changes.
        /// </summary>
        event Action<Vector3> OnOffsetChanged;
        /// <summary>
        /// Occurs when the <see cref="Rotation"/> changes.
        /// </summary>
        event Action<Quaternion> OnRotationChanged;
        /// <summary>
        /// Occurs when the <see cref="Zoom"/> changes.
        /// </summary>
        event Action<float> OnZoomChanged;

        /// <summary>
        /// Whether the Naninovel camera/rendering is currently enabled.
        /// The enabled state is transient and not serialized with the game state.
        /// </summary>
        bool Enabled { get; set; }
        /// <summary>
        /// Main render camera used by the engine.
        /// </summary>
        Camera Camera { get; }
        /// <summary>
        /// Optional camera used for UI rendering.
        /// </summary>
        [CanBeNull] Camera UICamera { get; }
        /// <summary>
        /// Whether to render the UI objects.
        /// </summary>
        bool RenderUI { get; set; }
        /// <summary>
        /// Local camera position offset in units by X and Y axis relative to the initial position set in the configuration.
        /// </summary>
        Vector3 Offset { get; set; }
        /// <summary>
        /// Local camera rotation.
        /// </summary>
        Quaternion Rotation { get; set; }
        /// <summary>
        /// The main camera zoom level in 0.0 to 1.0 range.
        /// </summary>
        /// <remarks>
        /// Scales main camera's orthographic size or FOV depending on <see cref="Orthographic"/>.
        /// </remarks>
        float Zoom { get; set; }
        /// <summary>
        /// The initial (reference) camera orthographic size, unaffected by the <see cref="Zoom"/> level.
        /// </summary>
        float OrthographicSize { get; }
        /// <summary>
        /// The initial (reference) camera FOV size, unaffected by the <see cref="Zoom"/> level.
        /// </summary>
        float FOV { get; }
        /// <summary>
        /// Whether the camera should render in orthographic (true) or perspective (false) mode.
        /// </summary>
        bool Orthographic { get; set; }
        /// <summary>
        /// Current rendering quality level (<see cref="QualitySettings"/>) index.
        /// </summary>
        int QualityLevel { get; set; }

        /// <summary>
        /// Activates/disables camera look mode, when player can offset the main camera with input devices 
        /// (eg, by moving a mouse or using gamepad analog stick).
        /// </summary>
        void SetLookMode (bool enabled, Vector2 lookZone, Vector2 lookSpeed, bool gravity);
        /// <summary>
        /// Save current content of the screen to be used as a thumbnail (eg, for save slots).
        /// Returns null when <see cref="CameraConfiguration.CaptureThumbnails"/> is disabled.
        /// </summary>
        Awaitable<Texture2D> CaptureThumbnail ();
        /// <summary>
        /// Modifies <see cref="Offset"/> over time.
        /// </summary>
        Awaitable ChangeOffset (Vector3 offset, Tween tween, AsyncToken token = default);
        /// <summary>
        /// Modifies <see cref="Rotation"/> over time.
        /// </summary>
        Awaitable ChangeRotation (Quaternion rotation, Tween tween, AsyncToken token = default);
        /// <summary>
        /// Modifies <see cref="Zoom"/> over time.
        /// </summary>
        Awaitable ChangeZoom (float zoom, Tween tween, AsyncToken token = default);
    }
}
