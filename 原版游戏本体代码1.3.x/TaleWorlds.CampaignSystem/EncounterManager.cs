using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000084 RID: 132
	public static class EncounterManager
	{
		// Token: 0x1700044C RID: 1100
		// (get) Token: 0x06001101 RID: 4353 RVA: 0x0005092E File Offset: 0x0004EB2E
		public static EncounterModel EncounterModel
		{
			get
			{
				return Campaign.Current.Models.EncounterModel;
			}
		}

		// Token: 0x06001102 RID: 4354 RVA: 0x0005093F File Offset: 0x0004EB3F
		public static void Tick(float dt)
		{
			EncounterManager.HandleEncounters(dt);
		}

		// Token: 0x06001103 RID: 4355 RVA: 0x00050948 File Offset: 0x0004EB48
		private static void HandleEncounters(float dt)
		{
			if (Campaign.Current.TimeControlMode != CampaignTimeControlMode.Stop)
			{
				for (int i = 0; i < Campaign.Current.MobileParties.Count; i++)
				{
					EncounterManager.HandleEncounterForMobileParty(Campaign.Current.MobileParties[i], dt);
				}
			}
		}

		// Token: 0x06001104 RID: 4356 RVA: 0x00050994 File Offset: 0x0004EB94
		public static void HandleEncounterForMobileParty(MobileParty mobileParty, float dt)
		{
			PartyBase partyBase;
			PartyBase partyBase2;
			if (mobileParty.IsActive && mobileParty.AttachedTo == null && mobileParty.MapEventSide == null && (mobileParty.CurrentSettlement == null || mobileParty.IsGarrison) && (mobileParty.BesiegedSettlement == null || mobileParty.ShortTermBehavior == AiBehavior.AssaultSettlement) && (mobileParty.IsCurrentlyEngagingParty || mobileParty.IsCurrentlyEngagingSettlement || (mobileParty.Ai.AiBehaviorInteractable != null && mobileParty.ShortTermBehavior == AiBehavior.GoToPoint && ((partyBase = mobileParty.Ai.AiBehaviorInteractable as PartyBase) == null || !partyBase.IsSettlement) && ((partyBase2 = mobileParty.Ai.AiBehaviorInteractable as PartyBase) == null || !partyBase2.IsMobile) && (mobileParty.Party != PartyBase.MainParty || PlayerEncounter.Current == null))))
			{
				if (PlayerEncounter.EncounteredMobileParty == mobileParty)
				{
					PlayerEncounter playerEncounter = PlayerEncounter.Current;
					if (playerEncounter == null || playerEncounter.PlayerSide != BattleSideEnum.Defender)
					{
						return;
					}
				}
				if (mobileParty.IsCurrentlyEngagingSettlement && mobileParty.ShortTermTargetSettlement != null && mobileParty.ShortTermTargetSettlement == mobileParty.CurrentSettlement)
				{
					return;
				}
				if (mobileParty.IsCurrentlyEngagingParty && (!mobileParty.ShortTermTargetParty.IsActive || (mobileParty.ShortTermTargetParty.CurrentSettlement != null && (mobileParty.ShortTermTargetParty.MapEvent == null || (mobileParty.ShortTermTargetParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker).MapFaction != mobileParty.MapFaction && mobileParty.ShortTermTargetParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender).MapFaction != mobileParty.MapFaction)))))
				{
					return;
				}
				if (mobileParty.Ai.AiBehaviorInteractable.CanPartyInteract(mobileParty, dt))
				{
					mobileParty.Ai.AiBehaviorInteractable.OnPartyInteraction(mobileParty);
				}
			}
		}

		// Token: 0x06001105 RID: 4357 RVA: 0x00050B3C File Offset: 0x0004ED3C
		public static void StartPartyEncounter(PartyBase attackerParty, PartyBase defenderParty)
		{
			bool flag = PartyBase.MainParty.MapEvent != null && (PartyBase.MainParty.MapEvent.InvolvedParties.Contains(attackerParty) || PartyBase.MainParty.MapEvent.InvolvedParties.Contains(defenderParty));
			if (defenderParty == PartyBase.MainParty && PlayerSiege.PlayerSiegeEvent != null)
			{
				Debug.Print("\nPlayerSiege is interrupted\n", 0, Debug.DebugColor.DarkGreen, 64UL);
			}
			if (attackerParty == PartyBase.MainParty || defenderParty == PartyBase.MainParty)
			{
				MapEvent mapEvent = PartyBase.MainParty.MapEvent;
				if (mapEvent != null && PlayerEncounter.IsActive && mapEvent.AttackerSide.TroopCount > 0 && mapEvent.DefenderSide.TroopCount > 0)
				{
					PlayerEncounter.Current.OnPartyJoinEncounter(attackerParty.MobileParty);
				}
				else if (((attackerParty == PartyBase.MainParty || defenderParty == PartyBase.MainParty) && !PlayerEncounter.IsActive) || (PlayerEncounter.EncounterSettlement != null && Settlement.CurrentSettlement != null && PlayerEncounter.EncounterSettlement == Settlement.CurrentSettlement))
				{
					EncounterManager.RestartPlayerEncounter(attackerParty, defenderParty);
				}
			}
			else if (flag)
			{
				EncounterManager.RestartPlayerEncounter(attackerParty, defenderParty);
			}
			else if (attackerParty.IsActive && defenderParty.IsActive && (attackerParty.MobileParty.Army == null || defenderParty != PartyBase.MainParty))
			{
				if (attackerParty.MapFaction == defenderParty.MapFaction)
				{
					attackerParty.MapEventSide = defenderParty.MapEventSide;
				}
				else
				{
					StartBattleAction.Apply(attackerParty, defenderParty);
				}
			}
			if (defenderParty.SiegeEvent != null && defenderParty != PartyBase.MainParty && defenderParty.SiegeEvent.BesiegerCamp != null && defenderParty.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(PartyBase.MainParty, MapEvent.BattleTypes.Siege) && (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty))
			{
				EncounterManager.StartPartyEncounter(PartyBase.MainParty, attackerParty);
			}
			if (attackerParty != PartyBase.MainParty && attackerParty.MapEvent != null && attackerParty.MapEvent.IsSallyOut && attackerParty.MapEvent.MapEventSettlement == MobileParty.MainParty.CurrentSettlement && MobileParty.MainParty.Army == null)
			{
				GameMenu.SwitchToMenu("join_sally_out");
			}
		}

		// Token: 0x06001106 RID: 4358 RVA: 0x00050D40 File Offset: 0x0004EF40
		public static void StartSettlementEncounter(MobileParty attackerParty, Settlement settlement)
		{
			if (attackerParty.DefaultBehavior == AiBehavior.BesiegeSettlement && attackerParty.TargetSettlement == settlement && attackerParty.ShortTermBehavior != AiBehavior.AssaultSettlement)
			{
				if (attackerParty.BesiegedSettlement == null)
				{
					if (settlement.SiegeEvent == null)
					{
						Campaign.Current.SiegeEventManager.StartSiegeEvent(settlement, attackerParty);
					}
					else
					{
						MapEventSide mapEventSide = settlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEventSide;
						attackerParty.BesiegerCamp = settlement.SiegeEvent.BesiegerCamp;
						if (mapEventSide != null)
						{
							attackerParty.MapEventSide = mapEventSide;
						}
					}
				}
				if (settlement.Party.MapEvent == null)
				{
					return;
				}
			}
			if (attackerParty.DefaultBehavior == AiBehavior.DefendSettlement && attackerParty.IsCurrentlyAtSea && attackerParty.IsTargetingPort && settlement.SiegeEvent != null)
			{
				if (settlement.SiegeEvent.IsBlockadeActive)
				{
					if (settlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEventSide == null)
					{
						BlockadeBattleMapEvent.CreateBlockadeBattleMapEvent(attackerParty.Party, settlement.SiegeEvent.BesiegerCamp.LeaderParty.Party, false);
						return;
					}
					attackerParty.Party.MapEventSide = settlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEventSide.OtherSide;
					return;
				}
				else if (settlement.Party.MapEvent != null)
				{
					EnterSettlementAction.ApplyForParty(attackerParty, settlement);
					attackerParty.Party.MapEventSide = settlement.Party.MapEventSide;
					return;
				}
			}
			if (!attackerParty.IsVillager && attackerParty != MobileParty.MainParty && settlement.IsVillage && settlement.Village.VillageState == Village.VillageStates.Looted)
			{
				attackerParty.SetMoveModeHold();
				return;
			}
			if (attackerParty != MobileParty.MainParty)
			{
				if (attackerParty.Aggressiveness > 0.01f && PartyBase.MainParty.MapEvent != null && PartyBase.MainParty.MapEvent.MapEventSettlement == settlement)
				{
					if (PlayerEncounter.IsActive)
					{
						if (attackerParty.MapFaction == MobileParty.MainParty.MapFaction || (PartyBase.MainParty.MapEvent.AttackerSide.LeaderParty != PartyBase.MainParty && PartyBase.MainParty.MapEvent.DefenderSide.LeaderParty != PartyBase.MainParty))
						{
							PlayerEncounter.Current.OnPartyJoinEncounter(attackerParty);
						}
						else
						{
							if (PlayerEncounter.IsActive)
							{
								PlayerEncounter.Finish(true);
							}
							EncounterManager.RestartPlayerEncounter(attackerParty.Party, PartyBase.MainParty);
						}
					}
				}
				else
				{
					bool flag = MobileParty.MainParty.CurrentSettlement == settlement;
					MapEvent mapEvent = settlement.Party.MapEvent;
					if (mapEvent != null && !mapEvent.IsFinalized && (mapEvent.AttackerSide.MapFaction == attackerParty.MapFaction || mapEvent.DefenderSide.MapFaction == attackerParty.MapFaction))
					{
						if (flag && attackerParty.AttachedTo == null)
						{
							PlayerEncounter.Finish(true);
						}
						settlement.Party.MapEventSide = ((mapEvent.AttackerSide.MapFaction == attackerParty.MapFaction) ? mapEvent.DefenderSide : mapEvent.AttackerSide);
					}
					else if (settlement.Party.MapEvent == null && attackerParty != MobileParty.MainParty && attackerParty.ShortTermBehavior == AiBehavior.RaidSettlement && attackerParty.ShortTermTargetSettlement == settlement && FactionManager.IsAtWarAgainstFaction(attackerParty.MapFaction, settlement.MapFaction))
					{
						if (flag)
						{
							PlayerEncounter.Finish(false);
						}
						if (settlement.SettlementHitPoints > 0.001f)
						{
							StartBattleAction.ApplyStartRaid(attackerParty, settlement);
						}
						if (flag)
						{
							if (MobileParty.MainParty.MapFaction == settlement.MapFaction)
							{
								PlayerEncounter.Start();
								PlayerEncounter.Current.Init(attackerParty.Party, settlement.Party, settlement);
							}
							else
							{
								LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
							}
						}
					}
					else if (attackerParty != MobileParty.MainParty && attackerParty.ShortTermBehavior == AiBehavior.AssaultSettlement && attackerParty.ShortTermTargetSettlement == settlement && FactionManager.IsAtWarAgainstFaction(attackerParty.MapFaction, settlement.MapFaction))
					{
						if (flag)
						{
							PlayerEncounter.Finish(false);
						}
						bool flag2 = settlement.Party.MapEvent == null;
						StartBattleAction.ApplyStartAssaultAgainstWalls(attackerParty, settlement);
						if (attackerParty.MapEvent.DefenderSide.TroopCount == 0 && (PlayerSiege.PlayerSiegeEvent == null || PlayerSiege.PlayerSide != BattleSideEnum.Defender || MobileParty.MainParty.CurrentSettlement != settlement))
						{
							bool flag3 = MobileParty.MainParty.BesiegedSettlement == settlement;
							if (flag3 && PlayerEncounter.Current == null)
							{
								EncounterManager.StartSettlementEncounter((MobileParty.MainParty.Army != null) ? MobileParty.MainParty.Army.LeaderParty : MobileParty.MainParty, settlement);
								MobileParty.MainParty.MapEventSide = ((PlayerSiege.PlayerSide != BattleSideEnum.Defender) ? attackerParty.MapEventSide : attackerParty.MapEventSide.OtherSide);
							}
							attackerParty.MapEvent.SetOverrideWinner(BattleSideEnum.Attacker);
							attackerParty.MapEvent.FinalizeEvent();
							if (flag3)
							{
								GameMenu.SwitchToMenu("menu_settlement_taken");
							}
							return;
						}
						if (attackerParty.ShortTermBehavior == AiBehavior.AssaultSettlement && flag2 && attackerParty != MobileParty.MainParty && PlayerEncounter.Current != null && PlayerEncounter.EncounterSettlement == settlement && MobileParty.MainParty.CurrentSettlement == null)
						{
							PlayerEncounter.Finish(true);
						}
						if (MobileParty.MainParty.BesiegedSettlement == settlement && (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty))
						{
							EncounterManager.StartSettlementEncounter(MobileParty.MainParty, settlement);
						}
						else if (flag)
						{
							if (attackerParty.MapEvent.CanPartyJoinBattle(PartyBase.MainParty, settlement.BattleSide))
							{
								PlayerEncounter.Start();
								PlayerEncounter.Current.Init(attackerParty.Party, settlement.Party, settlement);
							}
							else
							{
								LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
							}
						}
					}
					else if (((attackerParty.ShortTermBehavior == AiBehavior.GoToSettlement || attackerParty.ShortTermBehavior == AiBehavior.FleeToGate) && attackerParty.ShortTermTargetSettlement == settlement) || attackerParty.Ai.IsDisabled || (attackerParty.Army != null && attackerParty.Army.LeaderParty.AttachedParties.Contains(attackerParty) && attackerParty.Army.LeaderParty.CurrentSettlement == settlement))
					{
						EnterSettlementAction.ApplyForParty(attackerParty, settlement);
					}
				}
				bool flag4 = attackerParty != null && (attackerParty.Army == null || attackerParty.Army.LeaderParty == attackerParty) && attackerParty.CurrentSettlement == settlement && !attackerParty.IsVillager && !attackerParty.IsMilitia && attackerParty != MobileParty.MainParty && attackerParty.MapEvent == null && settlement != null && settlement.IsVillage;
				if (attackerParty.Army != null && attackerParty.Army.LeaderParty == attackerParty && attackerParty != MobileParty.MainParty && !flag4)
				{
					foreach (MobileParty mobileParty in attackerParty.Army.LeaderParty.AttachedParties)
					{
						if (mobileParty.MapEvent == null)
						{
							EncounterManager.StartSettlementEncounter(mobileParty, settlement);
						}
					}
				}
				if (flag4)
				{
					LeaveSettlementAction.ApplyForParty(attackerParty);
					attackerParty.SetMoveModeHold();
					if (attackerParty != MobileParty.MainParty && (MobileParty.MainParty.Army == null || attackerParty != MobileParty.MainParty.Army.LeaderParty))
					{
						attackerParty.Ai.RethinkAtNextHourlyTick = true;
					}
				}
				return;
			}
			PlayerEncounter.Start();
			MapEvent mapEvent2 = settlement.Party.MapEvent;
			if (mapEvent2 == null || !mapEvent2.IsRaid || !attackerParty.MapFaction.IsAtWarWith(settlement.MapFaction))
			{
				PlayerEncounter.Current.Init(attackerParty.Party, settlement.Party, settlement);
				return;
			}
			MBList<MapEventParty> mblist = mapEvent2.DefenderSide.Parties.WhereQ((MapEventParty x) => x.Party.IsMobile && x.Party.MobileParty.IsLordParty && x.Party.MemberRoster.TotalHealthyCount > 0).ToMBList<MapEventParty>();
			if (mblist.AnyQ<MapEventParty>())
			{
				PartyBase party = mblist[0].Party;
				PartyBase leaderParty = mapEvent2.GetLeaderParty(BattleSideEnum.Attacker);
				MBReadOnlyList<MapEventParty> mbreadOnlyList = mapEvent2.PartiesOnSide(BattleSideEnum.Attacker);
				mapEvent2.FinalizeEvent();
				StartBattleAction.Apply(party, leaderParty);
				foreach (MapEventParty mapEventParty in mblist)
				{
					if (mapEventParty.Party != party)
					{
						mapEventParty.Party.MapEventSide = party.MapEventSide;
					}
				}
				foreach (MapEventParty mapEventParty2 in mbreadOnlyList)
				{
					if (mapEventParty2.Party != leaderParty)
					{
						mapEventParty2.Party.MapEventSide = leaderParty.MapEventSide;
					}
				}
				PlayerEncounter.Current.Init(leaderParty, party, null);
				return;
			}
			PlayerEncounter.Current.Init(attackerParty.Party, settlement.Party, settlement);
		}

		// Token: 0x06001107 RID: 4359 RVA: 0x00051598 File Offset: 0x0004F798
		private static void RestartPlayerEncounter(PartyBase attackerParty, PartyBase defenderParty)
		{
			Settlement settlement = null;
			if (MobileParty.MainParty.MapEvent != null && MobileParty.MainParty.MapEvent.IsRaid)
			{
				settlement = MobileParty.MainParty.MapEvent.MapEventSettlement;
			}
			if (PlayerEncounter.Current != null && (PlayerEncounter.EncounteredParty != attackerParty || PartyBase.MainParty != defenderParty) && (PlayerEncounter.EncounteredParty != defenderParty || PartyBase.MainParty != attackerParty))
			{
				PlayerEncounter.Finish(false);
			}
			if (PlayerEncounter.Current == null)
			{
				PlayerEncounter.Start();
			}
			if (attackerParty == PartyBase.MainParty && defenderParty.IsMobile && defenderParty.MobileParty.IsCurrentlyEngagingParty && defenderParty.MobileParty.ShortTermTargetParty == MobileParty.MainParty)
			{
				attackerParty = defenderParty;
				defenderParty = PartyBase.MainParty;
			}
			PlayerEncounter.Current.Init(attackerParty, defenderParty, settlement);
		}
	}
}
