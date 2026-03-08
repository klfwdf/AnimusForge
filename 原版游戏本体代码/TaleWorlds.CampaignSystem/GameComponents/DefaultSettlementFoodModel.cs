using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000149 RID: 329
	public class DefaultSettlementFoodModel : SettlementFoodModel
	{
		// Token: 0x17000698 RID: 1688
		// (get) Token: 0x060019AA RID: 6570 RVA: 0x000809EF File Offset: 0x0007EBEF
		public override int FoodStocksUpperLimit
		{
			get
			{
				return 300;
			}
		}

		// Token: 0x17000699 RID: 1689
		// (get) Token: 0x060019AB RID: 6571 RVA: 0x000809F6 File Offset: 0x0007EBF6
		public override int NumberOfProsperityToEatOneFood
		{
			get
			{
				return 40;
			}
		}

		// Token: 0x1700069A RID: 1690
		// (get) Token: 0x060019AC RID: 6572 RVA: 0x000809FA File Offset: 0x0007EBFA
		public override int NumberOfMenOnGarrisonToEatOneFood
		{
			get
			{
				return 20;
			}
		}

		// Token: 0x1700069B RID: 1691
		// (get) Token: 0x060019AD RID: 6573 RVA: 0x000809FE File Offset: 0x0007EBFE
		public override int CastleFoodStockUpperLimitBonus
		{
			get
			{
				return 150;
			}
		}

		// Token: 0x060019AE RID: 6574 RVA: 0x00080A05 File Offset: 0x0007EC05
		public override ExplainedNumber CalculateTownFoodStocksChange(Town town, bool includeMarketStocks = true, bool includeDescriptions = false)
		{
			return this.CalculateTownFoodChangeInternal(town, includeMarketStocks, includeDescriptions);
		}

		// Token: 0x060019AF RID: 6575 RVA: 0x00080A10 File Offset: 0x0007EC10
		private ExplainedNumber CalculateTownFoodChangeInternal(Town town, bool includeMarketStocks, bool includeDescriptions)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(0f, includeDescriptions, null);
			ExplainedNumber explainedNumber2 = new ExplainedNumber(0f, includeDescriptions, null);
			ExplainedNumber explainedNumber3 = new ExplainedNumber(town.Prosperity / (float)this.NumberOfProsperityToEatOneFood, false, null);
			MobileParty garrisonParty = town.GarrisonParty;
			int? num = ((garrisonParty != null) ? new int?(garrisonParty.Party.NumberOfAllMembers) : null);
			ExplainedNumber explainedNumber4 = new ExplainedNumber(((num != null) ? ((float)num.GetValueOrDefault()) : 0f) / (float)this.NumberOfMenOnGarrisonToEatOneFood, false, null);
			if (town.IsUnderSiege)
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.Gourmet, town, ref explainedNumber4);
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Medicine.TriageTent, town, ref explainedNumber2);
			}
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.MasterOfWarcraft, town, ref explainedNumber3);
			explainedNumber2.Add(explainedNumber3.ResultNumber, this.ProsperityText, null);
			explainedNumber2.Add(explainedNumber4.ResultNumber, this.GarrisonText, null);
			town.AddEffectOfBuildings(BuildingEffectEnum.FoodConsumption, ref explainedNumber2);
			Clan ownerClan = town.Settlement.OwnerClan;
			Kingdom kingdom = ((ownerClan != null) ? ownerClan.Kingdom : null);
			if (kingdom != null && kingdom.HasPolicy(DefaultPolicies.HuntingRights))
			{
				explainedNumber.Add(2f, DefaultPolicies.HuntingRights.Name, null);
			}
			if (!town.IsUnderSiege)
			{
				int num2 = (town.IsTown ? 15 : 10);
				explainedNumber.Add((float)num2, this.LandsAroundSettlementText, null);
				foreach (Village village in town.Owner.Settlement.BoundVillages)
				{
					float value = 0f;
					if (village.VillageState == Village.VillageStates.Normal)
					{
						value = (float)((village.GetHearthLevel() + 1) * 6);
					}
					explainedNumber.Add(value, village.Name, null);
				}
				town.AddEffectOfBuildings(BuildingEffectEnum.FoodProduction, ref explainedNumber);
			}
			else
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Roguery.DirtyFighting, town, ref explainedNumber);
			}
			if (includeMarketStocks)
			{
				foreach (Town.SellLog sellLog in town.SoldItems)
				{
					if (sellLog.Category.Properties == ItemCategory.Property.BonusToFoodStores)
					{
						explainedNumber.Add((float)sellLog.Number, includeDescriptions ? sellLog.Category.GetName() : null, null);
					}
				}
			}
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			result.AddFromExplainedNumber(explainedNumber, null);
			result.SubtractFromExplainedNumber(explainedNumber2, null);
			DefaultSettlementFoodModel.GetSettlementFoodChangeDueToIssues(town, ref result);
			return result;
		}

		// Token: 0x060019B0 RID: 6576 RVA: 0x00080CA0 File Offset: 0x0007EEA0
		private static void GetSettlementFoodChangeDueToIssues(Town town, ref ExplainedNumber explainedNumber)
		{
			Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(DefaultIssueEffects.SettlementFood, town.Settlement, ref explainedNumber);
		}

		// Token: 0x04000871 RID: 2161
		private readonly TextObject ProsperityText = GameTexts.FindText("str_prosperity", null);

		// Token: 0x04000872 RID: 2162
		private readonly TextObject GarrisonText = GameTexts.FindText("str_garrison", null);

		// Token: 0x04000873 RID: 2163
		private readonly TextObject LandsAroundSettlementText = GameTexts.FindText("str_lands_around_settlement", null);

		// Token: 0x04000874 RID: 2164
		private readonly TextObject NormalVillagesText = GameTexts.FindText("str_normal_villages", null);

		// Token: 0x04000875 RID: 2165
		private readonly TextObject RaidedVillagesText = GameTexts.FindText("str_raided_villages", null);

		// Token: 0x04000876 RID: 2166
		private readonly TextObject VillagesUnderSiegeText = GameTexts.FindText("str_villages_under_siege", null);

		// Token: 0x04000877 RID: 2167
		private readonly TextObject FoodBoughtByCiviliansText = GameTexts.FindText("str_food_bought_by_civilians", null);

		// Token: 0x04000878 RID: 2168
		private const int FoodProductionPerVillage = 10;
	}
}
