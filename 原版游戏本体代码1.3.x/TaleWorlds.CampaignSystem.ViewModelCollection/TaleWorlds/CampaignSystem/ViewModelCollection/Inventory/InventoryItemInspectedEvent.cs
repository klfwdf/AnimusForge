using System;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory
{
	// Token: 0x02000097 RID: 151
	public class InventoryItemInspectedEvent : EventBase
	{
		// Token: 0x170004B5 RID: 1205
		// (get) Token: 0x06000E8B RID: 3723 RVA: 0x0003CD61 File Offset: 0x0003AF61
		// (set) Token: 0x06000E8C RID: 3724 RVA: 0x0003CD69 File Offset: 0x0003AF69
		public ItemRosterElement Item { get; private set; }

		// Token: 0x170004B6 RID: 1206
		// (get) Token: 0x06000E8D RID: 3725 RVA: 0x0003CD72 File Offset: 0x0003AF72
		// (set) Token: 0x06000E8E RID: 3726 RVA: 0x0003CD7A File Offset: 0x0003AF7A
		public InventoryLogic.InventorySide ItemSide { get; private set; }

		// Token: 0x06000E8F RID: 3727 RVA: 0x0003CD83 File Offset: 0x0003AF83
		public InventoryItemInspectedEvent(ItemRosterElement item, InventoryLogic.InventorySide itemSide)
		{
			this.ItemSide = itemSide;
			this.Item = item;
		}
	}
}
