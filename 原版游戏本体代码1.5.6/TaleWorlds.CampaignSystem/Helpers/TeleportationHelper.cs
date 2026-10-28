using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace Helpers;

public static class TeleportationHelper
{
	public static float GetHoursLeftForTeleportingHeroToReachItsDestination(Hero teleportingHero)
	{
		return Campaign.Current.GetCampaignBehavior<ITeleportationCampaignBehavior>()?.GetHeroArrivalTimeToDestination(teleportingHero).RemainingHoursFromNow ?? 0f;
	}
}
