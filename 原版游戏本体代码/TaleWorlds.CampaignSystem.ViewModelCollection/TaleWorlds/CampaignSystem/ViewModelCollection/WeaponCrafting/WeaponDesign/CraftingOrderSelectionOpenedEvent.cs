using System;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x02000108 RID: 264
	public class CraftingOrderSelectionOpenedEvent : EventBase
	{
		// Token: 0x170007D7 RID: 2007
		// (get) Token: 0x06001782 RID: 6018 RVA: 0x00059D7E File Offset: 0x00057F7E
		// (set) Token: 0x06001783 RID: 6019 RVA: 0x00059D86 File Offset: 0x00057F86
		public bool IsOpen { get; private set; }

		// Token: 0x06001784 RID: 6020 RVA: 0x00059D8F File Offset: 0x00057F8F
		public CraftingOrderSelectionOpenedEvent(bool isOpen)
		{
			this.IsOpen = isOpen;
		}
	}
}
