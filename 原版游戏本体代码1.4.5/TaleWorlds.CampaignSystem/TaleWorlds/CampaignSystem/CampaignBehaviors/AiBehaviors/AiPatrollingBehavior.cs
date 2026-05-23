using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;

public class AiPatrollingBehavior : CampaignBehaviorBase
{
	private const float BasePatrolScore = 1.44f;

	private const float MinimumDefensivePatrolDistanceScore = 0.2f;

	private const float MaximumDefensivePatrolDistanceScore = 1f;

	private const float MinimumOffensivePatrolDistanceScore = 0.5f;

	private const float MaximumOffensivePatrolDistanceScore = 1.5f;

	private IDisbandPartyCampaignBehavior _disbandPartyCampaignBehavior;

	public override void RegisterEvents()
	{
		CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, AiHourlyTick);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnShipDestroyedEvent.AddNonSerializedListener(this, OnShipDestroyed);
		CampaignEvents.OnBlockadeActivatedEvent.AddNonSerializedListener(this, OnBlockadeActivated);
		CampaignEvents.OnShipOwnerChangedEvent.AddNonSerializedListener(this, OnShipOwnerChanged);
	}

	private void OnBlockadeActivated(SiegeEvent siegeEvent)
	{
		foreach (MobileParty item in MobileParty.All)
		{
			if (item.DefaultBehavior == AiBehavior.GoToSettlement && item.TargetSettlement == siegeEvent.BesiegedSettlement && item.CurrentSettlement != siegeEvent.BesiegedSettlement)
			{
				item.SetMoveModeHold();
			}
		}
	}

	private void OnShipOwnerChanged(Ship ship, PartyBase oldOwner, ChangeShipOwnerAction.ShipOwnerChangeDetail changeDetail)
	{
		CheckPartyIfNeeded(oldOwner);
	}

	private void OnShipDestroyed(PartyBase owner, Ship ship, DestroyShipAction.ShipDestroyDetail detail)
	{
		CheckPartyIfNeeded(owner);
	}

	private void CheckPartyIfNeeded(PartyBase party)
	{
		if (party != null && party.IsMobile && party.MobileParty.IsLordParty && party.MobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && !party.MobileParty.TargetPosition.IsOnLand && !party.MobileParty.HasNavalNavigationCapability)
		{
			party.MobileParty.SetMoveModeHold();
		}
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		_disbandPartyCampaignBehavior = Campaign.Current.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
	{
		if (mobileParty.IsMilitia || mobileParty.IsCaravan || mobileParty.IsVillager || mobileParty.IsBandit || mobileParty.IsPatrolParty || mobileParty.IsDisbanding || (!mobileParty.MapFaction.IsMinorFaction && !mobileParty.MapFaction.IsKingdomFaction && !mobileParty.MapFaction.Leader.IsLord) || (mobileParty.CurrentSettlement != null && mobileParty.CurrentSettlement.IsUnderSiege) || mobileParty.Army != null || mobileParty.GetNumDaysForFoodToLast() <= 6 || mobileParty.CurrentSettlement?.SiegeEvent != null)
		{
			return;
		}
		float num = 1f;
		if (mobileParty.Army != null)
		{
			float num2 = 0f;
			foreach (MobileParty party in mobileParty.Army.Parties)
			{
				float num3 = PartyBaseHelper.FindPartySizeNormalLimit(party);
				float num4 = party.PartySizeRatio / num3;
				num2 += num4;
			}
			num = num2 / (float)mobileParty.Army.Parties.Count;
		}
		else
		{
			float num5 = PartyBaseHelper.FindPartySizeNormalLimit(mobileParty);
			num = mobileParty.PartySizeRatio / num5;
		}
		float num6 = MathF.Sqrt(MathF.Min(1f, num));
		if (!mobileParty.IsDisbanding)
		{
			IDisbandPartyCampaignBehavior disbandPartyCampaignBehavior = _disbandPartyCampaignBehavior;
			if (disbandPartyCampaignBehavior == null || !disbandPartyCampaignBehavior.IsPartyWaitingForDisband(mobileParty))
			{
				goto IL_0157;
			}
		}
		num6 *= 0.25f;
		goto IL_0157;
		IL_0157:
		CalculateDefensivePatrollingScores(mobileParty, p, num6);
		CalculateOffensiveNavalPatrollingScores(mobileParty, p, num6);
	}

	private void CalculateOffensiveNavalPatrollingScores(MobileParty mobileParty, PartyThinkParams p, float scoreAdjustment)
	{
		if (!mobileParty.HasNavalNavigationCapability || !mobileParty.MapFaction.IsKingdomFaction || mobileParty.MapFaction.Leader == mobileParty.LeaderHero)
		{
			return;
		}
		foreach (IFaction item in mobileParty.MapFaction.FactionsAtWarWith)
		{
			foreach (Settlement settlement in item.Settlements)
			{
				if (settlement.HasPort)
				{
					GetDistanceScoreForOffensiveNavalPatrolling(settlement, mobileParty, out var bestDistanceScore);
					if (bestDistanceScore > 0.5f)
					{
						CalculateOffensiveNavalPatrollingScoreForSettlement(settlement, p, bestDistanceScore);
					}
				}
			}
		}
	}

	private void CalculateDefensivePatrollingScores(MobileParty mobileParty, PartyThinkParams p, float scoreAdjustment)
	{
		if (mobileParty.Party.MapFaction.Settlements.Count > 0)
		{
			SettlementHelper.FindFurthestFortificationToSettlement(mobileParty.MapFaction.Fiefs, MobileParty.NavigationType.Default, mobileParty.MapFaction.FactionMidSettlement, out var furthestDistance);
			{
				foreach (Settlement settlement2 in mobileParty.Party.MapFaction.Settlements)
				{
					if (!settlement2.IsTown && !settlement2.IsVillage)
					{
						continue;
					}
					float bestDistanceScore = float.MaxValue;
					if (settlement2.HasPort && mobileParty.HasNavalNavigationCapability && (!mobileParty.MapFaction.IsKingdomFaction || mobileParty.MapFaction.Leader != mobileParty.LeaderHero))
					{
						GetDistanceScoreForDefensiveNavalPatrolling(settlement2, mobileParty, out bestDistanceScore);
						if (bestDistanceScore > 0.2f)
						{
							CalculateDefensivePatrollingScoreForSettlement(settlement2, p, bestDistanceScore, isNavalPatrolling: true);
						}
					}
					GetDistanceScoreForLandPatrolling(settlement2, mobileParty, furthestDistance, out bestDistanceScore);
					if (bestDistanceScore > 0.2f)
					{
						CalculateDefensivePatrollingScoreForSettlement(settlement2, p, bestDistanceScore, isNavalPatrolling: false);
					}
				}
				return;
			}
		}
		float maxDistance = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(mobileParty.NavigationCapability) * 4f / (Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay) * Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay;
		int num = -1;
		do
		{
			num = SettlementHelper.FindNextSettlementAroundMobileParty(mobileParty, mobileParty.NavigationCapability, maxDistance, num, (Settlement x) => x.IsTown);
			if (num >= 0)
			{
				Settlement settlement = Settlement.All[num];
				float averageDistanceBetweenClosestTwoTownsWithNavigationType = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default);
				float num2 = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty.HomeSettlement, settlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
				if (num2 < averageDistanceBetweenClosestTwoTownsWithNavigationType)
				{
					num2 = averageDistanceBetweenClosestTwoTownsWithNavigationType;
				}
				float num3 = averageDistanceBetweenClosestTwoTownsWithNavigationType * 5f / num2;
				CalculateDefensivePatrollingScoreForSettlement(settlement, p, scoreAdjustment * num3, isNavalPatrolling: false);
			}
		}
		while (num >= 0);
	}

	private void GetDistanceScoreForDefensiveNavalPatrolling(Settlement targetSettlement, MobileParty mobileParty, out float bestDistanceScore)
	{
		bestDistanceScore = 0f;
		AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, targetSettlement, isTargetingPort: true, out var bestNavigationType, out var bestNavigationDistance, out var _);
		if (bestNavigationType != MobileParty.NavigationType.None)
		{
			float averageDistanceBetweenClosestTwoTownsWithNavigationType = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Naval);
			if (bestNavigationDistance > averageDistanceBetweenClosestTwoTownsWithNavigationType)
			{
				bestDistanceScore = -1f;
			}
			else
			{
				bestDistanceScore = MBMath.Map(1f - bestNavigationDistance / averageDistanceBetweenClosestTwoTownsWithNavigationType, 0f, 1f, 0.2f, 1f);
			}
		}
	}

	private void GetDistanceScoreForOffensiveNavalPatrolling(Settlement targetSettlement, MobileParty mobileParty, out float bestDistanceScore)
	{
		bestDistanceScore = 0f;
		AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, targetSettlement, isTargetingPort: true, out var bestNavigationType, out var bestNavigationDistance, out var _);
		if (bestNavigationType != MobileParty.NavigationType.None)
		{
			float num = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Naval) * 3f;
			if (bestNavigationDistance > num)
			{
				bestDistanceScore = -1f;
			}
			else
			{
				bestDistanceScore = MBMath.Map(1f - bestNavigationDistance / num, 0f, 1f, 0.5f, 1.5f);
			}
		}
	}

	private void GetDistanceScoreForLandPatrolling(Settlement targetSettlement, MobileParty mobileParty, float distanceToFurthestAllySettlementToFactionMidSettlement, out float bestDistanceScore)
	{
		float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty.MapFaction.FactionMidSettlement, targetSettlement, isFromPort: false, isTargetingPort: false, mobileParty.NavigationCapability);
		float num = 0f;
		num = ((distanceToFurthestAllySettlementToFactionMidSettlement != 0f) ? (distance / distanceToFurthestAllySettlementToFactionMidSettlement) : 0.5f);
		float num2 = MBMath.Map(num, 0f, 1f, 0.2f, 0.8f);
		if (mobileParty.PartySizeRatio >= num2)
		{
			bestDistanceScore = MBMath.Map(0.8f - (mobileParty.PartySizeRatio - num2), 0f, 0.8f, 0.2f, 1f);
		}
		else
		{
			bestDistanceScore = 0f;
		}
	}

	private void CalculateDefensivePatrollingScoreForSettlement(Settlement settlement, PartyThinkParams p, float scoreAdjustment, bool isNavalPatrolling)
	{
		MobileParty mobilePartyOf = p.MobilePartyOf;
		AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobilePartyOf, settlement, isNavalPatrolling, out var bestNavigationType, out var _, out var isFromPort);
		if (bestNavigationType == MobileParty.NavigationType.None)
		{
			return;
		}
		AIBehaviorData item = new AIBehaviorData(settlement, AiBehavior.PatrolAroundPoint, bestNavigationType, willGatherArmy: false, isFromPort, isNavalPatrolling);
		float num = Campaign.Current.Models.TargetScoreCalculatingModel.CalculateDefensivePatrollingScoreForSettlement(settlement, isNavalPatrolling, mobilePartyOf);
		num *= scoreAdjustment;
		if (num > 0f)
		{
			if (!mobilePartyOf.IsCurrentlyAtSea)
			{
				_ = 2;
			}
			p.AddBehaviorScore((item, 1.44f + num));
		}
	}

	private void CalculateOffensiveNavalPatrollingScoreForSettlement(Settlement settlement, PartyThinkParams p, float scoreAdjustment)
	{
		MobileParty mobilePartyOf = p.MobilePartyOf;
		AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobilePartyOf, settlement, isTargetingPort: true, out var bestNavigationType, out var _, out var isFromPort);
		if (bestNavigationType != MobileParty.NavigationType.None)
		{
			AIBehaviorData item = new AIBehaviorData(settlement, AiBehavior.PatrolAroundPoint, bestNavigationType, willGatherArmy: false, isFromPort, isTargetingPort: true);
			float num = Campaign.Current.Models.TargetScoreCalculatingModel.CalculateOffensivePatrollingScoreForSettlement(settlement, isTargetingPort: true, mobilePartyOf);
			num *= scoreAdjustment;
			if (num > 0f)
			{
				p.AddBehaviorScore((item, 1.44f + num));
			}
		}
	}
}
