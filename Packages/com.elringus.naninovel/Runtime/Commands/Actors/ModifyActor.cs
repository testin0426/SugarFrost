using System.Linq;
using UnityEngine;

namespace Naninovel.Commands
{
    public abstract class ModifyActor<TActor, TState, TMeta, TConfig, TManager> : Command, Command.IPreloadable
        where TActor : class, IActor
        where TState : ActorState<TActor>, new()
        where TMeta : ActorMetadata
        where TConfig : ActorManagerConfiguration<TMeta>, new()
        where TManager : class, IActorManager<TActor, TState, TMeta, TConfig>
    {
        [Doc("ID of the actor to modify; specify `*` to affect all visible actors.")]
        public StringParameter Id;
        [Doc("Appearance to set for the modified actor.")]
        [AppearanceContext]
        public StringParameter Appearance;
        [Doc("Pose to set for the modified actor.")]
        public StringParameter Pose;
        [Doc("Type of the [transition effect](/guide/special-effects#transition-effects) to use (crossfade is used by default).")]
        [Alias("via"), ConstantContext(typeof(TransitionType))]
        public StringParameter Transition;
        [Doc("Parameters of the transition effect.")]
        [Alias("params")]
        public DecimalListParameter TransitionParams;
        [Doc("Path to the [custom dissolve](/guide/special-effects#dissolve-mask) texture (path should be relative to a `Resources` folder). " +
             "Has effect only when the transition is set to `Custom` mode.")]
        [Alias("dissolve")]
        public StringParameter DissolveTexturePath;
        [Doc("Visibility status to set for the modified actor.")]
        public BooleanParameter Visible;
        [Doc("Position (in world space) to set for the modified actor. Use Z-component (third member) to move (sort) by depth while in ortho mode.")]
        [VectorContext("X,Y,Z")]
        public DecimalListParameter Position;
        [Doc("Rotation to set for the modified actor.")]
        [VectorContext("X,Y,Z")]
        public DecimalListParameter Rotation;
        [Doc("Scale to set for the modified actor.")]
        [VectorContext("X,Y,Z")]
        public DecimalListParameter Scale;
        [Doc(SharedDocs.TintParameter)]
        [Alias("tint"), ColorContext]
        public StringParameter TintColor;
        [Doc(SharedDocs.EasingParameter)]
        [Alias("easing"), ConstantContext(typeof(EasingType))]
        public StringParameter EasingTypeName;
        [Doc(SharedDocs.DurationParameter)]
        [Alias("time"), ParameterDefaultValue("0.35")]
        public DecimalParameter Duration;
        [Doc(SharedDocs.LazyParameter)]
        [ParameterDefaultValue("false")]
        public BooleanParameter Lazy;
        [Doc(SharedDocs.WaitParameter)]
        public BooleanParameter Wait;

        protected virtual string AssignedId => Id;
        protected virtual string AssignedTransition => Transition;
        protected virtual string AssignedAppearance => Assigned(Appearance) ? Appearance.Value : PosedAppearance ?? (PosedViaAppearance ? null : AlternativeAppearance);
        protected virtual bool? AssignedVisibility => Assigned(Visible) ? Visible.Value : PosedVisibility;
        protected virtual float?[] AssignedPosition => Assigned(Position) ? Position : PosedPosition;
        protected virtual float?[] AssignedRotation => Assigned(Rotation) ? Rotation : PosedRotation;
        protected virtual float?[] AssignedScale => Assigned(Scale) ? Scale : PosedScale;
        protected virtual Color? AssignedTintColor => Assigned(TintColor) ? ParseColor(TintColor) : PosedTintColor;
        protected virtual float AssignedDuration => Assigned(Duration) ? Duration.Value : ActorManager.ActorManagerConfiguration.DefaultDuration;
        protected virtual bool AssignedLazy => GetAssignedOrDefault(Lazy, false);
        protected virtual TManager ActorManager => Engine.GetServiceOrErr<TManager>();
        protected virtual TConfig Configuration => ActorManager.Configuration;
        protected virtual string AlternativeAppearance => null;
        protected virtual bool AllowPreload => Assigned(Id) && !Id.DynamicValue && Assigned(Appearance) && !Appearance.DynamicValue;
        protected virtual bool ShouldModifyAll => AssignedId == "*";
        protected virtual bool PosedViaAppearance => GetPoseOrNull() != null && !Assigned(Pose);
        protected virtual string PosedAppearance => GetPosed(nameof(ActorState.Appearance))?.Appearance;
        protected virtual bool? PosedVisibility => GetPosed(nameof(ActorState.Visible))?.Visible;
        protected virtual float?[] PosedPosition => GetPosed(nameof(ActorState.Position))?.Position.ToNullableArray();
        protected virtual float?[] PosedRotation => GetPosed(nameof(ActorState.Rotation))?.Rotation.eulerAngles.ToNullableArray();
        protected virtual float?[] PosedScale => GetPosed(nameof(ActorState.Scale))?.Scale.ToNullableArray();
        protected virtual Color? PosedTintColor => GetPosed(nameof(ActorState.TintColor))?.TintColor;
        protected virtual Texture2D PreloadedDissolveTexture { get; private set; }

        public virtual async Awaitable PreloadResources ()
        {
            if (Assigned(DissolveTexturePath) && !DissolveTexturePath.DynamicValue)
            {
                var loadTask = Resources.LoadAsync<Texture2D>(DissolveTexturePath);
                await loadTask;
                PreloadedDissolveTexture = loadTask.asset as Texture2D;
            }

            if (!AllowPreload || string.IsNullOrEmpty(AssignedId) || ShouldModifyAll) return;
            await ActorManager.GetOrAddActor(AssignedId);
            var loader = ActorManager.GetAppearanceLoader(AssignedId);
            if (loader != null && !string.IsNullOrEmpty(AssignedAppearance))
                await loader.Load(AssignedAppearance, this); // don't throw here - we warn on missing appearances
        }

        public virtual void ReleaseResources ()
        {
            PreloadedDissolveTexture = null;

            if (!AllowPreload || string.IsNullOrEmpty(AssignedId)) return;
            var loader = ActorManager?.GetAppearanceLoader(AssignedId);
            if (loader != null && !string.IsNullOrEmpty(AssignedAppearance))
                loader.Release(AssignedAppearance, this);
        }

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            if (string.IsNullOrEmpty(AssignedId))
                throw Fail("Failed to modify actor: ID was not specified.");

            if (!ShouldModifyAll)
            {
                // Make sure the actor is always loaded before proceeding, even when the command is not awaited.
                await ActorManager.GetOrAddActor(AssignedId);
                ctx.Token.ThrowIfCanceled();
            }

            await WaitOrForget(Modify, Wait, ctx);
        }

