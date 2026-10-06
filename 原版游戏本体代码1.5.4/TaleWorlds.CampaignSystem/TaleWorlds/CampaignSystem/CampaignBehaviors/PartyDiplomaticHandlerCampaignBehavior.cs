using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PartyDiplomaticHandlerCampaignBehavior : CampaignBehaviorBase
{
	private IFaction _lastFactionMadePeaceWithCausedPlayerToLeaveEvent;

	public override void RegisterEvents()
	{
		CampaignEvents.OnMapEventContinuityNeedsUpdateEvent.AddNonSerializedListener(this, OnMapEventContinuityNeedsUpdate);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	private void OnSessionLaunched(CampaignGameStarter gameSystemInitializer)
	{
		gameSystemInitializer.AddGameMenu("hostile_action_end_by_peace", "{=1rbg3Hz2}The {FACTION_1} and {FACTION_2} are no longer enemies.", game_menu_hostile_action_end_by_peace_on_init);
		gameSystemInitializer.AddGameMenuOption("hostile_action_end_by_peace", "hostile_action_en_by_peace_end", "{=WVkc4UgX}Continue.", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}, player_encounter_finished_with_diplomatic_change_consequence, isLeave: true);
		gameSystemInitializer.AddGameMenu("village_raid_diplomatically_ended", "{=CnNUOM9Q}The owner of this fief and your kingdom are no longer enemies.", game_menu_hostile_action_end_by_peace_on_init);
		gameSystemInitializer.AddGameMenuOption("village_raid_diplomatically_ended", "leave", "{=3sRdGQou}Leave", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty;
		}, player_encounter_finished_with_diplomatic_change_consequence, isLeave: true);
	}

	private static void player_encounter_finished_with_diplomatic_change_consequence(MenuCallbackArgs args)
	{
		if (PlayerEncounter.Current != null)
		{
			PlayerEncounter.Finish();
		}
		else
		{
			GameMenu.ExitToLast();
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool arg5)
	{
		if (newKingdom != null)
		{
			CampaignEventDispatcher.Instance.OnMapEventContinuityNeedsUpdate(newKingdom);
		}
		else
		{
			CampaignEventDispatcher.Instance.OnMapEventContinuityNeedsUpdate(clan);
		}
	}

	private void OnMapEventContinuityNeedsUpdate(IFaction faction)
	{
		CheckMapEvents(faction);
		CheckSiegeEvents(faction);
		CheckFactionPartiesAndSettlements(faction);
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (detail != ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege && settlement.SiegeEvent != null)
		{
			CheckSiegeEventContinuity(settlement.SiegeEvent);
		}
		if (PlayerEncounter.Current != null && PlayerEncounter.Current.EncounterSettlementAux != null)
		{
			foreach (Village boundVillage in settlement.BoundVillages)
			{
				if (boundVillage.Settlement == PlayerEncounter.Current.EncounterSettlementAux && boundVillage.Settlement.IsUnderRaid && !Clan.PlayerClan.IsAtWarWith(newOwner.MapFaction))
				{
					_lastFactionMadePeaceWithCausedPlayerToLeaveEvent = oldOwner.MapFaction;
					GameMenu.ActivateGameMenu("village_raid_diplomatically_ended");
					break;
				}
			}
		}
		CheckSettlementSuitabilityForParties(settlement.Parties);
	}

	private void CheckFactionPartiesAndSettlements(IFaction faction)
	{
		CheckSettlementSuitabilityForParties(faction.WarPartyComponents.Select((WarPartyComponent x) => x.MobileParty));
		foreach (Settlement settlement in faction.Settlements)
		{
			CheckSettlementSuitabilityForParties(settlement.Parties);
		}
	}

	private void CheckMapEvents(IFaction faction)
	{
		MBReadOnlyList<MapEvent> obj = Campaign.Current.MapEventManager?.MapEvents;
		List<MapEvent> list = new List<MapEvent>();
		foreach (MapEvent item in obj)
		{
			if (!item.IsFinalized && item.InvolvedParties.Any((PartyBase x) => x.MapFaction == faction))
			{
				list.Add(item);
			}
		}
		foreach (MapEvent item2 in list)
		{
			List<MapEventParty> list2 = item2.AttackerSide.Parties.ToList();
			MapEventState state = item2.State;
			bool flag = false;
			foreach (MapEventParty item3 in list2)
			{
				if (!item2.CanPartyJoinBattle(item3.Party, BattleSideEnum.Attacker))
				{
					flag = flag || item2.IsPlayerMapEvent;
					if (item3.Party != PartyBase.MainParty)
					{
						item3.Party.MapEventSide = null;
					}
				}
			}
			if (state != MapEventState.WaitingRemoval && item2.State == MapEventState.WaitingRemoval)
			{
				item2.DiplomaticallyFinished = true;
			}
			if (flag)
			{
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.Finish();
				}
				else
				{
					GameMenu.ExitToLast();
				}
			}
		}
	}

	private void CheckSiegeEvents(IFaction faction)
	{
		MBReadOnlyList<SiegeEvent> obj = Campaign.Current.SiegeEventManager?.SiegeEvents;
		List<SiegeEvent> list = new List<SiegeEvent>();
		foreach (SiegeEvent item in obj)
		{
			if (!item.ReadyToBeRemoved && item.GetInvolvedPartiesForEventType(item.GetCurrentBattleType()).Any((PartyBase x) => x.MapFaction == faction))
			{
				list.Add(item);
			}
		}
		foreach (SiegeEvent item2 in list)
		{
			CheckSiegeEventContinuity(item2);
		}
	}

	private void CheckSiegeEventContinuity(SiegeEvent siegeEvent)
	{
		bool flag = siegeEvent == PlayerSiege.PlayerSiegeEvent;
		List<PartyBase> list = siegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType(siegeEvent.GetCurrentBattleType()).ToList();
		bool flag2 = false;
		for (int i = 0; i < list.Count; i++)
		{
			PartyBase partyBase = list[i];
			if (!siegeEvent.CanPartyJoinSide(partyBase, BattleSideEnum.Attacker))
			{
				if (flag && !flag2 && partyBase == PartyBase.MainParty)
				{
					flag2 = true;
					_lastFactionMadePeaceWithCausedPlayerToLeaveEvent = siegeEvent.BesiegedSettlement.MapFaction;
				}
				partyBase.MobileParty.BesiegerCamp = null;
			}
		}
		if (!siegeEvent.ReadyToBeRemoved && siegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType(siegeEvent.GetCurrentBattleType()).Any((PartyBase x) => x != PartyBase.MainParty))
		{
			List<MobileParty> list2 = siegeEvent.BesiegedSettlement.Parties.ToList();
			for (int num = 0; num < list2.Count; num++)
			{
				PartyBase party = list2[num].Party;
				if (siegeEvent.BesiegedSettlement.MapFaction != party.MapFaction && !siegeEvent.CanPartyJoinSide(party, BattleSideEnum.Defender))
				{
					if (flag && !flag2 && party == PartyBase.MainParty)
					{
						flag2 = true;
						_lastFactionMadePeaceWithCausedPlayerToLeaveEvent = siegeEvent.BesiegerCamp.LeaderParty.MapFaction;
					}
					else if (party.MobileParty.Army == null || party.MobileParty.Army.LeaderParty == party.MobileParty)
					{
						LeaveSettlementAction.ApplyForParty(party.MobileParty);
					}
				}
			}
		}
		if (flag2)
		{
			GameMenu.ActivateGameMenu("hostile_action_end_by_peace");
		}
	}

	private void CheckSettlementSuitabilityForParties(IEnumerable<MobileParty> parties)
	{
		foreach (MobileParty item in parties.ToList())
		{
			if (item.CurrentSettlement == null || !item.MapFaction.IsAtWarWith(item.CurrentSettlement.MapFaction))
			{
				continue;
			}
			if (item != MobileParty.MainParty)
			{
				if (item.Army != null && item.Army.LeaderParty != item)
				{
					continue;
				}
				if (item.Army != null && item.Army.Parties.Contains(MobileParty.MainParty))
				{
					GameMenu.SwitchToMenu("army_left_settlement_due_to_war_declaration");
					continue;
				}
				if (item.IsTransitionInProgress)
				{
					item.CancelNavigationTransition();
				}
				LeaveSettlementAction.ApplyForParty(item);
			}
			else if (item.CurrentSettlement.IsFortification)
			{
				GameMenu.SwitchToMenu("fortification_crime_rating");
			}
		}
	}

	[GameMenuInitializationHandler("hostile_action_end_by_peace")]
	public static void hostile_action_end_by_peace_on_init(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.BesiegedSettlement != null)
		{
			args.MenuContext.SetBackgroundMeshName(MobileParty.MainParty.BesiegedSettlement.SettlementComponent.WaitMeshName);
		}
		else if (MobileParty.MainParty.LastVisitedSettlement != null)
		{
			args.MenuContext.SetBackgroundMeshName(MobileParty.MainParty.LastVisitedSettlement.SettlementComponent.WaitMeshName);
		}
		else if (PlayerEncounter.EncounterSettlement != null)
		{
			args.MenuContext.SetBackgroundMeshName(PlayerEncounter.EncounterSettlement.SettlementComponent.WaitMeshName);
		}
		else
		{
			Debug.FailedAssert("No menu background to initialize!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\PartyDiplomaticHandlerCampaignBehavior.cs", "hostile_action_end_by_peace_on_init", 297);
		}
	}

	private void game_menu_hostile_action_end_by_peace_on_init(MenuCallbackArgs args)
	{
		if (_lastFactionMadePeaceWithCausedPlayerToLeaveEvent == null)
		{
			IEnumerable<IFaction> source = Kingdom.All.Union<IFaction>(Clan.All);
			IFaction mapFaction = MobileParty.MainParty.MapFaction;
			StanceLink stanceLink = (from x in source
				where x != mapFaction
				select x.GetStanceWith(mapFaction) into x
				where !x.IsAtWar
				orderby x.PeaceDeclarationDate.ElapsedHoursUntilNow
				select x).FirstOrDefault();
			_lastFactionMadePeaceWithCausedPlayerToLeaveEvent = ((stanceLink.Faction1 != Clan.PlayerClan.MapFaction) ? stanceLink.Faction1 : stanceLink.Faction2);
		}
		GameTexts.SetVariable("FACTION_1", Clan.PlayerClan.MapFaction.EncyclopediaLinkWithName);
		GameTexts.SetVariable("FACTION_2", _lastFactionMadePeaceWithCausedPlayerToLeaveEvent.EncyclopediaLinkWithName);
		if (PlayerEncounter.Battle != null)
		{
			PlayerEncounter.Battle.DiplomaticallyFinished = true;
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_lastFactionMadePeaceWithCausedPlayerToLeaveEvent", ref _lastFactionMadePeaceWithCausedPlayerToLeaveEvent);
	}
}
