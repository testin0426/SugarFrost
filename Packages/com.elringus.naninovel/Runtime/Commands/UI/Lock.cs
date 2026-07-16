using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Sets an [unlockable item](/guide/unlockable-items) with the specified ID to `locked` state.",
        @"
The unlocked state of the items is stored in [global scope](/guide/state-management#global-state).<br/>
In case item with the specified ID is not registered in the global state map,
the corresponding record will automatically be added.",
        @"
; Lock an unlockable CG record with ID 'FightScene1'.
@lock CG/FightScene1"
    )]
    [Serializable, UIGroup, Icon("Lock")]
    public class Lock : Command
    {
        [Doc("ID of the unlockable item. Use `*` to lock all the registered unlockable items.")]
        [Alias(NamelessParameterAlias), RequiredParameter, ResourceContext(UnlockablesConfiguration.DefaultPathPrefix)]
        public StringParameter Id;

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            var locks = Engine.GetServiceOrErr<IUnlockableManager>();
            if (Id.Value.EqualsIgnoreCase("*")) locks.LockAllItems();
            else locks.LockItem(Id);
            await Engine.GetServiceOrErr<IStateManager>().SaveGlobal();
        }
    }
}
