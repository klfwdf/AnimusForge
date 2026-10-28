using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultVillageTradeModel : VillageTradeModel
{
	public override float TradeBoundDistanceLimitAsDays(MobileParty.NavigationType navigationType)
	{
		return Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(navigationType) * 3f / (Campaign.Current.EstimatedAverageVillagerPartySpeed * (float)CampaignTime.HoursInDay);
	}

	public override Settlement GetTradeBoundToAssignForVillage(Village village)
	{
		MobileParty.NavigationType navigationType = MobileParty.NavigationType.Default;
		Settlement settlement = SettlementHelper.FindNearestSettlementToSettlement(village.Settlement, navigationType, (Settlement x) => x.IsTown && x.Town.MapFaction == village.Settlement.MapFaction);
		float distanceLimit = Campaign.Current.Models.VillageTradeModel.TradeBoundDistanceLimitAsDays(navigationType) * Campaign.Current.EstimatedAverageVillagerPartySpeed * (float)CampaignTime.HoursInDay;
		if (settlement != null && Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, village.Settlement, isFromPort: false, isTargetingPort: false, navigationType) < distanceLimit)
		{
			return settlement;
		}
		Settlement settlement2 = SettlementHelper.FindNearestSettlementToSettlement(village.Settlement, navigationType, (Settlement x) => x.IsTown && x.Town.MapFaction != village.Settlement.MapFaction && !x.Town.MapFaction.IsAtWarWith(village.Settlement.MapFaction) && Campaign.Current.Models.MapDistanceModel.GetDistance(x, village.Settlement, isFromPort: false, isTargetingPort: false, navigationType) <= distanceLimit);
		if (settlement2 != null && Campaign.Current.Models.MapDistanceModel.GetDistance(settlement2, village.Settlement, isFromPort: false, isTargetingPort: false, navigationType) < distanceLimit)
		{
			return settlement2;
		}
		return null;
	}
}
