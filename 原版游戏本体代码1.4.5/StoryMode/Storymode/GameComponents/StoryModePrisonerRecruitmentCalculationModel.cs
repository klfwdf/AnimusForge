using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;

namespace StoryMode.GameComponents;

public class StoryModePrisonerRecruitmentCalculationModel : PrisonerRecruitmentCalculationModel
{
	public override int CalculateRecruitableNumber(PartyBase party, CharacterObject character)
	{
		return base.BaseModel.CalculateRecruitableNumber(party, character);
	}

	public override ExplainedNumber GetConformityChangePerHour(PartyBase party, CharacterObject character)
	{
		if (party == PartyBase.MainParty && !StoryModeManager.Current.MainStoryLine.TutorialPhase.IsCompleted)
		{
			return new ExplainedNumber(0f, includeDescriptions: false, null);
		}
		return base.BaseModel.GetConformityChangePerHour(party, character);
	}

	public override int GetConformityNeededToRecruitPrisoner(CharacterObject character)
	{
		return base.BaseModel.GetConformityNeededToRecruitPrisoner(character);
	}

	public override int GetPrisonerRecruitmentMoraleEffect(PartyBase party, CharacterObject character, int num)
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
}
