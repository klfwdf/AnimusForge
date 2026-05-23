using System;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection
{
	// Token: 0x0200014A RID: 330
	public class PerkSelectionToggleEvent : EventBase
	{
		// Token: 0x17000AB0 RID: 2736
		// (get) Token: 0x06001F5F RID: 8031 RVA: 0x00072E74 File Offset: 0x00071074
		// (set) Token: 0x06001F60 RID: 8032 RVA: 0x00072E7C File Offset: 0x0007107C
		public bool IsCurrentlyActive { get; private set; }

		// Token: 0x06001F61 RID: 8033 RVA: 0x00072E85 File Offset: 0x00071085
		public PerkSelectionToggleEvent(bool isCurrentlyActive)
		{
			this.IsCurrentlyActive = isCurrentlyActive;
		}
	}
}
