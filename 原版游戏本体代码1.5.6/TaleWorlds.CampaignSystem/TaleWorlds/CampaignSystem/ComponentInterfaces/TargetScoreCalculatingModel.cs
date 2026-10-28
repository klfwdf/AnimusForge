using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class TargetScoreCalculatingModel : MBGameModel<TargetScoreCalculatingModel>
{
	public abstract float TravelingToAssignmentFactor { get; }

	public abstract float BesiegingFactor { get; }

	public abstract float AssaultingTownFactor { get; }

	public abstract float RaidingFactor { get; }

	public abstract float DefendingFactor { get; }

	public abstract float GetDefensivePatrollingFactor(bool isNavalPatrolling);

	public abstract float GetOffensivePatrollingFactor(bool isNavalPatrolling);

	public abstract float GetTargetScoreForFaction(Settlement targetSettlement, Army.ArmyTypes missionType, MobileParty mobileParty, float ourStrength);

	public abstract float CalculateDefensivePatrollingScoreForSettlement(Settlement settlement, bool isTargetingPort, MobileParty mobileParty);

	public abstract float CalculateOffensivePatrollingScoreForSettlement(Settlement settlement, bool isTargetingPort, MobileParty mobileParty);

	public abstract float CurrentObjectiveValue(MobileParty mobileParty);
}
