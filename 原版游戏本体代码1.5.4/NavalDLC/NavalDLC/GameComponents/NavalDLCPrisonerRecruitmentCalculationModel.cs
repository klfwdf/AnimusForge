using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;

namespace NavalDLC.GameComponents;

internal class NavalDLCPrisonerRecruitmentCalculationModel : PrisonerRecruitmentCalculationModel
{
	public override int GetConformityNeededToRecruitPrisoner(CharacterObject character)
	{
		return base.BaseModel.GetConformityNeededToRecruitPrisoner(character);
	}

	public override ExplainedNumber GetConformityChangePerHour(PartyBase party, CharacterObject troopToBoost)
	{
		ExplainedNumber stat = base.BaseModel.GetConformityChangePerHour(party, troopToBoost);
		if (party.IsMobile && troopToBoost.IsPirate())
		{
			PerkHelper.AddPerkBonusForParty(NavalPerks.Mariner.RollingThunder, party.MobileParty, isPrimaryBonus: false, ref stat);
		}
		return stat;
	}

	public override float GetPrisonerRecruitmentMoraleEffect(PartyBase party, CharacterObject character, int num)
	{
		return base.BaseModel.GetPrisonerRecruitmentMoraleEffect(party, character, num);
	}

	public override bool IsPrisonerRecruitable(PartyBase party, CharacterObject character, out int conformityNeeded)
	{
		return base.BaseModel.IsPrisonerRecruitable(party, character, out conformityNeeded);
	}

	public override bool ShouldPartyRecruitPrisoners(PartyBase party)
	{
		return base.BaseModel.ShouldPartyRecruitPrisoners(party);
	}

	public override int CalculateRecruitableNumber(PartyBase party, CharacterObject character)
	{
		return base.BaseModel.CalculateRecruitableNumber(party, character);
	}
}
