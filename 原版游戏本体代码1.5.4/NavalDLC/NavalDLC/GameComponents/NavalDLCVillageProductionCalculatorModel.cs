using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace NavalDLC.GameComponents;

public class NavalDLCVillageProductionCalculatorModel : VillageProductionCalculatorModel
{
	public override float CalculateProductionSpeedOfItemCategory(ItemCategory item)
	{
		return base.BaseModel.CalculateProductionSpeedOfItemCategory(item);
	}

	public override ExplainedNumber CalculateDailyProductionAmount(Village village, ItemObject item)
	{
		ExplainedNumber bonuses = base.BaseModel.CalculateDailyProductionAmount(village, item);
		if (village.TradeBound != null)
		{
			if (item.ItemCategory == NavalItemCategories.WalrusTusk || item.ItemCategory == NavalItemCategories.WhaleOil)
			{
				PerkHelper.AddPerkBonusForTown(NavalPerks.Boatswain.PortAuthority, village.TradeBound.Town, isPrimaryBonus: false, ref bonuses);
			}
			if (item.ItemCategory == DefaultItemCategories.Fish)
			{
				PerkHelper.AddPerkBonusForTown(NavalPerks.Boatswain.BlessingsOfTheSea, village.TradeBound.Town, isPrimaryBonus: false, ref bonuses);
			}
		}
		Kingdom kingdom = village.Bound.OwnerClan?.Kingdom;
		if (kingdom != null)
		{
			if (kingdom.HasPolicy(NavalPolicies.MaritimeWealEdict))
			{
				bonuses.AddFactor(0.25f, NavalPolicies.MaritimeWealEdict.Name);
			}
			if (kingdom.HasPolicy(NavalPolicies.BolsterTheFyrd))
			{
				bonuses.AddFactor(-0.05f, NavalPolicies.BolsterTheFyrd.Name);
			}
		}
		return bonuses;
	}

	public override float CalculateDailyFoodProductionAmount(Village village)
	{
		return base.BaseModel.CalculateDailyFoodProductionAmount(village);
	}
}
