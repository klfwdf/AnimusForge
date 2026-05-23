using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200014B RID: 331
	public class DefaultSettlementLoyaltyModel : SettlementLoyaltyModel
	{
		// Token: 0x1700069C RID: 1692
		// (get) Token: 0x060019B9 RID: 6585 RVA: 0x00081291 File Offset: 0x0007F491
		public override float HighLoyaltyProsperityEffect
		{
			get
			{
				return 0.5f;
			}
		}

		// Token: 0x1700069D RID: 1693
		// (get) Token: 0x060019BA RID: 6586 RVA: 0x00081298 File Offset: 0x0007F498
		public override int LowLoyaltyProsperityEffect
		{
			get
			{
				return -1;
			}
		}

		// Token: 0x1700069E RID: 1694
		// (get) Token: 0x060019BB RID: 6587 RVA: 0x0008129B File Offset: 0x0007F49B
		public override int ThresholdForTaxBoost
		{
			get
			{
				return 75;
			}
		}

		// Token: 0x1700069F RID: 1695
		// (get) Token: 0x060019BC RID: 6588 RVA: 0x0008129F File Offset: 0x0007F49F
		public override int ThresholdForTaxCorruption
		{
			get
			{
				return 50;
			}
		}

		// Token: 0x170006A0 RID: 1696
		// (get) Token: 0x060019BD RID: 6589 RVA: 0x000812A3 File Offset: 0x0007F4A3
		public override int ThresholdForHigherTaxCorruption
		{
			get
			{
				return 25;
			}
		}

		// Token: 0x170006A1 RID: 1697
		// (get) Token: 0x060019BE RID: 6590 RVA: 0x000812A7 File Offset: 0x0007F4A7
		public override int ThresholdForProsperityBoost
		{
			get
			{
				return 75;
			}
		}

		// Token: 0x170006A2 RID: 1698
		// (get) Token: 0x060019BF RID: 6591 RVA: 0x000812AB File Offset: 0x0007F4AB
		public override int ThresholdForProsperityPenalty
		{
			get
			{
				return 25;
			}
		}

		// Token: 0x170006A3 RID: 1699
		// (get) Token: 0x060019C0 RID: 6592 RVA: 0x000812AF File Offset: 0x0007F4AF
		public override int AdditionalStarvationPenaltyStartDay
		{
			get
			{
				return 14;
			}
		}

		// Token: 0x170006A4 RID: 1700
		// (get) Token: 0x060019C1 RID: 6593 RVA: 0x000812B3 File Offset: 0x0007F4B3
		public override int AdditionalStarvationLoyaltyEffect
		{
			get
			{
				return -1;
			}
		}

		// Token: 0x170006A5 RID: 1701
		// (get) Token: 0x060019C2 RID: 6594 RVA: 0x000812B6 File Offset: 0x0007F4B6
		public override int RebellionStartLoyaltyThreshold
		{
			get
			{
				return 15;
			}
		}

		// Token: 0x170006A6 RID: 1702
		// (get) Token: 0x060019C3 RID: 6595 RVA: 0x000812BA File Offset: 0x0007F4BA
		public override int RebelliousStateStartLoyaltyThreshold
		{
			get
			{
				return 25;
			}
		}

		// Token: 0x170006A7 RID: 1703
		// (get) Token: 0x060019C4 RID: 6596 RVA: 0x000812BE File Offset: 0x0007F4BE
		public override int LoyaltyBoostAfterRebellionStartValue
		{
			get
			{
				return 5;
			}
		}

		// Token: 0x170006A8 RID: 1704
		// (get) Token: 0x060019C5 RID: 6597 RVA: 0x000812C1 File Offset: 0x0007F4C1
		public override int MilitiaBoostPercentage
		{
			get
			{
				return 200;
			}
		}

		// Token: 0x170006A9 RID: 1705
		// (get) Token: 0x060019C6 RID: 6598 RVA: 0x000812C8 File Offset: 0x0007F4C8
		public override float ThresholdForNotableRelationBonus
		{
			get
			{
				return 75f;
			}
		}

		// Token: 0x170006AA RID: 1706
		// (get) Token: 0x060019C7 RID: 6599 RVA: 0x000812CF File Offset: 0x0007F4CF
		public override int DailyNotableRelationBonus
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x170006AB RID: 1707
		// (get) Token: 0x060019C8 RID: 6600 RVA: 0x000812D2 File Offset: 0x0007F4D2
		public override int SettlementLoyaltyChangeDueToSecurityThreshold
		{
			get
			{
				return 50;
			}
		}

		// Token: 0x170006AC RID: 1708
		// (get) Token: 0x060019C9 RID: 6601 RVA: 0x000812D6 File Offset: 0x0007F4D6
		public override int MaximumLoyaltyInSettlement
		{
			get
			{
				return 100;
			}
		}

		// Token: 0x170006AD RID: 1709
		// (get) Token: 0x060019CA RID: 6602 RVA: 0x000812DA File Offset: 0x0007F4DA
		public override int LoyaltyDriftMedium
		{
			get
			{
				return 50;
			}
		}

		// Token: 0x170006AE RID: 1710
		// (get) Token: 0x060019CB RID: 6603 RVA: 0x000812DE File Offset: 0x0007F4DE
		public override float HighSecurityLoyaltyEffect
		{
			get
			{
				return 1f;
			}
		}

		// Token: 0x170006AF RID: 1711
		// (get) Token: 0x060019CC RID: 6604 RVA: 0x000812E5 File Offset: 0x0007F4E5
		public override float LowSecurityLoyaltyEffect
		{
			get
			{
				return -2f;
			}
		}

		// Token: 0x170006B0 RID: 1712
		// (get) Token: 0x060019CD RID: 6605 RVA: 0x000812EC File Offset: 0x0007F4EC
		public override float GovernorSameCultureLoyaltyEffect
		{
			get
			{
				return 1f;
			}
		}

		// Token: 0x170006B1 RID: 1713
		// (get) Token: 0x060019CE RID: 6606 RVA: 0x000812F3 File Offset: 0x0007F4F3
		public override float GovernorDifferentCultureLoyaltyEffect
		{
			get
			{
				return -1f;
			}
		}

		// Token: 0x170006B2 RID: 1714
		// (get) Token: 0x060019CF RID: 6607 RVA: 0x000812FA File Offset: 0x0007F4FA
		public override float SettlementOwnerDifferentCultureLoyaltyEffect
		{
			get
			{
				return -3f;
			}
		}

		// Token: 0x060019D0 RID: 6608 RVA: 0x00081301 File Offset: 0x0007F501
		public override ExplainedNumber CalculateLoyaltyChange(Town town, bool includeDescriptions = false)
		{
			return this.CalculateLoyaltyChangeInternal(town, includeDescriptions);
		}

		// Token: 0x060019D1 RID: 6609 RVA: 0x0008130C File Offset: 0x0007F50C
		public override void CalculateGoldGainDueToHighLoyalty(Town town, ref ExplainedNumber explainedNumber)
		{
			float value = MBMath.Map(town.Loyalty, (float)this.ThresholdForTaxBoost, 100f, 0f, 0.2f);
			explainedNumber.AddFactor(value, this.LoyaltyText);
		}

		// Token: 0x060019D2 RID: 6610 RVA: 0x00081348 File Offset: 0x0007F548
		public override void CalculateGoldCutDueToLowLoyalty(Town town, ref ExplainedNumber explainedNumber)
		{
			float value = MBMath.Map(town.Loyalty, (float)this.ThresholdForHigherTaxCorruption, (float)this.ThresholdForTaxCorruption, -0.5f, 0f);
			explainedNumber.AddFactor(value, this.CorruptionText);
		}

		// Token: 0x060019D3 RID: 6611 RVA: 0x00081388 File Offset: 0x0007F588
		private ExplainedNumber CalculateLoyaltyChangeInternal(Town town, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			this.GetSettlementLoyaltyChangeDueToFoodStocks(town, ref result);
			this.GetSettlementLoyaltyChangeDueToGovernorCulture(town, ref result);
			this.GetSettlementLoyaltyChangeDueToOwnerCulture(town, ref result);
			this.GetSettlementLoyaltyChangeDueToPolicies(town, ref result);
			this.GetSettlementLoyaltyChangeDueToProjects(town, ref result);
			this.GetSettlementLoyaltyChangeDueToIssues(town, ref result);
			this.GetSettlementLoyaltyChangeDueToSecurity(town, ref result);
			this.GetSettlementLoyaltyChangeDueToNotableRelations(town, ref result);
			this.GetSettlementLoyaltyChangeDueToGovernorPerks(town, ref result);
			this.GetSettlementLoyaltyChangeDueToLoyaltyDrift(town, ref result);
			return result;
		}

		// Token: 0x060019D4 RID: 6612 RVA: 0x00081400 File Offset: 0x0007F600
		private void GetSettlementLoyaltyChangeDueToGovernorPerks(Town town, ref ExplainedNumber explainedNumber)
		{
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Leadership.HeroicLeader, town, ref explainedNumber);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Medicine.PhysicianOfPeople, town, ref explainedNumber);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Athletics.Durable, town, ref explainedNumber);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Bow.Discipline, town, ref explainedNumber);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Riding.WellStraped, town, ref explainedNumber);
			float num = 0f;
			for (int i = 0; i < town.Settlement.Parties.Count; i++)
			{
				MobileParty mobileParty = town.Settlement.Parties[i];
				if (mobileParty.ActualClan == town.OwnerClan)
				{
					if (mobileParty.IsMainParty)
					{
						for (int j = 0; j < mobileParty.MemberRoster.Count; j++)
						{
							CharacterObject characterAtIndex = mobileParty.MemberRoster.GetCharacterAtIndex(j);
							if (characterAtIndex.IsHero && characterAtIndex.HeroObject.GetPerkValue(DefaultPerks.Charm.Parade))
							{
								num += DefaultPerks.Charm.Parade.PrimaryBonus;
							}
						}
					}
					else if (mobileParty.LeaderHero != null && mobileParty.LeaderHero.GetPerkValue(DefaultPerks.Charm.Parade))
					{
						num += DefaultPerks.Charm.Parade.PrimaryBonus;
					}
				}
			}
			foreach (Hero hero in town.Settlement.HeroesWithoutParty)
			{
				if (hero.Clan == town.OwnerClan && hero.GetPerkValue(DefaultPerks.Charm.Parade))
				{
					num += DefaultPerks.Charm.Parade.PrimaryBonus;
				}
			}
			if (num > 0f)
			{
				explainedNumber.Add(num, DefaultPerks.Charm.Parade.Name, null);
			}
		}

		// Token: 0x060019D5 RID: 6613 RVA: 0x00081598 File Offset: 0x0007F798
		private void GetSettlementLoyaltyChangeDueToNotableRelations(Town town, ref ExplainedNumber explainedNumber)
		{
			float num = 0f;
			foreach (Hero hero in town.Settlement.Notables)
			{
				if (hero.SupporterOf != null)
				{
					if (hero.SupporterOf == town.Settlement.OwnerClan)
					{
						num += 0.5f;
					}
					else if (town.MapFaction.IsAtWarWith(hero.SupporterOf.MapFaction))
					{
						num += -0.5f;
					}
				}
			}
			if (!num.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				explainedNumber.Add(num, this.NotableText, null);
			}
		}

		// Token: 0x060019D6 RID: 6614 RVA: 0x00081654 File Offset: 0x0007F854
		private void GetSettlementLoyaltyChangeDueToOwnerCulture(Town town, ref ExplainedNumber explainedNumber)
		{
			if (town.Settlement.OwnerClan.Culture != town.Settlement.Culture)
			{
				explainedNumber.Add(this.SettlementOwnerDifferentCultureLoyaltyEffect, DefaultSettlementLoyaltyModel.CultureText, null);
			}
		}

		// Token: 0x060019D7 RID: 6615 RVA: 0x00081688 File Offset: 0x0007F888
		private void GetSettlementLoyaltyChangeDueToPolicies(Town town, ref ExplainedNumber explainedNumber)
		{
			Kingdom kingdom = town.Owner.Settlement.OwnerClan.Kingdom;
			if (kingdom != null)
			{
				if (kingdom.ActivePolicies.Contains(DefaultPolicies.Citizenship))
				{
					if (town.Settlement.OwnerClan.Culture == town.Settlement.Culture)
					{
						explainedNumber.Add(0.5f, DefaultPolicies.Citizenship.Name, null);
					}
					else
					{
						explainedNumber.Add(-0.5f, DefaultPolicies.Citizenship.Name, null);
					}
				}
				if (kingdom.ActivePolicies.Contains(DefaultPolicies.HuntingRights))
				{
					explainedNumber.Add(-0.2f, DefaultPolicies.HuntingRights.Name, null);
				}
				if (kingdom.ActivePolicies.Contains(DefaultPolicies.GrazingRights))
				{
					explainedNumber.Add(0.5f, DefaultPolicies.GrazingRights.Name, null);
				}
				if (kingdom.ActivePolicies.Contains(DefaultPolicies.TrialByJury))
				{
					explainedNumber.Add(0.5f, DefaultPolicies.TrialByJury.Name, null);
				}
				if (kingdom.ActivePolicies.Contains(DefaultPolicies.ImperialTowns))
				{
					if (kingdom.RulingClan == town.Settlement.OwnerClan)
					{
						explainedNumber.Add(1f, DefaultPolicies.ImperialTowns.Name, null);
					}
					else
					{
						explainedNumber.Add(-0.3f, DefaultPolicies.ImperialTowns.Name, null);
					}
				}
				if (kingdom.ActivePolicies.Contains(DefaultPolicies.ForgivenessOfDebts))
				{
					explainedNumber.Add(2f, DefaultPolicies.ForgivenessOfDebts.Name, null);
				}
				if (kingdom.ActivePolicies.Contains(DefaultPolicies.TribunesOfThePeople) && town.IsTown)
				{
					explainedNumber.Add(1f, DefaultPolicies.TribunesOfThePeople.Name, null);
				}
				if (kingdom.ActivePolicies.Contains(DefaultPolicies.DebasementOfTheCurrency))
				{
					explainedNumber.Add(-1f, DefaultPolicies.DebasementOfTheCurrency.Name, null);
				}
			}
		}

		// Token: 0x060019D8 RID: 6616 RVA: 0x00081859 File Offset: 0x0007FA59
		private void GetSettlementLoyaltyChangeDueToGovernorCulture(Town town, ref ExplainedNumber explainedNumber)
		{
			if (town.Governor != null)
			{
				explainedNumber.Add((town.Governor.Culture == town.Culture) ? this.GovernorSameCultureLoyaltyEffect : this.GovernorDifferentCultureLoyaltyEffect, DefaultSettlementLoyaltyModel.GovernorCultureText, null);
			}
		}

		// Token: 0x060019D9 RID: 6617 RVA: 0x00081890 File Offset: 0x0007FA90
		private void GetSettlementLoyaltyChangeDueToFoodStocks(Town town, ref ExplainedNumber explainedNumber)
		{
			if (town.Settlement.IsStarving)
			{
				float num = -1f;
				if (town.Settlement.Party.DaysStarving > 14f)
				{
					num += -1f;
				}
				explainedNumber.Add(num, this.StarvingText, null);
			}
		}

		// Token: 0x060019DA RID: 6618 RVA: 0x000818E0 File Offset: 0x0007FAE0
		private void GetSettlementLoyaltyChangeDueToSecurity(Town town, ref ExplainedNumber explainedNumber)
		{
			float value = ((town.Security > (float)this.SettlementLoyaltyChangeDueToSecurityThreshold) ? MBMath.Map(town.Security, (float)this.SettlementLoyaltyChangeDueToSecurityThreshold, (float)this.MaximumLoyaltyInSettlement, 0f, this.HighSecurityLoyaltyEffect) : MBMath.Map(town.Security, 0f, (float)this.SettlementLoyaltyChangeDueToSecurityThreshold, this.LowSecurityLoyaltyEffect, 0f));
			explainedNumber.Add(value, this.SecurityText, null);
		}

		// Token: 0x060019DB RID: 6619 RVA: 0x00081953 File Offset: 0x0007FB53
		private void GetSettlementLoyaltyChangeDueToProjects(Town town, ref ExplainedNumber explainedNumber)
		{
			town.AddEffectOfBuildings(BuildingEffectEnum.Loyalty, ref explainedNumber);
		}

		// Token: 0x060019DC RID: 6620 RVA: 0x0008195D File Offset: 0x0007FB5D
		private void GetSettlementLoyaltyChangeDueToIssues(Town town, ref ExplainedNumber explainedNumber)
		{
			Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(DefaultIssueEffects.SettlementLoyalty, town.Settlement, ref explainedNumber);
		}

		// Token: 0x060019DD RID: 6621 RVA: 0x0008197F File Offset: 0x0007FB7F
		private void GetSettlementLoyaltyChangeDueToLoyaltyDrift(Town town, ref ExplainedNumber explainedNumber)
		{
			explainedNumber.Add(-0.1f * (town.Loyalty - (float)this.LoyaltyDriftMedium), this.LoyaltyDriftText, null);
		}

		// Token: 0x04000885 RID: 2181
		private const float StarvationLoyaltyEffect = -1f;

		// Token: 0x04000886 RID: 2182
		private const int AdditionalStarvationLoyaltyEffectAfterDays = 14;

		// Token: 0x04000887 RID: 2183
		private const float NotableSupportsOwnerLoyaltyEffect = 0.5f;

		// Token: 0x04000888 RID: 2184
		private const float NotableSupportsEnemyLoyaltyEffect = -0.5f;

		// Token: 0x04000889 RID: 2185
		private readonly TextObject StarvingText = GameTexts.FindText("str_starving", null);

		// Token: 0x0400088A RID: 2186
		private static readonly TextObject CultureText = new TextObject("{=YjoXyFDX}Owner Culture", null);

		// Token: 0x0400088B RID: 2187
		private static readonly TextObject GovernorCultureText = new TextObject("{=5Vo8dJub}Governor's Culture", null);

		// Token: 0x0400088C RID: 2188
		private static readonly TextObject NoGovernorText = new TextObject("{=NH5N3kP5}No governor", null);

		// Token: 0x0400088D RID: 2189
		private readonly TextObject NotableText = GameTexts.FindText("str_notable_relations", null);

		// Token: 0x0400088E RID: 2190
		private readonly TextObject CrimeText = GameTexts.FindText("str_governor_criminal", null);

		// Token: 0x0400088F RID: 2191
		private readonly TextObject GovernorText = GameTexts.FindText("str_notable_governor", null);

		// Token: 0x04000890 RID: 2192
		private readonly TextObject SecurityText = GameTexts.FindText("str_security", null);

		// Token: 0x04000891 RID: 2193
		private readonly TextObject LoyaltyText = GameTexts.FindText("str_loyalty", null);

		// Token: 0x04000892 RID: 2194
		private readonly TextObject LoyaltyDriftText = GameTexts.FindText("str_loyalty_drift", null);

		// Token: 0x04000893 RID: 2195
		private readonly TextObject CorruptionText = GameTexts.FindText("str_corruption", null);
	}
}
