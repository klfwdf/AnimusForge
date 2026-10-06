using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;

public class AiArmyMemberBehavior : CampaignBehaviorBase
{
	private float FollowingArmyLeaderMaxScore => 20f;

	private float FollowingArmyLeaderMinScore => FollowingArmyLeaderMaxScore * 0.5f;

	private float ArmyLeaderIsUnreachableScore => 0.02475f;

	public override void RegisterEvents()
	{
		CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, AiHourlyTick);
		CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeEventStarted);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnSiegeEventStarted(SiegeEvent siegeEvent)
	{
		for (int i = 0; i < siegeEvent.BesiegedSettlement.Parties.Count; i++)
		{
			if (siegeEvent.BesiegedSettlement.Parties[i].IsLordParty)
			{
				siegeEvent.BesiegedSettlement.Parties[i].SetMoveModeHold();
			}
		}
	}

	public void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
	{
		if (mobileParty.Army == null || mobileParty.Army.LeaderParty == mobileParty || (mobileParty.AttachedTo == null && ((mobileParty.Army.LeaderParty.CurrentSettlement != null && mobileParty.Army.LeaderParty.CurrentSettlement.IsUnderSiege && (mobileParty.Army.LeaderParty.CurrentSettlement.SiegeEvent.IsBlockadeActive || !mobileParty.HasNavalNavigationCapability)) || (mobileParty.CurrentSettlement != null && mobileParty.CurrentSettlement.IsUnderSiege))))
		{
			return;
		}
		MobileParty.NavigationType bestNavigationType = MobileParty.NavigationType.None;
		float bestNavigationDistance = float.MaxValue;
		bool isTargetingPort = false;
		bool isFromPort = false;
		if (mobileParty.Army.LeaderParty.CurrentSettlement != null)
		{
			SiegeEvent siegeEvent = mobileParty.Army.LeaderParty.CurrentSettlement.SiegeEvent;
			bool num = siegeEvent == null;
			bool flag = mobileParty.HasNavalNavigationCapability && mobileParty.Army.LeaderParty.CurrentSettlement.HasPort && (siegeEvent == null || (!siegeEvent.IsBlockadeActive && mobileParty.HasNavalNavigationCapability));
			if (num)
			{
				AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, mobileParty.Army.LeaderParty.CurrentSettlement, isTargetingPort: false, out bestNavigationType, out bestNavigationDistance, out isFromPort);
			}
			if (flag)
			{
				AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, mobileParty.Army.LeaderParty.CurrentSettlement, isTargetingPort: true, out var bestNavigationType2, out var bestNavigationDistance2, out var isFromPort2);
				if (bestNavigationDistance2 < bestNavigationDistance)
				{
					bestNavigationType = bestNavigationType2;
					bestNavigationDistance = bestNavigationDistance2;
					isFromPort = isFromPort2;
					isTargetingPort = true;
				}
			}
		}
		else
		{
			AiHelper.GetBestNavigationTypeAndDistanceOfMobilePartyForMobileParty(mobileParty, mobileParty.Army.LeaderParty, out bestNavigationType, out bestNavigationDistance);
		}
		if (bestNavigationType != MobileParty.NavigationType.None)
		{
			float num2 = FollowingArmyLeaderMaxScore;
			float num3 = 1f;
			float num4 = (mobileParty.Army.LeaderParty.IsMainParty ? Campaign.Current.Models.ArmyManagementCalculationModel.PlayerMobilePartySizeRatioToCallToArmy : Campaign.Current.Models.ArmyManagementCalculationModel.AIMobilePartySizeRatioToCallToArmy);
			if ((float)mobileParty.GetNumDaysForFoodToLast() < Campaign.Current.Models.ArmyManagementCalculationModel.MinimumNeededFoodInDaysToCallToArmy || mobileParty.PartySizeRatio < num4)
			{
				num2 = FollowingArmyLeaderMinScore;
				float num5 = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(bestNavigationType) * 0.5f;
				if (num5 > bestNavigationDistance)
				{
					num3 = MathF.Clamp(num5 / (bestNavigationDistance + 0.1f), 1f, FollowingArmyLeaderMaxScore / FollowingArmyLeaderMinScore);
				}
			}
			AIBehaviorData item = new AIBehaviorData(mobileParty.Army.LeaderParty, AiBehavior.EscortParty, bestNavigationType, willGatherArmy: false, isFromPort, isTargetingPort);
			float item2 = MathF.Clamp(num2 * num3, FollowingArmyLeaderMinScore, FollowingArmyLeaderMaxScore);
			p.AddBehaviorScore((item, item2));
		}
		else
		{
			AIBehaviorData item3 = new AIBehaviorData(mobileParty.Army.LeaderParty, AiBehavior.EscortParty, mobileParty.NavigationCapability, willGatherArmy: false, isFromPort, isTargetingPort: false);
			float armyLeaderIsUnreachableScore = ArmyLeaderIsUnreachableScore;
			p.AddBehaviorScore((item3, armyLeaderIsUnreachableScore));
		}
	}
}
