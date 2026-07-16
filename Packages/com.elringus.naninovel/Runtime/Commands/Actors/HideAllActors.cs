using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Hides all the actors (characters, backgrounds, text printers, choice handlers) on scene.",
        null,
        @"
; Hide all the visible actors (chars, backs, printers, etc) on scene.
@hideAll"
    )]
    [Serializable, Alias("hideAll"), ActorsGroup, Icon("UsersSlashDuo")]
    public class HideAllActors : Command
    {
        [Doc(SharedDocs.DurationParameter)]
        [Alias("time"), ParameterDefaultValue("0.35")]
        public DecimalParameter Duration;
        [Doc(SharedDocs.LazyParameter)]
        [ParameterDefaultValue("false")]
        public BooleanParameter Lazy;
        [Doc(SharedDocs.WaitParameter)]
        public BooleanParameter Wait;

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            using var _ = Async.Rent(out var tasks);
            foreach (var manager in Engine.Services)
                if (manager is IActorManager actorManager)
                    tasks.Add(HideManagedActors(actorManager, ctx.Token));
            await WaitOrForget(ctx => Async.All(tasks), Wait, ctx);
        }

        protected virtual async Awaitable HideManagedActors (IActorManager manager, AsyncToken token)
        {
            var duration = Assigned(Duration) ? Duration.Value : manager.ActorManagerConfiguration.DefaultDuration;
            var easing = manager.ActorManagerConfiguration.DefaultEasing;
            var tween = new Tween(duration, easing, complete: !GetAssignedOrDefault(Lazy, false));
            using var _ = Async.Rent(out var tasks);
            using var __ = manager.RentActors(out var actors);
            foreach (var actor in actors)
                tasks.Add(actor.ChangeVisibility(false, tween, token));
            await Async.All(tasks);
        }
    }
}
