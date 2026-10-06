using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.ComponentInterfaces;
using NavalDLC.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.CampaignBehaviors;

public class NavalForceStartNavalMissionCampaignBehavior : CampaignBehaviorBase
{
	private static bool _forceStartNavalMission = false;

	private const string DefaultTestSceneName = "battle_terrain_opensea_northern";

	private static string _sceneName = "battle_terrain_opensea_northern";

	private static int _enemyMeleeTroopCount = 30;

	private static int _enemyRangedTroopCount = 30;

	private static int _playerMeleeTroopCount = 30;

	private static int _playerRangedTroopCount = 30;

	private static bool _maximizeTroopCounts = true;

	private static MBList<string> _defaultShipHullIds = new MBList<string> { "northern_trade_ship", "nord_medium_ship", "vlandia_heavy_ship" };

	private static MBList<string>[] _shipHullIds = new MBList<string>[3]
	{
		new MBList<string>(_defaultShipHullIds),
		new MBList<string>(),
		new MBList<string>(_defaultShipHullIds)
	};

	private PartyBase _enemyParty;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		AddGameMenus(starter);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void AddGameMenus(CampaignGameStarter starter)
	{
		starter.AddGameMenuOption("encounter", "attack_naval", "{=!}Start Naval Mission (Cheat)", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Mission;
			return true;
		}, StartNavalBattle, isLeave: false, 2);
	}

	private void OnTick(float dt)
	{
		if (_forceStartNavalMission && GameStateManager.Current.ActiveState is MapState)
		{
			StartNavalMissionFromCheats();
			_forceStartNavalMission = false;
		}
	}

	private void HealPartiesInPlayerEncounterCheat()
	{
		foreach (MapEventParty item in MapEvent.PlayerMapEvent.PartiesOnSide(PlayerEncounter.Current.PlayerSide))
		{
			PartyBase party = item.Party;
			for (int i = 0; i < party.MemberRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = party.MemberRoster.GetElementCopyAtIndex(i);
				if (elementCopyAtIndex.Character.IsHero)
				{
					elementCopyAtIndex.Character.HeroObject.Heal(elementCopyAtIndex.Character.HeroObject.MaxHitPoints);
				}
				else
				{
					party.AddToMemberRosterElementAtIndex(i, 0, -party.MemberRoster.GetElementWoundedNumber(i));
				}
			}
		}
		foreach (MapEventParty item2 in MapEvent.PlayerMapEvent.PartiesOnSide(PlayerEncounter.Current.OpponentSide))
		{
			PartyBase party2 = item2.Party;
			for (int j = 0; j < party2.MemberRoster.Count; j++)
			{
				TroopRosterElement elementCopyAtIndex2 = party2.MemberRoster.GetElementCopyAtIndex(j);
				if (elementCopyAtIndex2.Character.IsHero)
				{
					elementCopyAtIndex2.Character.HeroObject.Heal(elementCopyAtIndex2.Character.HeroObject.MaxHitPoints);
				}
				else
				{
					party2.AddToMemberRosterElementAtIndex(j, 0, -party2.MemberRoster.GetElementWoundedNumber(j));
				}
			}
		}
	}

	private void StartNavalMissionFromCheats()
	{
		StartNavalMissionWithHandlingCheat();
	}

	private void StartNavalMissionWithHandlingCheat()
	{
		PartyBase mainParty = PartyBase.MainParty;
		if (PlayerEncounter.Current == null)
		{
			SetupTeamForEncounterCheat(TeamSideEnum.PlayerTeam, mainParty);
			if (_enemyParty == null)
			{
				IEnumerable<MobileParty> e = MobileParty.AllLordParties.Where((MobileParty x) => x.IsActive);
				_enemyParty = e.GetRandomElementInefficiently().Party;
				_enemyParty.MemberRoster.Clear();
			}
			SetupTeamForEncounterCheat(TeamSideEnum.EnemyTeam, _enemyParty);
			if (_enemyParty.Position.IsOnLand)
			{
				CampaignVec2 position = NavigationHelper.FindPointAroundPosition(Campaign.Current.Settlements.Where((Settlement x) => x.HasPort).GetRandomElementInefficiently().PortPosition, MobileParty.NavigationType.Naval, 10f, 1f);
				_enemyParty.MobileParty.Position = position;
			}
			PlayerEncounter.RestartPlayerEncounter(_enemyParty, mainParty);
		}
		else if (_enemyParty == null)
		{
			_enemyParty = MapEvent.PlayerMapEvent.PartiesOnSide(PlayerEncounter.Current.OpponentSide)[0].Party;
		}
		if (mainParty.Ships.Count == 0)
		{
			AddShipsToTeamPartyForEncounterCheat(TeamSideEnum.PlayerTeam, mainParty);
		}
		if (_enemyParty.Ships.Count == 0)
		{
			AddShipsToTeamPartyForEncounterCheat(TeamSideEnum.EnemyTeam, _enemyParty);
		}
		if (mainParty.MemberRoster.TotalManCount == 1)
		{
			AddTroopsToTeamPartyForEncounterCheat(TeamSideEnum.PlayerTeam, mainParty);
		}
		if (_enemyParty.MemberRoster.TotalManCount == 0)
		{
			AddTroopsToTeamPartyForEncounterCheat(TeamSideEnum.EnemyTeam, _enemyParty);
		}
		if (_enemyParty.Position.IsOnLand != mainParty.Position.IsOnLand)
		{
			if (_enemyParty.Position.IsOnLand)
			{
				_enemyParty.MobileParty.Position = mainParty.Position;
			}
			else
			{
				mainParty.MobileParty.Position = _enemyParty.Position;
			}
		}
		if (!_enemyParty.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
		{
			DeclareWarAction.ApplyByDefault(_enemyParty.MapFaction, Clan.PlayerClan.MapFaction);
		}
		if (PlayerEncounter.Battle == null)
		{
			PlayerEncounter.StartBattle();
		}
		HealPartiesInPlayerEncounterCheat();
		string name = ((!string.IsNullOrEmpty(_sceneName)) ? _sceneName : "battle_terrain_opensea_northern");
		MissionInitializerRecord rec = new MissionInitializerRecord(name);
		TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace);
		rec.TerrainType = (int)faceTerrainType;
		rec.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		rec.DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		rec.NeedsRandomTerrain = false;
		rec.PlayingInCampaignMode = true;
		rec.RandomTerrainSeed = MBRandom.RandomInt(10000);
		rec.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position);
		rec.SceneHasMapPatch = false;
		rec.DecalAtlasGroup = 2;
		NavalMissions.OpenNavalBattleMission(rec);
	}

	private void SetupTeamForEncounterCheat(TeamSideEnum teamSide, PartyBase teamParty)
	{
		foreach (TroopRosterElement item in teamParty.MemberRoster.GetTroopRoster())
		{
			if (item.Character != CharacterObject.PlayerCharacter)
			{
				teamParty.MemberRoster.RemoveTroop(item.Character, item.Number);
			}
		}
		foreach (Ship item2 in teamParty.Ships.ToList())
		{
			DestroyShipAction.Apply(item2);
		}
		AddShipsToTeamPartyForEncounterCheat(teamSide, teamParty);
		AddTroopsToTeamPartyForEncounterCheat(teamSide, teamParty);
	}

	private static void AddTroopsToTeamPartyForEncounterCheat(TeamSideEnum teamSide, PartyBase teamParty)
	{
		int maxMeleeTroopCount = 0;
		int maxRangedTroopCount = 0;
		if (_maximizeTroopCounts)
		{
			GetMaximumTroopCountForShipList(teamParty.Ships, out maxMeleeTroopCount, out maxRangedTroopCount);
		}
		else
		{
			switch (teamSide)
			{
			case TeamSideEnum.PlayerTeam:
				maxMeleeTroopCount = _playerMeleeTroopCount;
				maxRangedTroopCount = _playerRangedTroopCount;
				break;
			case TeamSideEnum.EnemyTeam:
				maxMeleeTroopCount = _enemyMeleeTroopCount;
				maxRangedTroopCount = _enemyRangedTroopCount;
				break;
			default:
				Debug.FailedAssert("This team side is not currently supported", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\CampaignBehaviors\\NavalForceStartNavalMissionCampaignBehavior.cs", "AddTroopsToTeamPartyForEncounterCheat", 287);
				break;
			}
		}
		teamParty.MemberRoster.AddToCounts(MBObjectManager.Instance.GetObject<CharacterObject>("imperial_recruit"), maxMeleeTroopCount);
		teamParty.MemberRoster.AddToCounts(MBObjectManager.Instance.GetObject<CharacterObject>("imperial_archer"), maxRangedTroopCount);
	}

	private static MBList<Ship> AddShipsToTeamPartyForEncounterCheat(TeamSideEnum teamSide, PartyBase teamParty)
	{
		MBList<Ship> defaultShipSet = GetDefaultShipSet(teamSide);
		foreach (Ship item in defaultShipSet)
		{
			ChangeShipOwnerAction.ApplyByLooting(teamParty, item);
		}
		return defaultShipSet;
	}

	private static void GetMaximumTroopCountForShipList(MBReadOnlyList<Ship> shipList, out int maxMeleeTroopCount, out int maxRangedTroopCount)
	{
		int num = shipList.Sum((Ship ship) => ship.TotalCrewCapacity);
		maxRangedTroopCount = num / 2;
		maxMeleeTroopCount = num - _playerRangedTroopCount;
	}

	private void StartNavalBattle(MenuCallbackArgs args)
	{
		StartNavalMissionWithHandlingCheat();
	}

	private static Ship CreateShip(string shipHullId)
	{
		ShipHull shipHull = MBObjectManager.Instance.GetObject<ShipHull>(shipHullId);
		if (shipHull != null)
		{
			return new Ship(shipHull);
		}
		return null;
	}

	private static MBList<Ship> GetDefaultShipSet(TeamSideEnum teamSide)
	{
		MBList<Ship> mBList = new MBList<Ship>();
		foreach (string item2 in _shipHullIds[(int)teamSide])
		{
			Ship item = CreateShip(item2);
			mBList.Add(item);
		}
		return mBList;
	}

	private static string GetMissionSettings()
	{
		string text = "Scene Name: " + _sceneName + "\nTroop Counts Maximized: " + _maximizeTroopCounts;
		if (!_maximizeTroopCounts)
		{
			text = text + "\nPlayer Melee Troop Count: " + _playerMeleeTroopCount + "\nPlayer Ranged Troop Count: " + _playerRangedTroopCount + "\nEnemy Melee Troop Count: " + _enemyMeleeTroopCount + "\nEnemy Ranged Troop Count: " + _enemyRangedTroopCount;
		}
		for (int i = 0; i < _shipHullIds.Length; i++)
		{
			MBList<string> mBList = _shipHullIds[i];
			if (!mBList.IsEmpty())
			{
				TeamSideEnum teamSideEnum = (TeamSideEnum)i;
				text = text + "\n" + teamSideEnum.ToString() + " Mission Ships:";
				int num = mBList.Count - 1;
				for (int j = 0; j < num; j++)
				{
					text = text + mBList[j] + ", ";
				}
				text += mBList[num];
			}
		}
		return text;
	}

	private static void ResetMissionSettings()
	{
		_sceneName = "battle_terrain_opensea_northern";
		_maximizeTroopCounts = false;
		_playerMeleeTroopCount = 30;
		_playerRangedTroopCount = 30;
		_enemyMeleeTroopCount = 30;
		_enemyRangedTroopCount = 30;
		ResetShipHullsToDefault();
	}

	private static void ResetShipHullsToDefault()
	{
		_shipHullIds[0].Clear();
		_shipHullIds[0].AddRange(_defaultShipHullIds);
		_shipHullIds[1].Clear();
		_shipHullIds[2].Clear();
		_shipHullIds[2].AddRange(_defaultShipHullIds);
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("get_mission_settings", "naval")]
	public static string GetMissionSettings(List<string> strings)
	{
		return GetMissionSettings();
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("reset_mission_settings", "naval")]
	public static string ResetMissionSettings(List<string> strings)
	{
		ResetMissionSettings();
		return "Mission settings reset successfully.\n" + GetMissionSettings();
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_mission_scene", "naval")]
	public static string SetMissionScene(List<string> strings)
	{
		if (strings.Count == 1)
		{
			_sceneName = strings[0];
			return "Mission scene is set to " + _sceneName;
		}
		return "usage: naval.set_mission_scene [SceneName]";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_mission_ships", "naval")]
	public static string SetMissionShips(List<string> strings)
	{
		bool flag = false;
		string text = "";
		TeamSideEnum teamSideEnum = TeamSideEnum.None;
		if (strings.Count == 0)
		{
			text += "Invalid number of arguments provided\n";
			flag = true;
		}
		if (strings.Count == 1)
		{
			string text2 = strings[0];
			if (text2.ToLower() == "help")
			{
				flag = true;
			}
			else if (text2.ToLower() == "default")
			{
				teamSideEnum = TeamSideEnum.NumSides;
				ResetShipHullsToDefault();
			}
			else
			{
				text += "Unable to parse single parameter argument.\nFor single parameter calls, the parameter must either be \"default\" or \"help\"\n";
				flag = true;
			}
		}
		else
		{
			switch (strings[0].ToLower())
			{
			case "player":
			case "playerTeam":
				teamSideEnum = TeamSideEnum.PlayerTeam;
				break;
			case "playerAlly":
			case "playerAllyTeam":
				teamSideEnum = TeamSideEnum.PlayerAllyTeam;
				break;
			case "enemy":
			case "enemyTeam":
				teamSideEnum = TeamSideEnum.EnemyTeam;
				break;
			}
			if (teamSideEnum.IsValid())
			{
				int num = (int)teamSideEnum;
				MBList<string> mBList = _shipHullIds[num];
				if (strings.Count == 2 && strings[1].ToLower() == "default")
				{
					mBList.Clear();
					mBList.AddRange(_defaultShipHullIds);
				}
				else
				{
					mBList.Clear();
					int num2 = strings.Count - 1;
					if (num2 > 8)
					{
						text += "At most 8 ships hull ids can be passed as parameter\n";
						num2 = 8;
					}
					for (int i = 0; i < num2; i++)
					{
						string text3 = strings[i + 1];
						MBObjectManager instance = MBObjectManager.Instance;
						if (instance != null)
						{
							if (instance.GetObject<ShipHull>(text3) != null)
							{
								mBList.Add(text3);
							}
							else
							{
								text = text + "Passed ship hull id: " + text3 + " does not refer to a valid ship hull. Omitting this\n";
							}
						}
						else
						{
							mBList.Add(text3);
						}
					}
					if (mBList.IsEmpty())
					{
						text += "None of the passed ship hull ids refer to a valid ship hull\n";
						text = text + "Reverting to default ship hulls for " + teamSideEnum.ToString().ToLower() + "\n";
						if (teamSideEnum != TeamSideEnum.PlayerAllyTeam)
						{
							mBList.AddRange(_defaultShipHullIds);
						}
					}
				}
			}
			else
			{
				text += "Unable to parse team side argument\nIt must refer to a valid team side like \"player\",\"playerAlly\" or \"enemy\"\n";
				flag = true;
			}
		}
		if (flag)
		{
			text += "Mission will be loaded with the specified ship hulls for the given team\n\nUsage: naval.set_mission_ships [TeamSide] [ShipHullId0] [ShipHullId1] ...\n\n- TeamSide: is the side of the team for which starting ships will be changed.\n  Can be \"player\", \"playerAlly\" or \"enemy\"\n- ShipHullId(s): are the hull id(s) of the ships to be spawned for the given side.\n  These must exist in ShipHulls.xml file.\n\nRemarks: Passing \"default\" as the first parameter will reset ships to default for all teams\n          Passing \"default\" as the second parameter after the TeamSide parameter will set ships to default\n         for only the given team";
		}
		else if (teamSideEnum == TeamSideEnum.NumSides)
		{
			text += "Player and enemy teams will start with their default ships:\n";
			int num3 = _defaultShipHullIds.Count - 1;
			for (int j = 0; j < num3; j++)
			{
				text = text + _defaultShipHullIds[j] + ", ";
			}
			text = text + _defaultShipHullIds[num3] + "\n";
		}
		else if (teamSideEnum.IsValid())
		{
			int num4 = (int)teamSideEnum;
			text = text + teamSideEnum.ToString() + " will use the following ships:\n";
			MBList<string> mBList2 = _shipHullIds[num4];
			int num5 = mBList2.Count - 1;
			for (int k = 0; k < num5; k++)
			{
				text = text + mBList2[k] + ", ";
			}
			text = text + mBList2[num5] + "\n";
		}
		return text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_maximize_troop_counts", "naval")]
	public static string SetMaximizeTroopCounts(List<string> strings)
	{
		bool flag = false;
		string text = "";
		if (strings.Count == 1)
		{
			if (strings[0].ToLower() == "help")
			{
				flag = true;
			}
			else if (strings[0] == "1" || strings[0] == "0")
			{
				_maximizeTroopCounts = strings[0] == "1";
			}
			else
			{
				text = "Unable to parse parameter.\n";
				flag = true;
			}
		}
		else
		{
			_maximizeTroopCounts = !_maximizeTroopCounts;
		}
		if (flag)
		{
			return text + "\nIf set, mission will start with all ships having maximum number of troops\nusage: naval.set_maximize_troop_counts [value]\n- value: If passed 1 setting is enabled, if passed 0 it is disabled. Omitting the parameter toggles the setting";
		}
		if (_maximizeTroopCounts)
		{
			return text + "Troops counts will be maximized in next mission";
		}
		return text + "Troops counts will be specified manually in next mission\n- Player Melee Troop Count:" + _playerMeleeTroopCount + "\n- Player Ranged Troop Count:" + _playerRangedTroopCount + "\n- Enemy Melee Troop Count:" + _enemyMeleeTroopCount + "\n- Enemy Ranged Troop Count:" + _enemyRangedTroopCount;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_mission_troop_counts", "naval")]
	public static string SetMissionTroopCounts(List<string> strings)
	{
		string text = "";
		bool flag = false;
		if (strings.Count == 1 && strings[0].ToLower() == "help")
		{
			flag = true;
		}
		else if (strings.Count == 4 && int.TryParse(strings[0] ?? "error", out _playerMeleeTroopCount) && int.TryParse(strings[1] ?? "error", out _playerRangedTroopCount) && int.TryParse(strings[2] ?? "error", out _enemyMeleeTroopCount) && int.TryParse(strings[3] ?? "error", out _enemyRangedTroopCount))
		{
			if (_maximizeTroopCounts)
			{
				_maximizeTroopCounts = false;
				text += "Troop count maximization disabled\n";
			}
			text = text + "Mission troop counts are successfully set.\n- Player Melee Troop Count:" + _playerMeleeTroopCount + "\n- Player Ranged Troop Count:" + _playerRangedTroopCount + "\n- Enemy Melee Troop Count:" + _enemyMeleeTroopCount + "\n- Enemy Ranged Troop Count:" + _enemyRangedTroopCount;
		}
		else
		{
			text += "Unable to parse one or more of the parameters.\n";
			flag = true;
		}
		if (flag)
		{
			text += "usage: naval.set_mission_troop_counts [PlayerMeleeTroopCount] [PlayerRangedTroopCount] [EnemyMeleeTroopCount] [EnemyRangedTroopCount]";
		}
		return text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("start_mission", "naval")]
	public static string StartMission(List<string> strings)
	{
		if (!_forceStartNavalMission)
		{
			_forceStartNavalMission = true;
			ShipDeploymentModel.IgnoreDeploymentLimits = true;
			if (GameStateManager.Current.ActiveState is InitialState)
			{
				Module.CurrentModule.ExecuteInitialStateOptionWithId("SandBoxNewGame");
			}
			else
			{
				ModuleInfo moduleInfo = ModuleHelper.GetModuleInfo("NavalDLC");
				if (moduleInfo == null || !moduleInfo.IsActive)
				{
					_forceStartNavalMission = false;
					return "Naval DLC module isn't active!";
				}
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.UnstoppableFastForward;
			}
		}
		return "Starting mission with current mission settings...\n" + GetMissionSettings();
	}
}
