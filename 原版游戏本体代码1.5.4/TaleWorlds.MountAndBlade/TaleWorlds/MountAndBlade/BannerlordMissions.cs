using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.MissionSpawnHandlers;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;

namespace TaleWorlds.MountAndBlade;

[MissionManager]
public static class BannerlordMissions
{
	private enum CustomBattleGameTypes
	{
		AttackerGeneral,
		DefenderGeneral,
		AttackerSergeant,
		DefenderSergeant
	}

	private const string Level1Tag = "level_1";

	private const string Level2Tag = "level_2";

	private const string Level3Tag = "level_3";

	private const string SiegeTag = "siege";

	private const string SallyOutTag = "sally";

	private static Type GetSiegeWeaponType(SiegeEngineType siegeWeaponType)
	{
		if (siegeWeaponType == DefaultSiegeEngineTypes.Ladder)
		{
			return typeof(SiegeLadder);
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.Ballista)
		{
			return typeof(Ballista);
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.FireBallista)
		{
			return typeof(FireBallista);
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.Ram || siegeWeaponType == DefaultSiegeEngineTypes.ImprovedRam)
		{
			return typeof(BatteringRam);
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.SiegeTower || siegeWeaponType == DefaultSiegeEngineTypes.HeavySiegeTower)
		{
			return typeof(SiegeTower);
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.Onager || siegeWeaponType == DefaultSiegeEngineTypes.Catapult)
		{
			return typeof(Mangonel);
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.FireOnager || siegeWeaponType == DefaultSiegeEngineTypes.FireCatapult)
		{
			return typeof(FireMangonel);
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.Trebuchet || siegeWeaponType == DefaultSiegeEngineTypes.Bricole)
		{
			return typeof(Trebuchet);
		}
		return null;
	}

	private static Dictionary<Type, int> GetSiegeWeaponTypes(Dictionary<SiegeEngineType, int> values)
	{
		Dictionary<Type, int> dictionary = new Dictionary<Type, int>();
		foreach (KeyValuePair<SiegeEngineType, int> value in values)
		{
			dictionary.Add(GetSiegeWeaponType(value.Key), value.Value);
		}
		return dictionary;
	}

