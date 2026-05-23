using System;
using TaleWorlds.Core;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.Inventory
{
	// Token: 0x020000DA RID: 218
	public class InventoryTransferItemEvent : EventBase
	{
		// Token: 0x170005CA RID: 1482
		// (get) Token: 0x060014F3 RID: 5363 RVA: 0x00060311 File Offset: 0x0005E511
		// (set) Token: 0x060014F4 RID: 5364 RVA: 0x00060319 File Offset: 0x0005E519
		public ItemObject Item { get; private set; }

		// Token: 0x170005CB RID: 1483
		// (get) Token: 0x060014F5 RID: 5365 RVA: 0x00060322 File Offset: 0x0005E522
		// (set) Token: 0x060014F6 RID: 5366 RVA: 0x0006032A File Offset: 0x0005E52A
		public bool IsBuyForPlayer { get; private set; }

		// Token: 0x060014F7 RID: 5367 RVA: 0x00060333 File Offset: 0x0005E533
		public InventoryTransferItemEvent(ItemObject item, bool isBuyForPlayer)
		{
			this.Item = item;
			this.IsBuyForPlayer = isBuyForPlayer;
		}
	}
}
