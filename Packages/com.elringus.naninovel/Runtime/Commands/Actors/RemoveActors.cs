using System;
using System.Linq;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(@"
Removes (disposes) actors (character, background, text printer, choice handler) with the specified IDs.
In case multiple actors with the same ID found (eg, a character and a printer), will affect only the first found one.",
        @"
By default, Naninovel automatically removes unused actors when unloading script resources; only use this command when
`Remove Actors` is disabled in resource provider configuration or when you need to force-dispose an actor at specific
moment. Consult [memory management](/guide/memory-management#actor-resources) guide for more info.",
        @"
; Fade-off and then dispose Kohaku and Yuko actors.
@hide Kohaku,Yuko wait!
@remove Kohaku,Yuko",
        @"
; Fade-off and remove all actors.
@hideAll wait!
@remove *"
    )]
    [Serializable, Alias("remove"), ActorsGroup, Icon("TrashCan")]
    public class RemoveActors : Command
    {
        [Doc("IDs of the actors to remove or `*` to remove all actors.")]
        [Alias(NamelessParameterAlias), RequiredParameter, ActorContext]
        public StringListParameter ActorIds;

        public override Awaitable Execute (ExecutionContext ctx)
        {
            if (ShouldRemoveAll()) RemoveAll();
            else RemoveSpecified();
            return Async.Completed;
        }

        protected virtual bool ShouldRemoveAll ()
        {
            return ActorIds.FirstOrDefault() == "*";
        }

        protected virtual void RemoveAll ()
        {
            foreach (var manager in Engine.Services)
                if (manager is IActorManager actorManager)
                    actorManager.RemoveAllActors();
        }

        protected virtual void RemoveSpecified ()
        {
            using var _ = ListPool<IActorManager>.Rent(out var managers);
            Engine.FindAllServices<IActorManager, StringListParameter>(managers, ActorIds,
                static (manager, actorIds) => actorIds.Any(id => manager.ActorExists(id)));
            foreach (var actorId in ActorIds)
                if (managers.FirstOrDefault(m => m.ActorExists(actorId)) is { } manager)
                    manager.RemoveActor(actorId);
                else Err($"Failed to remove '{actorId}' actor: can't find any managers with '{actorId}' actor.");
        }
    }
}
