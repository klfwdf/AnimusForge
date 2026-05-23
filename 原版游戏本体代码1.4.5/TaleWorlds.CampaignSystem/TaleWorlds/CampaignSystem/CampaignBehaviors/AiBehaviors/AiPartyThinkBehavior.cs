using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;

public class AiPartyThinkBehavior : CampaignBehaviorBase
{
	private const int DefaultThinkingPeriodInHours = 6;

	public override void RegisterEvents()
	{
		CampaignEvents.TickPartialHourlyAiEvent.AddNonSerializedListener(this, PartyHourlyAiTick);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnMobilePartyCreated);
	}

	private void OnMobilePartyCreated(MobileParty mobileParty)
	{
		mobileParty.Ai.RethinkAtNextHourlyTick = true;
	}

	private void OnNewGameCreated(CampaignGameStarter gameStarter)
	{
		foreach (MobileParty item in MobileParty.All)
		{
			for (int i = 0; i < 6; i++)
			{
				PartyHourlyAiTick(item);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void PartyHourlyAiTick(MobileParty mobileParty)
	{
		if (mobileParty.Ai.IsDisabled || mobileParty.Ai.DoNotMakeNewDecisions)
		{
			return;
		}
		bool flag = mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty;
		bool flag2 = mobileParty.Army != null && mobileParty.AttachedTo == null;
		bool isTransitionInProgress = mobileParty.IsTransitionInProgress;
		int num = 6;
		if (flag || isTransitionInProgress || flag2 || mobileParty.Ai.RethinkAtNextHourlyTick || (mobileParty.MapEvent != null && (mobileParty.MapEvent.IsRaid || mobileParty.MapEvent.IsSiegeAssault)))
		{
			num = ((!flag2 || isTransitionInProgress) ? 1 : 3);
		}
		if (flag && MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty == mobileParty && (mobileParty.CurrentSettlement != null || (mobileParty.LastVisitedSettlement != null && mobileParty.MapEvent == null && mobileParty.LastVisitedSettlement.Position.Distance(mobileParty.Position) < 1f)))
		{
			num = 6;
		}
		if (mobileParty.Ai.HourCounter % num == 0 && mobileParty != MobileParty.MainParty && (mobileParty.MapEvent == null || (mobileParty.Party == mobileParty.MapEvent.AttackerSide.LeaderParty && ((mobileParty.MapEvent.IsRaid && !mobileParty.IsCurrentlyAtSea) || mobileParty.MapEvent.IsSiegeAssault))))
		{
			mobileParty.Ai.HourCounter = 0;
			AiBehavior aiBehavior = ((!flag) ? AiBehavior.None : mobileParty.Army.LeaderParty.DefaultBehavior);
			IMapPoint mapPoint = (flag ? mobileParty.Army.AiBehaviorObject : null);
			mobileParty.Ai.RethinkAtNextHourlyTick = false;
			PartyThinkParams thinkParamsCache = mobileParty.ThinkParamsCache;
			thinkParamsCache.Reset(mobileParty);
			CampaignEventDispatcher.Instance.AiHourlyTick(mobileParty, thinkParamsCache);
			AIBehaviorData aIBehaviorData = AIBehaviorData.Invalid;
			AIBehaviorData aIBehaviorData2 = AIBehaviorData.Invalid;
			float num2 = -1f;
			float num3 = -1f;
			foreach (var aIBehaviorScore in thinkParamsCache.AIBehaviorScores)
			{
				float item = aIBehaviorScore.Item2;
				if (item > num2)
				{
					num2 = item;
					(aIBehaviorData, _) = aIBehaviorScore;
				}
				if (item > num3 && !aIBehaviorScore.Item1.WillGatherArmy)
				{
					num3 = item;
					(aIBehaviorData2, _) = aIBehaviorScore;
				}
			}
			if (aIBehaviorData != AIBehaviorData.Invalid)
			{
				if (mobileParty.DefaultBehavior == AiBehavior.Hold || mobileParty.Ai.RethinkAtNextHourlyTick || thinkParamsCache.CurrentObjectiveValue < 0.05f)
				{
					num2 = 1f;
				}
				double num4 = ((aIBehaviorData.AiBehavior == AiBehavior.PatrolAroundPoint || aIBehaviorData.AiBehavior == AiBehavior.GoToSettlement) ? 0.03 : 0.1);
				num4 *= (double)(aIBehaviorData.WillGatherArmy ? 2f : ((mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty) ? 0.33f : 1f));
				bool flag3 = mobileParty.Army != null;
				for (int i = 0; i < num; i++)
				{
					if (flag3)
					{
						break;
					}
					flag3 = MBRandom.RandomFloat < num2;
				}
				if (((double)num2 > num4 && flag3) || (num2 > 0.01f && mobileParty.MapEvent == null && mobileParty.Army == null && mobileParty.DefaultBehavior == AiBehavior.Hold))
				{
					if (mobileParty.MapEvent != null && mobileParty.Party == mobileParty.MapEvent.AttackerSide.LeaderParty && !thinkParamsCache.DoNotChangeBehavior && (aIBehaviorData.Party != mobileParty.MapEvent.MapEventSettlement || (aIBehaviorData.AiBehavior != AiBehavior.RaidSettlement && aIBehaviorData.AiBehavior != AiBehavior.BesiegeSettlement && aIBehaviorData.AiBehavior != AiBehavior.AssaultSettlement)))
					{
						if (PlayerEncounter.Current != null && PlayerEncounter.Battle == mobileParty.MapEvent)
						{
							PlayerEncounter.Finish();
						}
						if (mobileParty.MapEvent != null)
						{
							mobileParty.MapEvent.FinalizeEvent();
						}
						if (mobileParty.SiegeEvent != null)
						{
							mobileParty.SiegeEvent.FinalizeSiegeEvent();
						}
					}
					if ((double)num2 <= num4)
					{
						aIBehaviorData = aIBehaviorData2;
					}
					bool flag4 = aIBehaviorData.AiBehavior == AiBehavior.RaidSettlement || aIBehaviorData.AiBehavior == AiBehavior.BesiegeSettlement || aIBehaviorData.AiBehavior == AiBehavior.DefendSettlement || aIBehaviorData.AiBehavior == AiBehavior.PatrolAroundPoint;
					if (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty && (mobileParty.CurrentSettlement == null || mobileParty.CurrentSettlement.SiegeEvent == null) && !(aIBehaviorData.AiBehavior == AiBehavior.GoAroundParty || aIBehaviorData.AiBehavior == AiBehavior.PatrolAroundPoint || aIBehaviorData.AiBehavior == AiBehavior.GoToSettlement || flag4))
					{
						DisbandArmyAction.ApplyByUnknownReason(mobileParty.Army);
					}
					if (flag4 && mobileParty.Army == null && aIBehaviorData.WillGatherArmy && !mobileParty.LeaderHero.Clan.IsUnderMercenaryService)
					{
						bool flag5 = MBRandom.RandomFloat < num2;
						if (aIBehaviorData.AiBehavior == AiBehavior.DefendSettlement || flag5)
						{
							Army.ArmyTypes selectedArmyType = ((aIBehaviorData.AiBehavior != AiBehavior.BesiegeSettlement) ? ((aIBehaviorData.AiBehavior == AiBehavior.RaidSettlement) ? Army.ArmyTypes.Raider : Army.ArmyTypes.Defender) : Army.ArmyTypes.Besieger);
							((Kingdom)mobileParty.MapFaction).CreateArmy(mobileParty.LeaderHero, aIBehaviorData.Party as Settlement, selectedArmyType, thinkParamsCache.PossibleArmyMembersUponArmyCreation);
						}
					}
					else if (!thinkParamsCache.DoNotChangeBehavior)
					{
						if (aIBehaviorData.AiBehavior == AiBehavior.PatrolAroundPoint)
						{
							if (aIBehaviorData.Party != null)
							{
								SetPartyAiAction.GetActionForPatrollingAroundSettlement(mobileParty, (Settlement)aIBehaviorData.Party, aIBehaviorData.NavigationType, aIBehaviorData.IsFromPort, aIBehaviorData.IsTargetingPort);
							}
							else
							{
								SetPartyAiAction.GetActionForPatrollingAroundPoint(mobileParty, aIBehaviorData.Position, aIBehaviorData.NavigationType, aIBehaviorData.IsFromPort);
							}
						}
						else if (aIBehaviorData.AiBehavior == AiBehavior.GoToSettlement)
						{
							if (MobilePartyHelper.GetCurrentSettlementOfMobilePartyForAICalculation(mobileParty) != aIBehaviorData.Party)
							{
								SetPartyAiAction.GetActionForVisitingSettlement(mobileParty, (Settlement)aIBehaviorData.Party, aIBehaviorData.NavigationType, aIBehaviorData.IsFromPort, aIBehaviorData.IsTargetingPort);
							}
						}
						else if (aIBehaviorData.AiBehavior == AiBehavior.EscortParty)
						{
							SetPartyAiAction.GetActionForEscortingParty(mobileParty, (MobileParty)aIBehaviorData.Party, aIBehaviorData.NavigationType, aIBehaviorData.IsFromPort, aIBehaviorData.IsTargetingPort);
						}
						else if (aIBehaviorData.AiBehavior == AiBehavior.RaidSettlement)
						{
							if (mobileParty.MapEvent == null || !mobileParty.MapEvent.IsRaid || mobileParty.MapEvent.MapEventSettlement != aIBehaviorData.Party)
							{
								SetPartyAiAction.GetActionForRaidingSettlement(mobileParty, (Settlement)aIBehaviorData.Party, aIBehaviorData.NavigationType, aIBehaviorData.IsFromPort, aIBehaviorData.IsTargetingPort);
							}
						}
						else if (aIBehaviorData.AiBehavior == AiBehavior.BesiegeSettlement)
						{
							if (mobileParty.MapEvent == null || !mobileParty.MapEvent.IsSiegeAssault || mobileParty.MapEvent.MapEventSettlement != aIBehaviorData.Party)
							{
								SetPartyAiAction.GetActionForBesiegingSettlement(mobileParty, (Settlement)aIBehaviorData.Party, aIBehaviorData.NavigationType, aIBehaviorData.IsFromPort);
							}
						}
						else if (aIBehaviorData.AiBehavior == AiBehavior.DefendSettlement && mobileParty.CurrentSettlement != aIBehaviorData.Party)
						{
							SetPartyAiAction.GetActionForDefendingSettlement(mobileParty, (Settlement)aIBehaviorData.Party, aIBehaviorData.NavigationType, aIBehaviorData.IsFromPort, aIBehaviorData.IsTargetingPort);
						}
						else if (aIBehaviorData.AiBehavior == AiBehavior.GoAroundParty)
						{
							SetPartyAiAction.GetActionForGoingAroundParty(mobileParty, (MobileParty)aIBehaviorData.Party, aIBehaviorData.NavigationType, aIBehaviorData.IsFromPort);
						}
						else if (aIBehaviorData.AiBehavior == AiBehavior.MoveToNearestLandOrPort)
						{
							SetPartyAiAction.GetActionForMovingToNearestLand(mobileParty, (Settlement)aIBehaviorData.Party);
						}
					}
				}
				else if (aIBehaviorData.AiBehavior != AiBehavior.None)
				{
					if (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty && !mobileParty.Army.IsWaitingForArmyMembers())
					{
						DisbandArmyAction.ApplyByUnknownReason(mobileParty.Army);
					}
					else if (mobileParty.Army != null && mobileParty.CurrentSettlement == null && mobileParty != mobileParty.Army.LeaderParty && !thinkParamsCache.DoNotChangeBehavior)
					{
						SetPartyAiAction.GetActionForEscortingParty(mobileParty, mobileParty.Army.LeaderParty, aIBehaviorData.NavigationType, aIBehaviorData.IsFromPort, aIBehaviorData.IsTargetingPort);
					}
				}
				if (MobileParty.MainParty.Army != null && mobileParty == MobileParty.MainParty.Army.LeaderParty && (aiBehavior != mobileParty.Army.LeaderParty.DefaultBehavior || mobileParty.Army.AiBehaviorObject != mapPoint))
				{
					CampaignEventDispatcher.Instance.OnPlayerArmyLeaderChangedBehavior();
				}
			}
		}
		mobileParty.Ai.HourCounter++;
	}

	private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
	{
		if (faction1.IsKingdomFaction && faction2.IsKingdomFaction)
		{
			FactionHelper.FinishAllRelatedHostileActions((Kingdom)faction1, (Kingdom)faction2);
		}
		else if (faction1.IsKingdomFaction || faction2.IsKingdomFaction)
		{
			if (faction1.IsKingdomFaction)
			{
				FactionHelper.FinishAllRelatedHostileActionsOfFactionToFaction((Clan)faction2, (Kingdom)faction1);
				FactionHelper.FinishAllRelatedHostileActionsOfFactionToFaction((Kingdom)faction1, (Clan)faction2);
			}
			else
			{
				FactionHelper.FinishAllRelatedHostileActionsOfFactionToFaction((Clan)faction1, (Kingdom)faction2);
				FactionHelper.FinishAllRelatedHostileActionsOfFactionToFaction((Kingdom)faction2, (Clan)faction1);
			}
		}
		else
		{
			FactionHelper.FinishAllRelatedHostileActions((Clan)faction1, (Clan)faction2);
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
		{
			if (warPartyComponent.MobileParty.TargetSettlement != null)
			{
				CheckMobilePartyActionAccordingToSettlement(warPartyComponent.MobileParty, warPartyComponent.MobileParty.TargetSettlement);
			}
		}
	}

	private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
		foreach (WarPartyComponent warPartyComponent in faction1.WarPartyComponents)
		{
			if (warPartyComponent.MobileParty.TargetSettlement != null)
			{
				CheckMobilePartyActionAccordingToSettlement(warPartyComponent.MobileParty, warPartyComponent.MobileParty.TargetSettlement);
			}
		}
		foreach (WarPartyComponent warPartyComponent2 in faction2.WarPartyComponents)
		{
			if (warPartyComponent2.MobileParty.TargetSettlement != null)
			{
				CheckMobilePartyActionAccordingToSettlement(warPartyComponent2.MobileParty, warPartyComponent2.MobileParty.TargetSettlement);
			}
		}
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		HandlePartyActionsAfterSettlementOwnerChange(settlement);
	}

	private void HandlePartyActionsAfterSettlementOwnerChange(Settlement settlement)
	{
		foreach (MobileParty item in MobileParty.All)
		{
			CheckMobilePartyActionAccordingToSettlement(item, settlement);
		}
	}

	private void CheckMobilePartyActionAccordingToSettlement(MobileParty mobileParty, Settlement settlement)
	{
		if (mobileParty.BesiegedSettlement == null || mobileParty.BesiegedSettlement == settlement)
		{
			return;
		}
		if (mobileParty.Army == null)
		{
			Settlement targetSettlement = mobileParty.TargetSettlement;
			if (targetSettlement == null || (targetSettlement != settlement && (!targetSettlement.IsVillage || targetSettlement.Village.Bound != settlement)))
			{
				return;
			}
			if (mobileParty.MapEvent == null)
			{
				if (mobileParty.CurrentSettlement == null)
				{
					mobileParty.SetMoveModeHold();
					return;
				}
				mobileParty.SetMoveGoToSettlement(mobileParty.CurrentSettlement, mobileParty.DesiredAiNavigationType, mobileParty.IsTargetingPort);
				mobileParty.RecalculateShortTermBehavior();
			}
			else
			{
				mobileParty.Ai.RethinkAtNextHourlyTick = true;
			}
		}
		else
		{
			if (mobileParty.Army.LeaderParty != mobileParty)
			{
				return;
			}
			Army army = mobileParty.Army;
			if (army.AiBehaviorObject == settlement || (army.AiBehaviorObject != null && ((Settlement)army.AiBehaviorObject).IsVillage && ((Settlement)army.AiBehaviorObject).Village.Bound == settlement))
			{
				army.AiBehaviorObject = null;
				if (army.LeaderParty.MapEvent == null)
				{
					army.LeaderParty.SetMoveModeHold();
				}
				else
				{
					army.LeaderParty.Ai.RethinkAtNextHourlyTick = true;
				}
				army.FinishArmyObjective();
			}
		}
	}
}
