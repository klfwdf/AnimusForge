using System;
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

namespace TaleWorlds.CampaignSystem.Siege
{
	// Token: 0x020002DC RID: 732
	public static class PlayerSiege
	{
		// Token: 0x170009BD RID: 2493
		// (get) Token: 0x060027DF RID: 10207 RVA: 0x000A5BD0 File Offset: 0x000A3DD0
		public static SiegeEvent PlayerSiegeEvent
		{
			get
			{
				SiegeEvent siegeEvent;
				if ((siegeEvent = MobileParty.MainParty.SiegeEvent) == null)
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

		// Token: 0x170009BE RID: 2494
		// (get) Token: 0x060027E0 RID: 10208 RVA: 0x000A5BF5 File Offset: 0x000A3DF5
		public static Settlement BesiegedSettlement
		{
			get
			{
				SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
				if (playerSiegeEvent == null)
				{
					return null;
				}
				return playerSiegeEvent.BesiegedSettlement;
			}
		}

		// Token: 0x170009BF RID: 2495
		// (get) Token: 0x060027E1 RID: 10209 RVA: 0x000A5C07 File Offset: 0x000A3E07
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

		// Token: 0x170009C0 RID: 2496
		// (get) Token: 0x060027E2 RID: 10210 RVA: 0x000A5C18 File Offset: 0x000A3E18
		public static bool IsRebellion
		{
			get
			{
				return PlayerSiege.BesiegedSettlement != null && PlayerSiege.BesiegedSettlement.IsUnderRebellionAttack();
			}
		}

		// Token: 0x060027E3 RID: 10211 RVA: 0x000A5C2D File Offset: 0x000A3E2D
		private static void SetPlayerSiegeEvent()
		{
		}

		// Token: 0x060027E4 RID: 10212 RVA: 0x000A5C2F File Offset: 0x000A3E2F
		public static void StartSiegePreparation()
		{
			if (Campaign.Current.CurrentMenuContext != null)
			{
				GameMenu.ExitToLast();
			}
			GameMenu.ActivateGameMenu("menu_siege_strategies");
		}

		// Token: 0x060027E5 RID: 10213 RVA: 0x000A5C4C File Offset: 0x000A3E4C
		public static void OnSiegeEventFinalized(bool besiegerPartyDefeated)
		{
			MapState mapState = Game.Current.GameStateManager.ActiveState as MapState;
			if (PlayerSiege.IsRebellion)
			{
				if (mapState != null && mapState.AtMenu)
				{
					GameMenu.ExitToLast();
					return;
				}
			}
			else if (PlayerSiege.PlayerSide == BattleSideEnum.Defender && !PlayerSiege.IsRebellion)
			{
				if (Settlement.CurrentSettlement != null)
				{
					if (mapState != null && !mapState.AtMenu)
					{
						GameMenu.ActivateGameMenu(besiegerPartyDefeated ? "siege_attacker_defeated" : "siege_attacker_left");
						return;
					}
					GameMenu.SwitchToMenu(besiegerPartyDefeated ? "siege_attacker_defeated" : "siege_attacker_left");
					return;
				}
			}
			else if (Hero.MainHero.PartyBelongedTo != null && Hero.MainHero.PartyBelongedTo.Army != null && Hero.MainHero.PartyBelongedTo.Army.LeaderParty != MobileParty.MainParty)
			{
				if (MobileParty.MainParty.CurrentSettlement != null)
				{
					LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
				}
				if (PlayerEncounter.Battle == null)
				{
					if (mapState != null)
					{
						if (mapState.AtMenu)
						{
							GameMenu.SwitchToMenu("army_wait");
							return;
						}
						GameMenu.ActivateGameMenu("army_wait");
						return;
					}
					else
					{
						Campaign.Current.GameMenuManager.SetNextMenu("army_wait");
					}
				}
			}
		}

		// Token: 0x060027E6 RID: 10214 RVA: 0x000A5D64 File Offset: 0x000A3F64
		public static void StartPlayerSiege(BattleSideEnum playerSide, bool isSimulation = false, Settlement settlement = null)
		{
			if (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
			{
				MobileParty.MainParty.SetMoveModeHold();
			}
			PlayerSiege.SetPlayerSiegeEvent();
			if (!isSimulation)
			{
				GameState gameState = Game.Current.GameStateManager.GameStates.FirstOrDefault((GameState s) => s is MapState);
				if (gameState != null)
				{
					MapState mapState = gameState as MapState;
					if (mapState != null)
					{
						mapState.OnPlayerSiegeActivated();
					}
				}
			}
			CampaignEventDispatcher.Instance.OnPlayerSiegeStarted();
		}

		// Token: 0x060027E7 RID: 10215 RVA: 0x000A5DF8 File Offset: 0x000A3FF8
		public static void FinalizePlayerSiege()
		{
			if (PlayerSiege.PlayerSiegeEvent == null)
			{
				return;
			}
			PlayerSiege.BesiegedSettlement.Party.SetVisualAsDirty();
			MobileParty.MainParty.SetMoveModeHold();
			GameState gameState = Game.Current.GameStateManager.GameStates.FirstOrDefault((GameState s) => s is MapState);
			if (gameState != null)
			{
				MapState mapState = gameState as MapState;
				if (mapState == null)
				{
					return;
				}
				mapState.OnPlayerSiegeDeactivated();
			}
		}

		// Token: 0x060027E8 RID: 10216 RVA: 0x000A5E70 File Offset: 0x000A4070
		public static void StartSiegeMission(Settlement settlement = null)
		{
			Settlement besiegedSettlement = PlayerSiege.BesiegedSettlement;
			Settlement.SiegeState currentSiegeState = besiegedSettlement.CurrentSiegeState;
			if (currentSiegeState == Settlement.SiegeState.OnTheWalls)
			{
				List<MissionSiegeWeapon> preparedAndActiveSiegeEngines = PlayerSiege.PlayerSiegeEvent.GetPreparedAndActiveSiegeEngines(PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker));
				List<MissionSiegeWeapon> preparedAndActiveSiegeEngines2 = PlayerSiege.PlayerSiegeEvent.GetPreparedAndActiveSiegeEngines(PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender));
				bool hasAnySiegeTower = preparedAndActiveSiegeEngines.Exists((MissionSiegeWeapon data) => data.Type == DefaultSiegeEngineTypes.SiegeTower);
				int wallLevel = besiegedSettlement.Town.GetWallLevel();
				CampaignMission.OpenSiegeMissionWithDeployment(besiegedSettlement.LocationComplex.GetLocationWithId("center").GetSceneName(wallLevel), besiegedSettlement.SettlementWallSectionHitPointsRatioList.ToArray(), hasAnySiegeTower, preparedAndActiveSiegeEngines, preparedAndActiveSiegeEngines2, PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker, wallLevel, false, false);
				return;
			}
			if (currentSiegeState != Settlement.SiegeState.Invalid)
			{
				return;
			}
			Debug.FailedAssert("Siege state is invalid!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Siege\\PlayerSiege.cs", "StartSiegeMission", 181);
		}
	}
}
