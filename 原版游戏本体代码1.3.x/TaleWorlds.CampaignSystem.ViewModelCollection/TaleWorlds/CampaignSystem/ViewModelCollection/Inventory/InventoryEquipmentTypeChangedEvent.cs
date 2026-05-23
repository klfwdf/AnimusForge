using System;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory
{
	// Token: 0x02000095 RID: 149
	public class InventoryEquipmentTypeChangedEvent : EventBase
	{
		// Token: 0x170004B3 RID: 1203
		// (get) Token: 0x06000E85 RID: 3717 RVA: 0x0003CD21 File Offset: 0x0003AF21
		// (set) Token: 0x06000E86 RID: 3718 RVA: 0x0003CD29 File Offset: 0x0003AF29
		public bool IsCurrentlyWarSet { get; private set; }

		// Token: 0x06000E87 RID: 3719 RVA: 0x0003CD32 File Offset: 0x0003AF32
		public InventoryEquipmentTypeChangedEvent(bool isCurrentlyWarSet)
		{
			this.IsCurrentlyWarSet = isCurrentlyWarSet;
		}
	}
}
