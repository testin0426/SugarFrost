using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Sets an [unlockable item](/guide/unlockable-items) with the specified ID to `unlocked` state.",
        @"
The unlocked state of the items is stored in [global scope](/guide/state-management#global-state).<br/>
In case item with the specified ID is not registered in the global state map,
the corresponding record will automatically be added.",
        @"
; Unlocks an unlockable CG record with ID 'FightScene1'.
@unlock CG/FightScene1"
    )]
    [Serializable, UIGroup, Icon("LockOpen")]
    public class Unlock : Command
    {
        [Doc("ID of the unlockable item. Use `*` to unlock all the registered unlockable items.")]
        [Alias(NamelessParameterAlias), RequiredParameter, ResourceContext(UnlockablesConfiguration.DefaultPathPrefix)]
        public StringParameter Id;

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            var locks = Engine.GetServiceOrErr<IUnlockableManager>();
            if (Id.Value.EqualsIgnoreCase("*")) locks.UnlockAllItems();
            else locks.UnlockItem(Id);
            await Engine.GetServiceOrErr<IStateManager>().SaveGlobal();
        }
    }
}
