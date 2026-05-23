using System;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000163 RID: 355
	public class DefaultVillageTradeModel : VillageTradeModel
	{
		// Token: 0x06001AD0 RID: 6864 RVA: 0x00089D2D File Offset: 0x00087F2D
		public override float TradeBoundDistanceLimitAsDays(MobileParty.NavigationType navigationType)
		{
			return Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(navigationType) * 3f / (Campaign.Current.EstimatedAverageVillagerPartySpeed * (float)CampaignTime.HoursInDay);
		}

		// Token: 0x06001AD1 RID: 6865 RVA: 0x00089D54 File Offset: 0x00087F54
		public override Settlement GetTradeBoundToAssignForVillage(Village village)
		{
			MobileParty.NavigationType navigationType = MobileParty.NavigationType.Default;
			Settlement settlement = SettlementHelper.FindNearestSettlementToSettlement(village.Settlement, navigationType, (Settlement x) => x.IsTown && x.Town.MapFaction == village.Settlement.MapFaction);
			float distanceLimit = Campaign.Current.Models.VillageTradeModel.TradeBoundDistanceLimitAsDays(navigationType) * Campaign.Current.EstimatedAverageVillagerPartySpeed * (float)CampaignTime.HoursInDay;
			if (settlement != null && Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, village.Settlement, false, false, navigationType) < distanceLimit)
			{
				return settlement;
			}
			Settlement settlement2 = SettlementHelper.FindNearestSettlementToSettlement(village.Settlement, navigationType, (Settlement x) => x.IsTown && x.Town.MapFaction != village.Settlement.MapFaction && !x.Town.MapFaction.IsAtWarWith(village.Settlement.MapFaction) && Campaign.Current.Models.MapDistanceModel.GetDistance(x, village.Settlement, false, false, navigationType) <= distanceLimit);
			if (settlement2 != null && Campaign.Current.Models.MapDistanceModel.GetDistance(settlement2, village.Settlement, false, false, navigationType) < distanceLimit)
			{
				return settlement2;
			}
			return null;
		}
	}
}
