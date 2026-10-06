using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions;

public static class SetPartyAiAction
{
	private enum SetPartyAiActionDetail
	{
		GoToSettlement,
		PatrolAroundSettlement,
		PatrolAroundPoint,
		RaidSettlement,
		BesiegeSettlement,
		EngageParty,
		GoAroundParty,
		DefendParty,
		EscortParty,
		MoveToNearestLand
	}

	private static void ApplyInternal(MobileParty owner, Settlement settlement, MobileParty mobileParty, CampaignVec2 position, SetPartyAiActionDetail detail, MobileParty.NavigationType navigationType, bool isFromPort, bool isTargetingPort)
	{
		switch (detail)
		{
		case SetPartyAiActionDetail.GoToSettlement:
			if (owner.DefaultBehavior != AiBehavior.GoToSettlement || owner.TargetSettlement != settlement || navigationType != owner.DesiredAiNavigationType || owner.IsTargetingPort != isTargetingPort || owner.StartTransitionNextFrameToExitFromPort != isFromPort)
			{
				if (isFromPort && !owner.IsTransitionInProgress)
				{
					owner.StartTransitionNextFrameToExitFromPort = true;
				}
				owner.SetMoveGoToSettlement(settlement, navigationType, isTargetingPort);
			}
			if (owner.Army != null && owner.Army.LeaderParty == owner)
			{
				owner.Army.ArmyType = Army.ArmyTypes.Defender;
				owner.Army.AiBehaviorObject = settlement;
			}
			break;
		case SetPartyAiActionDetail.PatrolAroundSettlement:
			if (owner.DefaultBehavior != AiBehavior.PatrolAroundPoint || owner.TargetSettlement != settlement || navigationType != owner.DesiredAiNavigationType || owner.IsTargetingPort != isTargetingPort || owner.StartTransitionNextFrameToExitFromPort != isFromPort)
			{
				if (isFromPort && !owner.IsTransitionInProgress)
				{
					owner.StartTransitionNextFrameToExitFromPort = true;
				}
				owner.SetMovePatrolAroundSettlement(settlement, navigationType, isTargetingPort);
			}
			if (owner.Army != null && owner.Army.LeaderParty == owner)
			{
				owner.Army.ArmyType = Army.ArmyTypes.Defender;
				owner.Army.AiBehaviorObject = settlement;
			}
			break;
		case SetPartyAiActionDetail.RaidSettlement:
			if (owner.DefaultBehavior != AiBehavior.RaidSettlement || owner.TargetSettlement != settlement || navigationType != owner.DesiredAiNavigationType || owner.StartTransitionNextFrameToExitFromPort != isFromPort)
			{
				if (isFromPort && !owner.IsTransitionInProgress)
				{
					owner.StartTransitionNextFrameToExitFromPort = true;
				}
				owner.SetMoveRaidSettlement(settlement, navigationType, isTargetingPort);
				if (owner.Army != null && owner.Army.LeaderParty == owner)
				{
					owner.Army.ArmyType = Army.ArmyTypes.Raider;
					owner.Army.AiBehaviorObject = settlement;
				}
			}
			break;
		case SetPartyAiActionDetail.BesiegeSettlement:
			if (owner.DefaultBehavior != AiBehavior.BesiegeSettlement || owner.TargetSettlement != settlement || navigationType != owner.DesiredAiNavigationType || owner.StartTransitionNextFrameToExitFromPort != isFromPort)
			{
				if (isFromPort && !owner.IsTransitionInProgress)
				{
					owner.StartTransitionNextFrameToExitFromPort = true;
				}
				owner.SetMoveBesiegeSettlement(settlement, navigationType);
				if (owner.Army != null && owner.Army.LeaderParty == owner)
				{
					owner.Army.ArmyType = Army.ArmyTypes.Besieger;
					owner.Army.AiBehaviorObject = settlement;
				}
			}
			break;
		case SetPartyAiActionDetail.GoAroundParty:
			if (owner.DefaultBehavior != AiBehavior.GoAroundParty || owner != mobileParty || navigationType != owner.DesiredAiNavigationType || owner.StartTransitionNextFrameToExitFromPort != isFromPort)
			{
				if (isFromPort && !owner.IsTransitionInProgress)
				{
					owner.StartTransitionNextFrameToExitFromPort = true;
				}
				owner.SetMoveGoAroundParty(mobileParty, navigationType);
			}
			break;
		case SetPartyAiActionDetail.EngageParty:
			if (owner.DefaultBehavior != AiBehavior.EngageParty || owner != mobileParty || navigationType != owner.DesiredAiNavigationType || owner.StartTransitionNextFrameToExitFromPort != isFromPort)
			{
				if (isFromPort && !owner.IsTransitionInProgress)
				{
					owner.StartTransitionNextFrameToExitFromPort = true;
				}
				owner.SetMoveEngageParty(mobileParty, navigationType);
			}
			break;
		case SetPartyAiActionDetail.DefendParty:
			if (owner.DefaultBehavior != AiBehavior.DefendSettlement || owner != mobileParty || navigationType != owner.DesiredAiNavigationType || owner.StartTransitionNextFrameToExitFromPort != isFromPort || owner.IsTargetingPort != isTargetingPort)
			{
				if (isFromPort && !owner.IsTransitionInProgress)
				{
					owner.StartTransitionNextFrameToExitFromPort = true;
				}
				owner.SetMoveDefendSettlement(settlement, isTargetingPort, navigationType);
				if (owner.Army != null && owner.Army.LeaderParty == owner)
				{
					owner.Army.ArmyType = Army.ArmyTypes.Defender;
					owner.Army.AiBehaviorObject = settlement;
				}
			}
			break;
		case SetPartyAiActionDetail.EscortParty:
			if (owner.DefaultBehavior != AiBehavior.EscortParty || owner.TargetParty != mobileParty || navigationType != owner.DesiredAiNavigationType || owner.StartTransitionNextFrameToExitFromPort != isFromPort || owner.IsTargetingPort != isTargetingPort)
			{
				if (isFromPort && !owner.IsTransitionInProgress)
				{
					owner.StartTransitionNextFrameToExitFromPort = true;
				}
				owner.SetMoveEscortParty(mobileParty, navigationType, isTargetingPort);
			}
			break;
		case SetPartyAiActionDetail.MoveToNearestLand:
			if (owner.DefaultBehavior != AiBehavior.MoveToNearestLandOrPort)
			{
				owner.SetMoveToNearestLand(settlement);
			}
			break;
		case SetPartyAiActionDetail.PatrolAroundPoint:
			if (owner.DefaultBehavior != AiBehavior.PatrolAroundPoint || navigationType != owner.DesiredAiNavigationType)
			{
				owner.SetMovePatrolAroundPoint(position, navigationType);
			}
			break;
		}
	}

