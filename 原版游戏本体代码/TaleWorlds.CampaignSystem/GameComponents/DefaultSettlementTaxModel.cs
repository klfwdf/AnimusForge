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
	// Token: 0x02000150 RID: 336
	public class DefaultSettlementTaxModel : SettlementTaxModel
	{
		// Token: 0x170006C3 RID: 1731
		// (get) Token: 0x06001A1B RID: 6683 RVA: 0x00083234 File Offset: 0x00081434
		public override float SettlementCommissionRateTown
		{
			get
			{
				return 0.7f;
			}
		}

		// Token: 0x170006C4 RID: 1732
		// (get) Token: 0x06001A1C RID: 6684 RVA: 0x0008323B File Offset: 0x0008143B
		public override float SettlementCommissionRateVillage
		{
			get
			{
				return 1f;
			}
		}

		// Token: 0x170006C5 RID: 1733
		// (get) Token: 0x06001A1D RID: 6685 RVA: 0x00083242 File Offset: 0x00081442
		public override int SettlementCommissionDecreaseSecurityThreshold
		{
			get
			{
				return 75;
			}
		}

		// Token: 0x170006C6 RID: 1734
		// (get) Token: 0x06001A1E RID: 6686 RVA: 0x00083246 File Offset: 0x00081446
		public override int MaximumDecreaseBasedOnSecuritySecurity
		{
			get
			{
				return 10;
			}
		}

		// Token: 0x06001A1F RID: 6687 RVA: 0x0008324C File Offset: 0x0008144C
		public override float GetTownTaxRatio(Town town)
		{
			float num = 1f;
			if (town.Settlement.OwnerClan.Kingdom != null && town.Settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.CrownDuty))
			{
				num += 0.05f;
			}
			return this.SettlementCommissionRateTown * num;
		}

		// Token: 0x06001A20 RID: 6688 RVA: 0x000832A4 File Offset: 0x000814A4
		public override float GetVillageTaxRatio(Village village)
		{
			float num = this.SettlementCommissionRateVillage;
			if (village.Settlement.OwnerClan.Kingdom != null && village.Settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.LandGrantsForVeteran))
			{
				num -= num * 0.05f;
			}
			return num;
		}

		// Token: 0x06001A21 RID: 6689 RVA: 0x000832F8 File Offset: 0x000814F8
		public override float GetTownCommissionChangeBasedOnSecurity(Town town, float commission)
		{
			if (town.Security < (float)this.SettlementCommissionDecreaseSecurityThreshold)
			{
				float num = MBMath.Map((float)this.SettlementCommissionDecreaseSecurityThreshold - town.Security, 0f, (float)this.SettlementCommissionDecreaseSecurityThreshold, (float)this.MaximumDecreaseBasedOnSecuritySecurity, 0f);
				commission -= commission * (num * 0.01f);
				return commission;
			}
			return commission;
		}

		// Token: 0x06001A22 RID: 6690 RVA: 0x00083350 File Offset: 0x00081550
		public override ExplainedNumber CalculateTownTax(Town town, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			this.CalculateDailyTaxInternal(town, ref result);
			return result;
		}

		// Token: 0x06001A23 RID: 6691 RVA: 0x00083378 File Offset: 0x00081578
		private float CalculateDailyTax(Town town, ref ExplainedNumber explainedNumber)
		{
			float prosperity = town.Prosperity;
			float num = 1f;
			if (town.Settlement.OwnerClan.Kingdom != null && town.Settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.CouncilOfTheCommons))
			{
				num -= 0.05f;
			}
			float num2 = 0.35f;
			float value = prosperity * num2 * num;
			explainedNumber.Add(value, this.ProsperityText, null);
			return explainedNumber.ResultNumber;
		}

		// Token: 0x06001A24 RID: 6692 RVA: 0x000833EC File Offset: 0x000815EC
		private void CalculateDailyTaxInternal(Town town, ref ExplainedNumber result)
		{
			float rawTax = this.CalculateDailyTax(town, ref result);
			this.CalculatePolicyGoldCut(town, rawTax, ref result);
			if (PerkHelper.GetPerkValueForTown(DefaultPerks.Bow.QuickDraw, town))
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Bow.QuickDraw, town, ref result);
			}
			if (town.Governor != null)
			{
				if (town.Governor.GetPerkValue(DefaultPerks.Steward.Logistician))
				{
					PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.Logistician, town, ref result);
				}
				if (town.Governor.GetPerkValue(DefaultPerks.Steward.PriceOfLoyalty))
				{
					int num = town.Governor.GetSkillValue(DefaultSkills.Steward) - Campaign.Current.Models.CharacterDevelopmentModel.MinSkillRequiredForEpicPerkBonus;
					result.AddFactor(DefaultPerks.Steward.PriceOfLoyalty.SecondaryBonus * (float)num, DefaultPerks.Steward.PriceOfLoyalty.Name);
				}
				if (PerkHelper.GetPerkValueForTown(DefaultPerks.Scouting.DesertBorn, town))
				{
					PerkHelper.AddPerkBonusForTown(DefaultPerks.Scouting.DesertBorn, town, ref result);
				}
			}
			if (town.IsTown && town.OwnerClan.Culture.HasFeat(DefaultCulturalFeats.KhuzaitDecreasedTaxFeat))
			{
				result.AddFactor(DefaultCulturalFeats.KhuzaitDecreasedTaxFeat.EffectBonus, GameTexts.FindText("str_culture", null));
			}
			this.GetSettlementTaxChangeDueToIssues(town, ref result);
			this.CalculateSettlementTaxDueToSecurity(town, ref result);
			this.CalculateSettlementTaxDueToLoyalty(town, ref result);
			this.CalculateSettlementTaxDueToBuildings(town, ref result);
			result.Clamp(0f, float.MaxValue);
		}

		// Token: 0x06001A25 RID: 6693 RVA: 0x00083528 File Offset: 0x00081728
		private void CalculateSettlementTaxDueToSecurity(Town town, ref ExplainedNumber explainedNumber)
		{
			SettlementSecurityModel settlementSecurityModel = Campaign.Current.Models.SettlementSecurityModel;
			if (town.Security >= (float)settlementSecurityModel.ThresholdForTaxBoost)
			{
				settlementSecurityModel.CalculateGoldGainDueToHighSecurity(town, ref explainedNumber);
				return;
			}
			if (town.Security >= (float)settlementSecurityModel.ThresholdForHigherTaxCorruption && town.Security < (float)settlementSecurityModel.ThresholdForTaxCorruption)
			{
				settlementSecurityModel.CalculateGoldCutDueToLowSecurity(town, ref explainedNumber);
			}
		}

		// Token: 0x06001A26 RID: 6694 RVA: 0x00083584 File Offset: 0x00081784
		private void CalculateSettlementTaxDueToLoyalty(Town town, ref ExplainedNumber explainedNumber)
		{
			SettlementLoyaltyModel settlementLoyaltyModel = Campaign.Current.Models.SettlementLoyaltyModel;
			if (town.Loyalty >= (float)settlementLoyaltyModel.ThresholdForTaxBoost)
			{
				settlementLoyaltyModel.CalculateGoldGainDueToHighLoyalty(town, ref explainedNumber);
				return;
			}
			if (town.Loyalty >= (float)settlementLoyaltyModel.ThresholdForHigherTaxCorruption && town.Loyalty <= (float)settlementLoyaltyModel.ThresholdForTaxCorruption)
			{
				settlementLoyaltyModel.CalculateGoldCutDueToLowLoyalty(town, ref explainedNumber);
				return;
			}
			if (town.Loyalty < (float)settlementLoyaltyModel.ThresholdForHigherTaxCorruption)
			{
				explainedNumber.AddFactor(-1f, DefaultSettlementTaxModel.VeryLowLoyalty);
			}
		}

		// Token: 0x06001A27 RID: 6695 RVA: 0x000835FF File Offset: 0x000817FF
		private void CalculateSettlementTaxDueToBuildings(Town town, ref ExplainedNumber result)
		{
			town.AddEffectOfBuildings(BuildingEffectEnum.TaxPerDay, ref result);
			town.AddEffectOfBuildings(BuildingEffectEnum.DenarByBoundVillageHeartPerDay, ref result);
		}

		// Token: 0x06001A28 RID: 6696 RVA: 0x00083614 File Offset: 0x00081814
		private void CalculatePolicyGoldCut(Town town, float rawTax, ref ExplainedNumber explainedNumber)
		{
			if (town.MapFaction.IsKingdomFaction)
			{
				Kingdom kingdom = (Kingdom)town.MapFaction;
				if (town.IsTown)
				{
					if (kingdom.ActivePolicies.Contains(DefaultPolicies.Magistrates))
					{
						explainedNumber.Add(-0.05f * rawTax, DefaultPolicies.Magistrates.Name, null);
					}
					if (kingdom.ActivePolicies.Contains(DefaultPolicies.Bailiffs))
					{
						explainedNumber.Add(-0.05f * rawTax, DefaultPolicies.Bailiffs.Name, null);
					}
					if (kingdom.ActivePolicies.Contains(DefaultPolicies.TribunesOfThePeople))
					{
						explainedNumber.Add(-0.05f * rawTax, DefaultPolicies.TribunesOfThePeople.Name, null);
					}
				}
				if (kingdom.ActivePolicies.Contains(DefaultPolicies.Cantons))
				{
					explainedNumber.Add(-0.1f * rawTax, DefaultPolicies.Cantons.Name, null);
				}
			}
		}

		// Token: 0x06001A29 RID: 6697 RVA: 0x000836ED File Offset: 0x000818ED
		private void GetSettlementTaxChangeDueToIssues(Town center, ref ExplainedNumber result)
		{
			Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(DefaultIssueEffects.SettlementTax, center.Owner.Settlement, ref result);
		}

		// Token: 0x06001A2A RID: 6698 RVA: 0x00083714 File Offset: 0x00081914
		public override int CalculateVillageTaxFromIncome(Village village, int marketIncome)
		{
			if (marketIncome == 0)
			{
				return 0;
			}
			return (int)((float)marketIncome * Campaign.Current.Models.SettlementTaxModel.GetVillageTaxRatio(village));
		}

		// Token: 0x040008B3 RID: 2227
		private readonly TextObject ProsperityText = GameTexts.FindText("str_prosperity", null);

		// Token: 0x040008B4 RID: 2228
		private static readonly TextObject VeryLowLoyalty = new TextObject("{=CcQzFnpN}Very Low Loyalty", null);
	}
}
