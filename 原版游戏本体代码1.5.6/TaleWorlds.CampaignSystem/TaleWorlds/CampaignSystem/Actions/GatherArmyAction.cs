using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.Actions;

public static class GatherArmyAction
{
	private static void ApplyInternal(MobileParty leaderParty, IMapPoint gatheringPoint, float playerInvolvement = 0f)
	{
		Army army = leaderParty.Army;
		CampaignEventDispatcher.Instance.OnArmyGathered(army, gatheringPoint);
	}

	public static void Apply(MobileParty leaderParty, IMapPoint gatheringPoint)
	{
		ApplyInternal(leaderParty, gatheringPoint, (leaderParty == MobileParty.MainParty) ? 1f : 0f);
	}
}
