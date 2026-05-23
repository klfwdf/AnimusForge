using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.MissionSpawnHandlers;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.Multiplayer.Missions;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;

namespace TaleWorlds.MountAndBlade.Multiplayer;

[MissionManager]
public static class MultiplayerPracticeMissions
{
	[MissionMethod]
	public static Mission OpenMultiplayerPracticeMission(string scene, BasicCharacterObject playerCharacter, CustomBattleCombatant playerParty, CustomBattleCombatant enemyParty, bool isPlayerGeneral, BasicCharacterObject playerSideGeneralCharacter, string sceneLevels = "", string seasonString = "", float timeOfDay = 6f)
	{
		BattleSideEnum playerSide = playerParty.Side;
		bool isPlayerAttacker = playerSide == BattleSideEnum.Attacker;
		IMissionTroopSupplier[] troopSuppliers = new IMissionTroopSupplier[2];
		CustomBattleTroopSupplier customBattleTroopSupplier = new CustomBattleTroopSupplier(playerParty, isPlayerSide: true, isPlayerGeneral, isSallyOut: false);
		troopSuppliers[(int)playerParty.Side] = customBattleTroopSupplier;
		CustomBattleTroopSupplier customBattleTroopSupplier2 = new CustomBattleTroopSupplier(enemyParty, isPlayerSide: false, isPlayerGeneral: false, isSallyOut: false);
		troopSuppliers[(int)enemyParty.Side] = customBattleTroopSupplier2;
		bool isPlayerSergeant = !isPlayerGeneral;
		return MissionState.OpenNew("MultiplayerPractice", new MissionInitializerRecord(scene)
		{
			DoNotUseLoadingScreen = false,
			PlayingInCampaignMode = false,
			AtmosphereOnCampaign = BannerlordMissions.CreateAtmosphereInfoForMission(seasonString, (int)timeOfDay),
			SceneLevels = sceneLevels,
			DecalAtlasGroup = 2
		}, (Mission missionController) => new MissionBehavior[26]
		{
			new DefaultBattleMissionAgentSpawnLogic(troopSuppliers, playerSide, Mission.BattleSizeType.Battle),
			new BattlePowerCalculationLogic(),
			new CustomBattleAgentLogic(),
			new BannerBearerLogic(),
			new CustomBattleMissionSpawnHandler((!isPlayerAttacker) ? playerParty : enemyParty, isPlayerAttacker ? playerParty : enemyParty),
			new MissionOptionsComponent(),
			new BattleEndLogic(),
			new BattleReinforcementsSpawnController(),
			new MissionCombatantsLogic(null, playerParty, (!isPlayerAttacker) ? playerParty : enemyParty, isPlayerAttacker ? playerParty : enemyParty, Mission.MissionTeamAITypeEnum.FieldBattle, isPlayerSergeant),
			new BattleObserverMissionLogic(),
			new AgentHumanAILogic(),
			new AgentVictoryLogic(),
			new MissionAgentPanicHandler(),
			new BattleMissionAgentInteractionLogic(),
			new AgentMoraleInteractionLogic(),
			new AssignPlayerRoleInTeamMissionController(isPlayerGeneral, isPlayerSergeant, isPlayerInArmy: false, isPlayerSergeant ? Enumerable.Repeat(playerCharacter.StringId, 1).ToList() : new List<string>()),
			new GeneralsAndCaptainsAssignmentLogic((isPlayerAttacker && isPlayerGeneral) ? playerCharacter.GetName() : ((isPlayerAttacker && isPlayerSergeant) ? playerSideGeneralCharacter.GetName() : null), (!isPlayerAttacker && isPlayerGeneral) ? playerCharacter.GetName() : ((!isPlayerAttacker && isPlayerSergeant) ? playerSideGeneralCharacter.GetName() : null)),
			new EquipmentControllerLeaveLogic(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new HighlightsController(),
			new BattleHighlightsController(),
			new BattleDeploymentMissionController(isPlayerAttacker),
			new BattleDeploymentHandler(isPlayerAttacker),
			new MultiplayerPracticeMissionComponent()
		});
	}
}
