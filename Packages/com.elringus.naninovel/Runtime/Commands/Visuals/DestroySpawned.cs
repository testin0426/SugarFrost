using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Destroys an object spawned with [@spawn] command.",
        @"
If prefab has a `MonoBehaviour` component attached to the root object, and the component implements
an `IParameterized` interface, will pass the specified `params` values before destroying the object;
if the component implements `IAwaitable` interface, command execution will wait for
the async completion task returned by the implementation before destroying the object.",
        @"
; Given '@spawn Rainbow' command was executed before, de-spawn (destroy) it.
@despawn Rainbow"
    )]
    [Serializable, Alias("despawn"), VisualsGroup, Icon("TrashCan")]
    public class DestroySpawned : Command
    {
        public interface IParameterized
        {
            void SetDestroyParameters ([CanBeNull] IReadOnlyList<string> parameters);
        }

        public interface IAwaitable
        {
            Awaitable AwaitDestroy (AsyncToken token = default);
        }

        [Doc("Name (path) of the prefab resource to destroy. A [@spawn] command with the same parameter is expected to be executed before.")]
        [Alias(NamelessParameterAlias), RequiredParameter]
        public StringParameter Path;
        [Doc("Parameters to set before destroying the prefab. Requires the prefab to have an `IParameterized` component attached to the root object.")]
        public StringListParameter Params;
        [Doc("Whether to wait while the spawn is destroying over time in case it implements `IAwaitable` interface.")]
        public BooleanParameter Wait;

        protected virtual ISpawnManager Spawns => Engine.GetServiceOrErr<ISpawnManager>();

        public override Awaitable Execute (ExecutionContext ctx)
        {
            if (Spawns.GetSpawned(Path) is not { } spawned)
            {
                Warn($"Failed to destroy '{Path}' spawned object: not spawned.");
                return Async.Completed;
            }
            Spawns.DestroySpawned(Path, false);
            return WaitOrForget(ctx => AwaitDestroy(spawned, ctx.Token), Wait, ctx);
        }

        protected virtual async Awaitable AwaitDestroy (SpawnedObject spawned, AsyncToken token)
        {
            if (!spawned.GameObject) return;
            spawned.SetDestroyParameters(ResolveParameters());
            await spawned.AwaitDestroy(token);
            ObjectUtils.DestroyOrImmediate(spawned.GameObject);
        }

        [CanBeNull]
        protected virtual IReadOnlyList<string> ResolveParameters ()
        {
            if (!Assigned(Params)) return null;
            return Params.ToReadOnlyList();
        }
    }
}
