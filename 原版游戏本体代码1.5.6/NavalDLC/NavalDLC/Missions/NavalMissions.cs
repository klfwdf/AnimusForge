using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.Missions.Deployment;
using NavalDLC.Missions.Handlers;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Storyline;
using NavalDLC.Storyline.MissionControllers;
using SandBox;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;

namespace NavalDLC.Missions;

[MissionManager]
public static class NavalMissions
{
	[MissionMethod]
	public static Mission OpenNavalBattleMission(MissionInitializerRecord rec)
	{
		MobileParty mainParty = MobileParty.MainParty;
		MapEvent mapEvent = mainParty.MapEvent;
		bool isPlayerSergeant = mapEvent.IsPlayerSergeant();
		bool isPlayerInArmy = mainParty.Army != null;
		bool isPlayerAttacker = !mapEvent.AttackerSide.Parties.Where((MapEventParty p) => p.Party == mainParty.Party).IsEmpty();
		rec.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		Mission mission = NavalMissionState.OpenNew("NavalBattle", rec, delegate(Mission mission2)
		{
			IMissionTroopSupplier[] suppliers = new IMissionTroopSupplier[2]
			{
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender),
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker)
			};
			BattleSideEnum playerSide = mapEvent.PlayerSide;
			BattleSideEnum otherSide = mapEvent.GetOtherSide(playerSide);
			MBReadOnlyList<MapEventParty> parties = mapEvent.GetMapEventSide(playerSide).Parties;
			NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetMapEventPartiesOfPlayerTeams(parties, isPlayerSergeant, out var playerMapEventParty, out var playerTeamMapEventParties, out var playerAllyTeamMapEventParties);
			NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetShipDeploymentLimitsOfPlayerTeams(playerTeamMapEventParties, playerAllyTeamMapEventParties, out var playerTeamDeploymentLimit, out var playerAllyTeamDeploymentLimit);
			MBList<IShipOrigin> mBList = new MBList<IShipOrigin>();
			Ship suitablePlayerShip = NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetSuitablePlayerShip(playerMapEventParty, playerTeamMapEventParties);
			mBList.Add(suitablePlayerShip);
			NavalDLCManager.Instance.GameModels.ShipDeploymentModel.FillShipsOfTeamParties(playerTeamMapEventParties, playerTeamDeploymentLimit, mBList);
			NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetOrderedCaptainsForPlayerTeamShips(playerTeamMapEventParties, mBList, out var playerTeamCaptainsByPriority);
			MBList<IShipOrigin> mBList2 = new MBList<IShipOrigin>();
			if (!playerAllyTeamMapEventParties.IsEmpty())
			{
				NavalDLCManager.Instance.GameModels.ShipDeploymentModel.FillShipsOfTeamParties(playerAllyTeamMapEventParties, playerAllyTeamDeploymentLimit, mBList2);
			}
			MBList<MapEventParty> teamMapEventParties = mapEvent.GetMapEventSide(otherSide).Parties.ToMBList();
			NavalShipDeploymentLimit teamShipDeploymentLimit = NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetTeamShipDeploymentLimit(teamMapEventParties);
			MBList<IShipOrigin> mBList3 = new MBList<IShipOrigin>();
			NavalDLCManager.Instance.GameModels.ShipDeploymentModel.FillShipsOfTeamParties(teamMapEventParties, teamShipDeploymentLimit, mBList3);
			int deployablePlayerShipCount = MathF.Min(mBList.Count, playerTeamDeploymentLimit.NetDeploymentLimitWithoutSkeletal);
			int maximumDeployableTroopCountForTeam = NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetMaximumDeployableTroopCountForTeam(mBList, isPlayerTeam: true);
			int maximumDeployableTroopCountForTeam2 = NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetMaximumDeployableTroopCountForTeam(mBList2);
			int maximumDeployableTroopCountForTeam3 = NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetMaximumDeployableTroopCountForTeam(mBList3);
			int[] maxDeployableTroopCountPerTeam = new int[3] { maximumDeployableTroopCountForTeam, maximumDeployableTroopCountForTeam2, maximumDeployableTroopCountForTeam3 };
			return new MissionBehavior[31]
			{
				new NavalShipsLogic(),
				new NavalFloatsamLogic(),
				new NavalAgentsLogic(),
				new DefaultNavalMissionLogic(mBList, mBList2, mBList3, playerTeamDeploymentLimit, playerAllyTeamDeploymentLimit, teamShipDeploymentLimit),
				new NavalTrajectoryPlanningLogic(),
				new DefaultNavalMissionAgentSpawnLogic(suppliers, playerSide, deployablePlayerShipCount, maxDeployableTroopCountPerTeam),
				new NavalMissionDeploymentPlanningLogic(mission2),
				new BattlePowerCalculationLogic(),
				new NavalBattleAgentLogic(),
				new WaveParametersComputerLogic(),
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new NavalAgentMoraleInteractionLogic(),
				new NavalBattleEndLogic(),
				new NavalMissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NavalBattle, isPlayerSergeant),
				new BattleObserverMissionLogic(),
				new AgentHumanAILogic(),
				new AgentVictoryLogic(),
				new ShipCollisionOutcomeLogic(mission2),
				new ShipRetreatLogic(),
				new NavalBoundaryForceFieldLogic(),
				new BattleMissionAgentInteractionLogic(),
				new NavalAssignPlayerRoleInTeamMissionController(!isPlayerSergeant, isPlayerSergeant, isPlayerInArmy, playerTeamCaptainsByPriority),
				new EquipmentControllerLeaveLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(30f),
				new HighlightsController(),
				new BattleHighlightsController(),
				new NavalDeploymentMissionController(isPlayerAttacker),
				new NavalDeploymentHandler(isPlayerAttacker)
			};
		});
		mission.SetPlayerCanTakeControlOfAnotherAgentWhenDead();
		return mission;
	}

	[MissionMethod]
	public static Mission OpenNavalRaidMission(TroopRoster navalRaidTroops, BattleSideEnum navalSide, List<Ship> allShips)
	{
		Settlement mapEventSettlement = PlayerEncounter.Battle.MapEventSettlement;
		string scene = mapEventSettlement.LocationComplex.GetScene("village_center", 1);
		MissionInitializerRecord missionInitializerRecord = new MissionInitializerRecord(scene);
		missionInitializerRecord.TerrainType = 11;
		missionInitializerRecord.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		missionInitializerRecord.DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		missionInitializerRecord.NeedsRandomTerrain = false;
		missionInitializerRecord.PlayingInCampaignMode = true;
		missionInitializerRecord.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(mapEventSettlement.Position);
		missionInitializerRecord.SceneHasMapPatch = false;
		missionInitializerRecord.DecalAtlasGroup = 2;
		MissionInitializerRecord rec = missionInitializerRecord;
		rec.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		rec.SceneLevels = "naval_raid";
		MBList<IShipOrigin> navalSideShips = new MBList<IShipOrigin>();
		foreach (Ship allShip in allShips)
		{
			navalSideShips.Add(allShip);
		}
		Mission mission = NavalMissionState.OpenNew("NavalRaid", rec, delegate(Mission mission2)
		{
			MapEvent mapEvent = MobileParty.MainParty.MapEvent;
			BattleSideEnum otherSide = mapEvent.GetOtherSide(navalSide);
			IMissionTroopSupplier[] array = new IMissionTroopSupplier[2];
			array[(int)otherSide] = new PartyGroupTroopSupplier(mapEvent, otherSide);
			array[(int)navalSide] = new PartyGroupTroopSupplier(mapEvent, navalSide, navalRaidTroops.ToFlattenedRoster());
			NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetOrderedCaptainsForPlayerTeamShips(mapEvent.PartiesOnSide(navalSide), navalSideShips, out var playerTeamCaptainsByPriority);
			int totalManCount = navalRaidTroops.TotalManCount;
			int totalHealthyTroopCountOfSide = mapEvent.GetMapEventSide(otherSide).GetTotalHealthyTroopCountOfSide();
			bool flag = navalSide == BattleSideEnum.Attacker;
			bool isPlayerAttacker = mapEvent.PlayerSide == BattleSideEnum.Attacker;
			NavalRaidMissionAgentSpawnLogic.ComputeInitialTroopCounts(flag ? totalManCount : totalHealthyTroopCountOfSide, flag ? totalHealthyTroopCountOfSide : totalManCount, out var initialAttackerTroopCount, out var initialDefenderTroopCount);
			return new MissionBehavior[30]
			{
				new NavalShipsLogic(),
				new NavalFloatsamLogic(),
				new NavalAgentsLogic(),
				new NavalRaidMissionController(),
				new NavalRaidMissionAgentSpawnLogic(array, mapEvent.PlayerSide, navalSideShips, new NavalShipDeploymentLimit(navalSideShips.Count), initialAttackerTroopCount, initialDefenderTroopCount),
				new NavalTrajectoryPlanningLogic(),
				new NavalRaidMissionDeploymentPlanningLogic(),
				new BattlePowerCalculationLogic(),
				new NavalBattleAgentLogic(),
				new WaveParametersComputerLogic(),
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new NavalAgentMoraleInteractionLogic(),
				new BattleEndLogic(),
				new NavalMissionCombatantsLogic(mapEvent.InvolvedParties, PartyBase.MainParty, flag ? mapEvent.GetLeaderParty(otherSide) : mapEvent.GetLeaderParty(navalSide), flag ? mapEvent.GetLeaderParty(navalSide) : mapEvent.GetLeaderParty(otherSide), Mission.MissionTeamAITypeEnum.NavalRaid, mapEvent.IsPlayerSergeant()),
				new BattleObserverMissionLogic(),
				new AgentHumanAILogic(),
				new AgentVictoryLogic(),
				new ShipCollisionOutcomeLogic(mission2),
				new NavalBoundaryForceFieldLogic(),
				new BattleMissionAgentInteractionLogic(),
				new NavalAssignPlayerRoleInTeamMissionController(!mapEvent.IsPlayerSergeant(), mapEvent.IsPlayerSergeant(), MobileParty.MainParty.Army != null, playerTeamCaptainsByPriority),
				new EquipmentControllerLeaveLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(30f),
				new HighlightsController(),
				new BattleHighlightsController(),
				new NavalRaidDeploymentMissionController(isPlayerAttacker),
				new NavalRaidDeploymentHandler(isPlayerAttacker)
			};
		});
		mission.SetPlayerCanTakeControlOfAnotherAgentWhenDead();
		return mission;
	}

	[MissionMethod]
	public static Mission OpenNavalSetPieceBattleMission(MissionInitializerRecord rec, MBList<IShipOrigin> playerShips, MBList<IShipOrigin> playerAllyShips, MBList<IShipOrigin> enemyShips)
	{
		bool isPlayerSergeant = MobileParty.MainParty.MapEvent.IsPlayerSergeant();
		bool isPlayerInArmy = MobileParty.MainParty.Army != null;
		List<string> heroesOnPlayerSideByPriority = HeroHelper.OrderHeroesOnPlayerSideByPriority();
		bool isPlayerAttacker = !MobileParty.MainParty.MapEvent.AttackerSide.Parties.Where((MapEventParty p) => p.Party == MobileParty.MainParty.Party).IsEmpty();
		rec.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		return NavalMissionState.OpenNew("NavalBattle", rec, delegate(Mission mission)
		{
			IMissionTroopSupplier[] suppliers = new IMissionTroopSupplier[2]
			{
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender),
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker)
			};
			BattleSideEnum playerSide = MobileParty.MainParty.MapEvent.PlayerSide;
			NavalShipDeploymentLimit playerTeamShipDeploymentLimit = NavalShipDeploymentLimit.Max();
			NavalShipDeploymentLimit playerAllyTeamShipDeploymentLimit = NavalShipDeploymentLimit.Max();
			NavalShipDeploymentLimit enemyTeamShipDeploymentLimit = NavalShipDeploymentLimit.Max();
			int deployablePlayerShipCount = MathF.Min(playerShips.Count, 8);
			int maximumDeployableTroopCountForTeam = NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetMaximumDeployableTroopCountForTeam(playerShips, isPlayerTeam: true);
			int maximumDeployableTroopCountForTeam2 = NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetMaximumDeployableTroopCountForTeam(playerAllyShips);
			int maximumDeployableTroopCountForTeam3 = NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetMaximumDeployableTroopCountForTeam(enemyShips);
			int[] maxDeployableTroopCountPerTeam = new int[3] { maximumDeployableTroopCountForTeam, maximumDeployableTroopCountForTeam2, maximumDeployableTroopCountForTeam3 };
			return new MissionBehavior[28]
			{
				new NavalShipsLogic(),
				new NavalFloatsamLogic(),
				new NavalAgentsLogic(),
				new DefaultNavalMissionLogic(playerShips, playerAllyShips, enemyShips, playerTeamShipDeploymentLimit, playerAllyTeamShipDeploymentLimit, enemyTeamShipDeploymentLimit),
				new NavalTrajectoryPlanningLogic(),
				new DefaultNavalMissionAgentSpawnLogic(suppliers, playerSide, deployablePlayerShipCount, maxDeployableTroopCountPerTeam),
				new NavalMissionDeploymentPlanningLogic(mission),
				new BattlePowerCalculationLogic(),
				new NavalBattleAgentLogic(),
				new WaveParametersComputerLogic(),
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new NavalBattleEndLogic(),
				new NavalMissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NavalBattle, isPlayerSergeant),
				new BattleObserverMissionLogic(),
				new AgentHumanAILogic(),
				new AgentVictoryLogic(),
				new ShipCollisionOutcomeLogic(mission),
				new BattleMissionAgentInteractionLogic(),
				new NavalAssignPlayerRoleInTeamMissionController(!isPlayerSergeant, isPlayerSergeant, isPlayerInArmy, heroesOnPlayerSideByPriority),
				new EquipmentControllerLeaveLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(30f),
				new HighlightsController(),
				new BattleHighlightsController(),
				new NavalDeploymentMissionController(isPlayerAttacker),
				new NavalDeploymentHandler(isPlayerAttacker)
			};
		});
	}

	[MissionMethod]
	public static Mission OpenBlockedEstuaryMission(MissionInitializerRecord rec, MobileParty enemyParty, bool startFromCheckPoint)
	{
		NavalStorylineData.SetNavalStorylineSetPieceBattleMissionType(NavalStorylineData.NavalStorylineSetPieceBattleMissionTypes.Act3Quest4);
		bool isPlayerSergeant = MobileParty.MainParty.MapEvent.IsPlayerSergeant();
		bool isPlayerInArmy = MobileParty.MainParty.Army != null;
		List<string> heroesOnPlayerSideByPriority = HeroHelper.OrderHeroesOnPlayerSideByPriority();
		MobileParty.MainParty.MapEvent.AttackerSide.Parties.Where((MapEventParty p) => p.Party == MobileParty.MainParty.Party).IsEmpty();
		rec.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		return NavalMissionState.OpenNew("BlockedEstuary", rec, delegate(Mission mission)
		{
			IMissionTroopSupplier[] suppliers = new IMissionTroopSupplier[2]
			{
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender),
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker)
			};
			BattleSideEnum playerSide = MobileParty.MainParty.MapEvent.PlayerSide;
			return new MissionBehavior[25]
			{
				new NavalShipsLogic(),
				new NavalFloatsamLogic(),
				new NavalAgentsLogic(),
				new NavalTrajectoryPlanningLogic(),
				new DefaultNavalMissionAgentSpawnLogic(suppliers, playerSide),
				new BattlePowerCalculationLogic(),
				new NavalBattleAgentLogic(),
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new BlockedEstuaryMissionController(enemyParty, startFromCheckPoint),
				new BlockedEstuaryBattleEndLogic(),
				new NavalMissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NavalBattle, isPlayerSergeant),
				new BattleObserverMissionLogic(),
				new AgentHumanAILogic(),
				new AgentVictoryLogic(),
				new ShipCollisionOutcomeLogic(mission),
				new MissionObjectiveLogic(),
				new BattleMissionAgentInteractionLogic(),
				new NavalAssignPlayerRoleInTeamMissionController(!isPlayerSergeant, isPlayerSergeant, isPlayerInArmy, heroesOnPlayerSideByPriority),
				new EquipmentControllerLeaveLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(30f),
				new HighlightsController(),
				new BattleHighlightsController()
			};
		});
	}

	[MissionMethod]
	public static Mission OpenNavalStorylineCaptivityMission(MissionInitializerRecord rec, CharacterObject allyCharacter, CharacterObject enemyCharacter, CharacterObject crewCharacter)
	{
		NavalStorylineData.SetNavalStorylineSetPieceBattleMissionType(NavalStorylineData.NavalStorylineSetPieceBattleMissionTypes.Act1);
		bool isPlayerSergeant = MobileParty.MainParty.MapEvent.IsPlayerSergeant();
		_ = MobileParty.MainParty.Army;
		HeroHelper.OrderHeroesOnPlayerSideByPriority();
		rec.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		return NavalMissionState.OpenNew("NavalCaptivityBattle", rec, delegate
		{
			_ = new IMissionTroopSupplier[2]
			{
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender),
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker)
			};
			BattleSideEnum playerSide = MobileParty.MainParty.MapEvent.PlayerSide;
			BattleSideEnum otherSide = MobileParty.MainParty.MapEvent.GetOtherSide(playerSide);
			MBList<IShipOrigin> mBList = new MBList<IShipOrigin>();
			MBList<IShipOrigin> mBList2 = new MBList<IShipOrigin>();
			MBList<IShipOrigin> mBList3 = new MBList<IShipOrigin>();
			mBList.AddRange(MobileParty.MainParty.Ships);
			foreach (MapEventParty party in MobileParty.MainParty.MapEvent.GetMapEventSide(playerSide).Parties)
			{
				if (party.IsNpcParty)
				{
					mBList2.AddRange(party.Party.Ships);
				}
			}
			foreach (MapEventParty party2 in MobileParty.MainParty.MapEvent.GetMapEventSide(otherSide).Parties)
			{
				mBList3.AddRange(party2.Party.Ships);
			}
			return new MissionBehavior[21]
			{
				new NavalShipsLogic(),
				new NavalFloatsamLogic(),
				new NavalAgentsLogic(),
				new NavalStorylineCaptivityMissionController(allyCharacter, enemyCharacter, crewCharacter),
				new MissionHintLogic(),
				new NavalTrajectoryPlanningLogic(),
				new NavalBattleAgentLogic(),
				new VisualTrackerMissionBehavior(),
				new MissionFightHandler(),
				new WaveParametersComputerLogic(),
				new MissionObjectiveLogic(),
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NavalBattle, isPlayerSergeant),
				new AgentHumanAILogic(),
				new BattleMissionAgentInteractionLogic(),
				new EquipmentControllerLeaveLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new HighlightsController(),
				new BattleHighlightsController()
			};
		});
	}

	[MissionMethod]
	public static Mission OpenNavalStorylinePirateBattleMission(MissionInitializerRecord rec, MobileParty pirateParty, int pirateTroopCount)
	{
		NavalStorylineData.SetNavalStorylineSetPieceBattleMissionType(NavalStorylineData.NavalStorylineSetPieceBattleMissionTypes.Act2);
		bool isPlayerSergeant = MobileParty.MainParty.MapEvent.IsPlayerSergeant();
		rec.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		return NavalMissionState.OpenNew("NavalStorylinePirateBattle", rec, delegate(Mission mission)
		{
			IMissionTroopSupplier[] suppliers = new IMissionTroopSupplier[2]
			{
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender),
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker)
			};
			BattleSideEnum playerSide = MobileParty.MainParty.MapEvent.PlayerSide;
			BattleSideEnum otherSide = MobileParty.MainParty.MapEvent.GetOtherSide(playerSide);
			MBList<IShipOrigin> mBList = new MBList<IShipOrigin>();
			MBList<IShipOrigin> mBList2 = new MBList<IShipOrigin>();
			MBList<IShipOrigin> mBList3 = new MBList<IShipOrigin>();
			mBList.AddRange(MobileParty.MainParty.Ships);
			foreach (MapEventParty party in MobileParty.MainParty.MapEvent.GetMapEventSide(playerSide).Parties)
			{
				if (party.IsNpcParty)
				{
					mBList2.AddRange(party.Party.Ships);
				}
			}
			foreach (MapEventParty party2 in MobileParty.MainParty.MapEvent.GetMapEventSide(otherSide).Parties)
			{
				mBList3.AddRange(party2.Party.Ships);
			}
			return new MissionBehavior[25]
			{
				new NavalShipsLogic(),
				new NavalFloatsamLogic(),
				new NavalAgentsLogic(),
				new NavalTrajectoryPlanningLogic(),
				new PirateBattleMissionController(pirateParty, pirateTroopCount),
				new NavalBattleAgentLogic(),
				new MissionFightHandler(),
				new WaveParametersComputerLogic(),
				new DefaultNavalMissionAgentSpawnLogic(suppliers, playerSide),
				new BattlePowerCalculationLogic(),
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new BattleObserverMissionLogic(),
				new NavalMissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NavalBattle, isPlayerSergeant),
				new AgentHumanAILogic(),
				new BattleMissionAgentInteractionLogic(),
				new EquipmentControllerLeaveLogic(),
				new NavalAgentMoraleInteractionLogic(),
				new ShipCollisionOutcomeLogic(mission),
				new MissionObjectiveLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(30f),
				new HighlightsController(),
				new BattleHighlightsController()
			};
		});
	}

	[MissionMethod]
	public static Mission OpenNavalStorylineQuest5SetPieceBattleMission(MissionInitializerRecord rec, MobileParty enemyParty, Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState lastHitCheckpoint = Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase1Part1)
	{
		NavalStorylineData.SetNavalStorylineSetPieceBattleMissionType(NavalStorylineData.NavalStorylineSetPieceBattleMissionTypes.Act3Quest5);
		bool isPlayerSergeant = MobileParty.MainParty.MapEvent.IsPlayerSergeant();
		rec.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		return NavalMissionState.OpenNew("NavalStorylineQuest5SetPieceBattleMission", rec, delegate(Mission mission)
		{
			_ = new IMissionTroopSupplier[2]
			{
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender),
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker)
			};
			BattleSideEnum playerSide = MobileParty.MainParty.MapEvent.PlayerSide;
			BattleSideEnum otherSide = MobileParty.MainParty.MapEvent.GetOtherSide(playerSide);
			MBList<IShipOrigin> mBList = new MBList<IShipOrigin>();
			MBList<IShipOrigin> mBList2 = new MBList<IShipOrigin>();
			MBList<IShipOrigin> mBList3 = new MBList<IShipOrigin>();
			mBList.AddRange(MobileParty.MainParty.Ships);
			foreach (MapEventParty party in MobileParty.MainParty.MapEvent.GetMapEventSide(playerSide).Parties)
			{
				if (party.IsNpcParty)
				{
					mBList2.AddRange(party.Party.Ships);
				}
			}
			foreach (MapEventParty party2 in MobileParty.MainParty.MapEvent.GetMapEventSide(otherSide).Parties)
			{
				mBList3.AddRange(party2.Party.Ships);
			}
			List<MissionBehavior> result = new List<MissionBehavior>
			{
				new NavalShipsLogic(),
				new NavalFloatsamLogic(),
				new NavalAgentsLogic(),
				new MissionObjectiveLogic(),
				new NavalTrajectoryPlanningLogic(),
				new Quest5NavalMissionDeploymentPlanningLogic(mission),
				new Quest5SetPieceBattleMissionController(lastHitCheckpoint, enemyParty),
				new NavalBattleAgentLogic(),
				new MissionFightHandler(),
				new CosmeticShipSpawnMissionLogic(),
				new LightScriptedFiresMissionController(),
				new BattlePowerCalculationLogic(),
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new Quest5BattleObserverMissionLogic(),
				new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NavalBattle, isPlayerSergeant),
				new AgentHumanAILogic(),
				new EquipmentControllerLeaveLogic(),
				new MissionConversationLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(30f),
				new HighlightsController(),
				new BattleHighlightsController(),
				new StealthPatrolPointMissionLogic()
			};
			if (lastHitCheckpoint != Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase1Part1)
			{
				_ = lastHitCheckpoint;
				_ = 5;
			}
			return result;
		});
	}

	[MissionMethod]
	public static Mission OpenNavalFinalConversationMission()
	{
		int wallLevel = Settlement.CurrentSettlement.Town.GetWallLevel();
		string civilianUpgradeLevelTag = Campaign.Current.Models.LocationModel.GetCivilianUpgradeLevelTag(wallLevel);
		Location location = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("port");
		List<Ship> townLordShips = new List<Ship>();
		List<Ship> mainPartyShips = MobileParty.MainParty.Ships.ToList();
		foreach (MobileParty party in Settlement.CurrentSettlement.Parties)
		{
			townLordShips.AddRange(party.Ships);
		}
		return MissionState.OpenNew("NavalFinalConversationMission", SandBoxMissions.CreateSandBoxMissionInitializerRecord(location.GetSceneName(wallLevel), civilianUpgradeLevelTag, doNotUseLoadingScreen: true, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[23]
		{
			new MissionOptionsComponent(),
			new CampaignMissionComponent(),
			new MissionBasicTeamLogic(),
			new BasicLeaveMissionLogic(),
			new LeaveMissionLogic(),
			new SandBoxMissionHandler(),
			new MissionAgentLookHandler(),
			new MissionConversationLogic(),
			new MissionAgentHandler(),
			new MissionLocationLogic(location),
			new HeroSkillHandler(),
			new MissionFightHandler(),
			new BattleAgentLogic(),
			new MountAgentLogic(),
			new AgentHumanAILogic(),
			new MissionCrimeHandler(),
			new MissionFacialAnimationHandler(),
			new LocationItemSpawnHandler(),
			new IndoorMissionController(),
			new VisualTrackerMissionBehavior(),
			new EquipmentControllerLeaveLogic(),
			new BattleSurgeonLogic(),
			new CivilianPortShipSpawnMissionLogic(mainPartyShips, townLordShips)
		});
	}

	[MissionMethod]
	public static Mission OpenNavalStorylineWoundedBeastBattleMission(MissionInitializerRecord rec)
	{
		NavalStorylineData.SetNavalStorylineSetPieceBattleMissionType(NavalStorylineData.NavalStorylineSetPieceBattleMissionTypes.Act3Quest2);
		bool isPlayerSergeant = true;
		HeroHelper.OrderHeroesOnPlayerSideByPriority();
		IMissionTroopSupplier[] suppliers = new IMissionTroopSupplier[2];
		suppliers[0] = new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender);
		suppliers[1] = new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker);
		rec.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		BattleSideEnum playerSide = MobileParty.MainParty.MapEvent.PlayerSide;
		return NavalMissionState.OpenNew("NavalStorylineWoundedBeastBattle", rec, (Mission mission) => new MissionBehavior[27]
		{
			new NavalShipsLogic(),
			new NavalFloatsamLogic(),
			new NavalAgentsLogic(),
			new MissionObjectiveLogic(),
			new WoundedBeastMissionController(),
			new BattleAgentLogic(),
			new MissionFightHandler(),
			new WaveParametersComputerLogic(),
			new DefaultNavalMissionAgentSpawnLogic(suppliers, playerSide),
			new NavalTrajectoryPlanningLogic(),
			new BattlePowerCalculationLogic(),
			new MissionOptionsComponent(),
			new CampaignMissionComponent(),
			new BattleObserverMissionLogic(),
			new NavalMissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NavalBattle, isPlayerSergeant),
			new AgentHumanAILogic(),
			new BattleMissionAgentInteractionLogic(),
			new EquipmentControllerLeaveLogic(),
			new NavalAgentMoraleInteractionLogic(),
			new ShipCollisionOutcomeLogic(mission),
			new AgentVictoryLogic(),
			new NavalBattleEndLogic(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(30f),
			new HighlightsController(),
			new BattleHighlightsController()
		});
	}

	[MissionMethod]
	public static Mission OpenHelpingAnAllySetPieceBattleMission(MissionInitializerRecord rec, MobileParty merchantParty, MobileParty seaHoundsParty)
	{
		NavalStorylineData.SetNavalStorylineSetPieceBattleMissionType(NavalStorylineData.NavalStorylineSetPieceBattleMissionTypes.Act3Quest1);
		bool isPlayerSergeant = MobileParty.MainParty.MapEvent.IsPlayerSergeant();
		rec.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		return NavalMissionState.OpenNew("HelpAnAllySetPieceBattle", rec, delegate
		{
			_ = new IMissionTroopSupplier[2]
			{
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender),
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker)
			};
			return new MissionBehavior[21]
			{
				new NavalShipsLogic(),
				new NavalFloatsamLogic(),
				new NavalAgentsLogic(),
				new MissionObjectiveLogic(),
				new NavalTrajectoryPlanningLogic(),
				new HelpingAnAllySetPieceBattleMissionController(merchantParty, seaHoundsParty),
				new NavalBattleAgentLogic(),
				new BattlePowerCalculationLogic(),
				new MissionFightHandler(),
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new BattleObserverMissionLogic(),
				new NavalMissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NavalBattle, isPlayerSergeant),
				new AgentHumanAILogic(),
				new BattleMissionAgentInteractionLogic(),
				new EquipmentControllerLeaveLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(30f),
				new HighlightsController(),
				new BattleHighlightsController()
			};
		});
	}

	[MissionMethod]
	public static Mission OpenFloatingFortressSetPieceBattleMission(MissionInitializerRecord rec, bool startFromCheckpoint)
	{
		NavalStorylineData.SetNavalStorylineSetPieceBattleMissionType(NavalStorylineData.NavalStorylineSetPieceBattleMissionTypes.Act3Quest4);
		bool isPlayerSergeant = MobileParty.MainParty.MapEvent.IsPlayerSergeant();
		rec.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		return NavalMissionState.OpenNew("FloatingFortressSetPieceBattleMission", rec, delegate
		{
			IMissionTroopSupplier[] suppliers = new IMissionTroopSupplier[2]
			{
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender),
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker)
			};
			BattleSideEnum playerSide = MobileParty.MainParty.MapEvent.PlayerSide;
			return new MissionBehavior[24]
			{
				new NavalShipsLogic(),
				new NavalFloatsamLogic(),
				new NavalAgentsLogic(),
				new NavalTrajectoryPlanningLogic(),
				new BattlePowerCalculationLogic(),
				new NavalBattleAgentLogic(),
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new BattleObserverMissionLogic(),
				new NavalMissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NavalBattle, isPlayerSergeant),
				new FloatingFortressSetPieceBattleMissionController(startFromCheckpoint),
				new AgentHumanAILogic(),
				new BattleMissionAgentInteractionLogic(),
				new EquipmentControllerLeaveLogic(),
				new DefaultNavalMissionAgentSpawnLogic(suppliers, playerSide),
				new MissionHintLogic(),
				new MissionObjectiveLogic(),
				new AgentVictoryLogic(),
				new NavalBattleEndLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(30f),
				new HighlightsController(),
				new BattleHighlightsController()
			};
		});
	}

	[MissionMethod]
	public static Mission OpenNavalStorylineAlleyFightMission(MissionInitializerRecord rec)
	{
		return MissionState.OpenNew("NavalStorylineAlleyFight", rec, (Mission mission) => new List<MissionBehavior>
		{
			new NavalStorylineAlleyFightMissionController(),
			new NavalStorylineAlleyFightCinematicController(),
			new MissionHintLogic(),
			new MissionOptionsComponent(),
			new AgentHumanAILogic(),
			new BattlePowerCalculationLogic(),
			new CampaignMissionComponent(),
			new BattleObserverMissionLogic(),
			new AgentVictoryLogic(),
			new MissionHardBorderPlacer(),
			new MissionAgentHandler(),
			new MissionFightHandler(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new HighlightsController(),
			new BattleHighlightsController(),
			new EquipmentControllerLeaveLogic()
		}.ToArray());
	}
}
