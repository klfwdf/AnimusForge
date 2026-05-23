using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPrisonerRecruitmentCalculationModel : PrisonerRecruitmentCalculationModel
{
	private const int AILordMinTierRequirementForRecruitPrisoners = 2;

	public override int GetConformityNeededToRecruitPrisoner(CharacterObject character)
	{
		return (character.Level + 6) * (character.Level + 6) - 10;
	}

	public override ExplainedNumber GetConformityChangePerHour(PartyBase party, CharacterObject troopToBoost)
	{
		ExplainedNumber stat = new ExplainedNumber(10f);
		if (party.LeaderHero != null)
		{
			stat.Add((float)party.LeaderHero.GetSkillValue(DefaultSkills.Leadership) * 0.05f);
		}
		if (troopToBoost.Tier <= 3 && party.MobileParty != null && !party.MobileParty.IsCurrentlyAtSea)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.FerventAttacker, party.MobileParty, isPrimaryBonus: false, ref stat);
		}
		if (troopToBoost.Tier >= 4 && !party.MobileParty.IsCurrentlyAtSea && party.MobileParty.HasPerk(DefaultPerks.Leadership.StoutDefender, checkSecondaryRole: true))
		{
			stat.AddFactor(DefaultPerks.Leadership.StoutDefender.SecondaryBonus);
		}
		if (troopToBoost.Occupation != Occupation.Bandit && !party.MobileParty.IsCurrentlyAtSea && party.MobileParty.HasPerk(DefaultPerks.Leadership.LoyaltyAndHonor, checkSecondaryRole: true))
		{
			stat.AddFactor(DefaultPerks.Leadership.LoyaltyAndHonor.SecondaryBonus);
		}
		if (troopToBoost.IsInfantry)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.LeadByExample, party.MobileParty, isPrimaryBonus: true, ref stat, party.MobileParty.IsCurrentlyAtSea);
		}
		if (troopToBoost.IsRanged)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.TrustedCommander, party.MobileParty, isPrimaryBonus: true, ref stat, party.MobileParty.IsCurrentlyAtSea);
		}
		if (troopToBoost.Occupation == Occupation.Bandit && !party.MobileParty.IsCurrentlyAtSea && party.MobileParty.HasPerk(DefaultPerks.Roguery.Promises, checkSecondaryRole: true))
		{
			stat.AddFactor(DefaultPerks.Roguery.Promises.SecondaryBonus);
		}
		return stat;
	}

	public override int GetPrisonerRecruitmentMoraleEffect(PartyBase party, CharacterObject character, int num)
	{
		if (character.Culture == party.LeaderHero?.Culture)
		{
			MobileParty mobileParty = party.MobileParty;
			if (mobileParty != null && mobileParty.HasPerk(DefaultPerks.Leadership.Presence, checkSecondaryRole: true))
			{
				goto IL_0058;
			}
		}
		if (character.Occupation == Occupation.Bandit)
		{
			MobileParty mobileParty2 = party.MobileParty;
			if (mobileParty2 != null && mobileParty2.HasPerk(DefaultPerks.Roguery.TwoFaced, checkSecondaryRole: true))
			{
				goto IL_0058;
			}
		}
		int num2 = ((character.Occupation != Occupation.Bandit) ? (-1) : (-2));
		return num2 * num;
		IL_0058:
		return 0;
	}

	public override bool IsPrisonerRecruitable(PartyBase party, CharacterObject character, out int conformityNeeded)
	{
		if (!character.IsRegular || character.Tier > Campaign.Current.Models.CharacterStatsModel.MaxCharacterTier || character.Tier < 2 || character.Culture.IsBandit)
		{
			conformityNeeded = 0;
			return false;
		}
		int elementXp = party.MobileParty.PrisonRoster.GetElementXp(character);
		conformityNeeded = GetConformityNeededToRecruitPrisoner(character);
		return elementXp >= conformityNeeded;
	}

	public override bool ShouldPartyRecruitPrisoners(PartyBase party)
	{
		if (party.IsMobile && party.PartySizeLimit > party.MobileParty.MemberRoster.TotalManCount && !party.MobileParty.IsWageLimitExceeded() && !party.MobileParty.IsPatrolParty)
		{
			if (!(party.MobileParty.Morale > 30f))
			{
				return party.MobileParty.HasPerk(DefaultPerks.Leadership.Presence, checkSecondaryRole: true);
			}
			return true;
		}
		return false;
	}

	public override int CalculateRecruitableNumber(PartyBase party, CharacterObject character)
	{
		if (character.IsHero || party.PrisonRoster.Count == 0 || party.PrisonRoster.TotalRegulars <= 0)
		{
			return 0;
		}
		int conformityNeededToRecruitPrisoner = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.GetConformityNeededToRecruitPrisoner(character);
		int elementXp = party.PrisonRoster.GetElementXp(character);
		return MathF.Min(b: party.PrisonRoster.GetElementNumber(character), a: elementXp / conformityNeededToRecruitPrisoner);
	}
}
