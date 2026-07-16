using System;
using System.Linq;
using UnityEngine;

namespace Naninovel.UI
{
    public class GameStateSlotsGrid : ScriptableGrid<GameStateSlot>
    {
        private Action<int> onSlotClicked, onDeleteClicked;
        private Func<int, Awaitable<GameStateMap>> loadStateAt;

        public async Awaitable Initialize (int itemsCount, Action<int> onSlotClicked,
            Action<int> onDeleteClicked, Func<int, Awaitable<GameStateMap>> loadStateAt)
        {
            this.onSlotClicked = onSlotClicked;
            this.onDeleteClicked = onDeleteClicked;
            this.loadStateAt = loadStateAt;
            await Initialize(itemsCount);
        }

        public virtual void BindSlot (int slotNumber, GameStateMap state)
        {
            Slots.FirstOrDefault(s => s.SlotNumber == slotNumber)?.Bind(slotNumber, state);
        }

        protected new Awaitable Initialize (int itemsCount) => base.Initialize(itemsCount);

        protected override void InitializeSlot (GameStateSlot slot)
        {
            slot.Initialize(onSlotClicked, onDeleteClicked);
        }

        protected override async void BindSlot (GameStateSlot slot, int itemIndex)
        {
            var slotNumber = itemIndex + 1;
            var state = await loadStateAt(slotNumber);
            if (!slot) return; // Don't use Engine.Initialized here, as it may be false.
            slot.Bind(slotNumber, state);
        }
    }
}
