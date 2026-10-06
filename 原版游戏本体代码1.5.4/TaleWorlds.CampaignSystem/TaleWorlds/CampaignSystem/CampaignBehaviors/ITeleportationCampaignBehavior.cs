using TaleWorlds.CampaignSystem.Map;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface ITeleportationCampaignBehavior : ICampaignBehavior
{
	bool GetTargetOfTeleportingHero(Hero teleportingHero, out bool isGovernor, out bool isPartyLeader, out IMapPoint target);

	CampaignTime GetHeroArrivalTimeToDestination(Hero teleportingHero);
}
