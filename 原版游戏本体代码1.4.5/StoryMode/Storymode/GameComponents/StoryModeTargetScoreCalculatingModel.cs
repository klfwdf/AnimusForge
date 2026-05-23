using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace StoryMode.GameComponents;

public class StoryModeTargetScoreCalculatingModel : TargetScoreCalculatingModel
{
	public override float TravelingToAssignmentFactor => base.BaseModel.TravelingToAssignmentFactor;

	public override float BesiegingFactor => base.BaseModel.BesiegingFactor;

	public override float AssaultingTownFactor => base.BaseModel.AssaultingTownFactor;

	public override float RaidingFactor => base.BaseModel.RaidingFactor;

	public override float DefendingFactor => base.BaseModel.DefendingFactor;

	public override float GetDefensivePatrollingFactor(bool isNavalPatrolling)
	{
		return base.BaseModel.GetDefensivePatrollingFactor(isNavalPatrolling);
	}

	public override float GetOffensivePatrollingFactor(bool isNavalPatrolling)
	{
		return base.BaseModel.GetOffensivePatrollingFactor(isNavalPatrolling);
	}

	public override float CalculateDefensivePatrollingScoreForSettlement(Settlement settlement, bool isTargetingPort, MobileParty mobileParty)
	{
		return base.BaseModel.CalculateDefensivePatrollingScoreForSettlement(settlement, isTargetingPort, mobileParty);
	}

	public override float CalculateOffensivePatrollingScoreForSettlement(Settlement settlement, bool isTargetingPort, MobileParty mobileParty)
	{
		return base.BaseModel.CalculateOffensivePatrollingScoreForSettlement(settlement, isTargetingPort, mobileParty);
	}

	public override float CurrentObjectiveValue(MobileParty mobileParty)
	{
		return base.BaseModel.CurrentObjectiveValue(mobileParty);
	}

	public override float GetTargetScoreForFaction(Settlement targetSettlement, Army.ArmyTypes missionType, MobileParty mobileParty, float ourStrength)
	{
		if (missionType == Army.ArmyTypes.Raider && targetSettlement != null && targetSettlement.StringId == "village_ES3_2" && TutorialPhase.Instance != null && !TutorialPhase.Instance.IsCompleted)
		{
			return 0f;
		}
		return base.BaseModel.GetTargetScoreForFaction(targetSettlement, missionType, mobileParty, ourStrength);
	}
}
