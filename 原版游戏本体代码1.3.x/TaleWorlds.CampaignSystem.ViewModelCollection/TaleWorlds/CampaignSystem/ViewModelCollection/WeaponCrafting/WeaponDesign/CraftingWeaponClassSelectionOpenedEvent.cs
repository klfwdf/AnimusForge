using System;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x02000106 RID: 262
	public class CraftingWeaponClassSelectionOpenedEvent : EventBase
	{
		// Token: 0x170007D5 RID: 2005
		// (get) Token: 0x0600177C RID: 6012 RVA: 0x00059D3E File Offset: 0x00057F3E
		// (set) Token: 0x0600177D RID: 6013 RVA: 0x00059D46 File Offset: 0x00057F46
		public bool IsOpen { get; private set; }

		// Token: 0x0600177E RID: 6014 RVA: 0x00059D4F File Offset: 0x00057F4F
		public CraftingWeaponClassSelectionOpenedEvent(bool isOpen)
		{
			this.IsOpen = isOpen;
		}
	}
}
