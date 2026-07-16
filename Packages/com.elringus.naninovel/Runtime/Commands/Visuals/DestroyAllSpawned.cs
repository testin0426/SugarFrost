using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Destroys all the objects spawned with [@spawn] command.
Equal to invoking [@despawn] for all the currently spawned objects.",
        null,
        @"
@spawn Rainbow
@spawn SunShafts
; Will de-spawn (destroy) both Rainbow and SunShafts.
@despawnAll"
    )]
    [Serializable, Alias("despawnAll"), VisualsGroup, Icon("TrashCan")]
    public class DestroyAllSpawned : Command
    {
        [Doc("Whether to wait while the spawns are destroying over time in case they implement `IAwaitable` interface.")]
        public BooleanParameter Wait;

        protected virtual ISpawnManager Spawns => Engine.GetServiceOrErr<ISpawnManager>();

        public override Awaitable Execute (ExecutionContext ctx)
        {
            var spawned = new List<SpawnedObject>();
            Spawns.CollectSpawned(spawned);
            foreach (var spawn in spawned)
                Spawns.DestroySpawned(spawn.Path, false);
            return WaitOrForget(ctx => WaitDestroy(spawned, ctx.Token), Wait, ctx);
        }

        protected virtual async Awaitable WaitDestroy (IReadOnlyCollection<SpawnedObject> spawned, AsyncToken token)
        {
            using var _ = Async.Rent(out var tasks);
            foreach (var spawn in spawned)
                if (spawn.GameObject)
                    tasks.Add(spawn.AwaitDestroy(token));
            await Async.All(tasks);
            token.ThrowIfCanceled();
            foreach (var spawn in spawned)
                ObjectUtils.DestroyOrImmediate(spawn.GameObject);
        }
    }
}
