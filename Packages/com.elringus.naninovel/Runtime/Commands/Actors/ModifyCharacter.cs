using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(@"
Modifies a [character actor](/guide/characters).",
        null,
        @"
; Shows character with ID 'Sora' with a default appearance.
@char Sora",
        @"
; Same as above, but sets appearance to 'Happy'.
@char Sora.Happy",
        @"
; Same as above, but additionally positions the character 45% away 
; from the left border of the scene and 10% away from the bottom border; 
; also makes it look to the left.
@char Sora.Happy look:left pos:45,10",
        @"
; Make Sora appear at the bottom-center and in front of Felix.
@char Sora pos:50,0,-1
@char Felix pos:,,0",
        @"
; Tint all visible characters on scene.
@char * tint:#ffdc22"
    )]
    [Serializable, Alias("char"), ActorsGroup, Icon("User")]
    [ActorContext(CharactersConfiguration.DefaultPathPrefix, paramId: nameof(Id))]
    [ConstantContext("Poses/Characters/{:Id??:IdAndAppearance[0]}+Poses/Characters/*", paramId: nameof(Pose))]
    public class ModifyCharacter : ModifyOrthoActor<ICharacterActor,
        CharacterState, CharacterMetadata, CharactersConfiguration, ICharacterManager>
    {
        [Doc("ID of the character to modify (specify `*` to affect all visible characters) and an appearance (or [pose](/guide/characters#poses)) to set. When appearance is not specified, will use either a `Default` (if exists) or a random one.")]
        [Alias(NamelessParameterAlias), RequiredParameter]
        [ActorContext(CharactersConfiguration.DefaultPathPrefix, 0), AppearanceContext(1)]
        public NamedStringParameter IdAndAppearance;
        [Doc("Look direction of the actor; supported values: left, right, center.")]
        [Alias("look"), ConstantContext(typeof(CharacterLookDirection))]
        public StringParameter LookDirection;
        [Doc("Name (path) of the [avatar texture](/guide/characters#avatar-textures) to assign for the character. Use `none` to remove (un-assign) avatar texture from the character.")]
        [Alias("avatar")]
        public StringParameter AvatarTexturePath;
        [Doc("Whether this command is an inlined prefix of a generic text line. Used internally by the script asset parser and serializer, don't assign manually.")]
        [Ignore]
        public BooleanParameter IsGenericPrefix;

        protected override bool AllowPreload => !IdAndAppearance.DynamicValue;
        protected override string AssignedId => base.AssignedId ?? IdAndAppearance?.Name;
        protected override string AlternativeAppearance => IdAndAppearance?.NamedValue;
        protected virtual CharacterLookDirection? AssignedLookDirection =>
            Assigned(LookDirection) ? ParseLookDirection(LookDirection) : PosedLookDirection;
        protected virtual CharacterLookDirection? PosedLookDirection =>
            GetPosed(nameof(CharacterState.LookDirection))?.LookDirection;

        protected override async Awaitable Modify (ExecutionContext ctx)
        {
            if (!Assigned(AvatarTexturePath)) // Check if we can map current appearance to an avatar texture path.
            {
                var avatarPath = $"{AssignedId}/{AssignedAppearance}";
                if (ActorManager.AvatarTextureExists(avatarPath))
                {
                    if (ActorManager.GetAvatarTexturePathFor(AssignedId) != avatarPath)
                        ActorManager.SetAvatarTexturePathFor(AssignedId, avatarPath);
                }
                else // Check if a default avatar texture for the character exists and assign if it does.
                {
                    var defaultAvatarPath = $"{AssignedId}/Default";
                    if (ActorManager.AvatarTextureExists(defaultAvatarPath) &&
                        ActorManager.GetAvatarTexturePathFor(AssignedId) != defaultAvatarPath)
                        ActorManager.SetAvatarTexturePathFor(AssignedId, defaultAvatarPath);
                }
            }
            else // User specified specific avatar texture path, assigning it.
            {
                if (AvatarTexturePath.Value.EqualsIgnoreCase("none"))
                    ActorManager.RemoveAvatarTextureFor(AssignedId);
                else ActorManager.SetAvatarTexturePathFor(AssignedId, AvatarTexturePath);
            }

            await base.Modify(ctx); // Wait for mods after applying the avatar to prevent concurrency issues.
        }

        protected override async Awaitable ApplyModifications (ICharacterActor actor, EasingType easingType,
            AsyncToken token)
        {
            var arrange = ShouldAutoArrange(actor);
            using var _ = Async.Rent(out var tasks);
            var duration = actor.Visible ? AssignedDuration : 0;
            var tween = new Tween(duration, easingType, complete: !AssignedLazy);
            tasks.Add(base.ApplyModifications(actor, easingType, token));
            tasks.Add(ApplyLookDirectionModification(actor, tween, token));
            if (arrange)
                tasks.Add(ActorManager.ArrangeCharacters(!AssignedLookDirection.HasValue,
                    new(AssignedDuration, easingType, complete: !AssignedLazy), token));
            await Async.All(tasks);
        }

        protected virtual bool ShouldAutoArrange (ICharacterActor actor)
        {
            if (!Configuration.AutoArrangeOnAdd) return false;
            var addingActor = !actor.Visible && (AssignedVisibility.HasValue && AssignedVisibility.Value ||
                                                 Configuration.AutoShowOnModify);
            var positionAssigned = AssignedPosition != null;
            var renderedToTexture = Configuration.GetMetadataOrDefault(AssignedId).RenderTexture != null;
            return addingActor && !positionAssigned && !renderedToTexture;
        }

        protected virtual Awaitable ApplyLookDirectionModification (ICharacterActor actor, Tween tween,
            AsyncToken token)
        {
            if (!AssignedLookDirection.HasValue) return Async.Completed;
            if (tween.Instant)
            {
                actor.LookDirection = AssignedLookDirection.Value;
                return Async.Completed;
            }
            return actor.ChangeLookDirection(AssignedLookDirection.Value, tween, token);
        }

        protected virtual CharacterLookDirection? ParseLookDirection (string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (ParseUtils.TryConstantParameter<CharacterLookDirection>(value, out var dir)) return dir;
            Err($"'{value}' is not a valid value for a character look direction; " +
                "see API guide for '@char' command for the list of supported values.");
            return null;
        }
    }
}
