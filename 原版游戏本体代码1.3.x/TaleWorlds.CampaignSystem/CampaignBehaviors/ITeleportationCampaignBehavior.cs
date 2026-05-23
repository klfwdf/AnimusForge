using System;
using TaleWorlds.CampaignSystem.Map;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200040B RID: 1035
	public interface ITeleportationCampaignBehavior : ICampaignBehavior
	{
		// Token: 0x060040AF RID: 16559
		bool GetTargetOfTeleportingHero(Hero teleportingHero, out bool isGovernor, out bool isPartyLeader, out IMapPoint target);

		// Token: 0x060040B0 RID: 16560
		CampaignTime GetHeroArrivalTimeToDestination(Hero teleportingHero);
	}
}
