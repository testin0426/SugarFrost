using System.Collections.Generic;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel
{
    public class CGGalleryGrid : ScriptableGrid<CGGalleryGridSlot>
    {
        protected virtual List<CGSlotData> SlotData { get; private set; }

        private CGViewerPanel viewerPanel;

        public async Awaitable Initialize (CGViewerPanel viewerPanel, List<CGSlotData> slotData)
        {
            this.viewerPanel = viewerPanel;
            SlotData = slotData;
            await Initialize(slotData.Count);
        }

        protected new Awaitable Initialize (int itemsCount) => base.Initialize(itemsCount);

        protected override void InitializeSlot (CGGalleryGridSlot slot)
        {
            slot.Initialize(viewerPanel.Show);
        }

        protected override void BindSlot (CGGalleryGridSlot slot, int itemIndex)
        {
            var slotData = SlotData[itemIndex];
            slot.Bind(slotData);
        }
    }
}
