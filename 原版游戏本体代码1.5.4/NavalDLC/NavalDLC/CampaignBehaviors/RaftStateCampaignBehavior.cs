using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.CampaignBehaviors;

public class RaftStateCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnMobilePartyRaftStateChangedEvent.AddNonSerializedListener(this, OnMobilePartyRaftStateChanged);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.OnShipDestroyedEvent.AddNonSerializedListener(this, OnShipDestroyed);
		CampaignEvents.OnPartyLeftArmyEvent.AddNonSerializedListener(this, OnPartyLeftArmy);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.CanHaveCampaignIssuesEvent.AddNonSerializedListener(this, CanHaveCampaignIssues);
		CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, OnPlayerCharacterChanged);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (!dataStore.IsLoading || !MBSaveLoad.IsUpdatingGameVersion || !(MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.5.0.117599")))
		{
			return;
		}
		Dictionary<Ship, List<ShipUpgradePiece>> data = new Dictionary<Ship, List<ShipUpgradePiece>>();
		AnchorPoint data2 = null;
		dataStore.SyncData("_playerCachedShips", ref data);
		dataStore.SyncData("_cachedAnchorPoint", ref data2);
		MBList<Ship> mBList = new MBList<Ship>();
		foreach (KeyValuePair<Ship, List<ShipUpgradePiece>> item in data)
		{
			Ship key = item.Key;
			List<ShipUpgradePiece> value = item.Value;
			key.CanHaveUnlockedPieces = true;
			foreach (ShipUpgradePiece item2 in value)
			{
				bool flag = false;
				foreach (KeyValuePair<string, ShipSlot> availableSlot in key.ShipHull.AvailableSlots)
				{
					if (item2.DoesPieceMatchSlot(availableSlot.Value) && key.GetPieceAtSlot(availableSlot.Key) != item2)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					key.AddToUnlockedPieces(item2);
				}
			}
			mBList.Add(key);
		}
		if (mBList.Count > 0)
		{
			Campaign.Current.PlayerDataForNavalAutoTravel = new PlayerDataForNavalAutoTravel(mBList, data2);
		}
	}

	private void OnGameLoadFinished()
	{
		Campaign.Current.PlayerDataForNavalAutoTravel?.ReservedAnchorPoint?.InitializeOnLoad(MobileParty.MainParty);
	}

	private void OnSessionLaunched(CampaignGameStarter gameStarter)
	{
		gameStarter.AddGameMenu("player_raft_state", "{=5ROdLNNo}You no longer have a seaworthy ship. Your party will land on the nearest shore.", player_raft_state_on_init);
		gameStarter.AddGameMenuOption("player_raft_state", "continue", "{=DM6luo3c}Continue", continue_condition, player_raft_state_continue_on_consequence);
		gameStarter.AddGameMenu("player_raft_state_after_prisoner", "{=BF4ybrgP}You are no longer a prisoner. Since you don't have a seaworthy ship, your party will land on the nearest shore.", player_raft_state_on_init);
		gameStarter.AddGameMenuOption("player_raft_state_after_prisoner", "continue", "{=DM6luo3c}Continue", continue_condition, player_raft_state_after_prisoner_on_consequence);
		gameStarter.AddWaitGameMenu("player_raft_state_wait", "{=nxA52tGB}Your party is stranded at sea.", null, null, null, player_raft_state_menu_on_tick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
		gameStarter.AddGameMenu("player_raft_state_end", "{=iQkp5KSA}Your party has washed ashore.", null);
		gameStarter.AddGameMenuOption("player_raft_state_end", "continue", "{=DM6luo3c}Continue", continue_condition, player_raft_state_end_continue_on_consequence);
	}

	[GameMenuInitializationHandler("player_raft_state")]
	[GameMenuInitializationHandler("player_raft_state_after_prisoner")]
	[GameMenuInitializationHandler("player_raft_state_wait")]
	public static void game_menu_player_raft_state_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("raft_state");
	}

	[GameMenuInitializationHandler("player_raft_state_end")]
	public static void game_menu_player_raft_state_end_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("captive_at_sea_escape");
	}

	private bool continue_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	private void player_raft_state_end_continue_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.ExitToLast();
		MBReadOnlyList<Ship> mBReadOnlyList = Campaign.Current.PlayerDataForNavalAutoTravel?.ReservedShips;
		AnchorPoint anchorPoint = Campaign.Current.PlayerDataForNavalAutoTravel?.ReservedAnchorPoint;
		if (mBReadOnlyList != null && mBReadOnlyList.Count > 0)
		{
			GiveCachedShipsToMainParty(mBReadOnlyList);
			if (anchorPoint != null && anchorPoint.IsValid)
			{
				MobileParty.MainParty.SetAnchor(anchorPoint);
			}
			else
			{
				MobileParty.MainParty.Anchor.ResetPosition();
			}
			Campaign.Current.PlayerDataForNavalAutoTravel = null;
		}
	}

	private void player_raft_state_menu_on_tick(MenuCallbackArgs args, CampaignTime dt)
	{
		if (!MobileParty.MainParty.IsCurrentlyAtSea)
		{
			GameMenu.SwitchToMenu("player_raft_state_end");
			MobileParty.MainParty.SetMoveGoToPoint(MobileParty.MainParty.Position, MobileParty.MainParty.NavigationCapability);
		}
	}

	private void player_raft_state_after_prisoner_on_consequence(MenuCallbackArgs args)
	{
		Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
		if (!MobileParty.MainParty.IsInRaftState)
		{
			if (MobileParty.MainParty.Ships.Count > 0)
			{
				AnchorPoint anchorPoint = (MobileParty.MainParty.Anchor.IsValid ? new AnchorPoint(MobileParty.MainParty.Anchor) : null);
				MBList<Ship> mBList = new MBList<Ship>();
				for (int num = MobileParty.MainParty.Ships.Count - 1; num >= 0; num--)
				{
					Ship ship = MobileParty.MainParty.Ships[num];
					mBList.Add(ship);
					ChangeShipOwnerAction.ApplyByTemporarilyRemovingShipsFromPlayer(ship);
				}
				Campaign.Current.PlayerDataForNavalAutoTravel = new PlayerDataForNavalAutoTravel(mBList, anchorPoint);
			}
			HandleRaftStateActivate(MobileParty.MainParty, null);
		}
		GameMenu.SwitchToMenu("player_raft_state_wait");
	}

	private void player_raft_state_continue_on_consequence(MenuCallbackArgs args)
	{
		Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
		if (!MobileParty.MainParty.IsInRaftState)
		{
			HandleRaftStateActivate(MobileParty.MainParty, null);
		}
		GameMenu.SwitchToMenu("player_raft_state_wait");
	}

	private void player_raft_state_on_init(MenuCallbackArgs args)
	{
		Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
	}

	private static void HandleRaftStateActivate(MobileParty mobileParty, MapEvent mapEvent)
	{
		if (mobileParty.HasLandNavigationCapability)
		{
			RaftStateChangeAction.ActivateRaftStateForParty(mobileParty);
			return;
		}
		if (mobileParty.IsCaravan && mobileParty.LeaderHero != null)
		{
			mobileParty.LeaderHero.ChangeState(Hero.CharacterStates.Fugitive);
		}
		DestroyPartyAction.Apply(mapEvent?.Winner.LeaderParty, mobileParty);
	}

	private bool ShouldActivateRaftStateForMobileParty(MobileParty mobileParty)
	{
		if (mobileParty.IsCurrentlyAtSea && !mobileParty.IsInRaftState && !mobileParty.HasNavalNavigationCapability)
		{
			return mobileParty.IsActive;
		}
		return false;
	}

	private void ConsiderMemberAndArmyRaftStateStatus(MobileParty party, Army army)
	{
		if (ShouldActivateRaftStateForMobileParty(party))
		{
			HandleRaftStateActivate(party, party.MapEvent);
		}
		if (army != null && army.LeaderParty.IsCurrentlyAtSea && !army.LeaderParty.HasNavalNavigationCapability)
		{
			DisbandArmyAction.ApplyByNoShip(army);
		}
	}

	private void ConsiderArmyRaftState(MobileParty mobileParty)
	{
		if (!mobileParty.Army.LeaderParty.HasNavalNavigationCapability)
		{
			DisbandArmyAction.ApplyByNoShip(mobileParty.Army);
		}
		else
		{
			mobileParty.Army = null;
		}
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		foreach (PartyBase item in mapEvent.InvolvedParties.ToList())
		{
			if (item.IsMobile && ShouldActivateRaftStateForMobileParty(item.MobileParty))
			{
				if (item.MobileParty.Army != null)
				{
					ConsiderMemberAndArmyRaftStateStatus(item.MobileParty, item.MobileParty.Army);
				}
				else
				{
					HandleRaftStateActivate(item.MobileParty, mapEvent);
				}
			}
		}
	}

	private void OnShipDestroyed(PartyBase owner, Ship ship, DestroyShipAction.ShipDestroyDetail detail)
	{
		if (owner != null && owner.MapEvent == null && owner.IsMobile && ShouldActivateRaftStateForMobileParty(owner.MobileParty))
		{
			if (owner.MobileParty.Army != null)
			{
				ConsiderMemberAndArmyRaftStateStatus(owner.MobileParty, owner.MobileParty.Army);
			}
			else
			{
				HandleRaftStateActivate(owner.MobileParty, null);
			}
		}
	}

	private void OnPartyLeftArmy(MobileParty party, Army army)
	{
		if (party.IsCurrentlyAtSea || army.LeaderParty.IsCurrentlyAtSea)
		{
			ConsiderMemberAndArmyRaftStateStatus(party, army);
		}
	}

	private void OnHeroPrisonerTaken(PartyBase party, Hero hero)
	{
		if (hero == Hero.MainHero && MobileParty.MainParty.IsInRaftState)
		{
			RaftStateChangeAction.DeactivateRaftStateForParty(MobileParty.MainParty);
		}
	}

	private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (mobileParty != null && mobileParty.IsInRaftState)
		{
			Debug.FailedAssert("this should not be possible natively.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\CampaignBehaviors\\RaftStateCampaignBehavior.cs", "OnSettlementEntered", 330);
			RaftStateChangeAction.DeactivateRaftStateForParty(MobileParty.MainParty);
		}
	}

	private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification = true)
	{
		if (prisoner != Hero.MainHero)
		{
			MakeHeroFugitiveAction.Apply(prisoner);
		}
		else if (MobileParty.MainParty.IsCurrentlyAtSea)
		{
			GameMenu.ActivateGameMenu("player_raft_state_after_prisoner");
		}
	}

	public void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newMainParty, bool isMainPartyChanged)
	{
		if (ShouldActivateRaftStateForMobileParty(newMainParty))
		{
			RaftStateChangeAction.ActivateRaftStateForParty(newMainParty);
		}
		else
		{
			PlayerDataForNavalAutoTravel playerDataForNavalAutoTravel = Campaign.Current.PlayerDataForNavalAutoTravel;
			if (playerDataForNavalAutoTravel != null && playerDataForNavalAutoTravel.ReservedShips?.Count > 0)
			{
				GiveCachedShipsToMainParty(Campaign.Current.PlayerDataForNavalAutoTravel.ReservedShips);
				Campaign.Current.PlayerDataForNavalAutoTravel = null;
			}
		}
		Army army = newMainParty.Army;
		if (army != null && army.LeaderParty.IsCurrentlyAtSea && !army.LeaderParty.HasNavalNavigationCapability)
		{
			DisbandArmyAction.ApplyByNoShip(army);
		}
	}

	private void OnMobilePartyRaftStateChanged(MobileParty mobileParty)
	{
		if (mobileParty.IsMainParty && mobileParty.IsActive && mobileParty.IsInRaftState)
		{
			GameMenu.ActivateGameMenu("player_raft_state");
		}
	}

	private void GiveCachedShipsToMainParty(MBReadOnlyList<Ship> playerShips)
	{
		foreach (Ship playerShip in playerShips)
		{
			ChangeShipOwnerAction.ApplyByGivingBackShipsToPlayer(playerShip);
		}
	}

	private void CanHaveCampaignIssues(Hero hero, ref bool canHaveCampaignIssues)
	{
		if ((hero.PartyBelongedTo != null) & canHaveCampaignIssues)
		{
			canHaveCampaignIssues = !hero.PartyBelongedTo.IsCurrentlyAtSea;
		}
	}
}
