using Helpers;
using NavalDLC.CharacterDevelopment;
using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalDLCArmyManagementCalculationModel : ArmyManagementCalculationModel
{
	public override float AIMobilePartySizeRatioToCallToArmy => base.BaseModel.AIMobilePartySizeRatioToCallToArmy;

	public override float PlayerMobilePartySizeRatioToCallToArmy => base.BaseModel.PlayerMobilePartySizeRatioToCallToArmy;

	public override float MinimumNeededFoodInDaysToCallToArmy => base.BaseModel.MinimumNeededFoodInDaysToCallToArmy;

	public override float MaximumDistanceToCallToArmy => base.BaseModel.MaximumDistanceToCallToArmy;

	public override int InfluenceValuePerGold => base.BaseModel.InfluenceValuePerGold;

	public override int AverageCallToArmyCost => base.BaseModel.AverageCallToArmyCost;

	public override int CohesionThresholdForDispersion => base.BaseModel.CohesionThresholdForDispersion;

	public override float MaximumWaitTime => base.BaseModel.MaximumWaitTime;

	public override ExplainedNumber CalculateDailyCohesionChange(Army army, bool includeDescriptions = false)
	{
		ExplainedNumber result = base.BaseModel.CalculateDailyCohesionChange(army, includeDescriptions);
		if (army.LeaderParty != null && !army.LeaderParty.IsCurrentlyAtSea)
		{
			FeatHelper.ApplyCultureFeat(army.LeaderParty.Party, NavalCulturalFeats.NordArmyCohesionFeat, ref result);
		}
		return result;
	}

	public override int CalculateNewCohesion(Army army, PartyBase newParty, int calculatedCohesion, int sign)
	{
		return base.BaseModel.CalculateNewCohesion(army, newParty, calculatedCohesion, sign);
	}

	public override int CalculatePartyInfluenceCost(MobileParty armyLeaderParty, MobileParty party)
	{
		return base.BaseModel.CalculatePartyInfluenceCost(armyLeaderParty, party);
	}

	public override bool CanLordCreateArmy(MobileParty leaderParty, out MBList<MobileParty> possibleArmyMembers)
	{
		return base.BaseModel.CanLordCreateArmy(leaderParty, out possibleArmyMembers);
	}

	public override int CalculateTotalInfluenceCost(Army army, float percentage)
	{
		return base.BaseModel.CalculateTotalInfluenceCost(army, percentage);
	}

	public override bool CanPlayerCreateArmy(out TextObject disabledReason)
	{
		if (NavalStorylineData.IsNavalStoryLineActive() || Campaign.Current.CurrentMenuContext?.GameMenu?.StringId == "naval_storyline_outside_town")
		{
			disabledReason = new TextObject("{=lwbwTg5b}You can't perform this action during this time.");
			return false;
		}
		return base.BaseModel.CanPlayerCreateArmy(out disabledReason);
	}

	public override bool CheckPartyEligibility(MobileParty party, out TextObject explanation)
	{
		return base.BaseModel.CheckPartyEligibility(party, out explanation);
	}

	public override float DailyBeingAtArmyInfluenceAward(MobileParty armyMemberParty)
	{
		return base.BaseModel.DailyBeingAtArmyInfluenceAward(armyMemberParty);
	}

	public override int GetCohesionBoostInfluenceCost(Army army, int percentageToBoost = 100)
	{
		return base.BaseModel.GetCohesionBoostInfluenceCost(army, percentageToBoost);
	}

	public override int GetPartyRelation(Hero hero)
	{
		return base.BaseModel.GetPartyRelation(hero);
	}

	public override float GetPartySizeScore(MobileParty party)
	{
		return base.BaseModel.GetPartySizeScore(party);
	}
}
