using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000F8 RID: 248
	public class DefaultBuildingConstructionModel : BuildingConstructionModel
	{
		// Token: 0x17000620 RID: 1568
		// (get) Token: 0x06001670 RID: 5744 RVA: 0x00066BFF File Offset: 0x00064DFF
		public override int TownBoostCost
		{
			get
			{
				return 500;
			}
		}

		// Token: 0x17000621 RID: 1569
		// (get) Token: 0x06001671 RID: 5745 RVA: 0x00066C06 File Offset: 0x00064E06
		public override int TownBoostBonus
		{
			get
			{
				return 50;
			}
		}

		// Token: 0x17000622 RID: 1570
		// (get) Token: 0x06001672 RID: 5746 RVA: 0x00066C0A File Offset: 0x00064E0A
		public override int CastleBoostCost
		{
			get
			{
				return 250;
			}
		}

		// Token: 0x17000623 RID: 1571
		// (get) Token: 0x06001673 RID: 5747 RVA: 0x00066C11 File Offset: 0x00064E11
		public override int CastleBoostBonus
		{
			get
			{
				return 20;
			}
		}

		// Token: 0x06001674 RID: 5748 RVA: 0x00066C18 File Offset: 0x00064E18
		public override ExplainedNumber CalculateDailyConstructionPower(Town town, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			this.CalculateDailyConstructionPowerInternal(town, ref result, false);
			return result;
		}

		// Token: 0x06001675 RID: 5749 RVA: 0x00066C40 File Offset: 0x00064E40
		public override int CalculateDailyConstructionPowerWithoutBoost(Town town)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(0f, false, null);
			return this.CalculateDailyConstructionPowerInternal(town, ref explainedNumber, true);
		}

		// Token: 0x06001676 RID: 5750 RVA: 0x00066C68 File Offset: 0x00064E68
		public override int GetBoostAmount(Town town)
		{
			object obj = (town.IsCastle ? this.CastleBoostBonus : this.TownBoostBonus);
			float num = 0f;
			if (town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Steward.Relocation))
			{
				num += DefaultPerks.Steward.Relocation.SecondaryBonus;
			}
			if (town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Trade.SpringOfGold))
			{
				num += DefaultPerks.Trade.SpringOfGold.SecondaryBonus;
			}
			object obj2 = obj;
			return obj2 + (int)(obj2 * num);
		}

		// Token: 0x06001677 RID: 5751 RVA: 0x00066CE5 File Offset: 0x00064EE5
		public override int GetBoostCost(Town town)
		{
			if (!town.IsCastle)
			{
				return this.TownBoostCost;
			}
			return this.CastleBoostCost;
		}

		// Token: 0x06001678 RID: 5752 RVA: 0x00066CFC File Offset: 0x00064EFC
		private int CalculateDailyConstructionPowerInternal(Town town, ref ExplainedNumber result, bool omitBoost = false)
		{
			float value = town.Prosperity * 0.01f;
			result.Add(value, GameTexts.FindText("str_prosperity", null), null);
			if (!omitBoost && town.BoostBuildingProcess > 0)
			{
				int num = (town.IsCastle ? this.CastleBoostCost : this.TownBoostCost);
				int num2 = this.GetBoostAmount(town);
				float num3 = MathF.Min(1f, (float)town.BoostBuildingProcess / (float)num);
				float num4 = 0f;
				if (town.IsTown && town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Engineering.Clockwork))
				{
					num4 += DefaultPerks.Engineering.Clockwork.SecondaryBonus;
				}
				num2 += MathF.Round((float)num2 * num4);
				result.Add((float)num2 * num3, DefaultBuildingConstructionModel.BoostText, null);
			}
			if (town.Governor != null)
			{
				Settlement currentSettlement = town.Governor.CurrentSettlement;
				if (((currentSettlement != null) ? currentSettlement.Town : null) == town)
				{
					SkillHelper.AddSkillBonusForTown(DefaultSkillEffects.TownProjectBuildingBonus, town, ref result);
					PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.ForcedLabor, town, ref result);
				}
			}
			if (town.Governor != null)
			{
				Settlement currentSettlement2 = town.Governor.CurrentSettlement;
				if (((currentSettlement2 != null) ? currentSettlement2.Town : null) == town && !town.BuildingsInProgress.IsEmpty<Building>())
				{
					if (town.Governor.GetPerkValue(DefaultPerks.Steward.ForcedLabor) && town.Settlement.Party.PrisonRoster.TotalManCount > 0)
					{
						float value2 = MathF.Min(0.3f, (float)town.Settlement.Party.PrisonRoster.TotalManCount / 3f * DefaultPerks.Steward.ForcedLabor.SecondaryBonus);
						result.AddFactor(value2, DefaultPerks.Steward.ForcedLabor.Name);
					}
					if (town.IsCastle && town.Governor.GetPerkValue(DefaultPerks.Engineering.MilitaryPlanner))
					{
						result.AddFactor(DefaultPerks.Engineering.MilitaryPlanner.SecondaryBonus, DefaultPerks.Engineering.MilitaryPlanner.Name);
					}
					else if (town.IsTown && town.Governor.GetPerkValue(DefaultPerks.Engineering.Carpenters))
					{
						result.AddFactor(DefaultPerks.Engineering.Carpenters.SecondaryBonus, DefaultPerks.Engineering.Carpenters.Name);
					}
					Building building = town.BuildingsInProgress.Peek();
					if ((building.BuildingType == DefaultBuildingTypes.SettlementFortifications || building.BuildingType == DefaultBuildingTypes.CastleBarracks || building.BuildingType == DefaultBuildingTypes.SettlementBarracks) && town.Governor.GetPerkValue(DefaultPerks.Engineering.Stonecutters))
					{
						result.AddFactor(DefaultPerks.Engineering.Stonecutters.PrimaryBonus, DefaultPerks.Engineering.Stonecutters.Name);
					}
				}
			}
			int num5 = town.SoldItems.Sum(delegate(Town.SellLog x)
			{
				if (x.Category.Properties != ItemCategory.Property.BonusToProduction)
				{
					return 0;
				}
				return x.Number;
			});
			if (num5 > 0)
			{
				result.Add(0.25f * (float)num5, DefaultBuildingConstructionModel.ProductionFromMarketText, null);
			}
			BuildingType buildingType = (town.BuildingsInProgress.IsEmpty<Building>() ? null : town.BuildingsInProgress.Peek().BuildingType);
			if (buildingType != null && buildingType.IsMilitaryProject)
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.TwoHanded.Confidence, town, ref result);
			}
			if (buildingType == DefaultBuildingTypes.SettlementMarketplace)
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Trade.SelfMadeMan, town, ref result);
			}
			town.AddEffectOfBuildings(BuildingEffectEnum.ConstructionPerDay, ref result);
			if (town.Loyalty >= 75f)
			{
				float value3 = MBMath.Map(town.Loyalty, 75f, 100f, 0f, 0.2f);
				result.AddFactor(value3, DefaultBuildingConstructionModel.HighLoyaltyBonusText);
			}
			else if (town.Loyalty > 25f && town.Loyalty <= 50f)
			{
				float num6 = MBMath.Map(town.Loyalty, 25f, 50f, 0.5f, 0f);
				result.AddFactor(-num6, DefaultBuildingConstructionModel.LowLoyaltyPenaltyText);
			}
			else if (town.Loyalty <= 25f)
			{
				result.LimitMax(0f, DefaultBuildingConstructionModel.VeryLowLoyaltyPenaltyText);
			}
			if (town.Loyalty > 25f && town.OwnerClan.Culture.HasFeat(DefaultCulturalFeats.BattanianConstructionFeat))
			{
				result.AddFactor(DefaultCulturalFeats.BattanianConstructionFeat.EffectBonus, this.CultureText);
			}
			result.LimitMin(0f);
			return (int)result.ResultNumber;
		}

		// Token: 0x04000768 RID: 1896
		private const float HammerMultiplier = 0.01f;

		// Token: 0x04000769 RID: 1897
		private const int VeryLowLoyaltyValue = 25;

		// Token: 0x0400076A RID: 1898
		private const float MediumLoyaltyValue = 50f;

		// Token: 0x0400076B RID: 1899
		private const float HighLoyaltyValue = 75f;

		// Token: 0x0400076C RID: 1900
		private const float HighestLoyaltyValue = 100f;

		// Token: 0x0400076D RID: 1901
		private static readonly TextObject ProductionFromMarketText = new TextObject("{=vaZDJGMx}Construction from Market", null);

		// Token: 0x0400076E RID: 1902
		private static readonly TextObject BoostText = new TextObject("{=yX1RycON}Boost from Reserve", null);

		// Token: 0x0400076F RID: 1903
		private static readonly TextObject HighLoyaltyBonusText = new TextObject("{=aSniKUJv}High Loyalty", null);

		// Token: 0x04000770 RID: 1904
		private static readonly TextObject LowLoyaltyPenaltyText = new TextObject("{=SJ2qsRdF}Low Loyalty", null);

		// Token: 0x04000771 RID: 1905
		private static readonly TextObject VeryLowLoyaltyPenaltyText = new TextObject("{=CcQzFnpN}Very Low Loyalty", null);

		// Token: 0x04000772 RID: 1906
		private readonly TextObject CultureText = GameTexts.FindText("str_culture", null);
	}
}
