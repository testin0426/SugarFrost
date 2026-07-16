using System;
using System.Linq;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Hides actors (character, background, text printer, choice handler) with the specified IDs.
In case multiple actors with the same ID found (eg, a character and a printer), will affect only the first found one.",
        null,
        @"
; Given an actor with ID 'Smoke' is visible, hide it over 3 seconds.
@hide Smoke time:3",
        @"
; Hide 'Kohaku' and 'Yuko' actors.
@hide Kohaku,Yuko"
    )]
    [Serializable, Alias("hide"), ActorsGroup, Icon("UserSlashDuo")]
    public class HideActors : Command
    {
        [Doc("IDs of the actors to hide.")]
        [Alias(NamelessParameterAlias), RequiredParameter, ActorContext]
        public StringListParameter ActorIds;
        [Doc(SharedDocs.DurationParameter)]
        [Alias("time"), ParameterDefaultValue("0.35")]
        public DecimalParameter Duration;
        [Doc(SharedDocs.LazyParameter)]
        [ParameterDefaultValue("false")]
        public BooleanParameter Lazy;
        [Doc(SharedDocs.WaitParameter)]
        public BooleanParameter Wait;

        public override Awaitable Execute (ExecutionContext ctx)
        {
            return WaitOrForget(Hide, Wait, ctx);
        }

        protected virtual async Awaitable Hide (ExecutionContext ctx)
        {
            using var _ = ListPool<IActorManager>.Rent(out var managers);
            Engine.FindAllServices(managers, ActorIds,
                static (manager, actorIds) => actorIds.Any(id => manager.ActorExists(id)));
            using var __ = Async.Rent(out var tasks);
            foreach (var actorId in ActorIds)
                if (managers.FirstOrDefault(m => m.ActorExists(actorId)) is { } manager)
                    tasks.Add(HideInManager(actorId, manager, ctx.Token));
                else Err($"Failed to hide '{actorId}' actor: can't find any managers with '{actorId}' actor.");
            await Async.All(tasks);
        }

        protected virtual async Awaitable HideInManager (string actorId, IActorManager manager, AsyncToken token)
        {
            var actor = manager.GetActorOrErr(actorId);
            var duration = Assigned(Duration) ? Duration.Value : manager.ActorManagerConfiguration.DefaultDuration;
            var easing = manager.ActorManagerConfiguration.DefaultEasing;
            await actor.ChangeVisibility(false, new(duration, easing, complete: !GetAssignedOrDefault(Lazy, false)), token);
        }
    }
}
