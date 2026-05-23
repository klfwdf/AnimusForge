using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001A4 RID: 420
	public abstract class PartyTransitionModel : MBGameModel<PartyTransitionModel>
	{
		// Token: 0x06001C83 RID: 7299
		public abstract CampaignTime GetTransitionTimeForEmbarking(MobileParty mobileParty);

		// Token: 0x06001C84 RID: 7300
		public abstract CampaignTime GetTransitionTimeDisembarking(MobileParty mobileParty);

		// Token: 0x06001C85 RID: 7301
		public abstract CampaignTime GetFleetTravelTimeToPoint(MobileParty owner, CampaignVec2 target);
	}
}
