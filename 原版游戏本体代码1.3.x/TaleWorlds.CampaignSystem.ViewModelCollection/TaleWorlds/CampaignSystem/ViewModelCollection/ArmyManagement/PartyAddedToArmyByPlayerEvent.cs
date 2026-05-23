using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement
{
	// Token: 0x0200015F RID: 351
	public class PartyAddedToArmyByPlayerEvent : EventBase
	{
		// Token: 0x17000B8B RID: 2955
		// (get) Token: 0x060021C6 RID: 8646 RVA: 0x0007A1D4 File Offset: 0x000783D4
		// (set) Token: 0x060021C7 RID: 8647 RVA: 0x0007A1DC File Offset: 0x000783DC
		public MobileParty AddedParty { get; private set; }

		// Token: 0x060021C8 RID: 8648 RVA: 0x0007A1E5 File Offset: 0x000783E5
		public PartyAddedToArmyByPlayerEvent(MobileParty addedParty)
		{
			this.AddedParty = addedParty;
		}
	}
}
