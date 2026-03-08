using System;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory
{
	// Token: 0x02000096 RID: 150
	public class InventoryFilterChangedEvent : EventBase
	{
		// Token: 0x170004B4 RID: 1204
		// (get) Token: 0x06000E88 RID: 3720 RVA: 0x0003CD41 File Offset: 0x0003AF41
		// (set) Token: 0x06000E89 RID: 3721 RVA: 0x0003CD49 File Offset: 0x0003AF49
		public SPInventoryVM.Filters NewFilter { get; private set; }

		// Token: 0x06000E8A RID: 3722 RVA: 0x0003CD52 File Offset: 0x0003AF52
		public InventoryFilterChangedEvent(SPInventoryVM.Filters newFilter)
		{
			this.NewFilter = newFilter;
		}
	}
}
