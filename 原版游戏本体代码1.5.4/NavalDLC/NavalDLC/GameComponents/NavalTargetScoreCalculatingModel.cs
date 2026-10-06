using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace NavalDLC.GameComponents;

public class NavalTargetScoreCalculatingModel : TargetScoreCalculatingModel
{
	public override float TravelingToAssignmentFactor => base.BaseModel.TravelingToAssignmentFactor;

	public override float BesiegingFactor => base.BaseModel.BesiegingFactor;

	public override float AssaultingTownFactor => base.BaseModel.AssaultingTownFactor;

	public override float RaidingFactor => base.BaseModel.RaidingFactor;

	public override float DefendingFactor => base.BaseModel.DefendingFactor;

	public override float GetDefensivePatrollingFactor(bool isNavalPatrolling)
	{
		float num = base.BaseModel.GetDefensivePatrollingFactor(isNavalPatrolling);
		if (isNavalPatrolling)
		{
			num *= 0.66f;
		}
		return num;
	}

	public override float GetOffensivePatrollingFactor(bool isNavalPatrolling)
	{
		return Campaign.Current.Models.TargetScoreCalculatingModel.GetDefensivePatrollingFactor(isNavalPatrolling) * 2f;
	}

	public override float GetTargetScoreForFaction(Settlement targetSettlement, Army.ArmyTypes missionType, MobileParty mobileParty, float ourStrength)
	{
		return base.BaseModel.GetTargetScoreForFaction(targetSettlement, missionType, mobileParty, ourStrength);
	}

	public override float CalculateDefensivePatrollingScoreForSettlement(Settlement settlement, bool isTargetingPort, MobileParty mobileParty)
	{
		if (isTargetingPort)
		{
			if (!mobileParty.HasNavalNavigationCapability || !settlement.HasPort || settlement.MapFaction != mobileParty.MapFaction)
			{
				return 0f;
			}
			float num = ((mobileParty.Food / (0f - mobileParty.FoodChange) > 5f) ? 1f : 0.2f);
			float num2 = ((settlement.OwnerClan == mobileParty.LeaderHero?.Clan) ? 1f : 0.5f);
			bool flag = mobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && !mobileParty.TargetPosition.IsOnLand && mobileParty.TargetSettlement != null && !mobileParty.TargetSettlement.MapFaction.IsAtWarWith(mobileParty.MapFaction);
			bool num3 = mobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && mobileParty.TargetPosition.IsOnLand;
			float num4 = (flag ? 1.35f : 1f);
			float num5 = (3f + settlement.NearbyNavalThreatIntensity - settlement.NearbyNavalAllyIntensity * 1.5f) * (flag ? 1.5f : 1f);
			float num6 = mobileParty.Ships.SumQ((Ship x) => x.HitPoints / x.MaxHitPoints) / (float)mobileParty.Ships.Count;
			float num7 = (num3 ? 0.5f : 1f);
			return num4 * num2 * num5 * num6 * num7 * num * Campaign.Current.Models.TargetScoreCalculatingModel.GetDefensivePatrollingFactor(isNavalPatrolling: true);
		}
		return base.BaseModel.CalculateDefensivePatrollingScoreForSettlement(settlement, isTargetingPort: false, mobileParty);
	}

	public override float CurrentObjectiveValue(MobileParty mobileParty)
	{
		return base.BaseModel.CurrentObjectiveValue(mobileParty);
	}

	public override float CalculateOffensivePatrollingScoreForSettlement(Settlement settlement, bool isTargetingPort, MobileParty mobileParty)
	{
		float num = ((mobileParty.Food / (0f - mobileParty.FoodChange) > 6f) ? 1f : 0.2f);
		bool num2 = mobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && !mobileParty.TargetPosition.IsOnLand && mobileParty.TargetSettlement != null && mobileParty.TargetSettlement == settlement && mobileParty.TargetSettlement.MapFaction.IsAtWarWith(mobileParty.MapFaction);
		bool flag = mobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && mobileParty.TargetPosition.IsOnLand;
		float num3 = (num2 ? 1.2f : 1f);
		float num4 = mobileParty.Ships.SumQ((Ship x) => x.HitPoints / x.MaxHitPoints) / (float)mobileParty.Ships.Count;
		float num5 = (flag ? 0.5f : 1f);
		float num6 = (settlement.IsVillage ? 1.2f : 1f);
		int num7 = 0;
		foreach (WarPartyComponent warPartyComponent in mobileParty.MapFaction.WarPartyComponents)
		{
			if (warPartyComponent.MobileParty != mobileParty && warPartyComponent.MobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && warPartyComponent.MobileParty.TargetSettlement == settlement && warPartyComponent.MobileParty.IsTargetingPort)
			{
				num7++;
			}
		}
		float num8 = MathF.Pow(0.5f, num7);
		return num3 * num4 * num * num5 * num8 * num6 * Campaign.Current.Models.TargetScoreCalculatingModel.GetOffensivePatrollingFactor(isNavalPatrolling: true);
	}
}
