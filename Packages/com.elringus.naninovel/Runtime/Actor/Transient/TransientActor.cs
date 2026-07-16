using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A transient <see cref="IActor"/> implementation with lifecycle managed outside of Naninovel.
    /// </summary>
    public abstract class TransientActor<TManager, TMeta> : MonoBehaviour, IActor
        where TManager : class, IActorManager
        where TMeta : ActorMetadata
    {
        public virtual event Action<string> OnAppearanceChanged;
        public virtual event Action<bool> OnVisibilityChanged;
        public virtual event Action<Vector3> OnPositionChanged;
        public virtual event Action<Quaternion> OnRotationChanged;
        public virtual event Action<Vector3> OnScaleChanged;
        public virtual event Action<Color> OnTintColorChanged;

        [field: SerializeField, Tooltip("The unique identifier of the actor. Will auto-generate a random unique value when not specified.")]
        public virtual string ActorId { get; private set; }
        [field: SerializeField, Tooltip("The actor's metadata.")]
        public virtual TMeta Metadata { get; private set; }
        [field: SerializeField, Tooltip("Whether to automatically initialize the actor when when entering the dialogue mode.")]
        public virtual bool AutoInitialize { get; private set; } = true;

        public virtual string Id => ActorId;
        public virtual string Appearance { get => appearance; set => SetAppearance(value); }
        public virtual bool Visible { get => visible; set => SetVisible(value); }
        public virtual Vector3 Position { get => position; set => SetPosition(value); }
        public virtual Quaternion Rotation { get => rotation; set => SetRotation(value); }
        public virtual Vector3 Scale { get => scale; set => SetScale(value); }
        public virtual Color TintColor { get => tintColor; set => SetTintColor(value); }

        private string appearance;
        private bool visible;
        private Vector3 position;
        private Quaternion rotation;
        private Vector3 scale;
        private Color tintColor;

        /// <summary>
        /// Registers the transient actor with the associated actor manager,
        /// wiring it with the scenario playback and other Naninovel services.
        /// </summary>
        public virtual void InitializeTransientActor ()
        {
            if (Engine.TryGetService<TManager>(out var manager))
                if (!manager.ActorExists(ActorId))
                    manager.ManageActor(this, Metadata);
        }

        /// <summary>
        /// Unregisters the transient actor from the associated actor manager,
        /// unwiring it from the scenario playback and other Naninovel services.
        /// </summary>
        public virtual void DeinitializeTransientActor ()
        {
            if (Engine.TryGetService<TManager>(out var manager))
                if (manager.ActorExists(ActorId))
                    manager.UnmanageActor(ActorId);
        }

        public virtual Awaitable ChangeAppearance (string appearance, Tween tween, Transition? transition = default, AsyncToken token = default)
        {
            SetAppearance(appearance);
            return Async.Completed;
        }

        public virtual Awaitable ChangeVisibility (bool visible, Tween tween, AsyncToken token = default)
        {
            SetVisible(visible);
            return Async.Completed;
        }

        public virtual Awaitable ChangePosition (Vector3 position, Tween tween, AsyncToken token = default)
        {
            SetPosition(position);
            return Async.Completed;
        }

        public virtual Awaitable ChangeRotation (Quaternion rotation, Tween tween, AsyncToken token = default)
        {
            SetRotation(rotation);
            return Async.Completed;
        }

        public virtual Awaitable ChangeScale (Vector3 scale, Tween tween, AsyncToken token = default)
        {
            SetScale(scale);
            return Async.Completed;
        }

        public virtual Awaitable ChangeTintColor (Color tintColor, Tween tween, AsyncToken token = default)
        {
            SetTintColor(tintColor);
            return Async.Completed;
        }

        protected virtual void Awake ()
        {
            if (string.IsNullOrWhiteSpace(ActorId))
                ActorId = GenerateActorId();
        }

        protected virtual void OnEnable ()
        {
            if (AutoInitialize) Dialogue.OnEntered += InitializeTransientActor;
        }

        protected virtual void OnDisable ()
        {
            if (AutoInitialize)
            {
                Dialogue.OnEntered -= InitializeTransientActor;
                DeinitializeTransientActor();
            }
        }

        protected virtual string GenerateActorId ()
        {
            return $"{GetType().Name}-{Guid.NewGuid():N}";
        }

        protected virtual void SetAppearance (string appearance)
        {
            var changed = this.appearance != appearance;
            this.appearance = appearance;
            if (changed) OnAppearanceChanged?.Invoke(appearance);
        }

        protected virtual void SetVisible (bool visible)
        {
            var changed = this.visible != visible;
            this.visible = visible;
            if (changed) OnVisibilityChanged?.Invoke(visible);
        }

        protected virtual void SetPosition (Vector3 position)
        {
            var changed = this.position != position;
            this.position = position;
            if (changed) OnPositionChanged?.Invoke(position);
        }

        protected virtual void SetRotation (Quaternion rotation)
        {
            var changed = this.rotation != rotation;
            this.rotation = rotation;
            if (changed) OnRotationChanged?.Invoke(rotation);
        }

        protected virtual void SetScale (Vector3 scale)
        {
            var changed = this.scale != scale;
            this.scale = scale;
            if (changed) OnScaleChanged?.Invoke(scale);
        }

        protected virtual void SetTintColor (Color tintColor)
        {
            var changed = this.tintColor != tintColor;
            this.tintColor = tintColor;
            if (changed) OnTintColorChanged?.Invoke(tintColor);
        }

        [Obsolete("To initialize transient actors use the 'InitializeTransientActor()' method.", true)]
        Awaitable IActor.Initialize () => throw Engine.Fail("Lifetime of the transient actors is not managed by Naninovel.");
    }
}
