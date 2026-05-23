using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultVillageProductionCalculatorModel : VillageProductionCalculatorModel
{
	private readonly TextObject _cultureEffect = GameTexts.FindText("str_culture");

	public override ExplainedNumber CalculateDailyProductionAmount(Village village, ItemObject item)
	{
		ExplainedNumber bonuses = new ExplainedNumber(0f, includeDescriptions: false, null);
		if (village.VillageState == Village.VillageStates.Normal)
		{
			foreach (var production in village.VillageType.Productions)
			{
				var (itemObject, num) = production;
				if (itemObject != item)
				{
					continue;
				}
				if (village.TradeBound != null)
				{
					float num2 = (float)(village.GetHearthLevel() + 1) * 0.5f;
					if (item.IsMountable && item.Tier == ItemObject.ItemTiers.Tier2 && PerkHelper.GetPerkValueForTown(DefaultPerks.Riding.Shepherd, village.TradeBound.Town) && MBRandom.RandomFloat < DefaultPerks.Riding.Shepherd.SecondaryBonus)
					{
						num += 1f;
					}
					bonuses.Add(num * num2);
					if (item.ItemCategory == DefaultItemCategories.Grain || item.ItemCategory == DefaultItemCategories.Olives || item.ItemCategory == DefaultItemCategories.Fish || item.ItemCategory == DefaultItemCategories.DateFruit)
					{
						PerkHelper.AddPerkBonusForTown(DefaultPerks.Trade.GranaryAccountant, village.TradeBound.Town, ref bonuses);
					}
					else if (item.ItemCategory == DefaultItemCategories.Clay || item.ItemCategory == DefaultItemCategories.Iron || item.ItemCategory == DefaultItemCategories.Cotton || item.ItemCategory == DefaultItemCategories.Silver)
					{
						PerkHelper.AddPerkBonusForTown(DefaultPerks.Trade.TradeyardForeman, village.TradeBound.Town, ref bonuses);
					}
					if (item.IsTradeGood)
					{
						PerkHelper.AddPerkBonusForTown(DefaultPerks.Athletics.Steady, village.TradeBound.Town, ref bonuses);
					}
					if (item.IsAnimal)
					{
						PerkHelper.AddPerkBonusForTown(DefaultPerks.Medicine.PerfectHealth, village.TradeBound.Town, ref bonuses);
					}
					PerkHelper.AddPerkBonusForTown(DefaultPerks.Riding.Breeder, village.TradeBound.Town, ref bonuses);
				}
				if ((item.ItemCategory == DefaultItemCategories.Sheep || item.ItemCategory == DefaultItemCategories.Cow || item.ItemCategory == DefaultItemCategories.WarHorse || item.ItemCategory == DefaultItemCategories.Horse || item.ItemCategory == DefaultItemCategories.PackAnimal) && village.Settlement.OwnerClan.Culture.HasFeat(DefaultCulturalFeats.KhuzaitAnimalProductionFeat))
				{
					bonuses.AddFactor(DefaultCulturalFeats.KhuzaitAnimalProductionFeat.EffectBonus, _cultureEffect);
				}
				if (item.ItemCategory == DefaultItemCategories.Grain && village.Settlement.OwnerClan.Culture.HasFeat(DefaultCulturalFeats.SturgianGrainProductionFeat))
				{
					bonuses.AddFactor(DefaultCulturalFeats.SturgianGrainProductionFeat.EffectBonus, _cultureEffect);
				}
				if (village.Bound.IsFortification)
				{
					village.Bound.Town.AddEffectOfBuildings(BuildingEffectEnum.VillageProduction, ref bonuses);
				}
				if (village.Bound.IsCastle && village.Settlement.OwnerClan.Culture.HasFeat(DefaultCulturalFeats.VlandianCastleVillageProductionFeat))
				{
					bonuses.AddFactor(DefaultCulturalFeats.VlandianCastleVillageProductionFeat.EffectBonus, _cultureEffect);
				}
			}
		}
		return bonuses;
	}

	public override float CalculateDailyFoodProductionAmount(Village village)
	{
		if (village.VillageState != Village.VillageStates.Normal)
		{
			return 0f;
		}
		float num = village.GetHearthLevel() + 1;
		if (GetIssueEffectOnFoodProduction(village.Settlement, out var issueEffect))
		{
			num *= issueEffect;
		}
		return num;
	}

	private bool GetIssueEffectOnFoodProduction(Settlement settlement, out float issueEffect)
	{
		issueEffect = 1f;
		if (settlement.IsVillage)
		{
			foreach (Hero item in SettlementHelper.GetAllHeroesOfSettlement(settlement, includePrisoners: false))
			{
				if (item.Issue != null && item.MapFaction == settlement.MapFaction)
				{
					float activeIssueEffectAmount = item.Issue.GetActiveIssueEffectAmount(DefaultIssueEffects.HalfVillageProduction);
					if (activeIssueEffectAmount != 0f)
					{
						issueEffect *= activeIssueEffectAmount;
					}
				}
			}
		}
		return !issueEffect.ApproximatelyEqualsTo(1f);
	}

	public override float CalculateProductionSpeedOfItemCategory(ItemCategory item)
	{
		float num = 0f;
		foreach (VillageType item2 in VillageType.All)
		{
			float productionPerDay = item2.GetProductionPerDay(item);
			if (productionPerDay > num)
			{
				num = productionPerDay;
			}
		}
		return num;
	}
}
