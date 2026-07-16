using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    [ActorResources(null, false)]
    public class PlaceholderCharacter : PlaceholderActor<PlaceholderCharacterBehaviour, CharacterMetadata>, ICharacterActor
    {
        public event Action<CharacterLookDirection> OnLookDirectionChanged;

        public virtual CharacterLookDirection LookDirection { get => GetLookDirection(); set => SetLookDirection(value); }

        protected override string ResourcePath { get; } = "Placeholder/Character";

        private ICharacterManager chars;
        private ICustomVariableManager vars;
        private string variableName;

        public PlaceholderCharacter (string id, CharacterMetadata meta) : base(id, meta)
        {
            meta.Pivot = new(.5f, -1);
            meta.BakedLookDirection = CharacterLookDirection.Center;
            meta.HasName = true;
            meta.HighlightWhenSpeaking = true;
            meta.HighlightCharacterCount = 0;
            meta.SpeakingPose = "Speaking";
            meta.NotSpeakingPose = "Silent";
            meta.PlaceOnTop = true;
            meta.HighlightDuration = 0.35f;
            meta.HighlightEasing = EasingType.SmoothStep;
            meta.Poses = new List<CharacterMetadata.Pose> {
                new("Speaking", new(scale: new(1.1f, 1.1f), tintColor: Color.white), "scale", "tintColor"),
                new("Silent", new(scale: Vector3.one, tintColor: new(.75f, .75f, .75f, .75f)), "scale", "tintColor"),
            };
        }

        public override async Awaitable Initialize ()
        {
            await base.Initialize();
            chars = Engine.GetServiceOrErr<ICharacterManager>();
            vars = Engine.GetServiceOrErr<ICustomVariableManager>();
            Behaviour.SetId(Id);
            var name = ActorMeta.DisplayName;
            variableName = name?.GetBetween("{", "}")?.Trim();
            Behaviour.SetName(string.IsNullOrWhiteSpace(name) ? Id : name);
            Behaviour.SetColor(ActorMeta.NameColor);
            vars.OnVariableUpdated += HandleVariableUpdated;
        }

        public override void Dispose ()
        {
            base.Dispose();
            vars.OnVariableUpdated -= HandleVariableUpdated;
        }

        public virtual Awaitable ChangeLookDirection (CharacterLookDirection lookDirection, Tween tween, AsyncToken token = default)
        {
            OnLookDirectionChanged?.Invoke(lookDirection);
            Behaviour.NotifyLookDirectionChanged(lookDirection);
            return TransitionalRenderer.ChangeLookDirection(lookDirection,
                ActorMeta.BakedLookDirection, tween, token);
        }

        protected virtual CharacterLookDirection GetLookDirection ()
        {
            return TransitionalRenderer.GetLookDirection(ActorMeta.BakedLookDirection);
        }

        protected virtual void SetLookDirection (CharacterLookDirection value)
        {
            OnLookDirectionChanged?.Invoke(value);
            Behaviour.NotifyLookDirectionChanged(value);
            TransitionalRenderer.SetLookDirection(value, ActorMeta.BakedLookDirection);
        }

        protected virtual void HandleVariableUpdated (CustomVariableUpdatedArgs args)
        {
            if (!string.IsNullOrEmpty(variableName) && vars.VariableExists(variableName))
                Behaviour.SetName(chars.GetAuthorName(Id));
        }
    }
}
