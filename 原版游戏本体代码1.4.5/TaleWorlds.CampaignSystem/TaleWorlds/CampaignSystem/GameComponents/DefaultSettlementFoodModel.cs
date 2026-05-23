using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultSettlementFoodModel : SettlementFoodModel
{
	private readonly TextObject ProsperityText = GameTexts.FindText("str_prosperity");

	private readonly TextObject GarrisonText = GameTexts.FindText("str_garrison");

	private readonly TextObject LandsAroundSettlementText = GameTexts.FindText("str_lands_around_settlement");

	private readonly TextObject NormalVillagesText = GameTexts.FindText("str_normal_villages");

	private readonly TextObject RaidedVillagesText = GameTexts.FindText("str_raided_villages");

	private readonly TextObject VillagesUnderSiegeText = GameTexts.FindText("str_villages_under_siege");

	private readonly TextObject FoodBoughtByCiviliansText = GameTexts.FindText("str_food_bought_by_civilians");

	private const int FoodProductionPerVillage = 10;

	public override int FoodStocksUpperLimit => 300;

	public override int NumberOfProsperityToEatOneFood => 40;

	public override int NumberOfMenOnGarrisonToEatOneFood => 20;

	public override int CastleFoodStockUpperLimitBonus => 150;

	public override ExplainedNumber CalculateTownFoodStocksChange(Town town, bool includeMarketStocks = true, bool includeDescriptions = false)
	{
		return CalculateTownFoodChangeInternal(town, includeMarketStocks, includeDescriptions);
	}

	private ExplainedNumber CalculateTownFoodChangeInternal(Town town, bool includeMarketStocks, bool includeDescriptions)
	{
		ExplainedNumber bonuses = new ExplainedNumber(0f, includeDescriptions);
		ExplainedNumber bonuses2 = new ExplainedNumber(0f, includeDescriptions);
		ExplainedNumber bonuses3 = new ExplainedNumber(town.Prosperity / (float)NumberOfProsperityToEatOneFood);
		ExplainedNumber bonuses4 = new ExplainedNumber((((float?)town.GarrisonParty?.Party.NumberOfAllMembers) ?? 0f) / (float)NumberOfMenOnGarrisonToEatOneFood);
		if (town.IsUnderSiege)
		{
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.Gourmet, town, ref bonuses4);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Medicine.TriageTent, town, ref bonuses2);
		}
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.MasterOfWarcraft, town, ref bonuses3);
		bonuses2.Add(bonuses3.ResultNumber, ProsperityText);
		bonuses2.Add(bonuses4.ResultNumber, GarrisonText);
		town.AddEffectOfBuildings(BuildingEffectEnum.FoodConsumption, ref bonuses2);
		Kingdom kingdom = town.Settlement.OwnerClan?.Kingdom;
		if (kingdom != null && kingdom.HasPolicy(DefaultPolicies.HuntingRights))
		{
			bonuses.Add(2f, DefaultPolicies.HuntingRights.Name);
		}
		if (!town.IsUnderSiege)
		{
			int num = (town.IsTown ? 15 : 10);
			bonuses.Add(num, LandsAroundSettlementText);
			foreach (Village boundVillage in town.Owner.Settlement.BoundVillages)
			{
				float value = 0f;
				if (boundVillage.VillageState == Village.VillageStates.Normal)
				{
					value = (boundVillage.GetHearthLevel() + 1) * 6;
				}
				bonuses.Add(value, boundVillage.Name);
			}
			town.AddEffectOfBuildings(BuildingEffectEnum.FoodProduction, ref bonuses);
		}
		else
		{
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Roguery.DirtyFighting, town, ref bonuses);
		}
		if (includeMarketStocks)
		{
			foreach (Town.SellLog soldItem in town.SoldItems)
			{
				if (soldItem.Category.Properties == ItemCategory.Property.BonusToFoodStores)
				{
					bonuses.Add(soldItem.Number, includeDescriptions ? soldItem.Category.GetName() : null);
				}
			}
		}
		ExplainedNumber explainedNumber = new ExplainedNumber(0f, includeDescriptions);
		explainedNumber.AddFromExplainedNumber(bonuses, null);
		explainedNumber.SubtractFromExplainedNumber(bonuses2, null);
		GetSettlementFoodChangeDueToIssues(town, ref explainedNumber);
		return explainedNumber;
	}

	private static void GetSettlementFoodChangeDueToIssues(Town town, ref ExplainedNumber explainedNumber)
	{
		Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(DefaultIssueEffects.SettlementFood, town.Settlement, ref explainedNumber);
	}
}
