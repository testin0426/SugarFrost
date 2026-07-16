using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation, which doesn't have any presence on scene
    /// and can be used to represent a narrator (author of the printed text messages).
    /// </summary>
    [ActorResources(null, false)]
    public class NarratorCharacter : ICharacterActor
    {
        public event Action<string> OnAppearanceChanged;
        public event Action<bool> OnVisibilityChanged;
        public event Action<Vector3> OnPositionChanged;
        public event Action<Quaternion> OnRotationChanged;
        public event Action<Vector3> OnScaleChanged;
        public event Action<Color> OnTintColorChanged;
        public event Action<CharacterLookDirection> OnLookDirectionChanged;

        public string Id { get; }
        public string Appearance
        {
            get => appearance;
            set
            {
                appearance = value;
                OnAppearanceChanged?.Invoke(value);
            }
        }
        public bool Visible
        {
            get => visible;
            set
            {
                visible = value;
                OnVisibilityChanged?.Invoke(value);
            }
        }
        public Vector3 Position
        {
            get => position;
            set
            {
                position = value;
                OnPositionChanged?.Invoke(value);
            }
        }
        public Quaternion Rotation
        {
            get => rotation;
            set
            {
                rotation = value;
                OnRotationChanged?.Invoke(value);
            }
        }
        public Vector3 Scale
        {
            get => scale;
            set
            {
                scale = value;
                OnScaleChanged?.Invoke(value);
            }
        }
        public Color TintColor
        {
            get => tintColor;
            set
            {
                tintColor = value;
                OnTintColorChanged?.Invoke(value);
            }
        }
        public CharacterLookDirection LookDirection
        {
            get => lookDirection;
            set
            {
                lookDirection = value;
                OnLookDirectionChanged?.Invoke(value);
            }
        }

        private string appearance;
        private bool visible;
        private Vector3 position;
        private Quaternion rotation;
        private Vector3 scale;
        private Color tintColor;
        private CharacterLookDirection lookDirection;

        public NarratorCharacter (string id, CharacterMetadata metadata)
        {
            Id = id;
        }

        public Awaitable Initialize () => Async.Completed;

        public Awaitable ChangeAppearance (string appearance, Tween tween,
            Transition? transition = default, AsyncToken token = default)
        {
            Appearance = appearance;
            return Async.Completed;
        }

        public Awaitable ChangeVisibility (bool visible, Tween tween,
            AsyncToken token = default)
        {
            Visible = visible;
            return Async.Completed;
        }

        public Awaitable ChangePosition (Vector3 position, Tween tween,
            AsyncToken token = default)
        {
            Position = position;
            return Async.Completed;
        }

        public Awaitable ChangeRotation (Quaternion rotation, Tween tween,
            AsyncToken token = default)
        {
            Rotation = rotation;
            return Async.Completed;
        }

        public Awaitable ChangeScale (Vector3 scale, Tween tween,
            AsyncToken token = default)
        {
            Scale = scale;
            return Async.Completed;
        }

        public Awaitable ChangeTintColor (Color tintColor, Tween tween,
            AsyncToken token = default)
        {
            TintColor = tintColor;
            return Async.Completed;
        }

        public Awaitable ChangeLookDirection (CharacterLookDirection lookDirection, Tween tween,
            AsyncToken token = default)
        {
            LookDirection = lookDirection;
            return Async.Completed;
        }
    }
}
