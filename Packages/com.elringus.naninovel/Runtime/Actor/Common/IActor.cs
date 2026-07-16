using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to represent an actor on scene.
    /// </summary>
    public interface IActor
    {
        /// <summary>
        /// Occurs when the <see cref="Appearance"/> is changed.
        /// </summary>
        event Action<string> OnAppearanceChanged;
        /// <summary>
        /// Occurs when the <see cref="Visible"/> is changed.
        /// </summary>
        event Action<bool> OnVisibilityChanged;
        /// <summary>
        /// Occurs when the <see cref="Position"/> is changed.
        /// </summary>
        event Action<Vector3> OnPositionChanged;
        /// <summary>
        /// Occurs when the <see cref="Rotation"/> is changed.
        /// </summary>
        event Action<Quaternion> OnRotationChanged;
        /// <summary>
        /// Occurs when the <see cref="Scale"/> is changed.
        /// </summary>
        event Action<Vector3> OnScaleChanged;
        /// <summary>
        /// Occurs when the <see cref="TintColor"/> is changed.
        /// </summary>
        event Action<Color> OnTintColorChanged;

        /// <summary>
        /// Unique identifier of the actor. 
        /// </summary>
        string Id { get; }
        /// <summary>
        /// Appearance of the actor. 
        /// </summary>
        string Appearance { get; set; }
        /// <summary>
        /// Whether the actor is currently visible on scene.
        /// </summary>
        bool Visible { get; set; }
        /// <summary>
        /// Position of the actor.
        /// </summary>
        Vector3 Position { get; set; }
        /// <summary>
        /// Rotation of the actor.
        /// </summary>
        Quaternion Rotation { get; set; }
        /// <summary>
        /// Scale of the actor.
        /// </summary>
        Vector3 Scale { get; set; }
        /// <summary>
        /// Tint color of the actor.
        /// </summary>
        Color TintColor { get; set; }

        /// <summary>
        /// Allows to perform an async initialization routine.
        /// Invoked once by <see cref="IActorManager"/> after actor is constructed.
        /// </summary>
        Awaitable Initialize ();

        /// <summary>
        /// Changes <see cref="Appearance"/> over specified time using specified animation tween and transition effect.
        /// </summary>
        Awaitable ChangeAppearance (string appearance, Tween tween, Transition? transition = default, AsyncToken token = default);
        /// <summary>
        /// Changes <see cref="Visible"/> over specified time using specified animation tween.
        /// </summary>
        Awaitable ChangeVisibility (bool visible, Tween tween, AsyncToken token = default);
        /// <summary>
        /// Changes <see cref="Position"/> over specified time using specified animation tween.
        /// </summary>
        Awaitable ChangePosition (Vector3 position, Tween tween, AsyncToken token = default);
        /// <summary>
        /// Changes <see cref="Rotation"/> over specified time using specified animation tween.
        /// </summary>
        Awaitable ChangeRotation (Quaternion rotation, Tween tween, AsyncToken token = default);
        /// <summary>
        /// Changes <see cref="Scale"/> factor over specified time using specified animation tween.
        /// </summary>
        Awaitable ChangeScale (Vector3 scale, Tween tween, AsyncToken token = default);
        /// <summary>
        /// Changes <see cref="TintColor"/> over specified time using specified animation tween.
        /// </summary>
        Awaitable ChangeTintColor (Color tintColor, Tween tween, AsyncToken token = default);
    }
}
