using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

namespace StoryMode.GameComponents;

public class StoryModePartyWageModel : PartyWageModel
{
	private const int StoryModeTutorialTroopCost = 50;

	public override int MaxWagePaymentLimit => base.BaseModel.MaxWagePaymentLimit;

	public override int GetCharacterWage(CharacterObject character)
	{
		return base.BaseModel.GetCharacterWage(character);
	}

	public override ExplainedNumber GetTotalWage(MobileParty mobileParty, TroopRoster troopRoster, bool includeDescriptions = false)
	{
		return base.BaseModel.GetTotalWage(mobileParty, troopRoster, includeDescriptions);
	}

	public override ExplainedNumber GetTroopRecruitmentCost(CharacterObject troop, Hero buyerHero, bool withoutItemCost = false)
	{
		if (StoryModeManager.Current.MainStoryLine.TutorialPhase.IsCompleted)
		{
			return base.BaseModel.GetTroopRecruitmentCost(troop, buyerHero, withoutItemCost);
		}
		if (!(troop.StringId == "tutorial_placeholder_volunteer"))
		{
			return base.BaseModel.GetTroopRecruitmentCost(troop, buyerHero, withoutItemCost);
		}
		return new ExplainedNumber(50f);
	}
}
