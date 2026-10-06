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
		if (troopToBoost.Tier <= 3)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.FerventAttacker, party.MobileParty, isPrimaryBonus: false, ref stat);
		}
		if (troopToBoost.Tier >= 4)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.StoutDefender, party.MobileParty, isPrimaryBonus: false, ref stat);
		}
		if (troopToBoost.Occupation != Occupation.Bandit)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.LoyaltyAndHonor, party.MobileParty, isPrimaryBonus: false, ref stat);
		}
		if (troopToBoost.IsInfantry)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.LeadByExample, party.MobileParty, isPrimaryBonus: true, ref stat);
		}
		if (troopToBoost.IsRanged)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.TrustedCommander, party.MobileParty, isPrimaryBonus: true, ref stat);
		}
		if (troopToBoost.Occupation == Occupation.Bandit)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Roguery.Promises, party.MobileParty, isPrimaryBonus: false, ref stat);
		}
		return stat;
	}

	public override float GetPrisonerRecruitmentMoraleEffect(PartyBase party, CharacterObject character, int num)
	{
		Hero perkOwnerHero = null;
		Hero perkOwnerHero2 = null;
		bool num2 = character.Culture == party.LeaderHero?.Culture && party.MobileParty != null && party.MobileParty.HasPerk(DefaultPerks.Leadership.Presence, out perkOwnerHero, checkSecondaryRole: true);
		bool flag = character.Occupation == Occupation.Bandit && party.MobileParty != null && party.MobileParty.HasPerk(DefaultPerks.Roguery.TwoFaced, out perkOwnerHero2, checkSecondaryRole: true);
		if (num2 || flag)
		{
			return 0f;
		}
		float num3 = ((character.Occupation != Occupation.Bandit) ? (-1f) : (-2f));
		float num4 = num3 * (float)num;
		if (party.LeaderHero != null)
		{
			float traitEffectBonus = TraitEffectHelper.GetTraitEffectBonus(party.LeaderHero, DefaultPersonalityTraitEffects.HonorRecruitPenaltyReductionEffect);
			if (traitEffectBonus != 0f)
			{
				num4 *= 1f + traitEffectBonus;
			}
		}
		return num4;
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
				return ShouldRecruitDueToPresencePerk(party.MobileParty);
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

	private static bool ShouldRecruitDueToPresencePerk(MobileParty mobileParty)
	{
		Hero perkOwnerHero = null;
		if (mobileParty.HasPerk(DefaultPerks.Leadership.Presence, out perkOwnerHero, checkSecondaryRole: true))
		{
			return true;
		}
		return false;
	}
}
