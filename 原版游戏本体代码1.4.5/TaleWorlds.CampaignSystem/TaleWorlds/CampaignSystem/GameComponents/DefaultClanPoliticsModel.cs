using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultClanPoliticsModel : ClanPoliticsModel
{
	private static readonly TextObject _supporterStr = new TextObject("{=RzFyGnWJ}Supporters");

	private static readonly TextObject _crimeStr = new TextObject("{=MvxW9rmf}Criminal");

	private static readonly TextObject _armyMemberStr = new TextObject("{=XAdBVsXV}Clan members in an army");

	private static readonly TextObject _townProjectStr = new TextObject("{=8Yb3IVvb}Settlement Buildings");

	private static readonly TextObject _courtshipPerkStr = new TextObject("{=zgzDwZKZ}Courtship from clan parties");

	private static readonly TextObject _mercenaryStr = new TextObject("{=qcaaJLhx}Mercenary Contract");

	private static readonly TextObject _kingBonusStr = new TextObject("{=JNS46jsG}King bonus");

	public override ExplainedNumber CalculateInfluenceChange(Clan clan, bool includeDescriptions = false)
	{
		ExplainedNumber influenceChange = new ExplainedNumber(0f, includeDescriptions);
		CalculateInfluenceChangeInternal(clan, ref influenceChange);
		return influenceChange;
	}

	private void CalculateInfluenceChangeInternal(Clan clan, ref ExplainedNumber influenceChange)
	{
		if (clan.Leader.GetPerkValue(DefaultPerks.Charm.ImmortalCharm))
		{
			influenceChange.Add(DefaultPerks.Charm.ImmortalCharm.PrimaryBonus, DefaultPerks.Charm.ImmortalCharm.Name);
		}
		if (clan.IsUnderMercenaryService)
		{
			int num = MathF.Ceiling(clan.Influence * (1f / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction()));
			influenceChange.Add(-num, _mercenaryStr);
		}
		float num2 = 0f;
		foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
		{
			MobileParty mobileParty = warPartyComponent.MobileParty;
			if (mobileParty.Army != null && mobileParty.Army.LeaderParty != mobileParty && mobileParty.LeaderHero != null && mobileParty.Army.LeaderParty.LeaderHero != null && warPartyComponent.Clan != mobileParty.Army.LeaderParty.LeaderHero.Clan)
			{
				num2 += Campaign.Current.Models.ArmyManagementCalculationModel.DailyBeingAtArmyInfluenceAward(mobileParty);
			}
		}
		influenceChange.Add(num2, _armyMemberStr);
		if (clan.MapFaction.Leader == clan.Leader && clan.MapFaction.IsKingdomFaction)
		{
			influenceChange.Add(3f, _kingBonusStr);
		}
		foreach (Settlement settlement in clan.Settlements)
		{
			if (settlement.IsFortification)
			{
				settlement.Town.AddEffectOfBuildings(BuildingEffectEnum.Influence, ref influenceChange);
			}
		}
		if (clan == Clan.PlayerClan && clan.MapFaction.MainHeroCrimeRating > 0f)
		{
			int num3 = (int)(clan.MapFaction.MainHeroCrimeRating * -0.5f);
			influenceChange.Add(num3, _crimeStr);
		}
		float num4 = 0f;
		foreach (Hero supporterNotable in clan.SupporterNotables)
		{
			if (supporterNotable.CurrentSettlement != null)
			{
				float influenceBonusToClan = Campaign.Current.Models.NotablePowerModel.GetInfluenceBonusToClan(supporterNotable);
				num4 += influenceBonusToClan;
			}
		}
		if (num4 > 0f)
		{
			influenceChange.Add(num4, _supporterStr);
		}
		if (clan.Kingdom != null && !clan.IsUnderMercenaryService)
		{
			CalculateInfluenceChangeDueToPolicies(clan, ref influenceChange);
		}
		CalculateInfluenceChangeDueToIssues(clan, ref influenceChange);
	}

	private void CalculateInfluenceChangeDueToIssues(Clan clan, ref ExplainedNumber influenceChange)
	{
		Campaign.Current.Models.IssueModel.GetIssueEffectOfClan(DefaultIssueEffects.ClanInfluence, clan, ref influenceChange);
	}

	private void CalculateInfluenceChangeDueToPolicies(Clan clan, ref ExplainedNumber influenceChange)
	{
		if (clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.FeudalInheritance))
		{
			influenceChange.Add(0.1f * (float)clan.Settlements.Count, DefaultPolicies.FeudalInheritance.Name);
		}
		if (clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Serfdom))
		{
			influenceChange.Add(0.2f * (float)clan.Settlements.Count((Settlement t) => t.IsVillage), DefaultPolicies.Serfdom.Name);
		}
		if (clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.LordsPrivyCouncil) && clan.Tier >= 5)
		{
			influenceChange.Add(0.5f, DefaultPolicies.LordsPrivyCouncil.Name);
		}
		if (clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Senate) && clan.Tier >= 3)
		{
			influenceChange.Add(0.5f, DefaultPolicies.Senate.Name);
		}
		if (clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.NobleRetinues) && clan.Tier >= 5)
		{
			influenceChange.Add(-1f, DefaultPolicies.NobleRetinues.Name);
		}
		if (clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Bailiffs))
		{
			int num = clan.Settlements.Count((Settlement settlement) => settlement.IsTown && settlement.Town.Security > 60f);
			influenceChange.Add(num, DefaultPolicies.Bailiffs.Name);
		}
		if (clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.CouncilOfTheCommons))
		{
			float num2 = 0f;
			foreach (Settlement settlement in clan.Settlements)
			{
				num2 += (float)settlement.Notables.Count * 0.1f;
			}
			influenceChange.Add(num2, DefaultPolicies.CouncilOfTheCommons.Name);
		}
		if (clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.TrialByJury))
		{
			influenceChange.Add(-1f, DefaultPolicies.TrialByJury.Name);
		}
		if (clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Lawspeakers))
		{
			float value = ((clan.Leader.GetSkillValue(DefaultSkills.Charm) > 100) ? 1f : (-1f));
			influenceChange.Add(value, DefaultPolicies.Lawspeakers.Name);
		}
		if (clan == clan.Kingdom.RulingClan)
		{
			if (clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.SacredMajesty))
			{
				influenceChange.Add(3f, DefaultPolicies.SacredMajesty.Name);
			}
			if (clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Marshals))
			{
				influenceChange.Add(-1f, DefaultPolicies.Marshals.Name);
			}
		}
		else
		{
			if (clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.SacredMajesty))
			{
				influenceChange.Add(-0.5f, DefaultPolicies.SacredMajesty.Name);
			}
			if (clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.RoyalGuard))
			{
				influenceChange.Add(-0.2f, DefaultPolicies.RoyalGuard.Name);
			}
		}
	}

	public override float CalculateSupportForPolicyInClan(Clan clan, PolicyObject policy)
	{
		float num = 1f;
		float num2 = (float)clan.Leader.GetTraitLevel(DefaultTraits.Authoritarian) * policy.AuthoritarianWeight * num;
		float num3 = (float)clan.Leader.GetTraitLevel(DefaultTraits.Egalitarian) * policy.EgalitarianWeight * num;
		float num4 = (float)clan.Leader.GetTraitLevel(DefaultTraits.Oligarchic) * policy.OligarchicWeight * num;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		if (clan.Tier == 1)
		{
			num5 = policy.EgalitarianWeight;
			num7 = 0f;
			num6 = 0f;
		}
		else if (clan.Tier == 2)
		{
			num5 = policy.EgalitarianWeight;
			num7 = 0f;
			num6 = 0f;
		}
		else if (clan.Tier == 3)
		{
			num5 = policy.EgalitarianWeight;
			num7 = 0f;
			num6 = 0f;
		}
		else if (clan.Tier == 4)
		{
			num5 = 0f;
			num7 = policy.OligarchicWeight;
			num6 = 0f;
		}
		else if (clan.Tier == 5)
		{
			num5 = 0f;
			num7 = policy.OligarchicWeight;
			num6 = 0f;
		}
		else
		{
			num5 = 0f;
			num7 = policy.OligarchicWeight;
			num6 = 0f;
		}
		float num8 = 0f;
		if (clan.Kingdom.RulingClan == clan)
		{
			if (clan.Leader.GetTraitLevel(DefaultTraits.Oligarchic) > 0 || clan.Leader.GetTraitLevel(DefaultTraits.Egalitarian) > 0)
			{
				num8 = -0.5f;
			}
			else if (clan.Leader.GetTraitLevel(DefaultTraits.Authoritarian) > 0)
			{
				num8 = 1f;
			}
		}
		return MathF.Clamp(0f + (num2 + num3 + num4 + num5 + num7 + num6 + num8), -2f, 2f);
	}

	public override float CalculateRelationshipChangeWithSponsor(Clan clan, Clan sponsorClan)
	{
		return MathF.Lerp(-2f, 2f, MathF.Clamp((float)clan.Leader.GetRelation(sponsorClan.Leader) / 60f, 0f, 1f));
	}

	public override int GetInfluenceRequiredToOverrideKingdomDecision(DecisionOutcome popularOption, DecisionOutcome overridingOption, KingdomDecision decision)
	{
		float totalSupportPoints = popularOption.TotalSupportPoints;
		float num = overridingOption.TotalSupportPoints;
		float num2 = 0f;
		if (decision.Kingdom.RulingClan == Clan.PlayerClan)
		{
			if (totalSupportPoints == num + 1f)
			{
				num += 1f;
				num2 += (float)decision.GetInfluenceCostOfSupport(Clan.PlayerClan, Supporter.SupportWeights.SlightlyFavor);
			}
			else if (totalSupportPoints == num + 2f)
			{
				num += 2f;
				num2 += (float)decision.GetInfluenceCostOfSupport(Clan.PlayerClan, Supporter.SupportWeights.StronglyFavor);
			}
			else if (totalSupportPoints > num + 2f)
			{
				num += 3f;
				num2 += (float)decision.GetInfluenceCostOfSupport(Clan.PlayerClan, Supporter.SupportWeights.FullyPush);
			}
		}
		if (totalSupportPoints > num)
		{
			float num3 = (totalSupportPoints - num) / 3f * (float)decision.GetInfluenceCostOfSupport(decision.Kingdom.RulingClan, Supporter.SupportWeights.FullyPush) * 1.4f;
			if (decision.Kingdom.RulingClan != Clan.PlayerClan)
			{
				num3 *= 0.8f;
			}
			num2 += num3;
		}
		num2 = 5 * (int)(num2 / 5f);
		if (totalSupportPoints > num && decision.Kingdom.ActivePolicies.Contains(DefaultPolicies.RoyalPrivilege))
		{
			num2 *= 0.8f;
		}
		return (int)num2;
	}

	public override bool CanHeroBeGovernor(Hero hero)
	{
		if (hero.IsActive && !hero.IsChild && !hero.IsHumanPlayerCharacter && !hero.IsPartyLeader && !hero.IsFugitive && !hero.IsReleased && !hero.IsTraveling && !hero.IsPrisoner && hero.CanBeGovernorOrHavePartyRole() && !hero.IsSpecial)
		{
			return !hero.IsTemplate;
		}
		return false;
	}
}