        protected virtual async Awaitable Modify (ExecutionContext ctx)
        {
            var easingType = Configuration.DefaultEasing;
            if (Assigned(EasingTypeName) && !ParseUtils.TryConstantParameter(EasingTypeName, out easingType))
                Warn($"Failed to parse '{EasingTypeName}' easing.");
            if (ShouldModifyAll)
            {
                using var _ = Async.Rent(out var tasks);
                using var __ = ActorManager.RentActors(out var actors);
                foreach (var actor in actors)
                    if (actor.Visible)
                        tasks.Add(ApplyModifications(actor, easingType, ctx.Token));
                await Async.All(tasks);
            }
            else await ApplyModifications(ActorManager.GetActorOrErr(AssignedId), easingType, ctx.Token);
        }

        protected virtual Awaitable ApplyModifications (TActor actor, EasingType easingType, AsyncToken token)
        {
            // In case the actor is hidden, apply all the modifications (except visibility) without animation.
            var durationOrZero = actor.Visible ? AssignedDuration : 0;
            // Change appearance with normal duration when a transition is assigned to preserve the effect.
            var appearDuration = string.IsNullOrEmpty(AssignedTransition) ? durationOrZero : AssignedDuration;
            var tw = new Tween(durationOrZero, easingType, complete: !AssignedLazy);
            return Async.All(
                ApplyPositionModification(actor, tw, token),
                ApplyRotationModification(actor, tw, token),
                ApplyScaleModification(actor, tw, token),
                ApplyTintColorModification(actor, tw, token),
                // Change appearance last, as it triggers re-rendering in some actor implementations,
                // which may capture an invalid pos/rotation/scale/tint in case of zero tween duration.
                ApplyAppearanceModification(actor, new(appearDuration, easingType, complete: !AssignedLazy), token),
                ApplyVisibilityModification(actor, new(AssignedDuration, easingType, complete: !AssignedLazy), token)
            );
        }

