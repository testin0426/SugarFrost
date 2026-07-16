using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IActor"/> implementation using <see cref="MonoBehaviour"/> to represent the actor.
    /// </summary>
    public abstract class MonoBehaviourActor : IActor, IDisposable
    {
        public virtual event Action<string> OnAppearanceChanged;
        public virtual event Action<bool> OnVisibilityChanged;
        public virtual event Action<Vector3> OnPositionChanged;
        public virtual event Action<Quaternion> OnRotationChanged;
        public virtual event Action<Vector3> OnScaleChanged;
        public virtual event Action<Color> OnTintColorChanged;

        public virtual string Id { get; }
        public virtual string Appearance
        {
            get => appearance;
            set
            {
                appearance = value;
                OnAppearanceChanged?.Invoke(value);
            }
        }
        public virtual bool Visible
        {
            get => visible;
            set
            {
                visible = value;
                OnVisibilityChanged?.Invoke(value);
            }
        }
        public virtual Vector3 Position
        {
            get => position;
            set
            {
                CompletePositionTween();
                position = value;
                SetBehaviourPosition(value);
                OnPositionChanged?.Invoke(value);
            }
        }
        public virtual Quaternion Rotation
        {
            get => rotation;
            set
            {
                CompleteRotationTween();
                rotation = value;
                SetBehaviourRotation(value);
                OnRotationChanged?.Invoke(value);
            }
        }
        public virtual Vector3 Scale
        {
            get => scale;
            set
            {
                CompleteScaleTween();
                scale = value;
                SetBehaviourScale(value);
                OnScaleChanged?.Invoke(value);
            }
        }
        public virtual Color TintColor
        {
            get => tintColor;
            set
            {
                CompleteTintColorTween();
                tintColor = value;
                SetBehaviourTintColor(value);
                OnTintColorChanged?.Invoke(value);
            }
        }
        public virtual GameObject GameObject { get; private set; }
        public virtual Transform Transform => GameObject.transform;
        public virtual List<ActorAnchor> Anchors { get; } = new();

        private readonly Tweener<VectorTween> positionTweener = new();
        private readonly Tweener<VectorTween> rotationTweener = new();
        private readonly Tweener<VectorTween> scaleTweener = new();
        private readonly Tweener<ColorTween> tintColorTweener = new();
        private string appearance;
        private bool visible;
        private Vector3 position = Vector3.zero;
        private Vector3 scale = Vector3.one;
        private Quaternion rotation = Quaternion.identity;
        private Color tintColor = Color.white;

        protected MonoBehaviourActor (string id)
        {
            Id = id;
        }

        public virtual Awaitable Initialize ()
        {
            GameObject = CreateHostObject();
            return Async.Completed;
        }

        public virtual Awaitable ChangeAppearance (string appearance, Tween tween,
            Transition? transition = default, AsyncToken token = default)
        {
            this.appearance = appearance;
            OnAppearanceChanged?.Invoke(appearance);
            return Async.Completed;
        }

        public virtual Awaitable ChangeVisibility (bool visible, Tween tween, AsyncToken token = default)
        {
            this.visible = visible;
            OnVisibilityChanged?.Invoke(visible);
            return Async.Completed;
        }

        public virtual Awaitable ChangePosition (Vector3 position, Tween tween, AsyncToken token = default)
        {
            CompletePositionTween();
            this.position = position;
            OnPositionChanged?.Invoke(position);

            var tw = new VectorTween(GetBehaviourPosition(), position, tween, SetBehaviourPosition);
            return positionTweener.Run(tw, token, GameObject);
        }

        public virtual Awaitable ChangeRotation (Quaternion rotation, Tween tween, AsyncToken token = default)
        {
            CompleteRotationTween();
            this.rotation = rotation;
            OnRotationChanged?.Invoke(rotation);

            var tw = new VectorTween(GetBehaviourRotation().ClampedEulerAngles(),
                rotation.ClampedEulerAngles(), tween, SetBehaviourRotation);
            return rotationTweener.Run(tw, token, GameObject);
        }

        public virtual Awaitable ChangeScale (Vector3 scale, Tween tween, AsyncToken token = default)
        {
            CompleteScaleTween();
            this.scale = scale;
            OnScaleChanged?.Invoke(scale);

            var tw = new VectorTween(GetBehaviourScale(), scale, tween, SetBehaviourScale);
            return scaleTweener.Run(tw, token, GameObject);
        }

        public virtual Awaitable ChangeTintColor (Color tintColor, Tween tween, AsyncToken token = default)
        {
            CompleteTintColorTween();
            this.tintColor = tintColor;
            OnTintColorChanged?.Invoke(tintColor);

            var tw = new ColorTween(GetBehaviourTintColor(), tintColor, tween,
                ColorTweenMode.All, SetBehaviourTintColor);
            return tintColorTweener.Run(tw, token, GameObject);
        }

        public virtual void Dispose () => ObjectUtils.DestroyOrImmediate(GameObject);

        public virtual CancellationToken GetDestroyCancellationToken ()
        {
            if (GameObject.TryGetComponent<CancelOnDestroy>(out var component))
                return component.Token;
            return GameObject.AddComponent<CancelOnDestroy>().Token;
        }

        protected virtual Vector3 GetBehaviourPosition () => Transform.position;
        protected virtual void SetBehaviourPosition (Vector3 position) => Transform.position = position;
        protected virtual Quaternion GetBehaviourRotation () => Transform.rotation;
        protected virtual void SetBehaviourRotation (Quaternion rotation) => Transform.rotation = rotation;
        protected virtual void SetBehaviourRotation (Vector3 rotation) => SetBehaviourRotation(Quaternion.Euler(rotation));
        protected virtual Vector3 GetBehaviourScale () => Transform.localScale;
        protected virtual void SetBehaviourScale (Vector3 scale) => Transform.localScale = scale;
        protected abstract Color GetBehaviourTintColor ();
        protected abstract void SetBehaviourTintColor (Color tintColor);
        protected abstract string BuildActorGroup ();

        protected virtual GameObject CreateHostObject ()
        {
            return Engine.CreateObject(new() { Name = Id, Parent = GetOrCreateParent() });
        }

        protected virtual Transform GetOrCreateParent ()
        {
            var group = BuildActorGroup();
            if (string.IsNullOrEmpty(group))
                throw new Error($"Failed to evaluate parent name for {Id} actor.");
            var obj = Engine.FindObject(group);
            return obj ? obj.transform : Engine.CreateObject(new() { Name = group }).transform;
        }

        private void CompletePositionTween ()
        {
            if (positionTweener.Running)
                positionTweener.Complete();
        }

        private void CompleteRotationTween ()
        {
            if (rotationTweener.Running)
                rotationTweener.Complete();
        }

        private void CompleteScaleTween ()
        {
            if (scaleTweener.Running)
                scaleTweener.Complete();
        }

        private void CompleteTintColorTween ()
        {
            if (tintColorTweener.Running)
                tintColorTweener.Complete();
        }
    }

    /// <inheritdoc cref="MonoBehaviourActor"/>
    public abstract class MonoBehaviourActor<TMeta> : MonoBehaviourActor
        where TMeta : ActorMetadata
    {
        public virtual TMeta ActorMeta { get; }

        protected MonoBehaviourActor (string id, TMeta meta) : base(id)
        {
            ActorMeta = meta;
        }

        protected override string BuildActorGroup ()
        {
            return typeof(TMeta).Name.GetBefore("Metadata");
        }
    }
}