	public static void GetActionForVisitingSettlement(MobileParty owner, Settlement settlement, MobileParty.NavigationType navigationType, bool isFromPort, bool isTargetingPort)
	{
		ApplyInternal(owner, settlement, null, CampaignVec2.Zero, SetPartyAiActionDetail.GoToSettlement, navigationType, isFromPort, isTargetingPort);
	}

	public static void GetActionForPatrollingAroundSettlement(MobileParty owner, Settlement settlement, MobileParty.NavigationType navigationType, bool isFromPort, bool isTargetingPort)
	{
		ApplyInternal(owner, settlement, null, CampaignVec2.Zero, SetPartyAiActionDetail.PatrolAroundSettlement, navigationType, isFromPort, isTargetingPort);
	}

	public static void GetActionForPatrollingAroundPoint(MobileParty owner, CampaignVec2 position, MobileParty.NavigationType navigationType, bool isFromPort)
	{
		ApplyInternal(owner, null, null, position, SetPartyAiActionDetail.PatrolAroundPoint, navigationType, isFromPort, isTargetingPort: false);
	}

	public static void GetActionForRaidingSettlement(MobileParty owner, Settlement settlement, MobileParty.NavigationType navigationType, bool isFromPort, bool isTargetingPort)
	{
		ApplyInternal(owner, settlement, null, CampaignVec2.Zero, SetPartyAiActionDetail.RaidSettlement, navigationType, isFromPort, isTargetingPort);
	}

	public static void GetActionForBesiegingSettlement(MobileParty owner, Settlement settlement, MobileParty.NavigationType navigationType, bool isFromPort)
	{
		ApplyInternal(owner, settlement, null, CampaignVec2.Zero, SetPartyAiActionDetail.BesiegeSettlement, navigationType, isFromPort, isTargetingPort: false);
	}

	public static void GetActionForEngagingParty(MobileParty owner, MobileParty mobileParty, MobileParty.NavigationType navigationType, bool isFromPort)
	{
		ApplyInternal(owner, null, mobileParty, CampaignVec2.Zero, SetPartyAiActionDetail.EngageParty, navigationType, isFromPort, isTargetingPort: false);
	}

	public static void GetActionForGoingAroundParty(MobileParty owner, MobileParty mobileParty, MobileParty.NavigationType navigationType, bool isFromPort)
	{
		ApplyInternal(owner, null, mobileParty, CampaignVec2.Zero, SetPartyAiActionDetail.GoAroundParty, navigationType, isFromPort, isTargetingPort: false);
	}

	public static void GetActionForDefendingSettlement(MobileParty owner, Settlement settlement, MobileParty.NavigationType navigationType, bool isFromPort, bool isTargetingPort)
	{
		ApplyInternal(owner, settlement, null, CampaignVec2.Zero, SetPartyAiActionDetail.DefendParty, navigationType, isFromPort, isTargetingPort);
	}

	public static void GetActionForEscortingParty(MobileParty owner, MobileParty mobileParty, MobileParty.NavigationType navigationType, bool isFromPort, bool isTargetingPort)
	{
		ApplyInternal(owner, null, mobileParty, CampaignVec2.Zero, SetPartyAiActionDetail.EscortParty, navigationType, isFromPort, isTargetingPort);
	}

	public static void GetActionForMovingToNearestLand(MobileParty owner, Settlement settlement)
	{
		ApplyInternal(owner, settlement, null, CampaignVec2.Zero, SetPartyAiActionDetail.MoveToNearestLand, MobileParty.NavigationType.Naval, isFromPort: false, isTargetingPort: false);
	}
}
