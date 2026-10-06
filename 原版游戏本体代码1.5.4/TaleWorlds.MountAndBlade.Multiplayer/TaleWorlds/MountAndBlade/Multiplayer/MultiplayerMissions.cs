using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Multiplayer.Missions;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace TaleWorlds.MountAndBlade.Multiplayer;

[MissionManager]
public static class MultiplayerMissions
{
	[MissionMethod]
	public static void OpenTeamDeathmatchMission(string scene)
	{
		MissionState.OpenNew("MultiplayerTeamDeathmatch", new MissionInitializerRecord(scene), (Mission missionController) => (!GameNetwork.IsServer) ? new MissionBehavior[21]
		{
			MissionLobbyComponent.CreateBehavior(),
			new MissionMultiplayerTeamDeathmatchClient(),
			new MultiplayerAchievementComponent(),
			new MultiplayerTimerComponent(),
			new MultiplayerBattleMissionAgentInteractionLogic(),
			new MultiplayerMissionAgentVisualSpawnComponent(),
			new ConsoleMatchStartEndHandler(),
			new MissionLobbyEquipmentNetworkComponent(),
			new MultiplayerTeamSelectComponent(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new MultiplayerPollComponent(),
			new MultiplayerAdminComponent(),
			new MultiplayerGameNotificationsComponent(),
			new MissionOptionsComponent(),
			new MissionScoreboardComponent(new TDMScoreboardData()),
			MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
			new EquipmentControllerLeaveLogic(),
			new MissionRecentPlayersComponent(),
			new MultiplayerPreloadHelper()
		} : new MissionBehavior[22]
		{
			MissionLobbyComponent.CreateBehavior(),
			new MissionMultiplayerTeamDeathmatch(),
			new MissionMultiplayerTeamDeathmatchClient(),
			new MultiplayerTimerComponent(),
			new MultiplayerBattleMissionAgentInteractionLogic(),
			new MultiplayerMissionAgentVisualSpawnComponent(),
			new ConsoleMatchStartEndHandler(),
			new SpawnComponent(new TeamDeathmatchSpawnFrameBehavior(), new TeamDeathmatchSpawningBehavior()),
			new MissionLobbyEquipmentNetworkComponent(),
			new MultiplayerTeamSelectComponent(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new MultiplayerPollComponent(),
			new MultiplayerAdminComponent(),
			new MultiplayerGameNotificationsComponent(),
			new MissionOptionsComponent(),
			new MissionScoreboardComponent(new TDMScoreboardData()),
			new MissionAgentPanicHandler(),
			new AgentHumanAILogic(),
			new EquipmentControllerLeaveLogic(),
			new MultiplayerPreloadHelper()
		});
	}

	[MissionMethod]
	public static void OpenDuelMission(string scene)
	{
		MissionState.OpenNew("MultiplayerDuel", new MissionInitializerRecord(scene), (Mission missionController) => (!GameNetwork.IsServer) ? new MissionBehavior[20]
		{
			MissionLobbyComponent.CreateBehavior(),
			new MissionMultiplayerGameModeDuelClient(),
			new MultiplayerAchievementComponent(),
			new MultiplayerTimerComponent(),
			new MultiplayerBattleMissionAgentInteractionLogic(),
			new MultiplayerMissionAgentVisualSpawnComponent(),
			new ConsoleMatchStartEndHandler(),
			new MissionLobbyEquipmentNetworkComponent(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new MultiplayerPollComponent(),
			new MultiplayerAdminComponent(),
			new MultiplayerGameNotificationsComponent(),
			new MissionOptionsComponent(),
			new MissionScoreboardComponent(new DuelScoreboardData()),
			MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
			new EquipmentControllerLeaveLogic(),
			new MissionRecentPlayersComponent(),
			new MultiplayerPreloadHelper()
		} : new MissionBehavior[21]
		{
			MissionLobbyComponent.CreateBehavior(),
			new MissionMultiplayerDuel(),
			new MissionMultiplayerGameModeDuelClient(),
			new MultiplayerTimerComponent(),
			new MultiplayerBattleMissionAgentInteractionLogic(),
			new MultiplayerMissionAgentVisualSpawnComponent(),
			new ConsoleMatchStartEndHandler(),
			new SpawnComponent(new DuelSpawnFrameBehavior(), new DuelSpawningBehavior()),
			new MissionLobbyEquipmentNetworkComponent(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new MultiplayerPollComponent(),
			new MultiplayerAdminComponent(),
			new MultiplayerGameNotificationsComponent(),
			new MissionOptionsComponent(),
			new MissionScoreboardComponent(new DuelScoreboardData()),
			new MissionAgentPanicHandler(),
			new AgentHumanAILogic(),
			new EquipmentControllerLeaveLogic(),
			new MultiplayerPreloadHelper()
		});
	}

	[MissionMethod]
	public static void OpenSiegeMission(string scene)
	{
		MissionState.OpenNew("MultiplayerSiege", new MissionInitializerRecord(scene)
		{
			SceneUpgradeLevel = 3,
			SceneLevels = ""
		}, (Mission missionController) => (!GameNetwork.IsServer) ? new MissionBehavior[22]
		{
			MissionLobbyComponent.CreateBehavior(),
			new MultiplayerWarmupComponent(),
			new MissionMultiplayerSiegeClient(),
			new MultiplayerAchievementComponent(),
			new MultiplayerTimerComponent(),
			new MultiplayerBattleMissionAgentInteractionLogic(),
			new MultiplayerMissionAgentVisualSpawnComponent(),
			new ConsoleMatchStartEndHandler(),
			new MissionLobbyEquipmentNetworkComponent(),
			new MultiplayerTeamSelectComponent(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new MultiplayerPollComponent(),
			new MultiplayerAdminComponent(),
			new MultiplayerGameNotificationsComponent(),
			new MissionOptionsComponent(),
			new MissionScoreboardComponent(new SiegeScoreboardData()),
			MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
			new EquipmentControllerLeaveLogic(),
			new MissionRecentPlayersComponent(),
			new MultiplayerPreloadHelper()
		} : new MissionBehavior[23]
		{
			MissionLobbyComponent.CreateBehavior(),
			new MissionMultiplayerSiege(),
			new MultiplayerWarmupComponent(),
			new MissionMultiplayerSiegeClient(),
			new MultiplayerTimerComponent(),
			new MultiplayerBattleMissionAgentInteractionLogic(),
			new MultiplayerMissionAgentVisualSpawnComponent(),
			new ConsoleMatchStartEndHandler(),
			new SpawnComponent(new SiegeSpawnFrameBehavior(), new SiegeSpawningBehavior()),
			new MissionLobbyEquipmentNetworkComponent(),
			new MultiplayerTeamSelectComponent(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new MultiplayerPollComponent(),
			new MultiplayerAdminComponent(),
			new MultiplayerGameNotificationsComponent(),
			new MissionOptionsComponent(),
			new MissionScoreboardComponent(new SiegeScoreboardData()),
			new MissionAgentPanicHandler(),
			new AgentHumanAILogic(),
			new EquipmentControllerLeaveLogic(),
			new MultiplayerPreloadHelper()
		});
	}

	[MissionMethod]
	public static void OpenBattleMission(string scene)
	{
		MissionState.OpenNew("MultiplayerBattle", new MissionInitializerRecord(scene), (Mission missionController) => (!GameNetwork.IsServer) ? new MissionBehavior[21]
		{
			MissionLobbyComponent.CreateBehavior(),
			new MultiplayerRoundComponent(),
			new MultiplayerWarmupComponent(),
			new MissionMultiplayerGameModeFlagDominationClient(),
			new MultiplayerTimerComponent(),
			new MultiplayerBattleMissionAgentInteractionLogic(),
			new MultiplayerMissionAgentVisualSpawnComponent(),
			new ConsoleMatchStartEndHandler(),
			new MissionLobbyEquipmentNetworkComponent(),
			new MultiplayerTeamSelectComponent(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new MultiplayerPollComponent(),
			new MultiplayerAdminComponent(),
			new MultiplayerGameNotificationsComponent(),
			new MissionOptionsComponent(),
			new MissionScoreboardComponent(new BattleScoreboardData()),
			MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
			new EquipmentControllerLeaveLogic(),
			new MultiplayerPreloadHelper()
		} : new MissionBehavior[24]
		{
			MissionLobbyComponent.CreateBehavior(),
			new MultiplayerRoundController(),
			new MissionMultiplayerFlagDomination(MultiplayerGameType.Battle),
			new MultiplayerWarmupComponent(),
			new MissionMultiplayerGameModeFlagDominationClient(),
			new MultiplayerTimerComponent(),
			new MultiplayerBattleMissionAgentInteractionLogic(),
			new MultiplayerMissionAgentVisualSpawnComponent(),
			new ConsoleMatchStartEndHandler(),
			new SpawnComponent(new FlagDominationSpawnFrameBehavior(), new FlagDominationSpawningBehavior()),
			new MissionLobbyEquipmentNetworkComponent(),
			new MultiplayerTeamSelectComponent(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new AgentVictoryLogic(),
			new AgentHumanAILogic(),
			new MissionBoundaryCrossingHandler(),
			new MultiplayerPollComponent(),
			new MultiplayerAdminComponent(),
			new MultiplayerGameNotificationsComponent(),
			new MissionOptionsComponent(),
			new MissionScoreboardComponent(new BattleScoreboardData()),
			new EquipmentControllerLeaveLogic(),
			new MultiplayerPreloadHelper()
		});
	}

	[MissionMethod]
	public static void OpenCaptainMission(string scene)
	{
		MissionState.OpenNew("MultiplayerCaptain", new MissionInitializerRecord(scene), (Mission missionController) => (!GameNetwork.IsServer) ? new MissionBehavior[23]
		{
			MissionLobbyComponent.CreateBehavior(),
			new MultiplayerAchievementComponent(),
			new MultiplayerWarmupComponent(),
			new MissionMultiplayerGameModeFlagDominationClient(),
			new MultiplayerRoundComponent(),
			new MultiplayerTimerComponent(),
			new MultiplayerBattleMissionAgentInteractionLogic(),
			new MultiplayerMissionAgentVisualSpawnComponent(),
			new ConsoleMatchStartEndHandler(),
			new MissionLobbyEquipmentNetworkComponent(),
			new MultiplayerTeamSelectComponent(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new MultiplayerPollComponent(),
			new MultiplayerAdminComponent(),
			new MultiplayerGameNotificationsComponent(),
			new MissionOptionsComponent(),
			new MissionScoreboardComponent(new CaptainScoreboardData()),
			MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
			new EquipmentControllerLeaveLogic(),
			new MissionRecentPlayersComponent(),
			new MultiplayerPreloadHelper()
		} : new MissionBehavior[25]
		{
			MissionLobbyComponent.CreateBehavior(),
			new MissionMultiplayerFlagDomination(MultiplayerGameType.Captain),
			new MultiplayerRoundController(),
			new MultiplayerWarmupComponent(),
			new MissionMultiplayerGameModeFlagDominationClient(),
			new MultiplayerTimerComponent(),
			new MultiplayerBattleMissionAgentInteractionLogic(),
			new MultiplayerMissionAgentVisualSpawnComponent(),
			new ConsoleMatchStartEndHandler(),
			new SpawnComponent(new FlagDominationSpawnFrameBehavior(), new FlagDominationSpawningBehavior()),
			new MissionLobbyEquipmentNetworkComponent(),
			new MultiplayerTeamSelectComponent(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new AgentVictoryLogic(),
			new AgentHumanAILogic(),
			new MissionAgentPanicHandler(),
			new MissionBoundaryCrossingHandler(),
			new MultiplayerPollComponent(),
			new MultiplayerAdminComponent(),
			new MultiplayerGameNotificationsComponent(),
			new MissionOptionsComponent(),
			new MissionScoreboardComponent(new CaptainScoreboardData()),
			new EquipmentControllerLeaveLogic(),
			new MultiplayerPreloadHelper()
		});
	}

	[MissionMethod]
	public static void OpenSkirmishMission(string scene)
	{
		MissionState.OpenNew("MultiplayerSkirmish", new MissionInitializerRecord(scene), (Mission missionController) => (!GameNetwork.IsServer) ? new MissionBehavior[24]
		{
			MissionLobbyComponent.CreateBehavior(),
			new MultiplayerAchievementComponent(),
			new MultiplayerWarmupComponent(),
			new MissionMultiplayerGameModeFlagDominationClient(),
			new MultiplayerRoundComponent(),
			new MultiplayerTimerComponent(),
			new MultiplayerBattleMissionAgentInteractionLogic(),
			new MultiplayerMissionAgentVisualSpawnComponent(),
			new ConsoleMatchStartEndHandler(),
			new MissionLobbyEquipmentNetworkComponent(),
			new MultiplayerTeamSelectComponent(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new MultiplayerPollComponent(),
			new MultiplayerAdminComponent(),
			new MultiplayerGameNotificationsComponent(),
			new MissionOptionsComponent(),
			new MissionScoreboardComponent(new SkirmishScoreboardData()),
			MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
			new EquipmentControllerLeaveLogic(),
			new MissionRecentPlayersComponent(),
			new VoiceChatHandler(),
			new MultiplayerPreloadHelper()
		} : new MissionBehavior[26]
		{
			MissionLobbyComponent.CreateBehavior(),
			new MissionMultiplayerFlagDomination(MultiplayerGameType.Skirmish),
			new MultiplayerRoundController(),
			new MultiplayerWarmupComponent(),
			new MissionMultiplayerGameModeFlagDominationClient(),
			new MultiplayerTimerComponent(),
			new MultiplayerBattleMissionAgentInteractionLogic(),
			new MultiplayerMissionAgentVisualSpawnComponent(),
			new ConsoleMatchStartEndHandler(),
			new SpawnComponent(new FlagDominationSpawnFrameBehavior(), new FlagDominationSpawningBehavior()),
			new MissionLobbyEquipmentNetworkComponent(),
			new MultiplayerTeamSelectComponent(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new AgentVictoryLogic(),
			new MissionAgentPanicHandler(),
			new AgentHumanAILogic(),
			new MissionBoundaryCrossingHandler(),
			new MultiplayerPollComponent(),
			new MultiplayerAdminComponent(),
			new MultiplayerGameNotificationsComponent(),
			new MissionOptionsComponent(),
			new MissionScoreboardComponent(new SkirmishScoreboardData()),
			new EquipmentControllerLeaveLogic(),
			new VoiceChatHandler(),
			new MultiplayerPreloadHelper()
		});
	}
}
