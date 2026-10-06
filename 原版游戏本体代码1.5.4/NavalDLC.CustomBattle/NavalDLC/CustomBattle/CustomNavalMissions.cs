using System.Collections.Generic;
using NavalDLC.Missions;
using NavalDLC.Missions.Deployment;
using NavalDLC.Missions.Handlers;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;

namespace NavalDLC.CustomBattle;

[MissionManager]
public static class CustomNavalMissions
{
	public static AtmosphereInfo CreateAtmosphereInfoForMission(string seasonId, int timeOfDay, float windStrength, Vec2 windDirection, TerrainType terrain)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		dictionary.Add("spring", 0);
		dictionary.Add("summer", 1);
		dictionary.Add("fall", 2);
		dictionary.Add("winter", 3);
		dictionary.TryGetValue(seasonId, out var value);
		if (!windDirection.IsNonZero())
		{
			windDirection = Vec2.Side;
		}
		return new AtmosphereInfo
		{
			TimeInfo = new TimeInformation
			{
				Season = value,
				TimeOfDay = timeOfDay
			},
			NauticalInfo = new NauticalInformation
			{
				WindVector = windStrength * windDirection.Normalized(),
				CanUseLowAltitudeAtmosphere = 1,
				IsRiverBattle = ((terrain == TerrainType.River) ? 1 : 0),
				UsesNavalSimulatedWater = ((terrain == TerrainType.River || terrain == TerrainType.Water || terrain == TerrainType.OpenSea || terrain == TerrainType.CoastalSea) ? 1 : 0)
			}
		};
	}

	[MissionMethod]
	public static Mission OpenNavalBattleForCustomMission(string scene, BasicCharacterObject playerCharacter, CustomBattleCombatant playerParty, MBList<IShipOrigin> playerTeamShips, CustomBattleCombatant enemyParty, MBList<IShipOrigin> enemyTeamShips, bool isPlayerGeneral, string seasonString, float timeOfDay, float windStrength, NavalCustomBattleWindConfig.Direction windDirection, TerrainType terrain, string forcedSceneLevel)
	{
		BattleSideEnum playerSide = playerParty.Side;
		bool isPlayerAttacker = playerSide == BattleSideEnum.Attacker;
		IMissionTroopSupplier[] troopSuppliers = new IMissionTroopSupplier[2];
		CustomBattleTroopSupplier customBattleTroopSupplier = new CustomBattleTroopSupplier(playerParty, isPlayerSide: true, isPlayerGeneral, isSallyOut: false);
		troopSuppliers[(int)playerParty.Side] = customBattleTroopSupplier;
		CustomBattleTroopSupplier customBattleTroopSupplier2 = new CustomBattleTroopSupplier(enemyParty, isPlayerSide: false, isPlayerGeneral: false, isSallyOut: false);
		troopSuppliers[(int)enemyParty.Side] = customBattleTroopSupplier2;
		bool isPlayerSergeant = !isPlayerGeneral;
		MissionInitializerRecord rec = new MissionInitializerRecord(scene);
		TerrainType terrainType = terrain;
		rec.TerrainType = (int)terrainType;
		rec.NeedsRandomTerrain = false;
		rec.PlayingInCampaignMode = false;
		rec.AtmosphereOnCampaign = CreateAtmosphereInfoForMission(seasonString, (int)timeOfDay, windStrength, new Vec2(0f, 1f), terrain);
		rec.SceneHasMapPatch = false;
		rec.PlayingInCampaignMode = true;
		rec.DecalAtlasGroup = 2;
		rec.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		rec.SceneLevels = forcedSceneLevel;
		int maximumDeployableTroopCountForTeam = NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetMaximumDeployableTroopCountForTeam(playerTeamShips, isPlayerTeam: true);
		int maximumDeployableTroopCountForTeam2 = NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetMaximumDeployableTroopCountForTeam(enemyTeamShips);
		int[] maxDeployableTroopCountPerTeam = new int[3] { maximumDeployableTroopCountForTeam, 0, maximumDeployableTroopCountForTeam2 };
		int deployablePlayerShipCount = MathF.Min(playerTeamShips.Count, 8);
		Mission mission = NavalMissionState.OpenNew("NavalCustomBattle", rec, (Mission mission2) => new MissionBehavior[31]
		{
			new NavalShipsLogic(),
			new NavalFloatsamLogic(),
			new NavalAgentsLogic(),
			new DefaultNavalMissionLogic(playerTeamShips, null, enemyTeamShips, NavalShipDeploymentLimit.Max(), NavalShipDeploymentLimit.Invalid(), NavalShipDeploymentLimit.Max()),
			new NavalTrajectoryPlanningLogic(),
			new DefaultNavalMissionAgentSpawnLogic(troopSuppliers, playerSide, deployablePlayerShipCount, maxDeployableTroopCountPerTeam),
			new NavalMissionDeploymentPlanningLogic(mission2),
			new BattlePowerCalculationLogic(),
			new CustomBattleAgentLogic(),
			new WaveParametersComputerLogic(),
			new MissionOptionsComponent(),
			new NavalAgentMoraleInteractionLogic(),
			new NavalBattleEndLogic(),
			new NavalBoundaryForceFieldLogic(),
			new NavalMissionCombatantsLogic(new List<CustomBattleCombatant> { playerParty, enemyParty }, playerParty, (!isPlayerAttacker) ? playerParty : enemyParty, isPlayerAttacker ? playerParty : enemyParty, Mission.MissionTeamAITypeEnum.NavalBattle, isPlayerSergeant),
			new BattleObserverMissionLogic(),
			new AgentHumanAILogic(),
			new AgentVictoryLogic(),
			new ShipCollisionOutcomeLogic(mission2),
			new ShipRetreatLogic(),
			new BattleMissionAgentInteractionLogic(),
			new NavalAssignPlayerRoleInTeamMissionController(!isPlayerSergeant, isPlayerSergeant, isPlayerInArmy: false),
			new EquipmentControllerLeaveLogic(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(30f),
			new HighlightsController(),
			new BattleHighlightsController(),
			new NavalDeploymentMissionController(isPlayerAttacker),
			new NavalDeploymentHandler(isPlayerAttacker),
			new NavalCustomBattleWindAndWaveLogic(windDirection, terrainType)
		});
		mission.SetPlayerCanTakeControlOfAnotherAgentWhenDead();
		return mission;
	}

	[MissionMethod]
	public static Mission OpenNavalRaidBattleForCustomMission(string scene, BasicCharacterObject playerCharacter, CustomBattleCombatant playerParty, CustomBattleCombatant enemyParty, MBList<IShipOrigin> attackerShips, bool isPlayerGeneral, string seasonString, float timeOfDay, float windStrength, NavalCustomBattleWindConfig.Direction windDirection, TerrainType terrain, string forcedSceneLevel)
	{
		BattleSideEnum playerSide = playerParty.Side;
		bool isPlayerAttacker = playerSide == BattleSideEnum.Attacker;
		IMissionTroopSupplier[] troopSuppliers = new IMissionTroopSupplier[2];
		CustomBattleTroopSupplier customBattleTroopSupplier = new CustomBattleTroopSupplier(playerParty, isPlayerSide: true, isPlayerGeneral, isSallyOut: false);
		troopSuppliers[(int)playerParty.Side] = customBattleTroopSupplier;
		CustomBattleTroopSupplier customBattleTroopSupplier2 = new CustomBattleTroopSupplier(enemyParty, isPlayerSide: false, isPlayerGeneral: false, isSallyOut: false);
		troopSuppliers[(int)enemyParty.Side] = customBattleTroopSupplier2;
		bool isPlayerSergeant = !isPlayerGeneral;
		MissionInitializerRecord rec = new MissionInitializerRecord(scene);
		TerrainType terrainType = terrain;
		rec.TerrainType = (int)terrainType;
		rec.NeedsRandomTerrain = false;
		rec.PlayingInCampaignMode = false;
		rec.AtmosphereOnCampaign = CreateAtmosphereInfoForMission(seasonString, (int)timeOfDay, windStrength, new Vec2(0f, 1f), terrain);
		rec.SceneHasMapPatch = false;
		rec.PlayingInCampaignMode = true;
		rec.DecalAtlasGroup = 2;
		rec.SceneLevels = "naval_raid";
		int attackerTeamMaxDeployableTroopCount = NavalDLCManager.Instance.GameModels.ShipDeploymentModel.GetMaximumDeployableTroopCountForTeam(attackerShips, isPlayerAttacker);
		attackerTeamMaxDeployableTroopCount = MathF.Min(troopSuppliers[1].NumTroopsNotSupplied, attackerTeamMaxDeployableTroopCount);
		int commonLimit = MathF.Min(attackerShips.Count, 8);
		NavalShipDeploymentLimit attackerShipsDeploymentLimit = new NavalShipDeploymentLimit(commonLimit);
		CustomBattleCombatant customBattleCombatant = (isPlayerAttacker ? enemyParty : playerParty);
		int defenderTeamMaxDeployableTroopCount = customBattleCombatant.NumberOfHealthyMembers;
		Mission mission = NavalMissionState.OpenNew("NavalRaidCustomBattle", rec, (Mission mission2) => new MissionBehavior[30]
		{
			new NavalShipsLogic(),
			new NavalFloatsamLogic(),
			new NavalAgentsLogic(),
			new NavalRaidMissionController(),
			new NavalRaidMissionAgentSpawnLogic(troopSuppliers, playerSide, attackerShips, attackerShipsDeploymentLimit, attackerTeamMaxDeployableTroopCount, defenderTeamMaxDeployableTroopCount),
			new NavalTrajectoryPlanningLogic(),
			new NavalRaidMissionDeploymentPlanningLogic(),
			new BattlePowerCalculationLogic(),
			new CustomBattleAgentLogic(),
			new WaveParametersComputerLogic(),
			new MissionOptionsComponent(),
			new NavalAgentMoraleInteractionLogic(),
			new BattleEndLogic(),
			new NavalBoundaryForceFieldLogic(),
			new NavalMissionCombatantsLogic(new List<CustomBattleCombatant> { playerParty, enemyParty }, playerParty, (!isPlayerAttacker) ? playerParty : enemyParty, isPlayerAttacker ? playerParty : enemyParty, Mission.MissionTeamAITypeEnum.NavalRaid, isPlayerSergeant),
			new BattleObserverMissionLogic(),
			new AgentHumanAILogic(),
			new AgentVictoryLogic(),
			new ShipCollisionOutcomeLogic(mission2),
			new BattleMissionAgentInteractionLogic(),
			new NavalAssignPlayerRoleInTeamMissionController(!isPlayerSergeant, isPlayerSergeant, isPlayerInArmy: false),
			new EquipmentControllerLeaveLogic(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(30f),
			new HighlightsController(),
			new BattleHighlightsController(),
			new NavalRaidDeploymentMissionController(isPlayerAttacker),
			new NavalRaidDeploymentHandler(isPlayerAttacker),
			new NavalCustomBattleWindAndWaveLogic(windDirection, terrainType)
		});
		mission.SetPlayerCanTakeControlOfAnotherAgentWhenDead();
		return mission;
	}
}