	public static AtmosphereInfo CreateAtmosphereInfoForMission(string seasonId, int timeOfDay)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		dictionary.Add("spring", 0);
		dictionary.Add("summer", 1);
		dictionary.Add("fall", 2);
		dictionary.Add("winter", 3);
		dictionary.TryGetValue(seasonId, out var value);
		Dictionary<int, string> dictionary2 = new Dictionary<int, string>();
		dictionary2.Add(6, "TOD_06_00_SemiCloudy");
		dictionary2.Add(12, "TOD_12_00_SemiCloudy");
		dictionary2.Add(15, "TOD_04_00_SemiCloudy");
		dictionary2.Add(18, "TOD_03_00_SemiCloudy");
		dictionary2.Add(22, "TOD_01_00_SemiCloudy");
		dictionary2.TryGetValue(timeOfDay, out var value2);
		return new AtmosphereInfo
		{
			AtmosphereName = value2,
			TimeInfo = new TimeInformation
			{
				Season = value
			}
		};
	}

	[MissionMethod]
	public static Mission OpenCustomBattleMission(string scene, BasicCharacterObject playerCharacter, CustomBattleCombatant playerParty, CustomBattleCombatant enemyParty, bool isPlayerGeneral, BasicCharacterObject playerSideGeneralCharacter, string sceneLevels = "", string seasonString = "", float timeOfDay = 6f)
	{
		BattleSideEnum playerSide = playerParty.Side;
		bool isPlayerAttacker = playerSide == BattleSideEnum.Attacker;
		IMissionTroopSupplier[] troopSuppliers = new IMissionTroopSupplier[2];
		CustomBattleTroopSupplier customBattleTroopSupplier = new CustomBattleTroopSupplier(playerParty, isPlayerSide: true, isPlayerGeneral, isSallyOut: false);
		troopSuppliers[(int)playerParty.Side] = customBattleTroopSupplier;
		CustomBattleTroopSupplier customBattleTroopSupplier2 = new CustomBattleTroopSupplier(enemyParty, isPlayerSide: false, isPlayerGeneral: false, isSallyOut: false);
		troopSuppliers[(int)enemyParty.Side] = customBattleTroopSupplier2;
		bool isPlayerSergeant = !isPlayerGeneral;
		Mission mission = MissionState.OpenNew("CustomBattle", new MissionInitializerRecord(scene)
		{
			DoNotUseLoadingScreen = false,
			PlayingInCampaignMode = false,
			AtmosphereOnCampaign = CreateAtmosphereInfoForMission(seasonString, (int)timeOfDay),
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
			new MissionObjectiveLogic()
		});
		mission.SetPlayerCanTakeControlOfAnotherAgentWhenDead();
		return mission;
	}

	[MissionMethod]
	public static Mission OpenSiegeMissionWithDeployment(string scene, BasicCharacterObject playerCharacter, CustomBattleCombatant playerParty, CustomBattleCombatant enemyParty, bool isPlayerGeneral, float[] wallHitPointPercentages, bool hasAnySiegeTower, List<MissionSiegeWeapon> siegeWeaponsOfAttackers, List<MissionSiegeWeapon> siegeWeaponsOfDefenders, bool isPlayerAttacker, int sceneUpgradeLevel = 0, string seasonString = "", bool isSallyOut = false, bool isReliefForceAttack = false, float timeOfDay = 6f)
	{
		string sceneLevels = sceneUpgradeLevel switch
		{
			2 => "level_2", 
			1 => "level_1", 
			_ => "level_3", 
		} + " siege";
		BattleSideEnum playerSide = playerParty.Side;
		IMissionTroopSupplier[] troopSuppliers = new IMissionTroopSupplier[2];
		CustomBattleTroopSupplier customBattleTroopSupplier = new CustomBattleTroopSupplier(playerParty, isPlayerSide: true, isPlayerGeneral, isSallyOut);
		troopSuppliers[(int)playerParty.Side] = customBattleTroopSupplier;
		CustomBattleTroopSupplier customBattleTroopSupplier2 = new CustomBattleTroopSupplier(enemyParty, isPlayerSide: false, isPlayerGeneral: false, isSallyOut);
		troopSuppliers[(int)enemyParty.Side] = customBattleTroopSupplier2;
		bool isPlayerSergeant = !isPlayerGeneral;
		Mission mission = MissionState.OpenNew("CustomSiegeBattle", new MissionInitializerRecord(scene)
		{
			PlayingInCampaignMode = false,
			AtmosphereOnCampaign = CreateAtmosphereInfoForMission(seasonString, (int)timeOfDay),
			SceneLevels = sceneLevels,
			DecalAtlasGroup = 2
		}, delegate
		{
			List<MissionBehavior> list = new List<MissionBehavior>
			{
				new BattleSpawnLogic(isSallyOut ? "sally_out_set" : (isReliefForceAttack ? "relief_force_attack_set" : "battle_set")),
				new MissionOptionsComponent(),
				new BattleEndLogic(),
				new BattleReinforcementsSpawnController(),
				new MissionCombatantsLogic(null, playerParty, (!isPlayerAttacker) ? playerParty : enemyParty, isPlayerAttacker ? playerParty : enemyParty, (!isSallyOut) ? Mission.MissionTeamAITypeEnum.Siege : Mission.MissionTeamAITypeEnum.SallyOut, isPlayerSergeant),
				new SiegeMissionPreparationHandler(isSallyOut, isReliefForceAttack, wallHitPointPercentages, hasAnySiegeTower)
			};
			Mission.BattleSizeType battleSizeType = ((!isSallyOut) ? Mission.BattleSizeType.Siege : Mission.BattleSizeType.SallyOut);
			list.Add(new DefaultBattleMissionAgentSpawnLogic(troopSuppliers, playerSide, battleSizeType));
			list.Add(new BattlePowerCalculationLogic());
			if (isSallyOut)
			{
				list.Add(new CustomSallyOutMissionController((!isPlayerAttacker) ? playerParty : enemyParty, isPlayerAttacker ? playerParty : enemyParty));
			}
			else if (isReliefForceAttack)
			{
				list.Add(new CustomSallyOutMissionController((!isPlayerAttacker) ? playerParty : enemyParty, isPlayerAttacker ? playerParty : enemyParty));
			}
			else
			{
				list.Add(new CustomSiegeMissionSpawnHandler((!isPlayerAttacker) ? playerParty : enemyParty, isPlayerAttacker ? playerParty : enemyParty, spawnWithHorses: false));
			}
			list.Add(new BattleObserverMissionLogic());
			list.Add(new CustomBattleAgentLogic());
			list.Add(new BannerBearerLogic());
			list.Add(new AgentHumanAILogic());
			if (!isSallyOut)
			{
				list.Add(new AmmoSupplyLogic(new List<BattleSideEnum> { BattleSideEnum.Defender }));
			}
			list.Add(new AgentVictoryLogic());
			list.Add(new AssignPlayerRoleInTeamMissionController(isPlayerGeneral, isPlayerSergeant, isPlayerInArmy: false));
			list.Add(new GeneralsAndCaptainsAssignmentLogic((isPlayerAttacker && isPlayerGeneral) ? playerCharacter.GetName() : null, null, null, null, createBodyguard: false));
			list.Add(new MissionAgentPanicHandler());
			list.Add(new MissionBoundaryPlacer());
			list.Add(new MissionBoundaryCrossingHandler());
			list.Add(new AgentMoraleInteractionLogic());
			list.Add(new HighlightsController());
			list.Add(new BattleHighlightsController());
			list.Add(new EquipmentControllerLeaveLogic());
			if (isSallyOut)
			{
				list.Add(new MissionSiegeEnginesLogic(new List<MissionSiegeWeapon>(), siegeWeaponsOfAttackers));
			}
			else
			{
				list.Add(new MissionSiegeEnginesLogic(siegeWeaponsOfDefenders, siegeWeaponsOfAttackers));
			}
			list.Add(new SiegeDeploymentHandler(isPlayerAttacker));
			list.Add(new SiegeDeploymentMissionController(isPlayerAttacker));
			return list.ToArray();
		});
		mission.SetPlayerCanTakeControlOfAnotherAgentWhenDead();
		return mission;
	}

	[MissionMethod]
	public static Mission OpenCustomBattleLordsHallMission(string scene, BasicCharacterObject playerCharacter, CustomBattleCombatant playerParty, CustomBattleCombatant enemyParty, BasicCharacterObject playerSideGeneralCharacter, string sceneLevels = "", int sceneUpgradeLevel = 0, string seasonString = "")
	{
		int remainingDefenderArcherCount = TaleWorlds.Library.MathF.Round(18.9f);
		BattleSideEnum playerSide = BattleSideEnum.Attacker;
		bool isPlayerAttacker = playerSide == BattleSideEnum.Attacker;
		IMissionTroopSupplier[] troopSuppliers = new IMissionTroopSupplier[2];
		CustomBattleTroopSupplier customBattleTroopSupplier = new CustomBattleTroopSupplier(playerParty, isPlayerSide: true, playerCharacter == playerSideGeneralCharacter, isSallyOut: false);
		troopSuppliers[(int)playerParty.Side] = customBattleTroopSupplier;
		CustomBattleTroopSupplier customBattleTroopSupplier2 = new CustomBattleTroopSupplier(enemyParty, isPlayerSide: false, isPlayerGeneral: false, isSallyOut: false, delegate(BasicCharacterObject basicCharacterObject)
		{
			bool result = true;
			if (basicCharacterObject.IsRanged)
			{
				if (remainingDefenderArcherCount > 0)
				{
					remainingDefenderArcherCount--;
				}
				else
				{
					result = false;
				}
			}
			return result;
		});
		troopSuppliers[(int)enemyParty.Side] = customBattleTroopSupplier2;
		return MissionState.OpenNew("CustomBattleLordsHall", new MissionInitializerRecord(scene)
		{
			DoNotUseLoadingScreen = false,
			PlayingInCampaignMode = false,
			SceneLevels = "siege",
			DecalAtlasGroup = 3
		}, (Mission missionController) => new MissionBehavior[17]
		{
			new MissionOptionsComponent(),
			new BattleEndLogic(),
			new MissionCombatantsLogic(null, playerParty, (!isPlayerAttacker) ? playerParty : enemyParty, isPlayerAttacker ? playerParty : enemyParty, Mission.MissionTeamAITypeEnum.NoTeamAI, isPlayerSergeant: false),
			new BattleMissionStarterLogic(),
			new AgentHumanAILogic(),
			new LordsHallFightMissionController(troopSuppliers, 3f, 0.7f, 19, 27, playerSide),
			new BattleObserverMissionLogic(),
			new CustomBattleAgentLogic(),
			new AgentVictoryLogic(),
			new AmmoSupplyLogic(new List<BattleSideEnum> { BattleSideEnum.Defender }),
			new EquipmentControllerLeaveLogic(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new BattleMissionAgentInteractionLogic(),
			new HighlightsController(),
			new BattleHighlightsController()
		});
	}
}
