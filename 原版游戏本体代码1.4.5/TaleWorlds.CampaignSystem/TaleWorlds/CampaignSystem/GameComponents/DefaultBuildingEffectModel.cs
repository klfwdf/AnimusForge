using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultBuildingEffectModel : BuildingEffectModel
{
	public override ExplainedNumber GetBuildingEffect(Building building, BuildingEffectEnum effect)
	{
		float baseBuildingEffectAmount = building.BuildingType.GetBaseBuildingEffectAmount(effect, building.CurrentLevel);
		ExplainedNumber bonuses = new ExplainedNumber(baseBuildingEffectAmount);
		if (effect == BuildingEffectEnum.DenarByBoundVillageHeartPerDay)
		{
			float num = 0f;
			foreach (Village village in building.Town.Villages)
			{
				num += village.Hearth;
			}
			bonuses = new ExplainedNumber(num * baseBuildingEffectAmount);
		}
		if (effect == BuildingEffectEnum.FoodStock && (building.BuildingType == DefaultBuildingTypes.CastleGranary || building.BuildingType == DefaultBuildingTypes.SettlementWarehouse))
		{
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Engineering.Battlements, building.Town, ref bonuses);
		}
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.Contractors, building.Town, ref bonuses);
		if (building.BuildingType.IsDailyProject)
		{
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.MasterOfPlanning, building.Town, ref bonuses);
		}
		if (building.BuildingType == DefaultBuildingTypes.SettlementMarketplace || building.BuildingType == DefaultBuildingTypes.SettlementDailyFestivalAndGames)
		{
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Charm.PublicSpeaker, building.Town, ref bonuses);
		}
		return bonuses;
	}
}
