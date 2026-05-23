using System;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003FB RID: 1019
	public interface IDisbandPartyCampaignBehavior : ICampaignBehavior
	{
		// Token: 0x06003FAA RID: 16298
		bool IsPartyWaitingForDisband(MobileParty party);
	}
}
