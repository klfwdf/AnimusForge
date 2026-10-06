using System.Collections.Generic;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace SandBox.Tournaments;

[MissionManager]
public static class TournamentMissionStarter
{
	[MissionMethod]
	public static Mission OpenTournamentArcheryMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
	{
		return MissionState.OpenNew("TournamentArchery", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", doNotUseLoadingScreen: false, DecalAtlasGroup.Town), delegate
		{
			TournamentArcheryMissionController tournamentArcheryMissionController = new TournamentArcheryMissionController(culture);
			return new MissionBehavior[12]
			{
				new CampaignMissionComponent(),
				new EquipmentControllerLeaveLogic(),
				tournamentArcheryMissionController,
				new TournamentBehavior(tournamentGame, settlement, tournamentArcheryMissionController, isPlayerParticipating),
				new AgentVictoryLogic(),
				new MissionAgentPanicHandler(),
				new AgentHumanAILogic(),
				new ArenaAgentStateDeciderLogic(),
				new BasicLeaveMissionLogic(askBeforeLeave: true),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionOptionsComponent()
			};
		});
	}

	[MissionMethod]
	public static Mission OpenTournamentFightMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
	{
		return MissionState.OpenNew("TournamentFight", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", doNotUseLoadingScreen: false, DecalAtlasGroup.Town), delegate
		{
			TournamentFightMissionController tournamentFightMissionController = new TournamentFightMissionController(culture);
			return new MissionBehavior[13]
			{
				new CampaignMissionComponent(),
				new EquipmentControllerLeaveLogic(),
				tournamentFightMissionController,
				new TournamentBehavior(tournamentGame, settlement, tournamentFightMissionController, isPlayerParticipating),
				new AgentVictoryLogic(),
				new MissionAgentPanicHandler(),
				new AgentHumanAILogic(),
				new ArenaAgentStateDeciderLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionOptionsComponent(),
				new HighlightsController(),
				new SandboxHighlightsController()
			};
		});
	}

	[MissionMethod]
	public static Mission OpenTournamentHorseRaceMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
	{
		return MissionState.OpenNew("TournamentHorseRace", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", doNotUseLoadingScreen: false, DecalAtlasGroup.Town), delegate
		{
			TownHorseRaceMissionController townHorseRaceMissionController = new TownHorseRaceMissionController(culture);
			return new MissionBehavior[11]
			{
				new CampaignMissionComponent(),
				new EquipmentControllerLeaveLogic(),
				townHorseRaceMissionController,
				new TournamentBehavior(tournamentGame, settlement, townHorseRaceMissionController, isPlayerParticipating),
				new AgentVictoryLogic(),
				new MissionAgentPanicHandler(),
				new AgentHumanAILogic(),
				new ArenaAgentStateDeciderLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionOptionsComponent()
			};
		});
	}

	[MissionMethod]
	public static Mission OpenTournamentJoustingMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
	{
		return MissionState.OpenNew("TournamentJousting", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", doNotUseLoadingScreen: false, DecalAtlasGroup.Town), delegate
		{
			TournamentJoustingMissionController tournamentJoustingMissionController = new TournamentJoustingMissionController(culture);
			return new MissionBehavior[12]
			{
				new CampaignMissionComponent(),
				new EquipmentControllerLeaveLogic(),
				tournamentJoustingMissionController,
				new TournamentBehavior(tournamentGame, settlement, tournamentJoustingMissionController, isPlayerParticipating),
				new AgentVictoryLogic(),
				new MissionAgentPanicHandler(),
				new AgentHumanAILogic(),
				new ArenaAgentStateDeciderLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(),
				new MissionOptionsComponent()
			};
		});
	}

	[MissionMethod]
	public static Mission OpenBattleChallengeMission(string scene, IList<Hero> priorityCharsAttacker, IList<Hero> priorityCharsDefender)
	{
		return null;
	}
}
