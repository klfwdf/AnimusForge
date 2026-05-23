using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection
{
	// Token: 0x02000149 RID: 329
	public class PerkSelectedByPlayerEvent : EventBase
	{
		// Token: 0x17000AAF RID: 2735
		// (get) Token: 0x06001F5C RID: 8028 RVA: 0x00072E54 File Offset: 0x00071054
		// (set) Token: 0x06001F5D RID: 8029 RVA: 0x00072E5C File Offset: 0x0007105C
		public PerkObject SelectedPerk { get; private set; }

		// Token: 0x06001F5E RID: 8030 RVA: 0x00072E65 File Offset: 0x00071065
		public PerkSelectedByPlayerEvent(PerkObject selectedPerk)
		{
			this.SelectedPerk = selectedPerk;
		}
	}
}
