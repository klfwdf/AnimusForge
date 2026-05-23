using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Siege;

public static class PlayerSiege
{
	public static SiegeEvent PlayerSiegeEvent
	{
		get
		{
			SiegeEvent siegeEvent = MobileParty.MainParty.SiegeEvent;
			if (siegeEvent == null)
			{
				Settlement currentSettlement = MobileParty.MainParty.CurrentSettlement;
				if (currentSettlement == null)
				{
					return null;
				}
				siegeEvent = currentSettlement.SiegeEvent;
			}
			return siegeEvent;
		}
	}

	public static Settlement BesiegedSettlement => PlayerSiegeEvent?.BesiegedSettlement;

	public static BattleSideEnum PlayerSide
	{
		get
		{
			if (MobileParty.MainParty.BesiegerCamp == null)
			{
				return BattleSideEnum.Defender;
			}
			return BattleSideEnum.Attacker;
		}
	}

	public static bool IsRebellion
	{
		get
		{
			if (BesiegedSettlement != null)
			{
				return BesiegedSettlement.IsUnderRebellionAttack();
			}
			return false;
		}
	}

	private static void SetPlayerSiegeEvent()
	{
	}

	public static void StartSiegePreparation()
	{
		if (Campaign.Current.CurrentMenuContext != null)
		{
			GameMenu.ExitToLast();
		}
		GameMenu.ActivateGameMenu("menu_siege_strategies");
	}

	public static void OnSiegeEventFinalized(bool besiegerPartyDefeated)
	{
		MapState mapState = Game.Current.GameStateManager.ActiveState as MapState;
		if (IsRebellion)
		{
			if (mapState != null && mapState.AtMenu)
			{
				GameMenu.ExitToLast();
			}
		}
		else if (PlayerSide == BattleSideEnum.Defender && !IsRebellion)
		{
			if (Settlement.CurrentSettlement != null)
			{
				if (mapState != null && !mapState.AtMenu)
				{
					GameMenu.ActivateGameMenu(besiegerPartyDefeated ? "siege_attacker_defeated" : "siege_attacker_left");
				}
				else
				{
					GameMenu.SwitchToMenu(besiegerPartyDefeated ? "siege_attacker_defeated" : "siege_attacker_left");
				}
			}
		}
		else
		{
			if (Hero.MainHero.PartyBelongedTo == null || Hero.MainHero.PartyBelongedTo.Army == null || Hero.MainHero.PartyBelongedTo.Army.LeaderParty == MobileParty.MainParty)
			{
				return;
			}
			if (MobileParty.MainParty.CurrentSettlement != null)
			{
				LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
			}
			if (PlayerEncounter.Battle != null)
			{
				return;
			}
			if (mapState != null)
			{
				if (mapState.AtMenu)
				{
					GameMenu.SwitchToMenu("army_wait");
				}
				else
				{
					GameMenu.ActivateGameMenu("army_wait");
				}
			}
			else
			{
				Campaign.Current.GameMenuManager.SetNextMenu("army_wait");
			}
		}
	}

	public static void StartPlayerSiege(BattleSideEnum playerSide, bool isSimulation = false, Settlement settlement = null)
	{
		if (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
		{
			MobileParty.MainParty.SetMoveModeHold();
		}
		SetPlayerSiegeEvent();
		if (!isSimulation)
		{
			TaleWorlds.Core.GameState gameState = Game.Current.GameStateManager.GameStates.FirstOrDefault((TaleWorlds.Core.GameState s) => s is MapState);
			if (gameState != null)
			{
				(gameState as MapState)?.OnPlayerSiegeActivated();
			}
		}
		CampaignEventDispatcher.Instance.OnPlayerSiegeStarted();
	}

	public static void FinalizePlayerSiege()
	{
		if (PlayerSiegeEvent != null)
		{
			BesiegedSettlement.Party.SetVisualAsDirty();
			MobileParty.MainParty.SetMoveModeHold();
			TaleWorlds.Core.GameState gameState = Game.Current.GameStateManager.GameStates.FirstOrDefault((TaleWorlds.Core.GameState s) => s is MapState);
			if (gameState != null)
			{
				(gameState as MapState)?.OnPlayerSiegeDeactivated();
			}
		}
	}

	public static void StartSiegeMission(Settlement settlement = null)
	{
		Settlement besiegedSettlement = BesiegedSettlement;
		switch (besiegedSettlement.CurrentSiegeState)
		{
		case Settlement.SiegeState.OnTheWalls:
		{
			List<MissionSiegeWeapon> preparedAndActiveSiegeEngines = PlayerSiegeEvent.GetPreparedAndActiveSiegeEngines(PlayerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker));
			List<MissionSiegeWeapon> preparedAndActiveSiegeEngines2 = PlayerSiegeEvent.GetPreparedAndActiveSiegeEngines(PlayerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender));
			bool hasAnySiegeTower = preparedAndActiveSiegeEngines.Exists((MissionSiegeWeapon data) => data.Type == DefaultSiegeEngineTypes.SiegeTower);
			int wallLevel = besiegedSettlement.Town.GetWallLevel();
			CampaignMission.OpenSiegeMissionWithDeployment(besiegedSettlement.LocationComplex.GetLocationWithId("center").GetSceneName(wallLevel), besiegedSettlement.SettlementWallSectionHitPointsRatioList.ToArray(), hasAnySiegeTower, preparedAndActiveSiegeEngines, preparedAndActiveSiegeEngines2, PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker, wallLevel);
			break;
		}
		case Settlement.SiegeState.Invalid:
			Debug.FailedAssert("Siege state is invalid!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Siege\\PlayerSiege.cs", "StartSiegeMission", 181);
			break;
		}
	}
}
