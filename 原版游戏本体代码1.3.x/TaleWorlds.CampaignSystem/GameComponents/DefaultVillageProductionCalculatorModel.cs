using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000162 RID: 354
	public class DefaultVillageProductionCalculatorModel : VillageProductionCalculatorModel
	{
		// Token: 0x06001ACB RID: 6859 RVA: 0x000898DC File Offset: 0x00087ADC
		public override ExplainedNumber CalculateDailyProductionAmount(Village village, ItemObject item)
		{
			ExplainedNumber result = new ExplainedNumber(0f, false, null);
			if (village.VillageState == Village.VillageStates.Normal)
			{
				foreach (ValueTuple<ItemObject, float> valueTuple in village.VillageType.Productions)
				{
					ItemObject item2 = valueTuple.Item1;
					float num = valueTuple.Item2;
					if (item2 == item)
					{
						if (village.TradeBound != null)
						{
							float num2 = (float)(village.GetHearthLevel() + 1) * 0.5f;
							if (item.IsMountable && item.Tier == ItemObject.ItemTiers.Tier2 && PerkHelper.GetPerkValueForTown(DefaultPerks.Riding.Shepherd, village.TradeBound.Town) && MBRandom.RandomFloat < DefaultPerks.Riding.Shepherd.SecondaryBonus)
							{
								num += 1f;
							}
							result.Add(num * num2, null, null);
							if (item.ItemCategory == DefaultItemCategories.Grain || item.ItemCategory == DefaultItemCategories.Olives || item.ItemCategory == DefaultItemCategories.Fish || item.ItemCategory == DefaultItemCategories.DateFruit)
							{
								PerkHelper.AddPerkBonusForTown(DefaultPerks.Trade.GranaryAccountant, village.TradeBound.Town, ref result);
							}
							else if (item.ItemCategory == DefaultItemCategories.Clay || item.ItemCategory == DefaultItemCategories.Iron || item.ItemCategory == DefaultItemCategories.Cotton || item.ItemCategory == DefaultItemCategories.Silver)
							{
								PerkHelper.AddPerkBonusForTown(DefaultPerks.Trade.TradeyardForeman, village.TradeBound.Town, ref result);
							}
							if (item.IsTradeGood)
							{
								PerkHelper.AddPerkBonusForTown(DefaultPerks.Athletics.Steady, village.TradeBound.Town, ref result);
							}
							if (item.IsAnimal)
							{
								PerkHelper.AddPerkBonusForTown(DefaultPerks.Medicine.PerfectHealth, village.TradeBound.Town, ref result);
							}
							PerkHelper.AddPerkBonusForTown(DefaultPerks.Riding.Breeder, village.TradeBound.Town, ref result);
						}
						if ((item.ItemCategory == DefaultItemCategories.Sheep || item.ItemCategory == DefaultItemCategories.Cow || item.ItemCategory == DefaultItemCategories.WarHorse || item.ItemCategory == DefaultItemCategories.Horse || item.ItemCategory == DefaultItemCategories.PackAnimal) && village.Settlement.OwnerClan.Culture.HasFeat(DefaultCulturalFeats.KhuzaitAnimalProductionFeat))
						{
							result.AddFactor(DefaultCulturalFeats.KhuzaitAnimalProductionFeat.EffectBonus, this._cultureEffect);
						}
						if (item.ItemCategory == DefaultItemCategories.Grain && village.Settlement.OwnerClan.Culture.HasFeat(DefaultCulturalFeats.SturgianGrainProductionFeat))
						{
							result.AddFactor(DefaultCulturalFeats.SturgianGrainProductionFeat.EffectBonus, this._cultureEffect);
						}
						if (village.Bound.IsFortification)
						{
							village.Bound.Town.AddEffectOfBuildings(BuildingEffectEnum.VillageProduction, ref result);
						}
						if (village.Bound.IsCastle && village.Settlement.OwnerClan.Culture.HasFeat(DefaultCulturalFeats.VlandianCastleVillageProductionFeat))
						{
							result.AddFactor(DefaultCulturalFeats.VlandianCastleVillageProductionFeat.EffectBonus, this._cultureEffect);
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06001ACC RID: 6860 RVA: 0x00089BD8 File Offset: 0x00087DD8
		public override float CalculateDailyFoodProductionAmount(Village village)
		{
			if (village.VillageState != Village.VillageStates.Normal)
			{
				return 0f;
			}
			float num = (float)(village.GetHearthLevel() + 1);
			float num2;
			if (this.GetIssueEffectOnFoodProduction(village.Settlement, out num2))
			{
				num *= num2;
			}
			return num;
		}

		// Token: 0x06001ACD RID: 6861 RVA: 0x00089C14 File Offset: 0x00087E14
		private bool GetIssueEffectOnFoodProduction(Settlement settlement, out float issueEffect)
		{
			issueEffect = 1f;
			if (settlement.IsVillage)
			{
				foreach (Hero hero in SettlementHelper.GetAllHeroesOfSettlement(settlement, false))
				{
					if (hero.Issue != null && hero.MapFaction == settlement.MapFaction)
					{
						float activeIssueEffectAmount = hero.Issue.GetActiveIssueEffectAmount(DefaultIssueEffects.HalfVillageProduction);
						if (activeIssueEffectAmount != 0f)
						{
							issueEffect *= activeIssueEffectAmount;
						}
					}
				}
			}
			return !issueEffect.ApproximatelyEqualsTo(1f, 1E-05f);
		}

		// Token: 0x06001ACE RID: 6862 RVA: 0x00089CB4 File Offset: 0x00087EB4
		public override float CalculateProductionSpeedOfItemCategory(ItemCategory item)
		{
			float num = 0f;
			foreach (VillageType villageType in VillageType.All)
			{
				float productionPerDay = villageType.GetProductionPerDay(item);
				if (productionPerDay > num)
				{
					num = productionPerDay;
				}
			}
			return num;
		}

		// Token: 0x040008F3 RID: 2291
		private readonly TextObject _cultureEffect = GameTexts.FindText("str_culture", null);
	}
}
