using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004C5 RID: 1221
	public static class SetPartyAiAction
	{
		// Token: 0x06004A19 RID: 18969 RVA: 0x0017547C File Offset: 0x0017367C
		private static void ApplyInternal(MobileParty owner, Settlement settlement, MobileParty mobileParty, CampaignVec2 position, SetPartyAiAction.SetPartyAiActionDetail detail, MobileParty.NavigationType navigationType, bool isFromPort, bool isTargetingPort)
		{
			if (detail == SetPartyAiAction.SetPartyAiActionDetail.GoToSettlement)
			{
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
					return;
				}
			}
			else if (detail == SetPartyAiAction.SetPartyAiActionDetail.PatrolAroundSettlement)
			{
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
					return;
				}
			}
			else if (detail == SetPartyAiAction.SetPartyAiActionDetail.RaidSettlement)
			{
				if (owner.DefaultBehavior != AiBehavior.RaidSettlement || owner.TargetSettlement != settlement || navigationType != owner.DesiredAiNavigationType || owner.StartTransitionNextFrameToExitFromPort != isFromPort)
				{
					if (isFromPort && !owner.IsTransitionInProgress)
					{
						owner.StartTransitionNextFrameToExitFromPort = true;
					}
					owner.SetMoveRaidSettlement(settlement, navigationType);
					if (owner.Army != null && owner.Army.LeaderParty == owner)
					{
						owner.Army.ArmyType = Army.ArmyTypes.Raider;
						owner.Army.AiBehaviorObject = settlement;
						return;
					}
				}
			}
			else if (detail == SetPartyAiAction.SetPartyAiActionDetail.BesiegeSettlement)
			{
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
						return;
					}
				}
			}
			else if (detail == SetPartyAiAction.SetPartyAiActionDetail.GoAroundParty)
			{
				if (owner.DefaultBehavior != AiBehavior.GoAroundParty || owner != mobileParty || navigationType != owner.DesiredAiNavigationType || owner.StartTransitionNextFrameToExitFromPort != isFromPort)
				{
					if (isFromPort && !owner.IsTransitionInProgress)
					{
						owner.StartTransitionNextFrameToExitFromPort = true;
					}
					owner.SetMoveGoAroundParty(mobileParty, navigationType);
					return;
				}
			}
			else if (detail == SetPartyAiAction.SetPartyAiActionDetail.EngageParty)
			{
				if (owner.DefaultBehavior != AiBehavior.EngageParty || owner != mobileParty || navigationType != owner.DesiredAiNavigationType || owner.StartTransitionNextFrameToExitFromPort != isFromPort)
				{
					if (isFromPort && !owner.IsTransitionInProgress)
					{
						owner.StartTransitionNextFrameToExitFromPort = true;
					}
					owner.SetMoveEngageParty(mobileParty, navigationType);
					return;
				}
			}
			else if (detail == SetPartyAiAction.SetPartyAiActionDetail.DefendParty)
			{
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
						return;
					}
				}
			}
			else if (detail == SetPartyAiAction.SetPartyAiActionDetail.EscortParty)
			{
				if (owner.DefaultBehavior != AiBehavior.EscortParty || owner.TargetParty != mobileParty || navigationType != owner.DesiredAiNavigationType || owner.StartTransitionNextFrameToExitFromPort != isFromPort || owner.IsTargetingPort != isTargetingPort)
				{
					if (isFromPort && !owner.IsTransitionInProgress)
					{
						owner.StartTransitionNextFrameToExitFromPort = true;
					}
					owner.SetMoveEscortParty(mobileParty, navigationType, isTargetingPort);
					return;
				}
			}
			else if (detail == SetPartyAiAction.SetPartyAiActionDetail.MoveToNearestLand)
			{
				if (owner.DefaultBehavior != AiBehavior.MoveToNearestLandOrPort)
				{
					owner.SetMoveToNearestLand(settlement);
					return;
				}
			}
			else if (detail == SetPartyAiAction.SetPartyAiActionDetail.PatrolAroundPoint && (owner.DefaultBehavior != AiBehavior.PatrolAroundPoint || navigationType != owner.DesiredAiNavigationType))
			{
				owner.SetMovePatrolAroundPoint(position, navigationType);
			}
		}

		// Token: 0x06004A1A RID: 18970 RVA: 0x00175842 File Offset: 0x00173A42
		public static void GetActionForVisitingSettlement(MobileParty owner, Settlement settlement, MobileParty.NavigationType navigationType, bool isFromPort, bool isTargetingPort)
		{
			SetPartyAiAction.ApplyInternal(owner, settlement, null, CampaignVec2.Zero, SetPartyAiAction.SetPartyAiActionDetail.GoToSettlement, navigationType, isFromPort, isTargetingPort);
		}

		// Token: 0x06004A1B RID: 18971 RVA: 0x00175856 File Offset: 0x00173A56
		public static void GetActionForPatrollingAroundSettlement(MobileParty owner, Settlement settlement, MobileParty.NavigationType navigationType, bool isFromPort, bool isTargetingPort)
		{
			SetPartyAiAction.ApplyInternal(owner, settlement, null, CampaignVec2.Zero, SetPartyAiAction.SetPartyAiActionDetail.PatrolAroundSettlement, navigationType, isFromPort, isTargetingPort);
		}

		// Token: 0x06004A1C RID: 18972 RVA: 0x0017586A File Offset: 0x00173A6A
		public static void GetActionForPatrollingAroundPoint(MobileParty owner, CampaignVec2 position, MobileParty.NavigationType navigationType, bool isFromPort)
		{
			SetPartyAiAction.ApplyInternal(owner, null, null, position, SetPartyAiAction.SetPartyAiActionDetail.PatrolAroundPoint, navigationType, isFromPort, false);
		}

		// Token: 0x06004A1D RID: 18973 RVA: 0x00175879 File Offset: 0x00173A79
		public static void GetActionForRaidingSettlement(MobileParty owner, Settlement settlement, MobileParty.NavigationType navigationType, bool isFromPort)
		{
			SetPartyAiAction.ApplyInternal(owner, settlement, null, CampaignVec2.Zero, SetPartyAiAction.SetPartyAiActionDetail.RaidSettlement, navigationType, isFromPort, false);
		}

		// Token: 0x06004A1E RID: 18974 RVA: 0x0017588C File Offset: 0x00173A8C
		public static void GetActionForBesiegingSettlement(MobileParty owner, Settlement settlement, MobileParty.NavigationType navigationType, bool isFromPort)
		{
			SetPartyAiAction.ApplyInternal(owner, settlement, null, CampaignVec2.Zero, SetPartyAiAction.SetPartyAiActionDetail.BesiegeSettlement, navigationType, isFromPort, false);
		}

		// Token: 0x06004A1F RID: 18975 RVA: 0x0017589F File Offset: 0x00173A9F
		public static void GetActionForEngagingParty(MobileParty owner, MobileParty mobileParty, MobileParty.NavigationType navigationType, bool isFromPort)
		{
			SetPartyAiAction.ApplyInternal(owner, null, mobileParty, CampaignVec2.Zero, SetPartyAiAction.SetPartyAiActionDetail.EngageParty, navigationType, isFromPort, false);
		}

		// Token: 0x06004A20 RID: 18976 RVA: 0x001758B2 File Offset: 0x00173AB2
		public static void GetActionForGoingAroundParty(MobileParty owner, MobileParty mobileParty, MobileParty.NavigationType navigationType, bool isFromPort)
		{
			SetPartyAiAction.ApplyInternal(owner, null, mobileParty, CampaignVec2.Zero, SetPartyAiAction.SetPartyAiActionDetail.GoAroundParty, navigationType, isFromPort, false);
		}

		// Token: 0x06004A21 RID: 18977 RVA: 0x001758C5 File Offset: 0x00173AC5
		public static void GetActionForDefendingSettlement(MobileParty owner, Settlement settlement, MobileParty.NavigationType navigationType, bool isFromPort, bool isTargetingPort)
		{
			SetPartyAiAction.ApplyInternal(owner, settlement, null, CampaignVec2.Zero, SetPartyAiAction.SetPartyAiActionDetail.DefendParty, navigationType, isFromPort, isTargetingPort);
		}

		// Token: 0x06004A22 RID: 18978 RVA: 0x001758D9 File Offset: 0x00173AD9
		public static void GetActionForEscortingParty(MobileParty owner, MobileParty mobileParty, MobileParty.NavigationType navigationType, bool isFromPort, bool isTargetingPort)
		{
			SetPartyAiAction.ApplyInternal(owner, null, mobileParty, CampaignVec2.Zero, SetPartyAiAction.SetPartyAiActionDetail.EscortParty, navigationType, isFromPort, isTargetingPort);
		}

		// Token: 0x06004A23 RID: 18979 RVA: 0x001758ED File Offset: 0x00173AED
		public static void GetActionForMovingToNearestLand(MobileParty owner, Settlement settlement)
		{
			SetPartyAiAction.ApplyInternal(owner, settlement, null, CampaignVec2.Zero, SetPartyAiAction.SetPartyAiActionDetail.MoveToNearestLand, MobileParty.NavigationType.Naval, false, false);
		}

		// Token: 0x02000896 RID: 2198
		private enum SetPartyAiActionDetail
		{
			// Token: 0x0400245D RID: 9309
			GoToSettlement,
			// Token: 0x0400245E RID: 9310
			PatrolAroundSettlement,
			// Token: 0x0400245F RID: 9311
			PatrolAroundPoint,
			// Token: 0x04002460 RID: 9312
			RaidSettlement,
			// Token: 0x04002461 RID: 9313
			BesiegeSettlement,
			// Token: 0x04002462 RID: 9314
			EngageParty,
			// Token: 0x04002463 RID: 9315
			GoAroundParty,
			// Token: 0x04002464 RID: 9316
			DefendParty,
			// Token: 0x04002465 RID: 9317
			EscortParty,
			// Token: 0x04002466 RID: 9318
			MoveToNearestLand
		}
	}
}
