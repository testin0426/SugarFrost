using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Hides all the visible characters on scene.",
        null,
        @"
; Hide all the visible character actors on scene.
@hideChars"
    )]
    [Serializable, Alias("hideChars"), ActorsGroup, Icon("UsersSlashDuo")]
    public class HideAllCharacters : Command
    {
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
            var manager = Engine.GetServiceOrErr<ICharacterManager>();
            var duration = Assigned(Duration) ? Duration.Value : manager.ActorManagerConfiguration.DefaultDuration;
            var easing = manager.ActorManagerConfiguration.DefaultEasing;
            var tween = new Tween(duration, easing, complete: !GetAssignedOrDefault(Lazy, false));
            using var _ = Async.Rent(out var tasks);
            using var __ = manager.RentActors(out var actors);
            foreach (var actor in actors)
                tasks.Add(actor.ChangeVisibility(false, tween, ctx.Token));
            await Async.All(tasks);
        }
    }
}
