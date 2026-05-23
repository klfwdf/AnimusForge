using System;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors
{
	// Token: 0x02000470 RID: 1136
	public class AiPartyThinkBehavior : CampaignBehaviorBase
	{
		// Token: 0x060047D4 RID: 18388 RVA: 0x00169670 File Offset: 0x00167870
		public override void RegisterEvents()
		{
			CampaignEvents.TickPartialHourlyAiEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.PartyHourlyAiTick));
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
			CampaignEvents.MakePeace.AddNonSerializedListener(this, new Action<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>(this.OnMakePeace));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, new Action<MobileParty>(this.OnMobilePartyCreated));
		}

		// Token: 0x060047D5 RID: 18389 RVA: 0x0016971E File Offset: 0x0016791E
		private void OnMobilePartyCreated(MobileParty mobileParty)
		{
			mobileParty.Ai.RethinkAtNextHourlyTick = true;
		}

		// Token: 0x060047D6 RID: 18390 RVA: 0x0016972C File Offset: 0x0016792C
		private void OnNewGameCreated(CampaignGameStarter gameStarter)
		{
			foreach (MobileParty mobileParty in MobileParty.All)
			{
				for (int i = 0; i < 6; i++)
				{
					this.PartyHourlyAiTick(mobileParty);
				}
			}
		}

		// Token: 0x060047D7 RID: 18391 RVA: 0x0016978C File Offset: 0x0016798C
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060047D8 RID: 18392 RVA: 0x00169790 File Offset: 0x00167990
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
				num = ((flag2 && !isTransitionInProgress) ? 3 : 1);
			}
			if (flag && MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty == mobileParty && (mobileParty.CurrentSettlement != null || (mobileParty.LastVisitedSettlement != null && mobileParty.MapEvent == null && mobileParty.LastVisitedSettlement.Position.Distance(mobileParty.Position) < 1f)))
			{
				num = 6;
			}
			if (mobileParty.Ai.HourCounter % num == 0 && mobileParty != MobileParty.MainParty && (mobileParty.MapEvent == null || (mobileParty.Party == mobileParty.MapEvent.AttackerSide.LeaderParty && (mobileParty.MapEvent.IsRaid || mobileParty.MapEvent.IsSiegeAssault))))
			{
				mobileParty.Ai.HourCounter = 0;
				AiBehavior aiBehavior = (flag ? mobileParty.Army.LeaderParty.DefaultBehavior : AiBehavior.None);
				IMapPoint mapPoint = (flag ? mobileParty.Army.AiBehaviorObject : null);
				mobileParty.Ai.RethinkAtNextHourlyTick = false;
				PartyThinkParams thinkParamsCache = mobileParty.ThinkParamsCache;
				thinkParamsCache.Reset(mobileParty);
				CampaignEventDispatcher.Instance.AiHourlyTick(mobileParty, thinkParamsCache);
				AIBehaviorData aibehaviorData = AIBehaviorData.Invalid;
				AIBehaviorData aibehaviorData2 = AIBehaviorData.Invalid;
				float num2 = -1f;
				float num3 = -1f;
				foreach (ValueTuple<AIBehaviorData, float> valueTuple in thinkParamsCache.AIBehaviorScores)
				{
					float item = valueTuple.Item2;
					if (item > num2)
					{
						num2 = item;
						aibehaviorData = valueTuple.Item1;
					}
					if (item > num3 && !valueTuple.Item1.WillGatherArmy)
					{
						num3 = item;
						aibehaviorData2 = valueTuple.Item1;
					}
				}
				if (aibehaviorData != AIBehaviorData.Invalid)
				{
					if (mobileParty.DefaultBehavior == AiBehavior.Hold || mobileParty.Ai.RethinkAtNextHourlyTick || thinkParamsCache.CurrentObjectiveValue < 0.05f)
					{
						num2 = 1f;
					}
					double num4 = ((aibehaviorData.AiBehavior == AiBehavior.PatrolAroundPoint || aibehaviorData.AiBehavior == AiBehavior.GoToSettlement) ? 0.03 : 0.1);
					num4 *= (double)(aibehaviorData.WillGatherArmy ? 2f : ((mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty) ? 0.33f : 1f));
					bool flag3 = mobileParty.Army != null;
					int num5 = 0;
					while (num5 < num && !flag3)
					{
						flag3 = MBRandom.RandomFloat < num2;
						num5++;
					}
					if (((double)num2 > num4 && flag3) || (num2 > 0.01f && mobileParty.MapEvent == null && mobileParty.Army == null && mobileParty.DefaultBehavior == AiBehavior.Hold))
					{
						if (mobileParty.MapEvent != null && mobileParty.Party == mobileParty.MapEvent.AttackerSide.LeaderParty && !thinkParamsCache.DoNotChangeBehavior && (aibehaviorData.Party != mobileParty.MapEvent.MapEventSettlement || (aibehaviorData.AiBehavior != AiBehavior.RaidSettlement && aibehaviorData.AiBehavior != AiBehavior.BesiegeSettlement && aibehaviorData.AiBehavior != AiBehavior.AssaultSettlement)))
						{
							if (PlayerEncounter.Current != null && PlayerEncounter.Battle == mobileParty.MapEvent)
							{
								PlayerEncounter.Finish(true);
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
							aibehaviorData = aibehaviorData2;
						}
						bool flag4 = aibehaviorData.AiBehavior == AiBehavior.RaidSettlement || aibehaviorData.AiBehavior == AiBehavior.BesiegeSettlement || aibehaviorData.AiBehavior == AiBehavior.DefendSettlement || aibehaviorData.AiBehavior == AiBehavior.PatrolAroundPoint;
						if (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty && (mobileParty.CurrentSettlement == null || mobileParty.CurrentSettlement.SiegeEvent == null) && (aibehaviorData.AiBehavior != AiBehavior.GoAroundParty && aibehaviorData.AiBehavior != AiBehavior.PatrolAroundPoint && aibehaviorData.AiBehavior != AiBehavior.GoToSettlement && !flag4))
						{
							DisbandArmyAction.ApplyByUnknownReason(mobileParty.Army);
						}
						if (flag4 && mobileParty.Army == null && aibehaviorData.WillGatherArmy && !mobileParty.LeaderHero.Clan.IsUnderMercenaryService)
						{
							bool flag5 = MBRandom.RandomFloat < num2;
							if (aibehaviorData.AiBehavior == AiBehavior.DefendSettlement || flag5)
							{
								Army.ArmyTypes selectedArmyType = ((aibehaviorData.AiBehavior == AiBehavior.BesiegeSettlement) ? Army.ArmyTypes.Besieger : ((aibehaviorData.AiBehavior == AiBehavior.RaidSettlement) ? Army.ArmyTypes.Raider : Army.ArmyTypes.Defender));
								((Kingdom)mobileParty.MapFaction).CreateArmy(mobileParty.LeaderHero, aibehaviorData.Party as Settlement, selectedArmyType, thinkParamsCache.PossibleArmyMembersUponArmyCreation);
							}
						}
						else if (!thinkParamsCache.DoNotChangeBehavior)
						{
							if (aibehaviorData.AiBehavior == AiBehavior.PatrolAroundPoint)
							{
								if (aibehaviorData.Party != null)
								{
									SetPartyAiAction.GetActionForPatrollingAroundSettlement(mobileParty, (Settlement)aibehaviorData.Party, aibehaviorData.NavigationType, aibehaviorData.IsFromPort, aibehaviorData.IsTargetingPort);
								}
								else
								{
									SetPartyAiAction.GetActionForPatrollingAroundPoint(mobileParty, aibehaviorData.Position, aibehaviorData.NavigationType, aibehaviorData.IsFromPort);
								}
							}
							else if (aibehaviorData.AiBehavior == AiBehavior.GoToSettlement)
							{
								if (MobilePartyHelper.GetCurrentSettlementOfMobilePartyForAICalculation(mobileParty) != aibehaviorData.Party)
								{
									SetPartyAiAction.GetActionForVisitingSettlement(mobileParty, (Settlement)aibehaviorData.Party, aibehaviorData.NavigationType, aibehaviorData.IsFromPort, aibehaviorData.IsTargetingPort);
								}
							}
							else if (aibehaviorData.AiBehavior == AiBehavior.EscortParty)
							{
								SetPartyAiAction.GetActionForEscortingParty(mobileParty, (MobileParty)aibehaviorData.Party, aibehaviorData.NavigationType, aibehaviorData.IsFromPort, aibehaviorData.IsTargetingPort);
							}
							else if (aibehaviorData.AiBehavior == AiBehavior.RaidSettlement)
							{
								if (mobileParty.MapEvent == null || !mobileParty.MapEvent.IsRaid || mobileParty.MapEvent.MapEventSettlement != aibehaviorData.Party)
								{
									SetPartyAiAction.GetActionForRaidingSettlement(mobileParty, (Settlement)aibehaviorData.Party, aibehaviorData.NavigationType, aibehaviorData.IsFromPort);
								}
							}
							else if (aibehaviorData.AiBehavior == AiBehavior.BesiegeSettlement)
							{
								if (mobileParty.MapEvent == null || !mobileParty.MapEvent.IsSiegeAssault || mobileParty.MapEvent.MapEventSettlement != aibehaviorData.Party)
								{
									SetPartyAiAction.GetActionForBesiegingSettlement(mobileParty, (Settlement)aibehaviorData.Party, aibehaviorData.NavigationType, aibehaviorData.IsFromPort);
								}
							}
							else if (aibehaviorData.AiBehavior == AiBehavior.DefendSettlement && mobileParty.CurrentSettlement != aibehaviorData.Party)
							{
								SetPartyAiAction.GetActionForDefendingSettlement(mobileParty, (Settlement)aibehaviorData.Party, aibehaviorData.NavigationType, aibehaviorData.IsFromPort, aibehaviorData.IsTargetingPort);
							}
							else if (aibehaviorData.AiBehavior == AiBehavior.GoAroundParty)
							{
								SetPartyAiAction.GetActionForGoingAroundParty(mobileParty, (MobileParty)aibehaviorData.Party, aibehaviorData.NavigationType, aibehaviorData.IsFromPort);
							}
							else if (aibehaviorData.AiBehavior == AiBehavior.MoveToNearestLandOrPort)
							{
								SetPartyAiAction.GetActionForMovingToNearestLand(mobileParty, (Settlement)aibehaviorData.Party);
							}
						}
					}
					else if (aibehaviorData.AiBehavior != AiBehavior.None)
					{
						if (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty && !mobileParty.Army.IsWaitingForArmyMembers())
						{
							DisbandArmyAction.ApplyByUnknownReason(mobileParty.Army);
						}
						else if (mobileParty.Army != null && mobileParty.CurrentSettlement == null && mobileParty != mobileParty.Army.LeaderParty && !thinkParamsCache.DoNotChangeBehavior)
						{
							SetPartyAiAction.GetActionForEscortingParty(mobileParty, mobileParty.Army.LeaderParty, aibehaviorData.NavigationType, aibehaviorData.IsFromPort, aibehaviorData.IsTargetingPort);
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

		// Token: 0x060047D9 RID: 18393 RVA: 0x00169FE4 File Offset: 0x001681E4
		private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
		{
			if (faction1.IsKingdomFaction && faction2.IsKingdomFaction)
			{
				FactionHelper.FinishAllRelatedHostileActions((Kingdom)faction1, (Kingdom)faction2);
				return;
			}
			if (!faction1.IsKingdomFaction && !faction2.IsKingdomFaction)
			{
				FactionHelper.FinishAllRelatedHostileActions((Clan)faction1, (Clan)faction2);
				return;
			}
			if (faction1.IsKingdomFaction)
			{
				FactionHelper.FinishAllRelatedHostileActionsOfFactionToFaction((Clan)faction2, (Kingdom)faction1);
				FactionHelper.FinishAllRelatedHostileActionsOfFactionToFaction((Kingdom)faction1, (Clan)faction2);
				return;
			}
			FactionHelper.FinishAllRelatedHostileActionsOfFactionToFaction((Clan)faction1, (Kingdom)faction2);
			FactionHelper.FinishAllRelatedHostileActionsOfFactionToFaction((Kingdom)faction2, (Clan)faction1);
		}

		// Token: 0x060047DA RID: 18394 RVA: 0x0016A084 File Offset: 0x00168284
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
			{
				if (warPartyComponent.MobileParty.TargetSettlement != null)
				{
					this.CheckMobilePartyActionAccordingToSettlement(warPartyComponent.MobileParty, warPartyComponent.MobileParty.TargetSettlement);
				}
			}
		}

		// Token: 0x060047DB RID: 18395 RVA: 0x0016A0F4 File Offset: 0x001682F4
		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			foreach (WarPartyComponent warPartyComponent in faction1.WarPartyComponents)
			{
				if (warPartyComponent.MobileParty.TargetSettlement != null)
				{
					this.CheckMobilePartyActionAccordingToSettlement(warPartyComponent.MobileParty, warPartyComponent.MobileParty.TargetSettlement);
				}
			}
			foreach (WarPartyComponent warPartyComponent2 in faction2.WarPartyComponents)
			{
				if (warPartyComponent2.MobileParty.TargetSettlement != null)
				{
					this.CheckMobilePartyActionAccordingToSettlement(warPartyComponent2.MobileParty, warPartyComponent2.MobileParty.TargetSettlement);
				}
			}
		}

		// Token: 0x060047DC RID: 18396 RVA: 0x0016A1C4 File Offset: 0x001683C4
		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			this.HandlePartyActionsAfterSettlementOwnerChange(settlement);
		}

		// Token: 0x060047DD RID: 18397 RVA: 0x0016A1D0 File Offset: 0x001683D0
		private void HandlePartyActionsAfterSettlementOwnerChange(Settlement settlement)
		{
			foreach (MobileParty mobileParty in MobileParty.All)
			{
				this.CheckMobilePartyActionAccordingToSettlement(mobileParty, settlement);
			}
		}

		// Token: 0x060047DE RID: 18398 RVA: 0x0016A224 File Offset: 0x00168424
		private void CheckMobilePartyActionAccordingToSettlement(MobileParty mobileParty, Settlement settlement)
		{
			if (mobileParty.BesiegedSettlement != null && mobileParty.BesiegedSettlement != settlement)
			{
				if (mobileParty.Army == null)
				{
					Settlement targetSettlement = mobileParty.TargetSettlement;
					if (targetSettlement != null && (targetSettlement == settlement || (targetSettlement.IsVillage && targetSettlement.Village.Bound == settlement)))
					{
						if (mobileParty.MapEvent != null)
						{
							mobileParty.Ai.RethinkAtNextHourlyTick = true;
							return;
						}
						if (mobileParty.CurrentSettlement == null)
						{
							mobileParty.SetMoveModeHold();
							return;
						}
						mobileParty.SetMoveGoToSettlement(mobileParty.CurrentSettlement, mobileParty.DesiredAiNavigationType, mobileParty.IsTargetingPort);
						mobileParty.RecalculateShortTermBehavior();
						return;
					}
				}
				else if (mobileParty.Army.LeaderParty == mobileParty)
				{
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

		// Token: 0x040013D4 RID: 5076
		private const int DefaultThinkingPeriodInHours = 6;
	}
}