        protected virtual Awaitable ApplyAppearanceModification (TActor actor, Tween tween, AsyncToken token)
        {
            if (string.IsNullOrEmpty(AssignedAppearance)) return Async.Completed;

            var transitionName = TransitionUtils.ResolveParameterValue(AssignedTransition);
            var defaultParams = TransitionUtils.GetDefaultParams(transitionName);
            var transitionParams = Assigned(TransitionParams)
                ? new(
                    TransitionParams.ElementAtOrNull(0) ?? defaultParams.x,
                    TransitionParams.ElementAtOrNull(1) ?? defaultParams.y,
                    TransitionParams.ElementAtOrNull(2) ?? defaultParams.z,
                    TransitionParams.ElementAtOrNull(3) ?? defaultParams.w)
                : defaultParams;
            if (Assigned(DissolveTexturePath) && !ObjectUtils.IsValid(PreloadedDissolveTexture))
                PreloadedDissolveTexture = Resources.Load<Texture2D>(DissolveTexturePath);
            var transition = new Transition(transitionName, transitionParams, PreloadedDissolveTexture);

            return actor.ChangeAppearance(AssignedAppearance, tween, transition, token);
        }

        protected virtual Awaitable ApplyVisibilityModification (TActor actor, Tween tween, AsyncToken token)
        {
            if (AssignedVisibility.HasValue)
                return actor.ChangeVisibility(AssignedVisibility.Value, tween, token);
            if (!actor.Visible && Configuration.AutoShowOnModify)
                return actor.ChangeVisibility(true, tween, token);
            return Async.Completed;
        }

        protected virtual Awaitable ApplyPositionModification (TActor actor, Tween tween, AsyncToken token)
        {
            var position = AssignedPosition;
            if (position is null) return Async.Completed;
            return actor.ChangePosition(new(
                position.ElementAtOrDefault(0) ?? actor.Position.x,
                position.ElementAtOrDefault(1) ?? actor.Position.y,
                position.ElementAtOrDefault(2) ?? actor.Position.z), tween, token);
        }

        protected virtual Awaitable ApplyRotationModification (TActor actor, Tween tween, AsyncToken token)
        {
            var rotation = AssignedRotation;
            if (rotation is null) return Async.Completed;
            return actor.ChangeRotation(Quaternion.Euler(
                rotation.ElementAtOrDefault(0) ?? actor.Rotation.eulerAngles.x,
                rotation.ElementAtOrDefault(1) ?? actor.Rotation.eulerAngles.y,
                rotation.ElementAtOrDefault(2) ?? actor.Rotation.eulerAngles.z), tween, token);
        }

        protected virtual Awaitable ApplyScaleModification (TActor actor, Tween tween, AsyncToken token)
        {
            var scale = AssignedScale;
            if (scale is null) return Async.Completed;
            return actor.ChangeScale(new(
                scale.ElementAtOrDefault(0) ?? actor.Scale.x,
                scale.ElementAtOrDefault(1) ?? actor.Scale.y,
                scale.ElementAtOrDefault(2) ?? actor.Scale.z), tween, token);
        }

        protected virtual Awaitable ApplyTintColorModification (TActor actor, Tween tween, AsyncToken token)
        {
            if (!AssignedTintColor.HasValue) return Async.Completed;
            return actor.ChangeTintColor(AssignedTintColor.Value, tween, token);
        }

        protected virtual Color? ParseColor (string color)
        {
            if (string.IsNullOrEmpty(color)) return null;

            if (!ColorUtility.TryParseHtmlString(TintColor, out var result))
            {
                Err($"Failed to parse '{TintColor}' color to apply tint modification. " +
                    "See the API docs for supported color formats.");
                return null;
            }
            return result;
        }

        protected virtual ActorPose<TState> GetPoseOrNull ()
        {
            var poseName = Assigned(Pose) ? Pose.Value : AlternativeAppearance;
            if (string.IsNullOrEmpty(poseName)) return null;
            return Configuration.GetActorOrSharedPose<TState>(AssignedId, poseName);
        }

        protected virtual TState GetPosed (string propertyName)
        {
            var pose = GetPoseOrNull();
            return pose != null && pose.IsPropertyOverridden(propertyName) ? pose.ActorState : null;
        }
    }
}
