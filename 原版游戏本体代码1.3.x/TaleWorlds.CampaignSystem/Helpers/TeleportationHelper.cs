using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace Helpers
{
	// Token: 0x02000013 RID: 19
	public static class TeleportationHelper
	{
		// Token: 0x060000A5 RID: 165 RVA: 0x00008E34 File Offset: 0x00007034
		public static float GetHoursLeftForTeleportingHeroToReachItsDestination(Hero teleportingHero)
		{
			ITeleportationCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<ITeleportationCampaignBehavior>();
			if (campaignBehavior != null)
			{
				return campaignBehavior.GetHeroArrivalTimeToDestination(teleportingHero).RemainingHoursFromNow;
			}
			return 0f;
		}
	}
}
