using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000142 RID: 322
	public class DefaultPrisonerRecruitmentCalculationModel : PrisonerRecruitmentCalculationModel
	{
		// Token: 0x06001977 RID: 6519 RVA: 0x0007F120 File Offset: 0x0007D320
		public override int GetConformityNeededToRecruitPrisoner(CharacterObject character)
		{
			return (character.Level + 6) * (character.Level + 6) - 10;
		}

		// Token: 0x06001978 RID: 6520 RVA: 0x0007F138 File Offset: 0x0007D338
		public override ExplainedNumber GetConformityChangePerHour(PartyBase party, CharacterObject troopToBoost)
		{
			ExplainedNumber result = new ExplainedNumber(10f, false, null);
			if (party.LeaderHero != null)
			{
				result.Add((float)party.LeaderHero.GetSkillValue(DefaultSkills.Leadership) * 0.05f, null, null);
			}
			if (troopToBoost.Tier <= 3 && party.MobileParty != null && !party.MobileParty.IsCurrentlyAtSea)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.FerventAttacker, party.MobileParty, false, ref result, false);
			}
			if (troopToBoost.Tier >= 4 && !party.MobileParty.IsCurrentlyAtSea && party.MobileParty.HasPerk(DefaultPerks.Leadership.StoutDefender, true))
			{
				result.AddFactor(DefaultPerks.Leadership.StoutDefender.SecondaryBonus, null);
			}
			if (troopToBoost.Occupation != Occupation.Bandit && !party.MobileParty.IsCurrentlyAtSea && party.MobileParty.HasPerk(DefaultPerks.Leadership.LoyaltyAndHonor, true))
			{
				result.AddFactor(DefaultPerks.Leadership.LoyaltyAndHonor.SecondaryBonus, null);
			}
			if (troopToBoost.IsInfantry)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.LeadByExample, party.MobileParty, true, ref result, party.MobileParty.IsCurrentlyAtSea);
			}
			if (troopToBoost.IsRanged)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.TrustedCommander, party.MobileParty, true, ref result, party.MobileParty.IsCurrentlyAtSea);
			}
			if (troopToBoost.Occupation == Occupation.Bandit && !party.MobileParty.IsCurrentlyAtSea && party.MobileParty.HasPerk(DefaultPerks.Roguery.Promises, true))
			{
				result.AddFactor(DefaultPerks.Roguery.Promises.SecondaryBonus, null);
			}
			return result;
		}

		// Token: 0x06001979 RID: 6521 RVA: 0x0007F2B0 File Offset: 0x0007D4B0
		public override int GetPrisonerRecruitmentMoraleEffect(PartyBase party, CharacterObject character, int num)
		{
			CultureObject culture = character.Culture;
			Hero leaderHero = party.LeaderHero;
			if (culture == ((leaderHero != null) ? leaderHero.Culture : null))
			{
				MobileParty mobileParty = party.MobileParty;
				if (mobileParty != null && mobileParty.HasPerk(DefaultPerks.Leadership.Presence, true))
				{
					return 0;
				}
			}
			if (character.Occupation == Occupation.Bandit)
			{
				MobileParty mobileParty2 = party.MobileParty;
				if (mobileParty2 != null && mobileParty2.HasPerk(DefaultPerks.Roguery.TwoFaced, true))
				{
					return 0;
				}
			}
			int num2;
			if (character.Occupation == Occupation.Bandit)
			{
				num2 = -2;
			}
			else
			{
				num2 = -1;
			}
			return num2 * num;
		}

		// Token: 0x0600197A RID: 6522 RVA: 0x0007F32C File Offset: 0x0007D52C
		public override bool IsPrisonerRecruitable(PartyBase party, CharacterObject character, out int conformityNeeded)
		{
			if (!character.IsRegular || character.Tier > Campaign.Current.Models.CharacterStatsModel.MaxCharacterTier)
			{
				conformityNeeded = 0;
				return false;
			}
			int elementXp = party.MobileParty.PrisonRoster.GetElementXp(character);
			conformityNeeded = this.GetConformityNeededToRecruitPrisoner(character);
			return elementXp >= conformityNeeded;
		}

		// Token: 0x0600197B RID: 6523 RVA: 0x0007F384 File Offset: 0x0007D584
		public override bool ShouldPartyRecruitPrisoners(PartyBase party)
		{
			return party.IsMobile && (party.MobileParty.Morale > 30f || party.MobileParty.HasPerk(DefaultPerks.Leadership.Presence, true)) && party.PartySizeLimit > party.MobileParty.MemberRoster.TotalManCount && !party.MobileParty.IsWageLimitExceeded() && !party.MobileParty.IsPatrolParty;
		}

		// Token: 0x0600197C RID: 6524 RVA: 0x0007F3F4 File Offset: 0x0007D5F4
		public override int CalculateRecruitableNumber(PartyBase party, CharacterObject character)
		{
			if (character.IsHero || party.PrisonRoster.Count == 0 || party.PrisonRoster.TotalRegulars <= 0)
			{
				return 0;
			}
			int conformityNeededToRecruitPrisoner = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.GetConformityNeededToRecruitPrisoner(character);
			int elementXp = party.PrisonRoster.GetElementXp(character);
			int elementNumber = party.PrisonRoster.GetElementNumber(character);
			return MathF.Min(elementXp / conformityNeededToRecruitPrisoner, elementNumber);
		}
	}
}
