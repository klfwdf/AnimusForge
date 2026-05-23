using System;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x0200010B RID: 267
	public class CraftingWeaponResultPopupToggledEvent : EventBase
	{
		// Token: 0x170007F3 RID: 2035
		// (get) Token: 0x060017C8 RID: 6088 RVA: 0x0005A74D File Offset: 0x0005894D
		public bool IsOpen { get; }

		// Token: 0x060017C9 RID: 6089 RVA: 0x0005A755 File Offset: 0x00058955
		public CraftingWeaponResultPopupToggledEvent(bool isOpen)
		{
			this.IsOpen = isOpen;
		}
	}
}
