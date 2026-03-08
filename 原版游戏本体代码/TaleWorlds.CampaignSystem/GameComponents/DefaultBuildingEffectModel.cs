using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000F9 RID: 249
	public class DefaultBuildingEffectModel : BuildingEffectModel
	{
		// Token: 0x0600167B RID: 5755 RVA: 0x0006717C File Offset: 0x0006537C
		public override ExplainedNumber GetBuildingEffect(Building building, BuildingEffectEnum effect)
		{
			float baseBuildingEffectAmount = building.BuildingType.GetBaseBuildingEffectAmount(effect, building.CurrentLevel);
			ExplainedNumber result = new ExplainedNumber(baseBuildingEffectAmount, false, null);
			if (effect == BuildingEffectEnum.DenarByBoundVillageHeartPerDay)
			{
				float num = 0f;
				foreach (Village village in building.Town.Villages)
				{
					num += village.Hearth;
				}
				result = new ExplainedNumber(num * baseBuildingEffectAmount, false, null);
			}
			if (effect == BuildingEffectEnum.FoodStock && (building.BuildingType == DefaultBuildingTypes.CastleGranary || building.BuildingType == DefaultBuildingTypes.SettlementWarehouse))
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Engineering.Battlements, building.Town, ref result);
			}
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.Contractors, building.Town, ref result);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.MasterOfPlanning, building.Town, ref result);
			if (building.BuildingType == DefaultBuildingTypes.SettlementMarketplace || building.BuildingType == DefaultBuildingTypes.SettlementDailyFestivalAndGames)
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Charm.PublicSpeaker, building.Town, ref result);
			}
			return result;
		}
	}
}
