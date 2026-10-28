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

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultSettlementLoyaltyModel : SettlementLoyaltyModel
{
	private const float StarvationLoyaltyEffect = -1f;

	private const int AdditionalStarvationLoyaltyEffectAfterDays = 14;

	private const float NotableSupportsOwnerLoyaltyEffect = 0.5f;

	private const float NotableSupportsEnemyLoyaltyEffect = -0.5f;

	private readonly TextObject StarvingText = GameTexts.FindText("str_starving");

	private static readonly TextObject CultureText = new TextObject("{=YjoXyFDX}Owner Culture");

	private readonly TextObject NotableText = GameTexts.FindText("str_notable_relations");

	private readonly TextObject SecurityText = GameTexts.FindText("str_security");

	private readonly TextObject LoyaltyText = GameTexts.FindText("str_loyalty");

	private readonly TextObject LoyaltyDriftText = GameTexts.FindText("str_loyalty_drift");

	private readonly TextObject CorruptionText = GameTexts.FindText("str_corruption");

	private readonly TextObject GovernorText = GameTexts.FindText("str_notable_governor");

	public override float HighLoyaltyProsperityEffect => 0.5f;

	public override int LowLoyaltyProsperityEffect => -1;

	public override int ThresholdForTaxBoost => 75;

	public override int ThresholdForTaxCorruption => 50;

	public override int ThresholdForHigherTaxCorruption => 25;

	public override int ThresholdForProsperityBoost => 75;

	public override int ThresholdForProsperityPenalty => 25;

	public override int AdditionalStarvationPenaltyStartDay => 14;

	public override int AdditionalStarvationLoyaltyEffect => -1;

	public override int RebellionStartLoyaltyThreshold
	{
		get
		{
			if (!Campaign.Current.Options.IsHighRebellionEnabled)
			{
				return 25;
			}
			return 50;
		}
	}

	public override int RebelliousStateStartLoyaltyThreshold
	{
		get
		{
			if (!Campaign.Current.Options.IsHighRebellionEnabled)
			{
				return 35;
			}
			return 60;
		}
	}

	public override int LoyaltyBoostAfterRebellionStartValue => 5;

	public override int MilitiaBoostPercentage => 200;

	public override float ThresholdForNotableRelationBonus => 75f;

	public override int DailyNotableRelationBonus => 1;

	public override int SettlementLoyaltyChangeDueToSecurityThreshold => 60;

	public override int MaximumLoyaltyInSettlement => 100;

	public override int LoyaltyDriftMedium => 50;

	public override float HighSecurityLoyaltyEffect => 1f;

	public override float LowSecurityLoyaltyEffect => -2.5f;

	public override float SettlementOwnerDifferentCultureLoyaltyEffect => -3f;

	public override ExplainedNumber CalculateLoyaltyChange(Town town, bool includeDescriptions = false)
	{
		return CalculateLoyaltyChangeInternal(town, includeDescriptions);
	}

	public override void CalculateGoldGainDueToHighLoyalty(Town town, ref ExplainedNumber explainedNumber)
	{
		float value = MBMath.Map(town.Loyalty, ThresholdForTaxBoost, 100f, 0f, 0.2f);
		explainedNumber.AddFactor(value, LoyaltyText);
	}

	public override void CalculateGoldCutDueToLowLoyalty(Town town, ref ExplainedNumber explainedNumber)
	{
		float value = MBMath.Map(town.Loyalty, ThresholdForHigherTaxCorruption, ThresholdForTaxCorruption, -0.5f, 0f);
		explainedNumber.AddFactor(value, CorruptionText);
	}

	private ExplainedNumber CalculateLoyaltyChangeInternal(Town town, bool includeDescriptions = false)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0f, includeDescriptions);
		GetSettlementLoyaltyChangeDueToFoodStocks(town, ref explainedNumber);
		GetSettlementLoyaltyChangeDueToOwnerCulture(town, ref explainedNumber);
		GetSettlementLoyaltyChangeDueToPolicies(town, ref explainedNumber);
		GetSettlementLoyaltyChangeDueToProjects(town, ref explainedNumber);
		GetSettlementLoyaltyChangeDueToIssues(town, ref explainedNumber);
		GetSettlementLoyaltyChangeDueToSecurity(town, ref explainedNumber);
		GetSettlementLoyaltyChangeDueToNotableRelations(town, ref explainedNumber);
		GetSettlementLoyaltyChangeDueToGovernorPerks(town, ref explainedNumber);
		GetSettlementLoyaltyChangeDueToLoyaltyDrift(town, ref explainedNumber);
		if (town.Governor != null && town.Governor.CurrentSettlement?.Town == town && explainedNumber.ResultNumber > 0f)
		{
			TraitEffectHelper.ApplyTraitEffect(town.Governor, DefaultPersonalityTraitEffects.HonorLoyaltyGainEffect, ref explainedNumber);
		}
		return explainedNumber;
	}

	private void GetSettlementLoyaltyChangeDueToGovernorPerks(Town town, ref ExplainedNumber explainedNumber)
	{
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Leadership.HeroicLeader, town, isPrimaryBonus: true, ref explainedNumber);
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Medicine.PhysicianOfPeople, town, isPrimaryBonus: true, ref explainedNumber);
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Athletics.Durable, town, isPrimaryBonus: false, ref explainedNumber);
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Bow.Discipline, town, isPrimaryBonus: false, ref explainedNumber);
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Riding.WellStraped, town, isPrimaryBonus: false, ref explainedNumber);
		float num = 0f;
		for (int i = 0; i < town.Settlement.Parties.Count; i++)
		{
			MobileParty mobileParty = town.Settlement.Parties[i];
			if (mobileParty.ActualClan != town.OwnerClan)
			{
				continue;
			}
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
		foreach (Hero item in town.Settlement.HeroesWithoutParty)
		{
			if (item.Clan == town.OwnerClan && item.GetPerkValue(DefaultPerks.Charm.Parade))
			{
				num += DefaultPerks.Charm.Parade.PrimaryBonus;
			}
		}
		if (num > 0f)
		{
			explainedNumber.Add(num, DefaultPerks.Charm.Parade.Name);
		}
	}

	private void GetSettlementLoyaltyChangeDueToNotableRelations(Town town, ref ExplainedNumber explainedNumber)
	{
		float num = 0f;
		foreach (Hero notable in town.Settlement.Notables)
		{
			if (notable.SupporterOf != null)
			{
				if (notable.SupporterOf == town.Settlement.OwnerClan)
				{
					num += 0.5f;
				}
				else if (town.MapFaction.IsAtWarWith(notable.SupporterOf.MapFaction))
				{
					num += -0.5f;
				}
			}
		}
		if (!num.ApproximatelyEqualsTo(0f))
		{
			explainedNumber.Add(num, NotableText);
		}
	}

	private void GetSettlementLoyaltyChangeDueToOwnerCulture(Town town, ref ExplainedNumber explainedNumber)
	{
		if (town.Settlement.OwnerClan.Culture != town.Settlement.Culture)
		{
			explainedNumber.Add(SettlementOwnerDifferentCultureLoyaltyEffect, CultureText);
		}
	}

	private void GetSettlementLoyaltyChangeDueToPolicies(Town town, ref ExplainedNumber explainedNumber)
	{
		Kingdom kingdom = town.Owner.Settlement.OwnerClan.Kingdom;
		if (kingdom == null)
		{
			return;
		}
		if (kingdom.ActivePolicies.Contains(DefaultPolicies.Citizenship))
		{
			if (town.Settlement.OwnerClan.Culture == town.Settlement.Culture)
			{
				explainedNumber.Add(0.5f, DefaultPolicies.Citizenship.Name);
			}
			else
			{
				explainedNumber.Add(-0.5f, DefaultPolicies.Citizenship.Name);
			}
		}
		if (kingdom.ActivePolicies.Contains(DefaultPolicies.HuntingRights))
		{
			explainedNumber.Add(-0.2f, DefaultPolicies.HuntingRights.Name);
		}
		if (kingdom.ActivePolicies.Contains(DefaultPolicies.GrazingRights))
		{
			explainedNumber.Add(0.5f, DefaultPolicies.GrazingRights.Name);
		}
		if (kingdom.ActivePolicies.Contains(DefaultPolicies.TrialByJury))
		{
			explainedNumber.Add(0.5f, DefaultPolicies.TrialByJury.Name);
		}
		if (town.IsTown && kingdom.ActivePolicies.Contains(DefaultPolicies.ImperialTowns))
		{
			if (kingdom.RulingClan == town.Settlement.OwnerClan)
			{
				explainedNumber.Add(1f, DefaultPolicies.ImperialTowns.Name);
			}
			else
			{
				explainedNumber.Add(-0.3f, DefaultPolicies.ImperialTowns.Name);
			}
		}
		if (kingdom.ActivePolicies.Contains(DefaultPolicies.ForgivenessOfDebts))
		{
			explainedNumber.Add(2f, DefaultPolicies.ForgivenessOfDebts.Name);
		}
		if (kingdom.ActivePolicies.Contains(DefaultPolicies.TribunesOfThePeople) && town.IsTown)
		{
			explainedNumber.Add(1f, DefaultPolicies.TribunesOfThePeople.Name);
		}
		if (kingdom.ActivePolicies.Contains(DefaultPolicies.DebasementOfTheCurrency))
		{
			explainedNumber.Add(-1f, DefaultPolicies.DebasementOfTheCurrency.Name);
		}
	}

	private void GetSettlementLoyaltyChangeDueToFoodStocks(Town town, ref ExplainedNumber explainedNumber)
	{
		if (town.Settlement.IsStarving)
		{
			float num = -1f;
			if (town.Settlement.Party.DaysStarving > 14f)
			{
				num += -1f;
			}
			explainedNumber.Add(num, StarvingText);
		}
	}

	private void GetSettlementLoyaltyChangeDueToSecurity(Town town, ref ExplainedNumber explainedNumber)
	{
		float value = ((town.Security > (float)SettlementLoyaltyChangeDueToSecurityThreshold) ? MBMath.Map(town.Security, SettlementLoyaltyChangeDueToSecurityThreshold, MaximumLoyaltyInSettlement, 0f, HighSecurityLoyaltyEffect) : MBMath.Map(town.Security, 0f, SettlementLoyaltyChangeDueToSecurityThreshold, LowSecurityLoyaltyEffect, 0f));
		explainedNumber.Add(value, SecurityText);
	}

	private void GetSettlementLoyaltyChangeDueToProjects(Town town, ref ExplainedNumber explainedNumber)
	{
		town.AddEffectOfBuildings(BuildingEffectEnum.Loyalty, ref explainedNumber);
	}

	private void GetSettlementLoyaltyChangeDueToIssues(Town town, ref ExplainedNumber explainedNumber)
	{
		Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(DefaultIssueEffects.SettlementLoyalty, town.Settlement, ref explainedNumber);
	}

	private void GetSettlementLoyaltyChangeDueToLoyaltyDrift(Town town, ref ExplainedNumber explainedNumber)
	{
		explainedNumber.Add(-0.1f * (town.Loyalty - (float)LoyaltyDriftMedium), LoyaltyDriftText);
	}
}
