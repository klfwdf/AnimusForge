using System;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x02000107 RID: 263
	public class CraftingOrderTabOpenedEvent : EventBase
	{
		// Token: 0x170007D6 RID: 2006
		// (get) Token: 0x0600177F RID: 6015 RVA: 0x00059D5E File Offset: 0x00057F5E
		// (set) Token: 0x06001780 RID: 6016 RVA: 0x00059D66 File Offset: 0x00057F66
		public bool IsOpen { get; private set; }

		// Token: 0x06001781 RID: 6017 RVA: 0x00059D6F File Offset: 0x00057F6F
		public CraftingOrderTabOpenedEvent(bool isOpen)
		{
			this.IsOpen = isOpen;
		}
	}
}
