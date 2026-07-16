using System;
using System.Linq;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(@"
Shows (makes visible) actors (character, background, text printer, choice handler, etc) with the specified IDs.
In case multiple actors with the same ID found (eg, a character and a printer), will affect only the first found one.",
        null,
        @"
; Given an actor with ID 'Smoke' is hidden, reveal it over 3 seconds.
@show Smoke time:3",
        @"
; Show 'Kohaku' and 'Yuko' actors.
@show Kohaku,Yuko"
    )]
    [Serializable, Alias("show"), ActorsGroup]
    public class ShowActors : Command
    {
        [Doc("IDs of the actors to show.")]
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
            return WaitOrForget(Show, Wait, ctx);
        }

        protected virtual async Awaitable Show (ExecutionContext ctx)
        {
            using var _ = ListPool<IActorManager>.Rent(out var managers);
            Engine.FindAllServices<IActorManager, StringListParameter>(managers, ActorIds,
                static (manager, actorIds) => actorIds.Any(id => manager.ActorExists(id)));
            using var __ = Async.Rent(out var tasks);
            foreach (var actorId in ActorIds)
                if (managers.FirstOrDefault(m => m.ActorExists(actorId)) is { } manager)
                    tasks.Add(manager.GetActorOrErr(actorId).ChangeVisibility(true, new(GetDuration(manager), GetEasing(manager),
                        complete: !GetAssignedOrDefault(Lazy, false)), ctx.Token));
                else Err($"Failed to show '{actorId}' actor: can't find any managers with '{actorId}' actor.");
            await Async.All(tasks);
        }

        protected virtual float GetDuration (IActorManager manager)
        {
            return Assigned(Duration) ? Duration.Value : manager.ActorManagerConfiguration.DefaultDuration;
        }

        protected virtual EasingType GetEasing (IActorManager manager)
        {
            return manager.ActorManagerConfiguration.DefaultEasing;
        }
    }
}
