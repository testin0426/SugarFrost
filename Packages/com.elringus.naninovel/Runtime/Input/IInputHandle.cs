using System;
using System.Threading;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A player input handle that allows sampling input status and listening for activation events.
    /// </summary>
    public interface IInputHandle
    {
        /// <summary>
        /// Occurs when input activation starts.
        /// </summary>
        event Action OnStart;
        /// <summary>
        /// Occurs when input activation ends.
        /// </summary>
        event Action OnEnd;
        /// <summary>
        /// Occurs when the <see cref="Force"/> changes.
        /// </summary>
        event Action<Vector2> OnChange;

        /// <summary>
        /// Identifier of the input handle.
        /// </summary>
        string Id { get; }
        /// <summary>
        /// Whether the input is currently active; i.e., <see cref="Force"/> is not zero.
        /// </summary>
        bool Active { get; }
        /// <summary>
        /// The current activation force of the input, in the -1.0 to 1.0 range per axis.
        /// </summary>
        Vector2 Force { get; }
        /// <summary>
        /// Whether the input's activation events are currently muted.
        /// The mute switch is persistent and serialized with the game state.
        /// </summary>
        bool Muted { get; set; }

        /// <summary>
        /// Simulates input activation with the specified force and triggers associated callbacks.
        /// </summary>
        void Activate (Vector2 force);
        /// <summary>
        /// Returns a token that will be cancelled once the input is activated.
        /// </summary>
        CancellationToken GetNext ();
        /// <summary>
        /// Returns a token that will be cancelled once the input is activated, and no other
        /// handlers are notified about the event occurrence, unless the specified token is cancelled.
        /// </summary>
        /// <remarks>
        /// Intercepting requests are stack-based: when the event occurs, only the last callee is notified.
        /// If the specified token is cancelled while processing the event, the returned token won't be cancelled,
        /// and the next intercept request in the stack is processed instead, if any; otherwise, the event is handled normally.
        /// </remarks>
        /// <param name="token">The interception is ignored if this token is cancelled when the event occurs.</param>
        CancellationToken InterceptNext (CancellationToken token = default);
    }
}
