using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Manages player input processing.
    /// </summary>
    public interface IInputManager : IEngineService<InputConfiguration>
    {
        /// <summary>
        /// Occurs when the <see cref="InputMode"/> changes.
        /// </summary>
        event Action<InputMode> OnInputModeChanged;
        /// <summary>
        /// Occurs when the <see cref="Muted"/> changes.
        /// </summary>
        event Action<bool> OnMutedChanged;

        /// <summary>
        /// Whether the Naninovel input handling is currently enabled.
        /// The enabled state is transient and not serialized with the game state.
        /// </summary>
        bool Enabled { get; set; }
        /// <summary>
        /// Whether all input activation events are currently muted.
        /// Individual inputs can be muted with <see cref="IInputHandle.Muted"/>.
        /// The mute state is persistent and serialized with the game state.
        /// </summary>
        /// <remarks>
        /// The mute switch is independent of reference-based muting provided by
        /// <see cref="AddMuter"/>: even when this global (or per-input) switch is disabled,
        /// events from some inputs may still be muted due to an added muter.
        /// </remarks>
        bool Muted { get; set; }
        /// <summary>
        /// Current input mode detected from the last active input device type.
        /// </summary>
        InputMode InputMode { get; set; }

        /// <summary>
        /// Returns an input handle with the specified identifier, or null if unavailable.
        /// Available predefined input identifiers can be found in <see cref="Inputs"/>.
        /// </summary>
        [CanBeNull] IInputHandle GetInput (string id);
        /// <summary>
        /// Returns the current pointer (mouse, pen, or touch) position in screen space,
        /// or null if the pointer is not available.
        /// </summary>
        Vector2? GetPointerPosition ();
        /// <summary>
        /// Mutes all input events except the specified allowed ones until removed with
        /// <see cref="RemoveMuter"/>, regardless of the <see cref="Muted"/> state.
        /// The effect is transient and not serialized with the game state.
        /// </summary>
        void AddMuter (object muter, [CanBeNull] IReadOnlyCollection<string> allowedIds = null);
        /// <summary>
        /// Removes a muter previously added with <see cref="AddMuter"/>.
        /// </summary>
        void RemoveMuter (object muter);
        /// <summary>
        /// Whether an input with the specified identifier is currently muted,
        /// either via the mute switch or by a muter added with <see cref="AddMuter"/>.
        /// </summary>
        bool IsMuted (string id);
    }
}
