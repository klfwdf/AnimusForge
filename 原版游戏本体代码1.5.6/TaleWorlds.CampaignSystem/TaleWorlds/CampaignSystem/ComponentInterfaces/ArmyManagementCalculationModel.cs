using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class ArmyManagementCalculationModel : MBGameModel<ArmyManagementCalculationModel>
{
	public abstract float AIMobilePartySizeRatioToCallToArmy { get; }

	public abstract float PlayerMobilePartySizeRatioToCallToArmy { get; }

	public abstract float MinimumNeededFoodInDaysToCallToArmy { get; }

	public abstract float MaximumDistanceToCallToArmy { get; }

	public abstract int InfluenceValuePerGold { get; }

	public abstract int AverageCallToArmyCost { get; }

	public abstract int CohesionThresholdForDispersion { get; }

	public abstract float MaximumWaitTime { get; }

	public abstract bool CanPlayerCreateArmy(out TextObject disabledReason);

	public abstract int CalculatePartyInfluenceCost(MobileParty armyLeaderParty, MobileParty party);

	public abstract float DailyBeingAtArmyInfluenceAward(MobileParty armyMemberParty);

	public abstract bool CanLordCreateArmy(MobileParty leaderParty, out MBList<MobileParty> possibleArmyMembers);

	public abstract int CalculateTotalInfluenceCost(Army army, float percentage);

	public abstract float GetPartySizeScore(MobileParty party);

	public abstract bool CheckPartyEligibility(MobileParty party, out TextObject explanation);

	public abstract int GetPartyRelation(Hero hero);

	public abstract ExplainedNumber CalculateDailyCohesionChange(Army army, bool includeDescriptions = false);

	public abstract int CalculateNewCohesion(Army army, PartyBase newParty, int calculatedCohesion, int sign);

	public abstract int GetCohesionBoostInfluenceCost(Army army, int percentageToBoost = 100);
}
