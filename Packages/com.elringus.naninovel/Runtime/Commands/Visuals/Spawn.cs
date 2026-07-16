using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Instantiates a prefab or a [special effect](/guide/special-effects);
when performed over an already spawned object, will update the spawn parameters instead.",
        @"
If prefab has a `MonoBehaviour` component attached to the root object, and the component implements
an `IParameterized` interface, will pass the specified `params` values after the spawn;
if the component implements `IAwaitable` interface, command execution will be able to wait for
the async completion task returned by the implementation.",
        @"
; Given a 'Rainbow' prefab is assigned in spawn resources, instantiate it.
@spawn Rainbow"
    )]
    [Serializable, VisualsGroup]
    public class Spawn : Command, Command.IPreloadable
    {
        /// <summary>
        /// When implemented by a component on the spawned object root,
        /// enables the object to receive spawn parameters from scenario scripts.
        /// </summary>
        public interface IParameterized
        {
            /// <summary>
            /// Applies parameters to the spawned object.
            /// </summary>
            /// <param name="parameters">The parameters to apply.</param>
            /// <param name="asap">Whether to apply all the parameters instantly, even when a duration is specified.</param>
            void SetSpawnParameters ([CanBeNull] IReadOnlyList<string> parameters, bool asap);
        }

        /// <summary>
        /// When implemented by a component on the spawned object root, enables the object to be awaited on spawn.
        /// </summary>
        public interface IAwaitable
        {
            /// <summary>
            /// Returns task to be awaited on spawn.
            /// </summary>
            Awaitable AwaitSpawn (AsyncToken token = default);
        }

        [Doc("Name (path) of the prefab resource to spawn.")]
        [Alias(NamelessParameterAlias), RequiredParameter, ResourceContext(SpawnConfiguration.DefaultPathPrefix)]
        public StringParameter Path;
        [Doc("Parameters to set when spawning the prefab. Requires the prefab to have an `IParameterized` component attached to the root object.")]
        public StringListParameter Params;
        [Doc("Position (relative to the scene borders, in percents) to set for the spawned object. " +
             "Position is described as follows: `0,0` is the bottom left, `50,50` is the center and `100,100` is the top right corner of the scene. " +
             "Use Z-component (third member, eg `,,10`) to move (sort) by depth while in ortho mode.")]
        [Alias("pos"), VectorContext("X,Y,Z")]
        public DecimalListParameter ScenePosition;
        [Doc("Position (in world space) to set for the spawned object.")]
        [VectorContext("X,Y,Z")]
        public DecimalListParameter Position;
        [Doc("Rotation to set for the spawned object.")]
        [VectorContext("X,Y,Z")]
        public DecimalListParameter Rotation;
        [Doc("Scale to set for the spawned object.")]
        [VectorContext("X,Y,Z")]
        public DecimalListParameter Scale;
        [Doc("Whether to wait for the spawn to warm-up in case it implements `IAwaitable` interface.")]
        public BooleanParameter Wait;

        protected virtual ISpawnManager Spawns => Engine.GetServiceOrErr<ISpawnManager>();

        public virtual async Awaitable PreloadResources ()
        {
            if (!Assigned(Path) || Path.DynamicValue || string.IsNullOrWhiteSpace(Path)) return;
            await Spawns.HoldResources(Path, this);
        }

        public virtual void ReleaseResources ()
        {
            if (!Assigned(Path) || Path.DynamicValue || string.IsNullOrWhiteSpace(Path)) return;
            Spawns.ReleaseResources(Path, this);
        }

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            if (Spawns.GetSpawned(Path) is { } spawned)
            {
                if (ResolvePosition(spawned) is { } pos) spawned.Transform.position = pos;
                if (ResolveRotation(spawned) is { } rot) spawned.Transform.rotation = rot;
                if (ResolveScale(spawned) is { } sca) spawned.Transform.localScale = sca;
                spawned.SetSpawnParameters(ResolveParameters(), ctx.Token.Completed);
                await WaitOrForget(ctx => spawned.AwaitSpawn(ctx.Token), Wait, ctx);
                return;
            }

            var options = new InstantiateOptions {
                Position = ResolvePosition() ?? default,
                Rotation = ResolveRotation() ?? default,
                Scale = ResolveScale()
            };
            // Always wait for the spawn (don't "WaitOrForget"), otherwise the state is corrupted on rollback.
            spawned = await Spawns.Spawn(Path, options, ctx.Token);
            spawned.SetSpawnParameters(ResolveParameters(), ctx.Token.Completed);
            await WaitOrForget(ctx => spawned.AwaitSpawn(ctx.Token), Wait, ctx);
        }

        protected virtual Vector3? ResolvePosition ([CanBeNull] SpawnedObject spawned = null)
        {
            if (Assigned(ScenePosition))
            {
                var config = Engine.GetServiceOrErr<ICameraManager>().Configuration;
                return new(
                    ScenePosition.ElementAtOrDefault(0) != null
                        ? config.SceneToWorldSpace(new(ScenePosition[0] / 100f, 0)).x
                        : spawned?.Transform.position.x ?? 0,
                    ScenePosition.ElementAtOrDefault(1) != null
                        ? config.SceneToWorldSpace(new(0, ScenePosition[1] / 100f)).y
                        : spawned?.Transform.position.y ?? 0,
                    ScenePosition.ElementAtOrDefault(2) ?? spawned?.Transform.position.z ?? 0);
            }
            if (Assigned(Position))
                return new(
                    Position.ElementAtOrDefault(0) ?? spawned?.Transform.position.x ?? 0,
                    Position.ElementAtOrDefault(1) ?? spawned?.Transform.position.y ?? 0,
                    Position.ElementAtOrDefault(2) ?? spawned?.Transform.position.z ?? 0);
            return null;
        }

        protected virtual Quaternion? ResolveRotation ([CanBeNull] SpawnedObject spawned = null)
        {
            if (!Assigned(Rotation)) return null;
            return Quaternion.Euler(
                Rotation.ElementAtOrDefault(0) ?? spawned?.Transform.eulerAngles.x ?? 0,
                Rotation.ElementAtOrDefault(1) ?? spawned?.Transform.eulerAngles.y ?? 0,
                Rotation.ElementAtOrDefault(2) ?? spawned?.Transform.eulerAngles.z ?? 0);
        }

        protected virtual Vector3? ResolveScale ([CanBeNull] SpawnedObject spawned = null)
        {
            if (!Assigned(Scale)) return null;
            if (Scale.Length == 1 && Scale[0].HasValue) return new(Scale[0], Scale[0], Scale[0]);
            return new(
                Scale.ElementAtOrDefault(0) ?? spawned?.Transform.localScale.x ?? 1,
                Scale.ElementAtOrDefault(1) ?? spawned?.Transform.localScale.y ?? 1,
                Scale.ElementAtOrDefault(2) ?? spawned?.Transform.localScale.z ?? 1);
        }

        [CanBeNull]
        protected virtual IReadOnlyList<string> ResolveParameters ()
        {
            if (!Assigned(Params)) return null;
            return Params.ToReadOnlyList();
        }
    }
}
