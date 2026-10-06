using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.MissionObjects;
using NavalDLC.Missions;
using NavalDLC.Missions.AI.Tactics;
using NavalDLC.Missions.AI.TeamAI;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.Missions.ShipActuators;
using NavalDLC.Missions.ShipControl;
using NavalDLC.Missions.ShipInput;
using NavalDLC.Storyline.Objectives.Quest5;
using SandBox;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects;
using SandBox.Objects.Usables;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Missions.Objectives;
using TaleWorlds.MountAndBlade.Objects.Usables;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Storyline.MissionControllers;

public class Quest5SetPieceBattleMissionController : MissionLogic, IMissionAgentSpawnLogic, IMissionBehavior
{
	public class ConversationSound
	{
		public TextObject Line;

		public MBInformationManager.NotificationPriority Priority;

		public CharacterObject Character;

		public ConversationSound(TextObject line, MBInformationManager.NotificationPriority priority, CharacterObject character)
		{
			Line = line;
			Priority = priority;
			Character = character;
		}
	}

	public enum Quest5SetPieceBattleMissionState
	{
		None,
		InitializePhase1Part1,
		InitializePhase1Part2,
		Phase1GoToEnemyShip,
		Phase1SwimmingPhase,
		InitializeStealthPhasePart1,
		InitializeStealthPhasePart2,
		Phase1StealthPhase,
		Phase1GoToShipInteriorFadeOut,
		Phase1InitializeShipInteriorPhase,
		Phase1GoToShipInteriorFadeIn,
		Phase1ShipInteriorPhase,
		Phase1GoBackToShipFadeOut,
		Phase1InitializeGoBackToShip,
		Phase1GoBackToShipFadeIn,
		Phase1EscapePhase,
		Phase1ToPhase2FadeOut,
		InitializePhase2Part1,
		InitializePhase2Part2,
		InitializePhase2Part3,
		InitializePhase2Part4,
		Phase1ToPhase2FadeIn,
		Phase2InProgress,
		Phase2ToPhase3FadeOut,
		InitializePhase3Part1,
		InitializePhase3Part2,
		InitializePhase3Part3,
		Phase2ToPhase3FadeIn,
		Phase3InProgress,
		Phase3ToPhase4FadeOut,
		InitializePhase4Part1,
		InitializePhase4Part2,
		Phase3ToPhase4FadeIn,
		Phase4InProgress,
		Phase4ToBossFightFadeOut,
		InitializeBossFightPart1,
		InitializeBossFightPart2,
		Phase4ToBossFightFadeIn,
		StartBossFightConversation,
		BossFightConversationInProgress,
		BossFightInProgressAsDuel,
		BossFightInProgressAsAll,
		End,
		Exit
	}

	private enum Quest5InstructionState
	{
		None,
		Approach,
		WaitForJump,
		Jump,
		WaitForSwim,
		Swim,
		WaitForClearGuards,
		ClearGuards,
		WaitForCheckInterior,
		CheckInterior,
		WaitForTalkSister,
		TalkSister,
		WaitForReturnToDeck,
		ReturnToDeck,
		WaitForCutLoose,
		CutLoose,
		WaitForGunnarUsesShip,
		GunnarUsesShip,
		WaitForEscapeQuietly,
		EscapeQuietly,
		WaitForReachAllies,
		ReachAllies,
		WaitForDefeatEnemies,
		DefeatEnemies,
		WaitForDefeatPurigsShip,
		DefeatPurigsShip,
		WaitForDefeatPurig,
		DefeatPurig,
		WaitForEnd,
		End
	}

	private enum GunnarMovementState
	{
		None,
		GoToInitialJumpingPosition,
		WaitForReachingInitialJumpingPosition,
		GoToJumpingTargetPosition,
		WaitForReachingJumpingTargetPosition,
		SwimToTheHidingSpot,
		WaitForTeleportingToTheHidingSpot,
		TeleportToTargetPosition,
		WaitAtTheHidingSpot,
		GoToTheEscapeShip,
		WaitForReachingToTheEscapeShip,
		UseTheEscapeShip,
		End
	}

	private enum GunnarMovementStateForClimbingShip
	{
		None,
		Start,
		GoingToTheTargetClimbingMachine,
		TargetReached,
		UsingClimbingMachine,
		OnDeck,
		GoToFinalTargetPoint,
		End
	}

	public enum BossFightOutComeEnum
	{
		None,
		PlayerRefusedTheDuel,
		PlayerAcceptedAndWonTheDuel,
		PlayerDefeatedWaitingForConversation,
		PlayerAcceptedTheDuelLostItAndLetPurigGo,
		PlayerAcceptedTheDuelLostItAndHadPurigKilledAnyway
	}

	private enum BossFightStateEnum
	{
		None,
		Duel,
		All
	}

	private const string SceneStealthPhaseAtmosphereName = "TOD_02_00_SemiCloudy";

	private const string SceneInteriorAtmosphereName = "TOD_01_00_SemiCloudy";

	private const string ScenePhase2AtmosphereName = "TOD_naval_03_00_sunset";

	private const string ScenePhase3AtmosphereName = "TOD_naval_05_30_sunset";

	private const string MainOarPrefabName = "oars_holder";

	private const float GunnarFellIntoTheWaterTimer = 10f;

	private const string RampHolderId = "ramp_holder";

	private const string GunnarInitialJumpOffPositionTag = "gangradir_jump_off_initial";

	private const string GunnarJumpOffTargetPositionTag = "gangradir_jump_off_target";

	private const string Phase1EnemyShip4GunnarHidingSpotStringId = "sp_gangradir_hiding_spot";

	private const float MaximumAllowedReachDistanceToPhase1EnemyShip1 = 25f;

	private const float AllowedSwimRadius = 200f;

	private const float AllowedSwimRadiusCheckFrequencyAsSeconds = 5f;

	private const string Phase1CustomStealthEquipmentId = "naval_storyline_quest5_stealth_set";

	private const string Phase1ApproachPointTag = "phase_1_approach_point";

	private const float Phase1ApproachDistance = 30f;

	private const float Phase1EscapePhaseAutoCutLooseTimer = 300f;

	private const string Phase1SlaveTraderAgentCharacterStringId = "sea_hounds";

	private const string Phase1StealthAgentCharacterStringId = "sea_hound_captivity";

	private const string Phase1PlayerShipStringId = "crusas_roundship_nested_q5";

	private const string Phase1PlayerShipSpawnPointTag = "phase_1_player_ship_sp";

	private const string Phase1EnemyShip1StringId = "sturgia_heavy_ship";

	private const string Phase1EnemyShip1SpawnPointTag = "phase_1_enemy_ship_1_sp_initial";

	private const string Phase1EnemyShip1TargetPointTag = "phase_1_enemy_ship_1_sp";

	private const int Phase1EnemyShip1TroopCount = 7;

	private const string Phase1EnemyShip2StringId = "ship_lodya_storyline";

	private const string Phase1EnemyShip2SpawnPointTag = "phase_1_enemy_ship_2_sp";

	private const int Phase1EnemyShip2TroopCount = 6;

	private const string Phase1EnemyShip2AttachmentPoint1Tag = "bridge_a";

	private const string Phase1EnemyShip2AttachmentPoint2Tag = "bridge_b";

	private const string Phase1EnemyShip2AttachmentPoint3Tag = "bridge_c";

	private const string Phase1EnemyShip3StringId = "ship_dromon_storyline";

	private const string Phase1EnemyShip3SpawnPointTag = "phase_1_enemy_ship_3_sp";

	private const int Phase1EnemyShip3TroopCount = 100;

	private const string Phase1EnemyShip3AttachmentPoint1Tag = "bridge_a";

	private const string Phase1EnemyShip3AttachmentPoint2Tag = "bridge_b";

	private const string Phase1EnemyShip3ToInteriorDoorTag = "phase_1_enemy_ship_3_to_interior_door_tag";

	private const string Phase1EnemyShip4StringId = "ship_birlinn_storyline";

	private const string Phase1EnemyShip4AttachmentPoint1Tag = "bridge_d";

	private const string Phase1EnemyShip4SpawnPointTag = "phase_1_enemy_ship_4_sp";

	private const int Phase1EnemyShip4TroopCount = 6;

	private const string Phase1EnemyShip4StealthCheckpointSpawnPointStringId = "sp_player_stealth_checkpoint";

	private const string Phase1InteriorMissionPlayerSpawnPointTag = "phase_1_interior_player_sp";

	private const string Phase1InteriorMissionSisterSpawnPointTag = "phase_1_interior_sister_sp";

	private const string Phase1InteriorToEnemyShip3DoorTag = "phase_1_interior_to_enemy_ship_3_door_tag";

	private const string CrusasPhase1EquipmentStringId = "npc_merchant_equipment_empire";

	private const string EscapeShipRoofUpgradeId = "roof_5";

	private const string EscapeShipDeckUpgradeId = "deck_large_arrow_and_javelin_crates_lvl3";

	private const string SlaveTraderShipOarsmanActionId = "act_sit_2";

	private const string SisterWoundedActionId = "act_conversation_weary2_loop";

	private const string Phase1InteriorCameraSisterTag = "phase_1_interior_camera_sister";

	private const string Phase2EscapeShipPirateTargetFrame1Tag = "phase_2_anchor_1";

	private const string Phase2EscapeShipPirateTargetFrame2Tag = "phase_2_anchor_2";

	private const string Phase2EscapeShipPirateTargetFrame3Tag = "phase_2_anchor_3";

	private const string Phase2EscapeShipPirateTargetFrame4Tag = "phase_2_anchor_4";

	private const string Phase2EscapeShipPirateTargetFrame5Tag = "phase_2_anchor_5";

	private const string Phase2EnemyShip1SpawnPointTag = "phase_2_enemy_ship_1_sp";

	private const string Phase2EnemyShip2SpawnPointTag = "phase_2_enemy_ship_2_sp";

	private const string Phase2EnemyShip3SpawnPointTag = "phase_2_enemy_ship_3_sp";

	private const string Phase2EnemyShip4SpawnPointTag = "phase_2_enemy_ship_4_sp";

	private const string Phase2EnemyShip5SpawnPointTag = "phase_2_enemy_ship_5_sp";

	private const string Phase2EnemyShipStationary1SpawnPointTag = "phase_2_enemy_ship_stationary_1";

	private const string Phase2EnemyShip1TargetPointTag = "phase_2_enemy_ship_1_target";

	private const string Phase2EnemyShip2TargetPointTag = "phase_2_enemy_ship_2_target";

	private const string Phase2EnemyShip3TargetPointTag = "phase_2_enemy_ship_3_target";

	private const string Phase2EnemyShip4TargetPointTag = "phase_2_enemy_ship_4_target";

	private const string Phase2EnemyShip5TargetPointTag = "phase_2_enemy_ship_5_target";

	private const string Phase2EnemyShip1StringId = "ship_meditlight_storyline_q5";

	private const string Phase2EnemyShip2StringId = "ship_meditlight_storyline_q5";

	private const string Phase2EnemyShip3StringId = "ship_meditlight_storyline_q5";

	private const string Phase2EnemyShip4StringId = "ship_meditlight_storyline_q5";

	private const string Phase2EnemyShip5StringId = "ship_meditlight_storyline_q5";

	private const string Phase2EnemyShipStationary1StringId = "western_medium_ship";

	private const string Phase2AllyShip1SpawnPointTag = "phase_2_ally_ship_1_sp";

	private const string Phase2AllyShip2SpawnPointTag = "phase_2_ally_ship_2_sp";

	private const string Phase2AllyShip3SpawnPointTag = "phase_2_ally_ship_3_sp";

	private const string Phase2AllyShip4SpawnPointTag = "phase_2_ally_ship_4_sp";

	private const string Phase2AllyShip5SpawnPointTag = "phase_2_ally_ship_5_sp";

	private const string Phase2AllyShip1StringId = "aserai_heavy_ship";

	private const string Phase2AllyShip2StringId = "nord_medium_ship";

	private const string Phase2AllyShip3StringId = "northern_medium_ship";

	private const string Phase2AllyShip4StringId = "sturgia_heavy_ship";

	private const string Phase2AllyShip5StringId = "northern_medium_ship";

	private const float AutoCutLoosePirateShipTimer = 25f;

	private const float AutoEstablishConnectionsForPirateShipsTimer = 7f;

	private const string Phase2EscapeShipTargetPointPrefix = "phase_2_escape_ship_target";

	private const string Phase2EscapeShipTargetPointExpression = "phase_2_escape_ship_target(_\\d+)*";

	private const string Phase2EscapeShipBarrierTag = "phase_2_barricade";

	private const string Phase3TriggerVolumeBoxTag = "phase_3_trigger_volume_box_tag";

	private const string Phase3EnemyShip1StringId = "eastern_heavy_ship";

	private const string Phase3EnemyShip2StringId = "aserai_heavy_ship";

	private const string Phase3EnemyShip3StringId = "nord_medium_ship";

	private const string Phase3EnemyShip4StringId = "nord_medium_ship";

	private const string Phase3EnemyShip5StringId = "khuzait_heavy_ship";

	private const string Phase3EnemyShip1SpawnPointTag = "phase_3_enemy_ship_1_sp";

	private const string Phase3EnemyShip2SpawnPointTag = "phase_3_enemy_ship_2_sp";

	private const string Phase3EnemyShip3SpawnPointTag = "phase_3_enemy_ship_3_sp";

	private const string Phase3EnemyShip4SpawnPointTag = "phase_3_enemy_ship_4_sp";

	private const string Phase3EnemyShip5SpawnPointTag = "phase_3_enemy_ship_5_sp";

	private const string Phase3EnemyShipReinforcementSpawnPoint1Tag = "phase_3_enemy_reinforcement_1_sp";

	private const string Phase3EnemyShipReinforcementSpawnPoint2Tag = "phase_3_enemy_reinforcement_2_sp";

	private const string Phase3EnemyReinforcementShip1StringId = "empire_medium_ship";

	private const string Phase3EnemyReinforcementShip2StringId = "nord_medium_ship";

	private const string Phase3EnemyReinforcementShip3StringId = "sturgia_heavy_ship";

	private const string Phase3AllyShip1SpawnPointTag = "phase_3_ally_ship_1_sp";

	private const string Phase3AllyShip2SpawnPointTag = "phase_3_ally_ship_2_sp";

	private const string Phase3AllyShip3SpawnPointTag = "phase_3_ally_ship_3_sp";

	private const string Phase3AllyShip4SpawnPointTag = "phase_3_ally_ship_4_sp";

	private const string Phase3AllyShip5SpawnPointTag = "phase_3_ally_ship_5_sp";

	private const string Phase3PlayerShipSpawnPointTag = "phase_3_player_ship_sp";

	private const string Phase3PlayerShipStringId = "empire_heavy_ship";

	private const string Phase3PlayerShipUsePointStringId = "sp_troop_captain";

	private const string PurigsEnterenceTriggerBoxTag = "phase_4_purigs_entrance_trigger_box";

	private const string PurigImmortalShipSpawnPointTag = "sp_immortal_purig";

	private const string PurigBodyguard1ImmortalShipSpawnPointTag = "sp_immortal_bodyguard_1";

	private const string PurigBodyguard2ImmortalShipSpawnPointTag = "sp_immortal_bodyguard_2";

	private const string PurigShipSpawnPointTag = "phase_4_purig_ship_sp";

	private const string PurigShipStringId = "purigs_roundship_storyline";

	private const string PurigShipTroopStringId = "sea_hounds";

	private const int PurigShipTroopCount = 40;

	private const string NavalBossFightPlayerSpawnPointTag = "naval_boss_fight_player_sp";

	private const string NavalBossFightPlayerAllySpawnPointTagPrefix = "naval_boss_fight_player_ally_sp_";

	private const string NavalBossFightEnemyBossSpawnPointTag = "naval_boss_fight_enemy_boss_sp";

	private const string NavalBossFightEnemyTroopSpawnPointTagPrefix = "naval_boss_fight_player_enemy_sp_";

	private const int NavalBossFightAllyTroopCount = 2;

	private const int NavalBossFightEnemyTroopCount = 2;

	private const string NavalBossFightPlayerBodyguardTroopStringId = "gangradirs_kin_melee";

	private const string NavalBossFightEnemyBodyguardTroopStringId = "sea_hounds";

	private const string BossFightConversationCameraTag = "sp_boss_fight_camera";

	private readonly List<KeyValuePair<string, string>> _phase1EnemyShip2UpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", ""),
		new KeyValuePair<string, string>("aft", ""),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", ""),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", ""),
		new KeyValuePair<string, string>("roof", "roof_7"),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "")
	};

	private readonly List<KeyValuePair<string, string>> _escapeShipUpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", ""),
		new KeyValuePair<string, string>("aft", ""),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", "deck_large_arrow_and_javelin_crates_lvl3"),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", ""),
		new KeyValuePair<string, string>("roof", "roof_5"),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "")
	};

	private readonly List<KeyValuePair<string, string>> _phase2AllyShip1UpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("aft", "aft_battlement_lvl3_wbarracks"),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", "deck_arrow_and_javelin_crates_lvl2"),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", "sails_lvl2"),
		new KeyValuePair<string, string>("roof", ""),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "side_southern_shields_lvl1")
	};

	private readonly List<KeyValuePair<string, string>> _phase2AllyShip2UpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", ""),
		new KeyValuePair<string, string>("aft", ""),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", "deck_arrow_and_javelin_crates_lvl2"),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", "sails_lvl3"),
		new KeyValuePair<string, string>("roof", ""),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "side_northern_shields_lvl1")
	};

	private readonly List<KeyValuePair<string, string>> _phase2AllyShip3UpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", ""),
		new KeyValuePair<string, string>("aft", ""),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", "deck_large_arrow_and_javelin_crates_lvl3"),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", "sails_lvl3"),
		new KeyValuePair<string, string>("roof", ""),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "side_northern_shields_lvl2")
	};

	private readonly List<KeyValuePair<string, string>> _phase2AllyShip4UpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", ""),
		new KeyValuePair<string, string>("aft", ""),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", "deck_large_arrow_and_javelin_crates_lvl3"),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", "sails_lvl2"),
		new KeyValuePair<string, string>("roof", ""),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "side_northern_shields_lvl2")
	};

	private readonly List<KeyValuePair<string, string>> _phase2AllyShip5UpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", ""),
		new KeyValuePair<string, string>("aft", ""),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", "deck_arrow_and_javelin_crates_lvl2"),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", "sails_lvl2"),
		new KeyValuePair<string, string>("roof", ""),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "side_northern_shields_lvl1")
	};

	private readonly List<KeyValuePair<string, string>> _phase3EnemyShip1UpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", ""),
		new KeyValuePair<string, string>("aft", ""),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", "deck_arrow_and_javelin_crates_lvl2"),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", "sails_lvl3"),
		new KeyValuePair<string, string>("roof", ""),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "side_southern_shields_lvl2")
	};

	private readonly List<KeyValuePair<string, string>> _phase3EnemyShip2UpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", ""),
		new KeyValuePair<string, string>("aft", ""),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", "deck_ammo_crates_lvl2"),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", "sails_lvl2"),
		new KeyValuePair<string, string>("roof", ""),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "side_southern_shields_lvl2")
	};

	private readonly List<KeyValuePair<string, string>> _phase3EnemyShip3UpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", ""),
		new KeyValuePair<string, string>("aft", ""),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", "deck_ammo_crates_lvl2"),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", "sails_lvl2"),
		new KeyValuePair<string, string>("roof", ""),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "")
	};

	private readonly List<KeyValuePair<string, string>> _phase3EnemyShip4UpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", ""),
		new KeyValuePair<string, string>("aft", ""),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", "deck_ammo_bins_lvl1"),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", "sails_lvl3"),
		new KeyValuePair<string, string>("roof", ""),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "side_northern_shields_lvl1")
	};

	private readonly List<KeyValuePair<string, string>> _phase3EnemyShip5UpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("aft", ""),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", "deck_ammo_bins_lvl1"),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", "sails_lvl2"),
		new KeyValuePair<string, string>("roof", ""),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "side_southern_shields_lvl1")
	};

	private readonly List<KeyValuePair<string, string>> _phase3EnemyReinforcementShip1UpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", ""),
		new KeyValuePair<string, string>("aft", ""),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", "deck_boarding_weapons_lvl3"),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", "sails_lvl2"),
		new KeyValuePair<string, string>("roof", ""),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "side_southern_shields_lvl2")
	};

	private readonly List<KeyValuePair<string, string>> _phase3EnemyReinforcementShip2UpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", ""),
		new KeyValuePair<string, string>("aft", ""),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", "deck_boarding_weapons_lvl3"),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", "sails_lvl3"),
		new KeyValuePair<string, string>("roof", ""),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "side_northern_shields_lvl1")
	};

	private readonly List<KeyValuePair<string, string>> _phase3EnemyReinforcementShip3UpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", ""),
		new KeyValuePair<string, string>("aft", ""),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", "deck_boarding_weapons_lvl3"),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", "sails_lvl2"),
		new KeyValuePair<string, string>("roof", ""),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "side_northern_shields_lvl2")
	};

	private readonly List<KeyValuePair<string, string>> _phase4PurigsShipUpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", "fore_battlement_lvl3_wbarracks"),
		new KeyValuePair<string, string>("aft", "aft_battlement_lvl3_wbarracks"),
		new KeyValuePair<string, string>("hull", ""),
		new KeyValuePair<string, string>("deck", ""),
		new KeyValuePair<string, string>("oars", ""),
		new KeyValuePair<string, string>("sail", "sails_lvl3"),
		new KeyValuePair<string, string>("roof", ""),
		new KeyValuePair<string, string>("bow", ""),
		new KeyValuePair<string, string>("side", "")
	};

	private Quest5InstructionState _instructionState;

	private Quest5ApproachObjective _approachObjective;

	private Quest5JumpObjective _jumpObjective;

	private Quest5SwimObjective _swimObjective;

	private Quest5ClearGuardsObjective _clearGuardsObjective;

	private Quest5CheckInteriorObjective _checkInteriorObjective;

	private Quest5TalkWithYourSisterObjective _talkWithYourSisterObjective;

	private Quest5ReturnToDeckObjective _returnToDeckObjective;

	private Quest5CutLooseObjective _cutLooseObjective;

	private Quest5GunnarUsesShipObjective _gunnarUsesShipObjective;

	private Quest5EscapeObjective _escapeObjective;

	private Quest5ReachAlliesObjective _reachAlliesObjective;

	private Quest5DefeatEnemiesObjective _defeatEnemiesObjective;

	private Quest5DefeatPurigsShipObjective _defeatPurigsShipObjective;

	private Quest5DefeatPurigObjective _defeatPurigObjective;

	private GunnarMovementState _gunnarMovementState;

	private GunnarMovementStateForClimbingShip _gunnarMovementStateForClimbingShip;

	private ClimbingMachine _targetClimbingMachine;

	private MissionTimer _gunnarFellIntoTheWaterTimer;

	private GameEntity _jumpOffInitialPositionGameEntity;

	private GameEntity _jumpOffTargetPositionGameEntity;

	private GameEntity _hidingSpot1PositionGameEntity;

	private MissionShip _phase1EnemyShip1;

	private MissionShip _phase1EnemyShip2;

	private MissionShip _phase1EnemyShip3;

	private MissionShip _phase1EnemyShip4;

	private Figurehead EscapeShipFigurehead = DefaultFigureheads.Lion;

	private bool _talkedWithSister;

	private bool _crusasAndSeaHoundMovedToTheConversationPoints;

	private List<GameEntity> _dynamicPatrolAreas = new List<GameEntity>();

	private List<Agent> _stealthAgents = new List<Agent>();

	private WeakGameEntity _crusasConversationPointFrame;

	private WeakGameEntity _slaveTraderConversationPointFrame;

	private GameEntity _approachPointEntity;

	private GameEntity _phase1EnemyShipToInteriorShipDoorEntity;

	private GameEntity _phase1InteriorToEnemyShip3ShipDoorEntity;

	private GameEntity _phase1EnemyShip1InitialSpawnEntity;

	private GameEntity _phase1EnemyShip1TargetEntity;

	private Queue<ConversationSound> _conversationSounds = new Queue<ConversationSound>();

	private List<MBInformationManager.DialogNotificationHandle> _dialogNotificationHandleCache = new List<MBInformationManager.DialogNotificationHandle>();

	private float _lastCachedPlayerShipDistanceToTargetApproachPoint;

	private MissionTimer _playerShipsTargetApproachPointDistanceCheckTimer;

	private MissionTimer _escapeShipCutLooseTimer;

	private MissionTimer _allowedSwimRadiusCheckTimer;

	private ActionIndexCache _sisterWoundedAnimationActionIndexCache;

	private ActionIndexCache _slaveTraderShipOarsmanActionIndexCache;

	private Vec3 _phase1PlayerShipSpawnPosition = Vec3.Invalid;

	private Equipment _mainAgentEquipmentCopyForInteriorMission;

	private MissionShip _phase2EnemyShip1;

	private MissionShip _phase2EnemyShip2;

	private MissionShip _phase2EnemyShip3;

	private MissionShip _phase2EnemyShip4;

	private MissionShip _phase2EnemyShip5;

	private MissionShip _phase2EnemyShipStationary1;

	private GameEntity _phase2EscapeShipPirateTargetFrame1;

	private GameEntity _phase2EscapeShipPirateTargetFrame2;

	private GameEntity _phase2EscapeShipPirateTargetFrame3;

	private GameEntity _phase2EscapeShipPirateTargetFrame4;

	private GameEntity _phase2EscapeShipPirateTargetFrame5;

	private GameEntity _currentPhase2EscapeShipTargetPoint;

	private MissionShip _phase2AllyShip1;

	private MissionShip _phase2AllyShip2;

	private MissionShip _phase2AllyShip3;

	private MissionShip _phase2AllyShip4;

	private MissionShip _phase2AllyShip5;

	private Dictionary<MissionShip, GameEntity> _pirateShipTriggerPoints = new Dictionary<MissionShip, GameEntity>();

	private Dictionary<MissionShip, bool> _isPirateShipMovementDisabled = new Dictionary<MissionShip, bool>();

	private Dictionary<MissionShip, ShipAttachmentMachine> _pirateShipEnabledAttachmentMachine = new Dictionary<MissionShip, ShipAttachmentMachine>();

	private Dictionary<MissionShip, bool> _isPirateShipTriggered = new Dictionary<MissionShip, bool>();

	private Dictionary<MissionShip, bool> _isPirateShipMovingToTheEscapeShip = new Dictionary<MissionShip, bool>();

	private Dictionary<MissionShip, bool> _isPirateShipLostItsCrew = new Dictionary<MissionShip, bool>();

	private Dictionary<MissionShip, bool> _limitPirateShipChasingSpeed = new Dictionary<MissionShip, bool>();

	private Dictionary<MissionShip, MissionTimer> _autoCutLooseTimersForPirateShips = new Dictionary<MissionShip, MissionTimer>();

	private Dictionary<MissionShip, MissionTimer> _autoEstablishConnectionsForPirateShips = new Dictionary<MissionShip, MissionTimer>();

	private Dictionary<MissionShip, bool> _isMissionShipBoardedToTheEscapeShip = new Dictionary<MissionShip, bool>();

	private List<GameEntity> _phase2EscapeShipTargetPointEntities = new List<GameEntity>(32);

	private Queue<GameEntity> _phase2EscapeShipTargetPoints = new Queue<GameEntity>();

	private MissionTimer _playerLeftTheEscapeShipTimer;

	private MissionTimer _phase2EscapeShipStuckTimer;

	private Vec3 _phase2EscapeShipStuckCheckPosition = Vec3.Invalid;

	private float _escapeShipTargetSpeed;

	private float _escapeShipSpeed;

	private Vec2 _escapeShipTargetDirection;

	private Vec2 _escapeShipDirection;

	private readonly List<KeyValuePair<string, int>> _phase2AllyShip1Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("aserai_marine_t5", 54),
		new KeyValuePair<string, int>("southern_pirates_chief", 18)
	};

	private readonly List<KeyValuePair<string, int>> _phase2AllyShip2Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("skolderbrotva_tier_2", 5),
		new KeyValuePair<string, int>("skolderbrotva_tier_3", 34)
	};

	private readonly List<KeyValuePair<string, int>> _phase2AllyShip3Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("gangradirs_kin_ranged", 18),
		new KeyValuePair<string, int>("gangradirs_kin_melee", 19)
	};

	private readonly List<KeyValuePair<string, int>> _phase2AllyShip4Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("skolderbrotva_tier_2", 32),
		new KeyValuePair<string, int>("skolderbrotva_tier_3", 34)
	};

	private readonly List<KeyValuePair<string, int>> _phase2AllyShip5Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("skolderbrotva_tier_3", 18),
		new KeyValuePair<string, int>("skolderbrotva_tier_2", 17)
	};

	private readonly List<KeyValuePair<string, int>> _phase2EnemyShip1Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("sea_hound_captivity", 4),
		new KeyValuePair<string, int>("sea_hound_captivity", 1)
	};

	private readonly List<KeyValuePair<string, int>> _phase2EnemyShip2Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("sea_hound_captivity", 3),
		new KeyValuePair<string, int>("sea_hound_captivity", 2)
	};

	private readonly List<KeyValuePair<string, int>> _phase2EnemyShip3Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("sea_hound_captivity", 3),
		new KeyValuePair<string, int>("sea_hound_captivity", 2)
	};

	private readonly List<KeyValuePair<string, int>> _phase2EnemyShip4Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("sea_hound_captivity", 3),
		new KeyValuePair<string, int>("sea_hound_captivity", 2)
	};

	private readonly List<KeyValuePair<string, int>> _phase2EnemyShip5Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("sea_hound_captivity", 3),
		new KeyValuePair<string, int>("sea_hound_captivity", 2)
	};

	private readonly List<KeyValuePair<string, int>> _phase2EnemyShipStationary1Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("sea_hounds_marksman", 8)
	};

	private MissionShip _phase3EnemyShip1;

	private MissionShip _phase3EnemyShip2;

	private MissionShip _phase3EnemyShip3;

	private MissionShip _phase3EnemyShip4;

	private MissionShip _phase3EnemyShip5;

	private MissionShip _phase3EnemyReinforcementShip1;

	private MissionShip _phase3EnemyReinforcementShip2;

	private VolumeBox _phase3TriggerVolumeBox;

	private readonly List<MissionShip> _allyShipTargetKeysBuffer = new List<MissionShip>(16);

	private readonly HashSet<MissionShip> _assignedEnemyShips = new HashSet<MissionShip>();

	private bool _isReinforcementCalled;

	private bool _isReinforcementInitialized;

	private readonly List<KeyValuePair<string, int>> _phase3PlayerShipTroops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("gangradirs_kin_melee", 40),
		new KeyValuePair<string, int>("gangradirs_kin_melee", 40)
	};

	private readonly List<KeyValuePair<string, int>> _phase3EnemyShip1Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("sea_hounds", 52),
		new KeyValuePair<string, int>("sea_hounds_marksman", 10)
	};

	private readonly List<KeyValuePair<string, int>> _phase3EnemyShip2Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("sea_hounds_pups", 64),
		new KeyValuePair<string, int>("sea_hounds_marksman", 10)
	};

	private readonly List<KeyValuePair<string, int>> _phase3EnemyShip3Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("sea_hounds_pups", 25),
		new KeyValuePair<string, int>("sea_hounds", 44)
	};

	private readonly List<KeyValuePair<string, int>> _phase3EnemyShip4Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("sea_hounds_pups", 15),
		new KeyValuePair<string, int>("sea_hounds", 50)
	};

	private readonly List<KeyValuePair<string, int>> _phase3EnemyShip5Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("sea_hounds_marksman", 16),
		new KeyValuePair<string, int>("sea_hounds", 50)
	};

	private readonly List<KeyValuePair<string, int>> _phase3EnemyReinforcementShip1Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("sea_hounds_marksman", 15),
		new KeyValuePair<string, int>("sea_hound_captivity", 30)
	};

	private readonly List<KeyValuePair<string, int>> _phase3EnemyReinforcementShip2Troops = new List<KeyValuePair<string, int>>
	{
		new KeyValuePair<string, int>("sea_hounds_marksman", 15),
		new KeyValuePair<string, int>("sea_hounds", 30)
	};

	private int _phase3TotalEnemyCount;

	private BossFightStateEnum BossFightState;

	private List<Agent> _purigShipAgents = new List<Agent>();

	private List<Agent> _duelPhaseAllyAgents;

	private List<Agent> _duelPhaseEnemyAgents;

	private Queue<ConversationSound> _purigNotifications = new Queue<ConversationSound>();

	private Agent _purigBodyguard1;

	private Agent _purigBodyguard2;

	private bool _isPurigCutsceneStarted;

	private bool _isPlayerUsingShipAtTheStartOfThePurigCutscene;

	private StandingPoint _playerStandingPointAtTheStartOfThePurigCutscene;

	private VolumeBox _phase4TriggerVolumeBox;

	private GameEntity _playerSpawnPointEntity;

	private GameEntity _enemyBossSpawnPointEntity;

	private BattleSideEnum _winnerSide = BattleSideEnum.None;

	private NavalAgentsLogic _navalAgentsLogic;

	private NavalShipsLogic _navalShipsLogic;

	private NavalTrajectoryPlanningLogic _navalTrajectoryPlanningLogic;

	private MissionObjectiveLogic _missionObjectiveLogic;

	private LightScriptedFiresMissionController _lightScriptedFiresMissionController;

	private List<Formation> _availableAllyFormations = new List<Formation>();

	private List<Formation> _availableEnemyFormations = new List<Formation>();

	private MissionTimer _endMissionTimer;

	private Formation _playerFormation;

	private MissionShip _playerShip;

	private readonly MobileParty _enemyParty;

	private Agent _laharAgent;

	private Agent _bjolgurAgent;

	private Agent _crusasAgent;

	private Agent _gunnarAgent;

	private Agent _purigAgent;

	private Agent _slaveTraderAgent;

	private CharacterObject _slaveTraderCharacter;

	private Agent[] _slaveTraderShipOarsmen = new Agent[6];

	private AgentNavalComponent _gunnarAgentNavalComponent;

	private bool _isCheckpointInitialize;

	private bool _isMissionFailPopUpTriggered;

	private GameEntity JumpOffInitialPosition
	{
		get
		{
			if (_jumpOffInitialPositionGameEntity == null)
			{
				_jumpOffInitialPositionGameEntity = Mission.Current.Scene.FindEntityWithTag("gangradir_jump_off_initial");
			}
			return _jumpOffInitialPositionGameEntity;
		}
	}

	private GameEntity JumpOffTargetPosition
	{
		get
		{
			if (_jumpOffTargetPositionGameEntity == null)
			{
				_jumpOffTargetPositionGameEntity = Mission.Current.Scene.FindEntityWithTag("gangradir_jump_off_target");
			}
			return _jumpOffTargetPositionGameEntity;
		}
	}

	private GameEntity HidingSpot1Position
	{
		get
		{
			if (_hidingSpot1PositionGameEntity == null)
			{
				_hidingSpot1PositionGameEntity = Mission.Current.Scene.FindEntityWithTag("sp_gangradir_hiding_spot");
			}
			return _hidingSpot1PositionGameEntity;
		}
	}

	private MatrixFrame GunnarShipUsePosition => EscapeShip.GetCaptainSpawnGlobalFrame();

	public GameEntity Phase1InteriorCameraSisterEntity { get; private set; }

	private MissionShip EscapeShip => _phase1EnemyShip3 ?? _playerShip;

	public bool IsEscapeShipStuck { get; private set; }

	private int Phase2AllyShip1TroopCount => _phase2AllyShip1Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase2AllyShip2TroopCount => _phase2AllyShip2Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase2AllyShip3TroopCount => _phase2AllyShip3Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase2AllyShip4TroopCount => _phase2AllyShip4Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase2AllyShip5TroopCount => _phase2AllyShip5Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase2EnemyShip1TroopCount => _phase2EnemyShip1Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase2EnemyShip2TroopCount => _phase2EnemyShip2Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase2EnemyShip3TroopCount => _phase2EnemyShip3Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase2EnemyShip4TroopCount => _phase2EnemyShip4Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase2EnemyShip5TroopCount => _phase2EnemyShip5Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase2EnemyShipStationary1TroopCount => _phase2EnemyShipStationary1Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase3PlayerShipTroopCount => _phase3PlayerShipTroops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase3EnemyShip1TroopCount => _phase3EnemyShip1Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase3EnemyShip2TroopCount => _phase3EnemyShip2Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase3EnemyShip3TroopCount => _phase3EnemyShip3Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase3EnemyShip4TroopCount => _phase3EnemyShip4Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase3EnemyShip5TroopCount => _phase3EnemyShip5Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase3EnemyReinforcementShip1TroopCount => _phase3EnemyReinforcementShip1Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	private int Phase3EnemyReinforcementShip2TroopCount => _phase3EnemyReinforcementShip2Troops.Sum((KeyValuePair<string, int> kvp) => kvp.Value);

	public BossFightOutComeEnum BossFightOutCome { get; private set; }

	public GameEntity BossFightConversationCameraGameEntity { get; private set; }

	public MissionShip Phase4PurigShip { get; private set; }

	public Agent SisterAgent { get; private set; }

	public Quest5SetPieceBattleMissionState LastHitCheckpoint { get; private set; }

	public Quest5SetPieceBattleMissionState State { get; private set; }

	public bool ShouldMissionContinueFromCheckpoint { get; private set; }

	public BattleSideEnum PlayerSide => BattleSideEnum.None;

	public Quest5SetPieceBattleMissionController(Quest5SetPieceBattleMissionState lastHitCheckpoint, MobileParty enemyParty)
	{
		BossFightOutCome = BossFightOutComeEnum.None;
		State = Quest5SetPieceBattleMissionState.None;
		LastHitCheckpoint = lastHitCheckpoint;
		ShouldMissionContinueFromCheckpoint = false;
		_enemyParty = enemyParty;
		Hero.MainHero.HitPoints = Hero.MainHero.MaxHitPoints;
		NavalStorylineData.Gunnar.HitPoints = NavalStorylineData.Gunnar.MaxHitPoints;
		NavalStorylineData.Prusas.HitPoints = NavalStorylineData.Prusas.MaxHitPoints;
		NavalStorylineData.Purig.HitPoints = NavalStorylineData.Purig.MaxHitPoints;
		NavalStorylineData.Bjolgur.HitPoints = NavalStorylineData.Bjolgur.MaxHitPoints;
		NavalStorylineData.Lahar.HitPoints = NavalStorylineData.Lahar.MaxHitPoints;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		Mission.Current.Scene.SetAtmosphereWithName("TOD_02_00_SemiCloudy");
		_slaveTraderCharacter = MBObjectManager.Instance.GetObject<CharacterObject>("sea_hounds");
		AddConversationSounds();
		_navalAgentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_navalTrajectoryPlanningLogic = base.Mission.GetMissionBehavior<NavalTrajectoryPlanningLogic>();
		_missionObjectiveLogic = base.Mission.GetMissionBehavior<MissionObjectiveLogic>();
		_lightScriptedFiresMissionController = base.Mission.GetMissionBehavior<LightScriptedFiresMissionController>();
		Team team = Mission.GetTeam(TeamSideEnum.PlayerTeam);
		AddAvailableAllyFormation(team.GetFormation(FormationClass.Infantry));
		AddAvailableAllyFormation(team.GetFormation(FormationClass.Ranged));
		AddAvailableAllyFormation(team.GetFormation(FormationClass.Cavalry));
		AddAvailableAllyFormation(team.GetFormation(FormationClass.HorseArcher));
		AddAvailableAllyFormation(team.GetFormation(FormationClass.NumberOfDefaultFormations));
		AddAvailableAllyFormation(team.GetFormation(FormationClass.HeavyInfantry));
		AddAvailableAllyFormation(team.GetFormation(FormationClass.LightCavalry));
		AddAvailableAllyFormation(team.GetFormation(FormationClass.HeavyCavalry));
		Team team2 = Mission.GetTeam(TeamSideEnum.EnemyTeam);
		AddAvailableEnemyFormation(team2.GetFormation(FormationClass.Infantry));
		AddAvailableEnemyFormation(team2.GetFormation(FormationClass.Ranged));
		AddAvailableEnemyFormation(team2.GetFormation(FormationClass.Cavalry));
		AddAvailableEnemyFormation(team2.GetFormation(FormationClass.HorseArcher));
		AddAvailableEnemyFormation(team2.GetFormation(FormationClass.NumberOfDefaultFormations));
		AddAvailableEnemyFormation(team2.GetFormation(FormationClass.HeavyInfantry));
		AddAvailableEnemyFormation(team2.GetFormation(FormationClass.LightCavalry));
		AddAvailableEnemyFormation(team2.GetFormation(FormationClass.HeavyCavalry));
		_phase1InteriorToEnemyShip3ShipDoorEntity = Mission.Current.Scene.FindEntityWithTag("phase_1_interior_to_enemy_ship_3_door_tag");
		_phase1InteriorToEnemyShip3ShipDoorEntity.GetFirstScriptOfType<ShipDoorUsePoint>().SetShipDoorUsePointEnabled(isEnabled: false);
		foreach (GameEntity item2 in Mission.Current.Scene.FindEntitiesWithTagExpression("phase_2_escape_ship_target(_\\d+)*"))
		{
			_phase2EscapeShipTargetPointEntities.Add(item2);
		}
		GameEntity[] array = new GameEntity[_phase2EscapeShipTargetPointEntities.Count];
		foreach (GameEntity phase2EscapeShipTargetPointEntity in _phase2EscapeShipTargetPointEntities)
		{
			int num = int.Parse(phase2EscapeShipTargetPointEntity.Tags.FirstOrDefault().Split(new char[1] { '_' })[^1]);
			array[num - 1] = phase2EscapeShipTargetPointEntity;
		}
		GameEntity[] array2 = array;
		foreach (GameEntity item in array2)
		{
			_phase2EscapeShipTargetPoints.Enqueue(item);
		}
		_phase1EnemyShip1InitialSpawnEntity = Mission.Current.Scene.FindEntityWithTag("phase_1_enemy_ship_1_sp_initial");
		_phase1EnemyShip1TargetEntity = Mission.Current.Scene.FindEntityWithTag("phase_1_enemy_ship_1_sp");
		_phase3TriggerVolumeBox = Mission.Current.Scene.FindEntityWithTag("phase_3_trigger_volume_box_tag").GetFirstScriptOfType<VolumeBox>();
		_phase4TriggerVolumeBox = Mission.Current.Scene.FindEntityWithTag("phase_4_purigs_entrance_trigger_box").GetFirstScriptOfType<VolumeBox>();
		_approachPointEntity = Mission.Current.Scene.FindEntityWithTag("phase_1_approach_point");
		_navalShipsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic.SetTeamShipDeploymentLimit(TeamSideEnum.PlayerTeam, NavalShipDeploymentLimit.Max());
		_navalShipsLogic.SetTeamShipDeploymentLimit(TeamSideEnum.PlayerAllyTeam, NavalShipDeploymentLimit.Max());
		_navalShipsLogic.SetTeamShipDeploymentLimit(TeamSideEnum.EnemyTeam, NavalShipDeploymentLimit.Max());
		_navalShipsLogic.SetDeploymentMode(value: false);
		base.Mission.GetMissionBehavior<MissionConversationLogic>().DisableStartConversation(isDisabled: true);
		_playerFormation = GetAvailableAllyFormation();
		Hero.MainHero.Heal(Hero.MainHero.MaxHitPoints);
		NavalStorylineData.Gunnar.Heal(NavalStorylineData.Gunnar.MaxHitPoints);
		NavalStorylineData.Prusas.Heal(NavalStorylineData.Prusas.MaxHitPoints);
		StoryModeHeroes.LittleSister.Heal(StoryModeHeroes.LittleSister.MaxHitPoints);
		_sisterWoundedAnimationActionIndexCache = ActionIndexCache.Create("act_conversation_weary2_loop");
		_slaveTraderShipOarsmanActionIndexCache = ActionIndexCache.Create("act_sit_2");
		_navalAgentsLogic.SetSpawnReinforcementsOnTick(value: false);
		State = LastHitCheckpoint;
	}

	public override void OnBehaviorInitialize()
	{
		if (!SailWindProfile.IsSailWindProfileInitialized)
		{
			SailWindProfile.InitializeProfile();
		}
		if (_navalShipsLogic == null)
		{
			_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		}
		_navalShipsLogic.ShipAttachmentBrokenEvent += OnAttachmentBroken;
	}

	public override void OnFixedMissionTick(float fixedDt)
	{
		base.OnFixedMissionTick(fixedDt);
		Quest5SetPieceBattleMissionState state = State;
		if (state == Quest5SetPieceBattleMissionState.Phase2InProgress)
		{
			HandlePirateShipGettingCloseToEscapeShip(_phase2EnemyShip1, _phase2EscapeShipPirateTargetFrame1, 5f, fixedDt);
			HandlePirateShipGettingCloseToEscapeShip(_phase2EnemyShip2, _phase2EscapeShipPirateTargetFrame2, 5f, fixedDt);
			HandlePirateShipGettingCloseToEscapeShip(_phase2EnemyShip3, _phase2EscapeShipPirateTargetFrame3, 5f, fixedDt);
			HandlePirateShipGettingCloseToEscapeShip(_phase2EnemyShip4, _phase2EscapeShipPirateTargetFrame4, 5f, fixedDt);
			HandlePirateShipGettingCloseToEscapeShip(_phase2EnemyShip5, _phase2EscapeShipPirateTargetFrame5, 5f, fixedDt);
			MoveEscapeShipAlongTheTrack(fixedDt);
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		HandleStealthShipsBridgeConnections();
		switch (State)
		{
		case Quest5SetPieceBattleMissionState.InitializePhase1Part1:
			InitializePhase1Part1();
			State = Quest5SetPieceBattleMissionState.InitializePhase1Part2;
			break;
		case Quest5SetPieceBattleMissionState.InitializePhase1Part2:
			InitializePhase1Part2();
			HandlePlayersBridgeAndControlPointUsagesForPhase1GoToEnemyShip();
			State = Quest5SetPieceBattleMissionState.Phase1GoToEnemyShip;
			break;
		case Quest5SetPieceBattleMissionState.Phase1GoToEnemyShip:
			if (_instructionState == Quest5InstructionState.None)
			{
				_instructionState = Quest5InstructionState.Approach;
			}
			AdjustWindDirectionAccordingToTargetFrame(_approachPointEntity.GetGlobalFrame(), 2f, addRandomRotation: true);
			if (_playerShip.GameEntity.GetGlobalFrame().origin.Distance(_approachPointEntity.GetGlobalFrame().origin) <= 30f)
			{
				DisableSlaveTraderShipAgents();
				OnPlayerShipReachedApproachDistance();
				HandlePlayersBridgeAndControlPointUsagesForPhase1SwimmingAndStealthPhase();
			}
			_phase1EnemyShip3.SetAnchor(isAnchored: true);
			_phase1EnemyShip3.ShipOrder.SetShipStopOrder();
			HandleStealthShipsBridgeConnections();
			MovePhase1EnemyShip1ToItsTargetPoint();
			break;
		case Quest5SetPieceBattleMissionState.Phase1SwimmingPhase:
			if (_instructionState == Quest5InstructionState.WaitForJump)
			{
				_instructionState = Quest5InstructionState.Jump;
			}
			else if (_instructionState == Quest5InstructionState.WaitForSwim && Agent.Main.IsInWater())
			{
				_instructionState = Quest5InstructionState.Swim;
			}
			_playerShip.ShipOrder.SetShipStopOrder();
			_playerShip.ShipOrder.SetOrderOarsmenLevel(0);
			CheckAndPlayCrusasAndSlaveTraderConversationSound();
			if (_phase1EnemyShip4.GetIsAgentOnShip(Agent.Main))
			{
				State = Quest5SetPieceBattleMissionState.InitializeStealthPhasePart1;
				SetLastCheckpoint(Quest5SetPieceBattleMissionState.InitializeStealthPhasePart1);
			}
			break;
		case Quest5SetPieceBattleMissionState.InitializeStealthPhasePart1:
			InitializeStealthPhasePart1();
			State = Quest5SetPieceBattleMissionState.InitializeStealthPhasePart2;
			break;
		case Quest5SetPieceBattleMissionState.InitializeStealthPhasePart2:
			InitializeStealthPhasePart2();
			HealMainHero();
			State = Quest5SetPieceBattleMissionState.Phase1StealthPhase;
			HandlePlayersBridgeAndControlPointUsagesForPhase1SwimmingAndStealthPhase();
			break;
		case Quest5SetPieceBattleMissionState.Phase1StealthPhase:
			HandleStealthShipsBridgeConnections();
			HandleEscapeShipInteriorDoorUsage();
			if (Agent.Main == null || !Agent.Main.IsActive())
			{
				EndMissionWithAutoContinueFromCheckpoint();
			}
			else
			{
				_phase1EnemyShip2.GetWorldPositionOnDeck(out var worldPosition);
				if (worldPosition.AsVec2.Distance(Agent.Main.Position.AsVec2) < 20f && _instructionState == Quest5InstructionState.WaitForClearGuards)
				{
					_instructionState = Quest5InstructionState.ClearGuards;
				}
				if (_stealthAgents.IsEmpty() && _instructionState == Quest5InstructionState.WaitForCheckInterior)
				{
					_instructionState = Quest5InstructionState.CheckInterior;
				}
			}
			_phase1EnemyShip3.SetAnchor(isAnchored: true);
			_phase1EnemyShip3.ShipOrder.SetShipStopOrder();
			break;
		case Quest5SetPieceBattleMissionState.Phase1InitializeShipInteriorPhase:
			InitializeShipInteriorPhase();
			break;
		case Quest5SetPieceBattleMissionState.Phase1ShipInteriorPhase:
			if (_talkedWithSister)
			{
				_phase1InteriorToEnemyShip3ShipDoorEntity.GetFirstScriptOfType<ShipDoorUsePoint>().SetShipDoorUsePointEnabled(isEnabled: true);
			}
			else if (SisterAgent.Position.Distance(Agent.Main.Position) < 3f)
			{
				Phase1InteriorCameraSisterEntity = Mission.Current.Scene.FindEntityWithTag("phase_1_interior_camera_sister");
			}
			SisterAgent.SetActionChannel(0, in _sisterWoundedAnimationActionIndexCache, ignorePriority: false, (AnimFlags)0uL);
			break;
		case Quest5SetPieceBattleMissionState.Phase1InitializeGoBackToShip:
			InitializeGoBackToShip();
			if (_stealthAgents.IsEmpty())
			{
				_instructionState = Quest5InstructionState.WaitForCutLoose;
			}
			break;
		case Quest5SetPieceBattleMissionState.Phase1EscapePhase:
			if (_talkedWithSister)
			{
				if (_instructionState < Quest5InstructionState.WaitForCutLoose)
				{
					_instructionState = Quest5InstructionState.WaitForCutLoose;
				}
				bool isThereActiveBridgeTo = _phase1EnemyShip3.GetIsThereActiveBridgeTo(_phase1EnemyShip2);
				if (isThereActiveBridgeTo && _instructionState == Quest5InstructionState.WaitForCutLoose && _stealthAgents.IsEmpty())
				{
					_instructionState = Quest5InstructionState.CutLoose;
					_escapeShipCutLooseTimer = new MissionTimer(300f);
				}
				else if (!isThereActiveBridgeTo && _instructionState == Quest5InstructionState.WaitForGunnarUsesShip && _stealthAgents.IsEmpty())
				{
					_instructionState = Quest5InstructionState.GunnarUsesShip;
				}
				else if (!isThereActiveBridgeTo)
				{
					State = Quest5SetPieceBattleMissionState.Phase1ToPhase2FadeOut;
				}
				HandleEscapeShipCutLoose();
			}
			break;
		case Quest5SetPieceBattleMissionState.InitializePhase2Part1:
			InitializePhase2Part1();
			State = Quest5SetPieceBattleMissionState.InitializePhase2Part2;
			break;
		case Quest5SetPieceBattleMissionState.InitializePhase2Part2:
			State = Quest5SetPieceBattleMissionState.InitializePhase2Part3;
			InitializePhase2Part2();
			break;
		case Quest5SetPieceBattleMissionState.InitializePhase2Part3:
			State = Quest5SetPieceBattleMissionState.InitializePhase2Part4;
			InitializePhase2Part3();
			break;
		case Quest5SetPieceBattleMissionState.InitializePhase2Part4:
			InitializePhase2Part4();
			HealMainHero();
			SetLastCheckpoint(Quest5SetPieceBattleMissionState.InitializePhase2Part1);
			break;
		case Quest5SetPieceBattleMissionState.Phase2InProgress:
			UpdatePhase2MovingShipParameters(dt);
			if (_isCheckpointInitialize)
			{
				_isCheckpointInitialize = false;
			}
			CheckForEscapeShipStuck();
			HandleEscapeShipSpeed();
			HandleEscapeShipMovement();
			HandlePirateShipMovement(_phase2EnemyShip1, _phase2EscapeShipPirateTargetFrame1);
			HandlePirateShipMovement(_phase2EnemyShip2, _phase2EscapeShipPirateTargetFrame2);
			HandlePirateShipMovement(_phase2EnemyShip3, _phase2EscapeShipPirateTargetFrame3);
			HandlePirateShipMovement(_phase2EnemyShip4, _phase2EscapeShipPirateTargetFrame4);
			HandlePirateShipMovement(_phase2EnemyShip5, _phase2EscapeShipPirateTargetFrame5);
			HandlePirateShipSailModeAccordingToTheGlobalWindVelocity(_phase2EnemyShip1);
			HandlePirateShipSailModeAccordingToTheGlobalWindVelocity(_phase2EnemyShip2);
			HandlePirateShipSailModeAccordingToTheGlobalWindVelocity(_phase2EnemyShip3);
			HandlePirateShipSailModeAccordingToTheGlobalWindVelocity(_phase2EnemyShip4);
			HandlePirateShipSailModeAccordingToTheGlobalWindVelocity(_phase2EnemyShip5);
			HandleStationaryShipMovement(_phase2EnemyShipStationary1);
			CheckIfMainAgentLeftTheEscapeShip();
			AutoCutLooseEmptyPirateShipIfPlayerDoesNotForALongTime(_phase2EnemyShip1);
			AutoCutLooseEmptyPirateShipIfPlayerDoesNotForALongTime(_phase2EnemyShip2);
			AutoCutLooseEmptyPirateShipIfPlayerDoesNotForALongTime(_phase2EnemyShip3);
			AutoCutLooseEmptyPirateShipIfPlayerDoesNotForALongTime(_phase2EnemyShip4);
			AutoCutLooseEmptyPirateShipIfPlayerDoesNotForALongTime(_phase2EnemyShip5);
			AutoEstablishConnectionsForPirateShips(_phase2EnemyShip1, _phase2EscapeShipPirateTargetFrame1);
			AutoEstablishConnectionsForPirateShips(_phase2EnemyShip2, _phase2EscapeShipPirateTargetFrame2);
			AutoEstablishConnectionsForPirateShips(_phase2EnemyShip3, _phase2EscapeShipPirateTargetFrame3);
			AutoEstablishConnectionsForPirateShips(_phase2EnemyShip4, _phase2EscapeShipPirateTargetFrame4);
			AutoEstablishConnectionsForPirateShips(_phase2EnemyShip5, _phase2EscapeShipPirateTargetFrame5);
			HandleAllyShipMovementDuringPhase2(_phase2AllyShip1);
			HandleAllyShipMovementDuringPhase2(_phase2AllyShip2);
			HandleAllyShipMovementDuringPhase2(_phase2AllyShip3);
			HandleAllyShipMovementDuringPhase2(_phase2AllyShip4);
			HandleAllyShipMovementDuringPhase2(_phase2AllyShip5);
			HandlePirateShipBridgeConnectionCount(_phase2EnemyShip1);
			HandlePirateShipBridgeConnectionCount(_phase2EnemyShip2);
			HandlePirateShipBridgeConnectionCount(_phase2EnemyShip3);
			HandlePirateShipBridgeConnectionCount(_phase2EnemyShip4);
			HandlePirateShipBridgeConnectionCount(_phase2EnemyShip5);
			if (_instructionState == Quest5InstructionState.WaitForReachAllies && AreAllPhase2PirateShipsEliminated())
			{
				_instructionState = Quest5InstructionState.ReachAllies;
			}
			if (Agent.Main != null && _phase3TriggerVolumeBox.IsPointIn(Agent.Main.Position))
			{
				State = Quest5SetPieceBattleMissionState.Phase2ToPhase3FadeOut;
			}
			break;
		case Quest5SetPieceBattleMissionState.InitializePhase3Part1:
			InitializePhase3Part1();
			State = Quest5SetPieceBattleMissionState.InitializePhase3Part2;
			SetLastCheckpoint(Quest5SetPieceBattleMissionState.InitializePhase3Part1);
			break;
		case Quest5SetPieceBattleMissionState.InitializePhase3Part2:
			InitializePhase3Part2();
			State = Quest5SetPieceBattleMissionState.InitializePhase3Part3;
			break;
		case Quest5SetPieceBattleMissionState.InitializePhase3Part3:
			InitializePhase3Part3();
			HealMainHero();
			foreach (MissionShip allShip in _navalShipsLogic.AllShips)
			{
				if (allShip != _playerShip)
				{
					allShip.ShipOrder.SetShipEngageOrder();
				}
			}
			break;
		case Quest5SetPieceBattleMissionState.Phase3InProgress:
		{
			if (_isCheckpointInitialize)
			{
				_isCheckpointInitialize = false;
			}
			if (_instructionState == Quest5InstructionState.WaitForDefeatEnemies)
			{
				_instructionState = Quest5InstructionState.DefeatEnemies;
			}
			int count = Mission.Current.PlayerEnemyTeam.ActiveAgents.Count;
			if (_isReinforcementCalled && _isReinforcementInitialized && CanProceedToPhase4())
			{
				if (Agent.Main.IsUsingGameObject && Agent.Main.CurrentlyUsedGameObject is StandingPoint && _playerShip.ShipControllerMachine.PilotStandingPoint == Agent.Main.CurrentlyUsedGameObject)
				{
					_isPlayerUsingShipAtTheStartOfThePurigCutscene = true;
					_playerStandingPointAtTheStartOfThePurigCutscene = Agent.Main.CurrentlyUsedGameObject as StandingPoint;
				}
				_playerShip.ShipOrder.SetShipStopOrder();
				State = Quest5SetPieceBattleMissionState.Phase3ToPhase4FadeOut;
			}
			else if ((float)count <= (float)_phase3TotalEnemyCount * 0.5f)
			{
				if (!_isReinforcementCalled && !_isReinforcementInitialized)
				{
					CallReinforcement();
				}
				else if (_isReinforcementCalled && !_isReinforcementInitialized)
				{
					InitializeReinforcement();
				}
			}
			if (_isReinforcementCalled && _isReinforcementInitialized)
			{
				if (_phase3EnemyReinforcementShip1.ShipOrder.MovementOrderEnum != ShipOrder.ShipMovementOrderEnum.Engage)
				{
					_phase3EnemyReinforcementShip1.ShipOrder.SetShipEngageOrder();
				}
				if (_phase3EnemyReinforcementShip2.ShipOrder.MovementOrderEnum != ShipOrder.ShipMovementOrderEnum.Engage)
				{
					_phase3EnemyReinforcementShip2.ShipOrder.SetShipEngageOrder();
				}
			}
			CheckIfEnemyAgentFallIntoTheWater();
			break;
		}
		case Quest5SetPieceBattleMissionState.InitializePhase4Part1:
			InitializePhase4Part1();
			break;
		case Quest5SetPieceBattleMissionState.InitializePhase4Part2:
			InitializePhase4Part2();
			HealMainHero();
			break;
		case Quest5SetPieceBattleMissionState.Phase4InProgress:
			if (_isCheckpointInitialize)
			{
				_isCheckpointInitialize = false;
			}
			if (_isPurigCutsceneStarted)
			{
				CheckAndPlayPurigCutsceneNotifications();
			}
			if (_purigShipAgents.Count == 0)
			{
				State = Quest5SetPieceBattleMissionState.Phase4ToBossFightFadeOut;
				_playerShip.SetAnchor(isAnchored: true);
				_playerShip.ShipOrder.SetShipStopOrder();
				Phase4PurigShip.SetAnchor(isAnchored: true);
				DisableAllShipOrderControllers();
			}
			break;
		case Quest5SetPieceBattleMissionState.InitializeBossFightPart1:
			InitializeNavalBossFightPart1();
			State = Quest5SetPieceBattleMissionState.InitializeBossFightPart2;
			break;
		case Quest5SetPieceBattleMissionState.InitializeBossFightPart2:
			InitializeNavalBossFightPart2();
			State = Quest5SetPieceBattleMissionState.Phase4ToBossFightFadeIn;
			break;
		case Quest5SetPieceBattleMissionState.StartBossFightConversation:
			State = Quest5SetPieceBattleMissionState.BossFightConversationInProgress;
			StartBossFightConversation();
			break;
		case Quest5SetPieceBattleMissionState.BossFightConversationInProgress:
			if (ActionIndexCache.act_conversation_naval_start == _purigAgent.GetCurrentAction(0) || ActionIndexCache.act_conversation_naval_idle_loop == _purigAgent.GetCurrentAction(0))
			{
				_purigAgent.SetCurrentActionProgress(0, 1f);
				_purigAgent.SetActionChannel(0, in ActionIndexCache.act_conversation_normal_loop, ignorePriority: false, (AnimFlags)0uL);
			}
			break;
		case Quest5SetPieceBattleMissionState.BossFightInProgressAsDuel:
			if (_purigAgent == null || !_purigAgent.IsActive())
			{
				OnDuelOver(base.Mission.PlayerTeam.Side);
			}
			else if (Agent.Main == null || !Agent.Main.IsActive())
			{
				OnDuelOver(base.Mission.PlayerEnemyTeam.Side);
			}
			break;
		case Quest5SetPieceBattleMissionState.BossFightInProgressAsAll:
		{
			bool flag = false;
			for (int i = 0; i < _duelPhaseEnemyAgents.Count; i++)
			{
				if (_duelPhaseEnemyAgents[i].IsActive())
				{
					flag = true;
					break;
				}
			}
			if (!flag && (_purigAgent == null || !_purigAgent.IsActive()))
			{
				OnDuelOver(base.Mission.PlayerTeam.Side);
				break;
			}
			bool flag2 = false;
			for (int j = 0; j < _duelPhaseAllyAgents.Count; j++)
			{
				if (_duelPhaseAllyAgents[j].IsActive())
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2 && (Agent.Main == null || !Agent.Main.IsActive()))
			{
				OnDuelOver(base.Mission.PlayerEnemyTeam.Side);
			}
			break;
		}
		case Quest5SetPieceBattleMissionState.End:
			if (_endMissionTimer == null)
			{
				_endMissionTimer = new MissionTimer(2f);
			}
			else
			{
				if (!_endMissionTimer.Check() && !_isMissionFailPopUpTriggered)
				{
					break;
				}
				foreach (MBInformationManager.DialogNotificationHandle item in _dialogNotificationHandleCache)
				{
					CampaignInformationManager.ClearDialogNotification(item);
				}
				_dialogNotificationHandleCache.Clear();
				if (_winnerSide == base.Mission.PlayerTeam.Side && !ShouldMissionContinueFromCheckpoint)
				{
					TriggerPurigsDeadPopUp();
				}
				else
				{
					base.Mission.EndMission();
				}
				State = Quest5SetPieceBattleMissionState.Exit;
			}
			break;
		}
		CheckAndPrintInstructionNotification();
		HandleGunnarMovement();
		HandleIfGunnarFallsIntoTheWater();
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		_navalShipsLogic.ShipAttachmentBrokenEvent -= OnAttachmentBroken;
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		if (base.Mission.Mode == MissionMode.Stealth && _stealthAgents.Contains(affectedAgent))
		{
			_stealthAgents.Remove(affectedAgent);
		}
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			if (((allShip != _phase3EnemyReinforcementShip1 && allShip != _phase3EnemyReinforcementShip2) || _isReinforcementInitialized) && allShip != _playerShip && _navalAgentsLogic.GetActiveAgentCountOfShip(allShip) <= 0 && allShip.HasController)
			{
				DisableShipOrderController(allShip);
			}
		}
	}

	public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
	{
		base.OnObjectUsed(userAgent, usedObject);
		if (userAgent.IsMainAgent && usedObject is ShipDoorUsePoint)
		{
			if (State == Quest5SetPieceBattleMissionState.Phase1StealthPhase)
			{
				State = Quest5SetPieceBattleMissionState.Phase1GoToShipInteriorFadeOut;
				State = Quest5SetPieceBattleMissionState.Phase1GoToShipInteriorFadeOut;
				_phase1EnemyShipToInteriorShipDoorEntity.GetFirstScriptOfType<ShipDoorUsePoint>().SetShipDoorUsePointEnabled(isEnabled: false);
			}
			else if (State == Quest5SetPieceBattleMissionState.Phase1ShipInteriorPhase)
			{
				State = Quest5SetPieceBattleMissionState.Phase1GoBackToShipFadeOut;
				_phase1InteriorToEnemyShip3ShipDoorEntity.GetFirstScriptOfType<ShipDoorUsePoint>().SetShipDoorUsePointEnabled(isEnabled: false);
			}
		}
	}

	public override void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
	{
		base.OnAgentTeamChanged(prevTeam, newTeam, agent);
		if (newTeam == base.Mission.PlayerEnemyTeam && State < Quest5SetPieceBattleMissionState.Phase1ToPhase2FadeOut)
		{
			AgentFlag agentFlags = agent.GetAgentFlags();
			agent.SetAgentFlags(agentFlags | AgentFlag.CanGetAlarmed);
			agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator().AddBehaviorGroup<AlarmedBehaviorGroup>()
				.AddBehavior<CautiousBehavior>();
		}
	}

	public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectedAgent.IsMainAgent)
		{
			if (State <= Quest5SetPieceBattleMissionState.BossFightConversationInProgress)
			{
				Agent.Main.Health = Agent.Main.HealthLimit;
				EndMissionWithAutoContinueFromCheckpoint();
			}
			MakeGunnarStopUsingGameObjectBeforeMissionEnd();
		}
		if (_purigShipAgents.Contains(affectedAgent))
		{
			_purigShipAgents.Remove(affectedAgent);
		}
	}

	public override InquiryData OnEndMissionRequest(out bool canLeave)
	{
		MakeGunnarStopUsingGameObjectBeforeMissionEnd();
		return base.OnEndMissionRequest(out canLeave);
	}

	protected override void OnEndMission()
	{
		foreach (MBInformationManager.DialogNotificationHandle item in _dialogNotificationHandleCache)
		{
			CampaignInformationManager.ClearDialogNotification(item);
		}
		_dialogNotificationHandleCache.Clear();
		MakeGunnarStopUsingGameObjectBeforeMissionEnd();
		base.OnEndMission();
		((Ship)(_playerShip?.ShipOrigin)).Owner = null;
		if (_phase2AllyShip1 != null)
		{
			((Ship)_phase2AllyShip1.ShipOrigin).Owner = null;
		}
		if (_phase2AllyShip2 != null)
		{
			((Ship)_phase2AllyShip2.ShipOrigin).Owner = null;
		}
		if (_phase2AllyShip3 != null)
		{
			((Ship)_phase2AllyShip3.ShipOrigin).Owner = null;
		}
		if (_phase2AllyShip4 != null)
		{
			((Ship)_phase2AllyShip4.ShipOrigin).Owner = null;
		}
		if (_phase2AllyShip5 != null)
		{
			((Ship)_phase2AllyShip5.ShipOrigin).Owner = null;
		}
	}

	public override void OnRetreatMission()
	{
		MakeGunnarStopUsingGameObjectBeforeMissionEnd();
		base.OnRetreatMission();
	}

	public override void OnSurrenderMission()
	{
		MakeGunnarStopUsingGameObjectBeforeMissionEnd();
		base.OnSurrenderMission();
	}

	private void DeactivateObjectiveIfItIsActive(MissionObjective objective)
	{
		if (objective != null && objective.IsActive)
		{
			_missionObjectiveLogic.CompleteCurrentObjective();
		}
	}

	private void CheckAndPrintInstructionNotification()
	{
		switch (_instructionState)
		{
		case Quest5InstructionState.Approach:
			DisplayCurrentInstructionNotification();
			if (_missionObjectiveLogic != null)
			{
				_approachObjective = new Quest5ApproachObjective(Mission.Current, _playerShip, _approachPointEntity.GetGlobalFrame(), 30f);
				_missionObjectiveLogic.StartObjective(_approachObjective);
			}
			_instructionState = Quest5InstructionState.WaitForJump;
			break;
		case Quest5InstructionState.Jump:
			DisplayCurrentInstructionNotification();
			if (_missionObjectiveLogic != null)
			{
				DeactivateObjectiveIfItIsActive(_approachObjective);
				_jumpObjective = new Quest5JumpObjective(Mission.Current, _gunnarAgent);
				_missionObjectiveLogic.StartObjective(_jumpObjective);
			}
			_instructionState = Quest5InstructionState.WaitForSwim;
			break;
		case Quest5InstructionState.Swim:
			DisplayCurrentInstructionNotification();
			if (_missionObjectiveLogic != null)
			{
				DeactivateObjectiveIfItIsActive(_jumpObjective);
				_swimObjective = new Quest5SwimObjective(Mission.Current, _gunnarAgent, _phase1EnemyShip4);
				_missionObjectiveLogic.StartObjective(_swimObjective);
			}
			_instructionState = Quest5InstructionState.WaitForClearGuards;
			break;
		case Quest5InstructionState.ClearGuards:
			_instructionState = Quest5InstructionState.WaitForCheckInterior;
			if (_missionObjectiveLogic != null)
			{
				DeactivateObjectiveIfItIsActive(_talkWithYourSisterObjective);
				_clearGuardsObjective = new Quest5ClearGuardsObjective(Mission.Current, _stealthAgents);
				_missionObjectiveLogic.StartObjective(_clearGuardsObjective);
			}
			break;
		case Quest5InstructionState.CheckInterior:
			DisplayCurrentInstructionNotification();
			if (_missionObjectiveLogic != null)
			{
				DeactivateObjectiveIfItIsActive(_swimObjective);
				GameEntity interiorSpawnPointEntity = Mission.Current.Scene.FindEntityWithTag("phase_1_interior_player_sp");
				_checkInteriorObjective = new Quest5CheckInteriorObjective(Mission.Current, _phase1EnemyShipToInteriorShipDoorEntity, interiorSpawnPointEntity);
				_missionObjectiveLogic.StartObjective(_checkInteriorObjective);
			}
			_instructionState = Quest5InstructionState.WaitForTalkSister;
			break;
		case Quest5InstructionState.TalkSister:
			if (_missionObjectiveLogic != null)
			{
				DeactivateObjectiveIfItIsActive(_checkInteriorObjective);
				_talkWithYourSisterObjective = new Quest5TalkWithYourSisterObjective(Mission.Current, SisterAgent);
				_missionObjectiveLogic.StartObjective(_talkWithYourSisterObjective);
			}
			_instructionState = Quest5InstructionState.WaitForReturnToDeck;
			break;
		case Quest5InstructionState.ReturnToDeck:
			if (_missionObjectiveLogic != null)
			{
				DeactivateObjectiveIfItIsActive(_talkWithYourSisterObjective);
				_returnToDeckObjective = new Quest5ReturnToDeckObjective(Mission.Current, _phase1InteriorToEnemyShip3ShipDoorEntity, _phase1EnemyShipToInteriorShipDoorEntity);
				_missionObjectiveLogic.StartObjective(_returnToDeckObjective);
			}
			_instructionState = Quest5InstructionState.WaitForCutLoose;
			break;
		case Quest5InstructionState.CutLoose:
			DisplayCurrentInstructionNotification();
			if (_missionObjectiveLogic != null)
			{
				DeactivateObjectiveIfItIsActive(_returnToDeckObjective);
				_cutLooseObjective = new Quest5CutLooseObjective(base.Mission, _phase1EnemyShip3.AttachmentMachines, _phase1EnemyShip3.AttachmentPointMachines);
				_missionObjectiveLogic.StartObjective(_cutLooseObjective);
			}
			_instructionState = Quest5InstructionState.WaitForGunnarUsesShip;
			break;
		case Quest5InstructionState.GunnarUsesShip:
			if (State == Quest5SetPieceBattleMissionState.Phase2InProgress)
			{
				DisplayCurrentInstructionNotification();
				if (_missionObjectiveLogic != null)
				{
					DeactivateObjectiveIfItIsActive(_cutLooseObjective);
					_gunnarUsesShipObjective = new Quest5GunnarUsesShipObjective(Mission.Current);
					_missionObjectiveLogic.StartObjective(_gunnarUsesShipObjective);
				}
				_instructionState = Quest5InstructionState.WaitForEscapeQuietly;
			}
			break;
		case Quest5InstructionState.EscapeQuietly:
			DisplayCurrentInstructionNotification();
			if (_missionObjectiveLogic != null)
			{
				DeactivateObjectiveIfItIsActive(_gunnarUsesShipObjective);
				_escapeObjective = new Quest5EscapeObjective(Mission.Current, GetCurrentGunnarInstructionText(Quest5InstructionState.EscapeQuietly));
				_missionObjectiveLogic.StartObjective(_escapeObjective);
			}
			_instructionState = Quest5InstructionState.WaitForReachAllies;
			break;
		case Quest5InstructionState.ReachAllies:
			DisplayCurrentInstructionNotification();
			if (_missionObjectiveLogic != null)
			{
				DeactivateObjectiveIfItIsActive(_escapeObjective);
				_reachAlliesObjective = new Quest5ReachAlliesObjective(Mission.Current, _phase3TriggerVolumeBox);
				_missionObjectiveLogic.StartObjective(_reachAlliesObjective);
			}
			_instructionState = Quest5InstructionState.WaitForDefeatEnemies;
			break;
		case Quest5InstructionState.DefeatEnemies:
			if (_missionObjectiveLogic != null)
			{
				DeactivateObjectiveIfItIsActive(_reachAlliesObjective);
				_defeatEnemiesObjective = new Quest5DefeatEnemiesObjective(Mission.Current, _phase3TotalEnemyCount);
				_missionObjectiveLogic.StartObjective(_defeatEnemiesObjective);
			}
			_instructionState = Quest5InstructionState.WaitForDefeatPurigsShip;
			break;
		case Quest5InstructionState.DefeatPurigsShip:
			if (_missionObjectiveLogic != null)
			{
				DeactivateObjectiveIfItIsActive(_defeatEnemiesObjective);
				_defeatPurigsShipObjective = new Quest5DefeatPurigsShipObjective(Mission.Current, _purigShipAgents, Phase4PurigShip);
				_missionObjectiveLogic.StartObjective(_defeatPurigsShipObjective);
			}
			_instructionState = Quest5InstructionState.WaitForDefeatPurig;
			break;
		case Quest5InstructionState.DefeatPurig:
			if (_missionObjectiveLogic != null)
			{
				DeactivateObjectiveIfItIsActive(_defeatPurigsShipObjective);
				_defeatPurigObjective = new Quest5DefeatPurigObjective(Mission.Current, _purigAgent);
				_missionObjectiveLogic.StartObjective(_defeatPurigObjective);
			}
			_instructionState = Quest5InstructionState.WaitForEnd;
			break;
		case Quest5InstructionState.End:
			DeactivateObjectiveIfItIsActive(_defeatPurigObjective);
			break;
		case Quest5InstructionState.WaitForJump:
		case Quest5InstructionState.WaitForSwim:
		case Quest5InstructionState.WaitForClearGuards:
		case Quest5InstructionState.WaitForCheckInterior:
		case Quest5InstructionState.WaitForTalkSister:
		case Quest5InstructionState.WaitForReturnToDeck:
		case Quest5InstructionState.WaitForCutLoose:
		case Quest5InstructionState.WaitForGunnarUsesShip:
		case Quest5InstructionState.WaitForEscapeQuietly:
		case Quest5InstructionState.WaitForReachAllies:
		case Quest5InstructionState.WaitForDefeatEnemies:
		case Quest5InstructionState.WaitForDefeatPurigsShip:
		case Quest5InstructionState.WaitForDefeatPurig:
		case Quest5InstructionState.WaitForEnd:
			break;
		}
	}

	private TextObject GetCurrentGunnarInstructionText(Quest5InstructionState instructionState)
	{
		switch (instructionState)
		{
		case Quest5InstructionState.Approach:
		case Quest5InstructionState.WaitForJump:
			return new TextObject("{=Gap3mlD3}Do you see that big cluster of ships back there? That's got to be where they're holding the prisoners.");
		case Quest5InstructionState.Jump:
		case Quest5InstructionState.WaitForSwim:
			return new TextObject("{=DQNbUvkL}Into the water! Let's go, while Purig's men are distracted. Swim fast, but keep your distance from any lookouts.");
		case Quest5InstructionState.ClearGuards:
		case Quest5InstructionState.WaitForCheckInterior:
			return new TextObject("{=uQjanqh7}Be careful of the guards! Try to take them out without raising an alarm.");
		case Quest5InstructionState.CheckInterior:
		case Quest5InstructionState.WaitForCutLoose:
			return new TextObject("{=vOXiHDxu}Very good! Now, get to the hold.");
		case Quest5InstructionState.CutLoose:
		case Quest5InstructionState.WaitForGunnarUsesShip:
			return new TextObject("{=Ju7ku4LZ}Well done! But your sister is still within, and we need to get her to safety. Cut the lines tying us to the other ship, and let's be away.");
		case Quest5InstructionState.GunnarUsesShip:
		case Quest5InstructionState.WaitForEscapeQuietly:
			return new TextObject("{=P1nDlx4L}Good work! Now, let's get back to our people. The wind and current are in our favor. Even though it's just the two of us, I think we can rejoin Bjolgur and Lahar before they catch us. I'll look to the sails [and take the helm], and you can cut us loose.");
		case Quest5InstructionState.EscapeQuietly:
		case Quest5InstructionState.WaitForReachAllies:
			return new TextObject("{=wnhaoGoW}Gods' blood! We can't get past them! They're going to board. Shoot those bastards, cut them down as they come over the side, whatever it takes!");
		case Quest5InstructionState.ReachAllies:
		case Quest5InstructionState.WaitForDefeatEnemies:
			return new TextObject("{=igHojAHJ}Hah! We went through their net like a slippery old eel. Bjolgur and Lahar are right over there. Let's turn the tables on those bastards!");
		default:
			return TextObject.GetEmpty();
		}
	}

	private void DisplayCurrentInstructionNotification()
	{
		TextObject currentGunnarInstructionText = GetCurrentGunnarInstructionText(_instructionState);
		if (!currentGunnarInstructionText.IsEmpty())
		{
			MBInformationManager.DialogNotificationHandle item = CampaignInformationManager.AddDialogLine(currentGunnarInstructionText, NavalStorylineData.Gunnar.CharacterObject, null, 1000, MBInformationManager.NotificationPriority.High);
			_dialogNotificationHandleCache.Add(item);
		}
	}

	private void HandleGunnarMovement()
	{
		switch (_gunnarMovementState)
		{
		case GunnarMovementState.GoToInitialJumpingPosition:
		{
			Agent gunnarAgent2 = _gunnarAgent;
			if (gunnarAgent2 != null && gunnarAgent2.IsUsingGameObject)
			{
				_gunnarAgent.StopUsingGameObjectMT();
			}
			EnableRamp();
			_gunnarAgent.ClearTargetFrame();
			new WorldPosition(base.Mission.Scene, JumpOffInitialPosition.GlobalPosition);
			Vec3 targetDirection3 = JumpOffInitialPosition.GlobalPosition - _gunnarAgent.Position;
			_gunnarAgent.SetTargetPositionAndDirection(JumpOffInitialPosition.GlobalPosition.AsVec2, in targetDirection3);
			_gunnarAgent.LookDirection = targetDirection3.NormalizedCopy();
			_gunnarMovementState = GunnarMovementState.WaitForReachingInitialJumpingPosition;
			break;
		}
		case GunnarMovementState.WaitForReachingInitialJumpingPosition:
		{
			if (_gunnarAgent.Position.NearlyEquals(JumpOffInitialPosition.GlobalPosition, 1f))
			{
				_gunnarMovementState = GunnarMovementState.GoToJumpingTargetPosition;
				break;
			}
			Vec3 targetDirection4 = JumpOffInitialPosition.GlobalPosition - _gunnarAgent.Position;
			_gunnarAgent.SetTargetPositionAndDirection(JumpOffInitialPosition.GlobalPosition.AsVec2, in targetDirection4);
			break;
		}
		case GunnarMovementState.GoToJumpingTargetPosition:
		{
			_gunnarAgent.ClearTargetFrame();
			new WorldPosition(base.Mission.Scene, JumpOffTargetPosition.GlobalPosition);
			Vec3 targetDirection2 = JumpOffTargetPosition.GlobalPosition - _gunnarAgent.Position;
			_gunnarAgent.SetTargetPositionAndDirection(JumpOffTargetPosition.GlobalPosition.AsVec2, in targetDirection2);
			_gunnarAgent.LookDirection = targetDirection2.NormalizedCopy();
			_gunnarMovementState = GunnarMovementState.WaitForReachingJumpingTargetPosition;
			break;
		}
		case GunnarMovementState.WaitForReachingJumpingTargetPosition:
			if (_gunnarAgent.Position.NearlyEquals(JumpOffTargetPosition.GlobalPosition, 3f))
			{
				Agent main = Agent.Main;
				if (main != null && main.IsActive() && Agent.Main.IsInWater())
				{
					_gunnarMovementState = GunnarMovementState.SwimToTheHidingSpot;
				}
				else
				{
					_gunnarAgent.SetTargetPosition(_gunnarAgent.Position.AsVec2);
				}
			}
			_gunnarAgentNavalComponent.SetCanDrown(canDrown: false);
			break;
		case GunnarMovementState.SwimToTheHidingSpot:
		{
			Agent gunnarAgent = _gunnarAgent;
			if (gunnarAgent != null && gunnarAgent.IsUsingGameObject)
			{
				_gunnarAgent.StopUsingGameObjectMT();
			}
			_gunnarAgent.ClearTargetFrame();
			Vec3 targetDirection = HidingSpot1Position.GlobalPosition - _gunnarAgent.Position;
			_gunnarAgent.SetTargetPositionAndDirection(HidingSpot1Position.GlobalPosition.AsVec2, in targetDirection);
			_gunnarAgent.LookDirection = targetDirection.NormalizedCopy();
			_targetClimbingMachine = _phase1EnemyShip4.ClimbingMachines.First();
			_gunnarMovementStateForClimbingShip = GunnarMovementStateForClimbingShip.Start;
			_gunnarMovementState = GunnarMovementState.WaitForTeleportingToTheHidingSpot;
			break;
		}
		case GunnarMovementState.WaitForTeleportingToTheHidingSpot:
			MakeGunnarClimbToDeck();
			if (_gunnarMovementStateForClimbingShip == GunnarMovementStateForClimbingShip.End)
			{
				_gunnarAgent.SetCrouchMode(set: true);
				_gunnarAgent.Controller = AgentControllerType.None;
				_gunnarAgent.SetActionChannel(0, in ActionIndexCache.act_crouch_walk_idle_unarmed, ignorePriority: false, (AnimFlags)0uL);
				_gunnarMovementState = GunnarMovementState.WaitAtTheHidingSpot;
			}
			break;
		case GunnarMovementState.TeleportToTargetPosition:
		{
			Vec3 globalPosition = HidingSpot1Position.GlobalPosition;
			_gunnarAgent.TeleportToPosition(globalPosition);
			_gunnarAgent.SetTargetPositionAndDirection(globalPosition.AsVec2, globalPosition - _gunnarAgent.Position);
			_gunnarAgent.SetCrouchMode(set: true);
			_gunnarAgent.Controller = AgentControllerType.None;
			_gunnarAgent.SetActionChannel(0, in ActionIndexCache.act_crouch_walk_idle_unarmed, ignorePriority: false, (AnimFlags)0uL);
			_gunnarMovementState = GunnarMovementState.WaitAtTheHidingSpot;
			break;
		}
		case GunnarMovementState.WaitAtTheHidingSpot:
			_gunnarAgent.SetCrouchMode(set: true);
			_gunnarAgent.SetTargetPosition(HidingSpot1Position.GlobalPosition.AsVec2);
			_gunnarAgent.SetActionChannel(0, in ActionIndexCache.act_crouch_walk_idle_unarmed, ignorePriority: false, (AnimFlags)0uL);
			if (_stealthAgents.IsEmpty())
			{
				_gunnarAgent.ClearTargetFrame();
				_gunnarAgent.SetCrouchMode(set: false);
				_gunnarAgent.Controller = AgentControllerType.AI;
				_gunnarAgent.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL);
				_gunnarMovementState = GunnarMovementState.GoToTheEscapeShip;
			}
			CheckIfAnEnemyIsAttackingGunnar();
			break;
		case GunnarMovementState.GoToTheEscapeShip:
		{
			_gunnarAgent.ClearTargetFrame();
			WorldPosition scriptedPosition = new WorldPosition(base.Mission.Scene, GunnarShipUsePosition.origin);
			Vec3 vec = GunnarShipUsePosition.origin - _gunnarAgent.Position;
			_gunnarAgent.SetScriptedPositionAndDirection(ref scriptedPosition, vec.RotationX.ToRadians(), addHumanLikeDelay: false);
			_gunnarAgent.LookDirection = vec.NormalizedCopy();
			_gunnarMovementState = GunnarMovementState.WaitForReachingToTheEscapeShip;
			break;
		}
		case GunnarMovementState.WaitForReachingToTheEscapeShip:
			if ((_phase1EnemyShip2 == null || !EscapeShip.GetIsThereActiveBridgeTo(_phase1EnemyShip2)) && EscapeShip.Captain == _gunnarAgent && State == Quest5SetPieceBattleMissionState.Phase2InProgress && _gunnarAgent.CurrentlyUsedGameObject == EscapeShip.ShipControllerMachine.PilotStandingPoint)
			{
				_gunnarMovementState = GunnarMovementState.UseTheEscapeShip;
			}
			break;
		case GunnarMovementState.UseTheEscapeShip:
			HandleEscapeShipMovement();
			EscapeShip.Formation.SetControlledByAI(isControlledByAI: false);
			break;
		case GunnarMovementState.None:
		case GunnarMovementState.End:
			break;
		}
	}

	private void EnableRamp()
	{
		Mission.Current.Scene.FindEntityWithTag("ramp_holder").SetVisibilityExcludeParents(visible: true);
	}

	private void HandleIfGunnarFallsIntoTheWater()
	{
		if (_gunnarAgent == null || !_gunnarAgent.IsActive())
		{
			return;
		}
		switch (State)
		{
		case Quest5SetPieceBattleMissionState.Phase1StealthPhase:
			if (_gunnarFellIntoTheWaterTimer == null)
			{
				if (_gunnarAgent.IsInWater())
				{
					_gunnarFellIntoTheWaterTimer = new MissionTimer(10f);
				}
			}
			else if (_gunnarFellIntoTheWaterTimer.Check() && !_stealthAgents.IsEmpty())
			{
				Vec3 globalPosition = HidingSpot1Position.GlobalPosition;
				if ((_gunnarAgent.Position - globalPosition).LengthSquared > 1f)
				{
					_gunnarAgent.TeleportToPosition(globalPosition);
				}
				_gunnarAgent.SetTargetPositionAndDirection(globalPosition.AsVec2, (GunnarShipUsePosition.origin - _gunnarAgent.Position).NormalizedCopy());
				_gunnarAgent.SetCrouchMode(set: true);
			}
			break;
		case Quest5SetPieceBattleMissionState.Phase1EscapePhase:
		case Quest5SetPieceBattleMissionState.Phase2InProgress:
			if (_gunnarFellIntoTheWaterTimer == null)
			{
				if (_gunnarAgent.IsInWater())
				{
					_gunnarFellIntoTheWaterTimer = new MissionTimer(10f);
				}
			}
			else if (_gunnarFellIntoTheWaterTimer.Check() && (_gunnarAgent.Position - GunnarShipUsePosition.origin).LengthSquared > 1f)
			{
				_gunnarAgent.TeleportToPosition(GunnarShipUsePosition.origin);
			}
			break;
		}
	}

	private void MakeGunnarClimbToDeck()
	{
		switch (_gunnarMovementStateForClimbingShip)
		{
		case GunnarMovementStateForClimbingShip.Start:
		{
			WorldPosition position = new WorldPosition(base.Mission.Scene, _targetClimbingMachine.PilotStandingPoint.GameEntity.GlobalPosition);
			_gunnarAgent.SetScriptedPosition(ref position, addHumanLikeDelay: true);
			_gunnarMovementStateForClimbingShip = GunnarMovementStateForClimbingShip.GoingToTheTargetClimbingMachine;
			break;
		}
		case GunnarMovementStateForClimbingShip.GoingToTheTargetClimbingMachine:
			if (_gunnarAgent.Position.Distance(_targetClimbingMachine.GameEntity.GlobalPosition) < 2.5f)
			{
				_gunnarMovementStateForClimbingShip = GunnarMovementStateForClimbingShip.TargetReached;
			}
			else if (_phase1EnemyShip4.GetIsAgentOnShip(_gunnarAgent))
			{
				_gunnarAgent.SetCrouchMode(set: true);
				_gunnarMovementStateForClimbingShip = GunnarMovementStateForClimbingShip.OnDeck;
			}
			else
			{
				_gunnarAgent.SetTargetPosition(_targetClimbingMachine.PilotStandingPoint.GameEntity.GlobalPosition.AsVec2);
			}
			break;
		case GunnarMovementStateForClimbingShip.TargetReached:
			if (!_targetClimbingMachine.PilotStandingPoint.HasUser)
			{
				_gunnarAgent.UseGameObject(_targetClimbingMachine.PilotStandingPoint);
				_gunnarMovementStateForClimbingShip = GunnarMovementStateForClimbingShip.UsingClimbingMachine;
			}
			break;
		case GunnarMovementStateForClimbingShip.UsingClimbingMachine:
			if (_gunnarAgent.Position.Distance(_targetClimbingMachine.GameEntity.GlobalPosition) > 2.5f)
			{
				if (!_gunnarAgent.IsUsingGameObject)
				{
					_gunnarMovementStateForClimbingShip = GunnarMovementStateForClimbingShip.GoingToTheTargetClimbingMachine;
				}
			}
			else if (_phase1EnemyShip4.GetIsAgentOnShip(_gunnarAgent))
			{
				_gunnarAgent.SetCrouchMode(set: true);
				_gunnarMovementStateForClimbingShip = GunnarMovementStateForClimbingShip.OnDeck;
			}
			break;
		case GunnarMovementStateForClimbingShip.OnDeck:
		{
			_gunnarAgent.ClearTargetFrame();
			Vec3 targetDirection = HidingSpot1Position.GlobalPosition - _gunnarAgent.Position;
			_gunnarAgent.SetTargetPositionAndDirection(HidingSpot1Position.GlobalPosition.AsVec2, in targetDirection);
			_gunnarMovementStateForClimbingShip = GunnarMovementStateForClimbingShip.GoToFinalTargetPoint;
			break;
		}
		case GunnarMovementStateForClimbingShip.GoToFinalTargetPoint:
			if (_gunnarAgent.Position.NearlyEquals(HidingSpot1Position.GlobalPosition, 1f))
			{
				_gunnarMovementStateForClimbingShip = GunnarMovementStateForClimbingShip.End;
			}
			break;
		case GunnarMovementStateForClimbingShip.None:
		case GunnarMovementStateForClimbingShip.End:
			break;
		}
	}

	private void InitializePhase1Part1()
	{
		TeamAINavalComponent teamAI = new TeamAINavalComponent(base.Mission, base.Mission.AttackerTeam, 5f, 1f);
		base.Mission.AttackerTeam.AddTeamAI(teamAI);
		base.Mission.AttackerTeam.AddTacticOption(new TacticNavalBalancedOffense(base.Mission.AttackerTeam));
		TeamAINavalComponent teamAI2 = new TeamAINavalComponent(base.Mission, base.Mission.DefenderTeam, 5f, 1f);
		base.Mission.DefenderTeam.AddTeamAI(teamAI2);
		base.Mission.DefenderTeam.AddTacticOption(new TacticNavalBalancedOffense(base.Mission.DefenderTeam));
		_playerShip = CreateShip("crusas_roundship_nested_q5", "phase_1_player_ship_sp", _playerFormation);
		_phase1EnemyShip1 = CreateShip("sturgia_heavy_ship", "phase_1_enemy_ship_1_sp_initial", GetAvailableEnemyFormation(), spawnAnchored: false, null, null, checkForFreeArea: false);
		_phase1EnemyShip2 = CreateShip("ship_lodya_storyline", "phase_1_enemy_ship_2_sp", GetAvailableEnemyFormation(), spawnAnchored: true, _phase1EnemyShip2UpgradePieceList, null, checkForFreeArea: false);
		_phase1EnemyShip3 = CreateShip("ship_dromon_storyline", "phase_1_enemy_ship_3_sp", GetAvailableEnemyFormation(), spawnAnchored: true, _escapeShipUpgradePieceList, null, checkForFreeArea: false);
		_phase1EnemyShip4 = CreateShip("ship_birlinn_storyline", "phase_1_enemy_ship_4_sp", GetAvailableEnemyFormation(), spawnAnchored: true, null, null, checkForFreeArea: false);
		_phase1EnemyShip1.SetCanBeTakenOver(value: false);
		_phase1EnemyShip2.SetCanBeTakenOver(value: false);
		_phase1EnemyShip3.SetCanBeTakenOver(value: false);
		_phase1EnemyShip4.SetCanBeTakenOver(value: false);
		_phase1EnemyShipToInteriorShipDoorEntity = Mission.Current.Scene.FindEntityWithTag("phase_1_enemy_ship_3_to_interior_door_tag");
		_phase1EnemyShipToInteriorShipDoorEntity.GetFirstScriptOfType<ShipDoorUsePoint>().SetShipDoorUsePointEnabled(isEnabled: false);
		HandleStealthShipsBridgeConnections();
		base.Mission.SetMissionMode(MissionMode.Stealth, atStart: true);
		foreach (ShipAttachmentMachine attachmentMachine in _phase1EnemyShip3.AttachmentMachines)
		{
			if (attachmentMachine.GameEntity.Parent.HasTag("bridge_a") || attachmentMachine.GameEntity.Parent.HasTag("bridge_b"))
			{
				continue;
			}
			foreach (StandingPoint standingPoint in attachmentMachine.StandingPoints)
			{
				standingPoint.IsDisabledForPlayers = true;
			}
		}
	}

	private void CheckIfAnEnemyIsAttackingGunnar()
	{
		if (_isMissionFailPopUpTriggered)
		{
			return;
		}
		bool flag = false;
		foreach (Agent stealthAgent in _stealthAgents)
		{
			if (stealthAgent.IsAlarmed() && stealthAgent.Position.Distance(_gunnarAgent.Position) < 2f)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			TriggerMissionFailPopup();
		}
	}

	private void InitializePhase1Part2()
	{
		_phase1PlayerShipSpawnPosition = _playerShip.GlobalFrame.origin;
		_phase1EnemyShip1.SetAnchor(isAnchored: true);
		_phase1EnemyShip1.ShipOrder.SetShipStopOrder();
		_phase1EnemyShip1.SetController(ShipControllerType.AI);
		_phase1EnemyShip1.SetShipOrderActive(isOrderActive: false);
		_phase1EnemyShip2.SetAnchor(isAnchored: true);
		_phase1EnemyShip2.ShipOrder.SetShipStopOrder();
		_phase1EnemyShip2.SetController(ShipControllerType.AI);
		_phase1EnemyShip2.SetShipOrderActive(isOrderActive: false);
		_phase1EnemyShip3.SetAnchor(isAnchored: true);
		_phase1EnemyShip3.ShipOrder.SetShipStopOrder();
		_phase1EnemyShip3.SetController(ShipControllerType.AI);
		_phase1EnemyShip3.SetShipOrderActive(isOrderActive: false);
		_phase1EnemyShip4.SetAnchor(isAnchored: true);
		_phase1EnemyShip4.ShipOrder.SetShipStopOrder();
		_phase1EnemyShip4.SetController(ShipControllerType.AI);
		_phase1EnemyShip4.SetShipOrderActive(isOrderActive: false);
		foreach (ShipAttachmentMachine attachmentMachine in _playerShip.AttachmentMachines)
		{
			foreach (StandingPoint standingPoint in attachmentMachine.StandingPoints)
			{
				standingPoint.IsDisabledForPlayers = true;
			}
		}
		foreach (ShipAttachmentMachine attachmentMachine2 in _phase1EnemyShip1.AttachmentMachines)
		{
			foreach (StandingPoint standingPoint2 in attachmentMachine2.StandingPoints)
			{
				standingPoint2.IsDisabledForPlayers = true;
			}
		}
		foreach (ShipAttachmentMachine attachmentMachine3 in _phase1EnemyShip2.AttachmentMachines)
		{
			if (attachmentMachine3.GameEntity.Parent.HasTag("bridge_a") || attachmentMachine3.GameEntity.Parent.HasTag("bridge_b") || attachmentMachine3.GameEntity.Parent.HasTag("bridge_c"))
			{
				continue;
			}
			foreach (StandingPoint standingPoint3 in attachmentMachine3.StandingPoints)
			{
				standingPoint3.IsDisabledForPlayers = true;
			}
		}
		foreach (ClimbingMachine climbingMachine in _phase1EnemyShip1.ClimbingMachines)
		{
			foreach (StandingPoint standingPoint4 in climbingMachine.StandingPoints)
			{
				standingPoint4.IsDisabledForPlayers = true;
			}
		}
		_navalAgentsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic.SetDeploymentMode(value: true);
		SpawnPhase1AllyTroops();
		SpawnPhase1EnemyTroops();
		base.Mission.PlayerTeam.SetPlayerRole(isPlayerGeneral: true, isPlayerSergeant: true);
		Agent.Main.SetClothingColor1(4279111698u);
		Agent.Main.SetClothingColor2(4279111698u);
		Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(GetScriptedStealthEquipment());
		_gunnarAgent.UpdateSpawnEquipmentAndRefreshVisuals(GetScriptedStealthEquipment());
		_navalAgentsLogic.AssignCaptainToShipForDeploymentMode(Agent.Main, _playerShip, _playerShip);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase1EnemyShip1);
		_navalAgentsLogic.SetDeploymentMode(value: false);
		_navalShipsLogic.SetDeploymentMode(value: false);
		base.Mission.OnInitialSpawnCompleted();
		Mission.Current.Scene.FindEntityWithTag("phase_2_barricade").SetVisibilityExcludeParents(visible: false);
		RemoveShipControlPointDescriptionOfAllEnemyShips();
	}

	private void SpawnPhase1AllyTroops()
	{
		_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, CharacterObject.PlayerCharacter), _playerShip);
		_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, Hero.MainHero.Culture.BasicTroop), _playerShip);
		_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, Hero.MainHero.Culture.BasicTroop), _playerShip);
		_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, Hero.MainHero.Culture.BasicTroop), _playerShip);
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.PlayerTeam);
		SpawnGunnarOnShip(_playerShip);
		SpawnCrusasOnShip(_playerShip);
		_crusasAgent.UpdateSpawnEquipmentAndRefreshVisuals(MBObjectManager.Instance.GetObject<MBEquipmentRoster>("npc_merchant_equipment_empire").DefaultEquipment);
		_gunnarAgent.SetMortalityState(Agent.MortalityState.Immortal);
		_playerShip.Formation.PlayerOwner = Agent.Main;
	}

	private void SpawnPhase1EnemyTroops()
	{
		base.Mission.Scene.GetAllEntitiesWithScriptComponent<DynamicPatrolAreaParent>(ref _dynamicPatrolAreas);
		CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>("sea_hound_captivity");
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase1EnemyShip1, 7);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase1EnemyShip2, 6);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase1EnemyShip3, 100);
		foreach (GameEntity dynamicPatrolArea in _dynamicPatrolAreas)
		{
			if (dynamicPatrolArea.GetFirstScriptOfType<DynamicPatrolAreaParent>().IsDisabled)
			{
				continue;
			}
			IEnumerable<GameEntity> children = dynamicPatrolArea.GetChildren();
			bool flag = false;
			MissionShip shipOfDynamicPartolArea = GetShipOfDynamicPartolArea(dynamicPatrolArea);
			foreach (GameEntity item in children)
			{
				PatrolPoint firstScriptOfType = item.GetChild(0).GetFirstScriptOfType<PatrolPoint>();
				shipOfDynamicPartolArea.Formation.JoinDetachment(item.GetFirstScriptOfType<UsablePlace>());
				if (firstScriptOfType == null || flag || firstScriptOfType.IsDisabled || string.IsNullOrEmpty(firstScriptOfType.SpawnGroupTag))
				{
					continue;
				}
				Equipment equipment = characterObject.BattleEquipments.GetRandomElementInefficiently().Clone();
				for (int i = 0; i < 12; i++)
				{
					if ((i == 0 || i == 1 || i == 2 || i == 3 || i == 4) && !equipment[i].IsEmpty && equipment[i].Item.WeaponComponent != null && equipment[i].Item.WeaponComponent.PrimaryWeapon.IsShield)
					{
						equipment[i] = EquipmentElement.Invalid;
					}
				}
				AgentBuildData agentBuildData = new AgentBuildData(characterObject).TroopOrigin(new SimpleAgentOrigin(characterObject)).Team(base.Mission.PlayerEnemyTeam);
				MatrixFrame globalFrame = item.GetGlobalFrame();
				AgentBuildData agentBuildData2 = agentBuildData.InitialPosition(in globalFrame.origin).InitialDirection(item.GetGlobalFrame().rotation.f.AsVec2).NoHorses(noHorses: true)
					.NoWeapons(noWeapons: false)
					.Equipment(equipment);
				Agent agent = base.Mission.SpawnAgent(agentBuildData2);
				MBActionSet actionSet = MBGlobals.GetActionSet("as_human_hideout_bandit");
				AnimationSystemData animationSystemData = agentBuildData2.AgentMonster.FillAnimationSystemData(actionSet, characterObject.GetStepSize(), hasClippingPlane: false);
				agent.SetActionSet(ref animationSystemData);
				AgentFlag agentFlags = agent.GetAgentFlags();
				agent.SetAgentFlags(agentFlags | AgentFlag.CanGetAlarmed);
				AgentNavigator agentNavigator = agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
				agentNavigator.AddBehaviorGroup<AlarmedBehaviorGroup>().AddBehavior<CautiousBehavior>();
				agentNavigator.AddBehaviorGroup<DailyBehaviorGroup>().AddBehavior<PatrolAgentBehavior>().SetDynamicPatrolArea(dynamicPatrolArea);
				_stealthAgents.Add(agent);
				flag = true;
			}
		}
		MatrixFrame globalFrame2 = _phase1EnemyShip1.ShipControllerMachine.PilotStandingPoint.GameEntity.GetGlobalFrame();
		AgentBuildData agentBuildData3 = new AgentBuildData(_slaveTraderCharacter).TroopOrigin(new SimpleAgentOrigin(_slaveTraderCharacter)).Team(base.Mission.PlayerEnemyTeam).InitialPosition(in globalFrame2.origin)
			.InitialDirection(globalFrame2.rotation.f.AsVec2)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false);
		_slaveTraderAgent = base.Mission.SpawnAgent(agentBuildData3);
		_navalAgentsLogic.AddAgentToShip(_slaveTraderAgent, _phase1EnemyShip1);
		MBActionSet actionSet2 = MBGlobals.GetActionSet("as_human_hideout_bandit");
		AnimationSystemData animationSystemData2 = agentBuildData3.AgentMonster.FillAnimationSystemData(actionSet2, characterObject.GetStepSize(), hasClippingPlane: false);
		_slaveTraderAgent.SetActionSet(ref animationSystemData2);
		_slaveTraderAgent.SetAgentFlags((AgentFlag)((uint)_slaveTraderAgent.GetAgentFlags() & 0xFFFEFFE7u));
		Queue<MatrixFrame> queue = new Queue<MatrixFrame>();
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _phase1EnemyShip1.AttachmentPointMachines)
		{
			queue.Enqueue(attachmentPointMachine.StandingPoints.First().GameEntity.GetGlobalFrame());
		}
		for (int j = 0; j < _slaveTraderShipOarsmen.Length; j++)
		{
			MatrixFrame matrixFrame = queue.Dequeue();
			AgentBuildData agentBuildData4 = new AgentBuildData(_slaveTraderCharacter).TroopOrigin(new SimpleAgentOrigin(_slaveTraderCharacter)).Team(base.Mission.PlayerEnemyTeam).InitialPosition(in matrixFrame.origin)
				.InitialDirection(matrixFrame.rotation.f.AsVec2)
				.NoHorses(noHorses: true)
				.NoWeapons(noWeapons: false);
			Agent agent2 = base.Mission.SpawnAgent(agentBuildData4);
			_slaveTraderShipOarsmen[j] = agent2;
			_navalAgentsLogic.AddAgentToShip(agent2, _phase1EnemyShip1);
			agent2.SetActionSet(ref animationSystemData2);
			agent2.SetAgentFlags((AgentFlag)((uint)agent2.GetAgentFlags() & 0xFFFEFFE7u));
		}
	}

	private void DisableSlaveTraderShipAgents()
	{
		_slaveTraderAgent.SetTeam(Team.Invalid, sync: true);
		for (int i = 0; i < _slaveTraderShipOarsmen.Length; i++)
		{
			_slaveTraderShipOarsmen[i].SetTeam(Team.Invalid, sync: true);
		}
	}

	private MissionShip GetShipOfDynamicPartolArea(GameEntity dynamicPatrolArea)
	{
		if (dynamicPatrolArea.Parent.Parent.Name.Equals(_phase1EnemyShip2.MissionShipObject.Prefab))
		{
			return _phase1EnemyShip2;
		}
		if (dynamicPatrolArea.Parent.Parent.Name.Equals(_phase1EnemyShip3.MissionShipObject.Prefab))
		{
			return _phase1EnemyShip3;
		}
		if (dynamicPatrolArea.Parent.Parent.Name.Equals(_phase1EnemyShip4.MissionShipObject.Prefab))
		{
			return _phase1EnemyShip4;
		}
		return null;
	}

	private void HandleStealthShipsBridgeConnections()
	{
		if (_phase1EnemyShip2 != null && _phase1EnemyShip3 != null && _phase1EnemyShip4 != null && !_talkedWithSister)
		{
			_phase1EnemyShip3.TryToMaintainConnectionToAnotherShip(_phase1EnemyShip2, forceBridge: true, unbreakableBridge: true);
			_phase1EnemyShip4.TryToMaintainConnectionToAnotherShip(_phase1EnemyShip2, forceBridge: true, unbreakableBridge: true);
		}
	}

	private void HandleEscapeShipInteriorDoorUsage()
	{
		_phase1EnemyShipToInteriorShipDoorEntity.GetFirstScriptOfType<ShipDoorUsePoint>().SetShipDoorUsePointEnabled(_stealthAgents.IsEmpty());
	}

	private void OnPlayerShipReachedApproachDistance()
	{
		State = Quest5SetPieceBattleMissionState.Phase1SwimmingPhase;
		_gunnarMovementState = GunnarMovementState.GoToInitialJumpingPosition;
		if (Agent.Main.IsUsingGameObject)
		{
			Agent.Main.StopUsingGameObject();
		}
		Agent crusasAgent = _crusasAgent;
		if (crusasAgent != null && crusasAgent.IsUsingGameObject)
		{
			_crusasAgent.StopUsingGameObject();
		}
		Agent slaveTraderAgent = _slaveTraderAgent;
		if (slaveTraderAgent != null && slaveTraderAgent.IsUsingGameObject)
		{
			_slaveTraderAgent.StopUsingGameObject();
		}
		_playerShip.SetCustomSailSetting(enableCustomSailSetting: true, SailInput.Raised);
		_playerShip.ShipOrder.SetShipStopOrder();
		_playerShip.SetAnchor(isAnchored: true);
		CalculateBuySlaveConversationPoint();
		_crusasAgent.ClearTargetFrame();
		_slaveTraderAgent.ClearTargetFrame();
		WorldPosition scriptedPosition = new WorldPosition(base.Mission.Scene, _crusasConversationPointFrame.GetGlobalFrame().origin);
		float scriptedDirection = (_crusasConversationPointFrame.GetGlobalFrame().origin - _crusasAgent.Position).RotationX.ToRadians();
		_crusasAgent.SetScriptedPositionAndDirection(ref scriptedPosition, scriptedDirection, addHumanLikeDelay: true);
		WorldPosition scriptedPosition2 = new WorldPosition(base.Mission.Scene, _slaveTraderConversationPointFrame.GetGlobalFrame().origin);
		float scriptedDirection2 = (_slaveTraderConversationPointFrame.GetGlobalFrame().origin - _slaveTraderAgent.Position).RotationX.ToRadians();
		_slaveTraderAgent.SetScriptedPositionAndDirection(ref scriptedPosition2, scriptedDirection2, addHumanLikeDelay: false);
		_crusasAgent.SetLookAgent(_slaveTraderAgent);
		_slaveTraderAgent.SetLookAgent(_crusasAgent);
		MakeShipOarsInvisible(_playerShip);
	}

	private void InitializeStealthPhasePart1()
	{
		if (_playerShip == null)
		{
			_isCheckpointInitialize = true;
			TeamAINavalComponent teamAI = new TeamAINavalComponent(base.Mission, base.Mission.AttackerTeam, 5f, 1f);
			base.Mission.AttackerTeam.AddTeamAI(teamAI);
			base.Mission.AttackerTeam.AddTacticOption(new TacticNavalBalancedOffense(base.Mission.AttackerTeam));
			TeamAINavalComponent teamAI2 = new TeamAINavalComponent(base.Mission, base.Mission.DefenderTeam, 5f, 1f);
			base.Mission.DefenderTeam.AddTeamAI(teamAI2);
			base.Mission.DefenderTeam.AddTacticOption(new TacticNavalBalancedOffense(base.Mission.DefenderTeam));
			_playerShip = CreateShip("crusas_roundship_nested_q5", "phase_1_player_ship_sp", _playerFormation);
			_phase1EnemyShip1 = CreateShip("sturgia_heavy_ship", "phase_1_enemy_ship_1_sp", GetAvailableEnemyFormation(), spawnAnchored: true, null, null, checkForFreeArea: false);
			_phase1EnemyShip2 = CreateShip("ship_lodya_storyline", "phase_1_enemy_ship_2_sp", GetAvailableEnemyFormation(), spawnAnchored: true, _phase1EnemyShip2UpgradePieceList, null, checkForFreeArea: false);
			_phase1EnemyShip3 = CreateShip("ship_dromon_storyline", "phase_1_enemy_ship_3_sp", GetAvailableEnemyFormation(), spawnAnchored: true, _escapeShipUpgradePieceList, null, checkForFreeArea: false);
			_phase1EnemyShip4 = CreateShip("ship_birlinn_storyline", "phase_1_enemy_ship_4_sp", GetAvailableEnemyFormation(), spawnAnchored: true, null, null, checkForFreeArea: false);
			_phase1EnemyShip1.SetCanBeTakenOver(value: false);
			_phase1EnemyShip2.SetCanBeTakenOver(value: false);
			_phase1EnemyShip3.SetCanBeTakenOver(value: false);
			_phase1EnemyShip4.SetCanBeTakenOver(value: false);
			_phase1EnemyShipToInteriorShipDoorEntity = Mission.Current.Scene.FindEntityWithTag("phase_1_enemy_ship_3_to_interior_door_tag");
			_phase1EnemyShipToInteriorShipDoorEntity.GetFirstScriptOfType<ShipDoorUsePoint>().SetShipDoorUsePointEnabled(isEnabled: false);
			HandleStealthShipsBridgeConnections();
			base.Mission.SetMissionMode(MissionMode.Stealth, atStart: true);
		}
	}

	private void InitializeStealthPhasePart2()
	{
		if (_isCheckpointInitialize)
		{
			_phase1EnemyShip1.SetAnchor(isAnchored: true);
			_phase1EnemyShip1.ShipOrder.SetShipStopOrder();
			_phase1EnemyShip1.SetController(ShipControllerType.AI);
			_phase1EnemyShip1.SetShipOrderActive(isOrderActive: false);
			_phase1EnemyShip2.SetAnchor(isAnchored: true);
			_phase1EnemyShip2.ShipOrder.SetShipStopOrder();
			_phase1EnemyShip2.SetController(ShipControllerType.AI);
			_phase1EnemyShip2.SetShipOrderActive(isOrderActive: false);
			_phase1EnemyShip3.SetAnchor(isAnchored: true);
			_phase1EnemyShip3.ShipOrder.SetShipStopOrder();
			_phase1EnemyShip3.SetController(ShipControllerType.AI);
			_phase1EnemyShip3.SetShipOrderActive(isOrderActive: false);
			_phase1EnemyShip4.SetAnchor(isAnchored: true);
			_phase1EnemyShip4.ShipOrder.SetShipStopOrder();
			_phase1EnemyShip4.SetController(ShipControllerType.AI);
			_phase1EnemyShip4.SetShipOrderActive(isOrderActive: false);
			foreach (ShipAttachmentMachine attachmentMachine in _phase1EnemyShip1.AttachmentMachines)
			{
				foreach (StandingPoint standingPoint in attachmentMachine.StandingPoints)
				{
					standingPoint.IsDisabledForPlayers = true;
				}
			}
			foreach (ShipAttachmentMachine attachmentMachine2 in _phase1EnemyShip2.AttachmentMachines)
			{
				if (attachmentMachine2.GameEntity.Parent.HasTag("bridge_a") || attachmentMachine2.GameEntity.Parent.HasTag("bridge_b") || attachmentMachine2.GameEntity.Parent.HasTag("bridge_c"))
				{
					continue;
				}
				foreach (StandingPoint standingPoint2 in attachmentMachine2.StandingPoints)
				{
					standingPoint2.IsDisabledForPlayers = true;
				}
			}
			foreach (ClimbingMachine climbingMachine in _phase1EnemyShip1.ClimbingMachines)
			{
				foreach (StandingPoint standingPoint3 in climbingMachine.StandingPoints)
				{
					standingPoint3.IsDisabledForPlayers = true;
				}
			}
			base.Mission.OnInitialSpawnCompleted();
			SpawnPhase1AllyTroops();
			SpawnPhase1EnemyTroops();
			base.Mission.PlayerTeam.SetPlayerRole(isPlayerGeneral: true, isPlayerSergeant: true);
			_playerShip.Formation.PlayerOwner = Agent.Main;
			Agent.Main.SetClothingColor1(4279111698u);
			Agent.Main.SetClothingColor2(4279111698u);
			Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(GetScriptedStealthEquipment());
			_gunnarAgent.UpdateSpawnEquipmentAndRefreshVisuals(GetScriptedStealthEquipment());
			_gunnarMovementState = GunnarMovementState.TeleportToTargetPosition;
			HandleGunnarMovement();
			GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("sp_player_stealth_checkpoint");
			Agent.Main.TeleportToPosition(gameEntity.GlobalPosition);
			Mission.Current.Scene.FindEntityWithTag("phase_2_barricade").SetVisibilityExcludeParents(visible: false);
			_isCheckpointInitialize = false;
			_instructionState = Quest5InstructionState.ClearGuards;
			Agent.Main.SetCrouchMode(set: true);
			RemoveShipControlPointDescriptionOfAllEnemyShips();
		}
		foreach (MBInformationManager.DialogNotificationHandle item in _dialogNotificationHandleCache)
		{
			CampaignInformationManager.ClearDialogNotification(item);
		}
		_dialogNotificationHandleCache.Clear();
	}

	private void MovePhase1EnemyShip1ToItsTargetPoint()
	{
		if (_phase1EnemyShip1TargetEntity.GetGlobalFrame().origin.Distance(_phase1EnemyShip1.GlobalFrame.origin) <= 2f)
		{
			_phase1EnemyShip1.ShipOrder.SetShipStopOrder();
			_phase1EnemyShip1.SetAnchor(isAnchored: true);
			Vec2 position = _phase1EnemyShip1TargetEntity.GetGlobalFrame().origin.AsVec2;
			Vec2 direction = (_phase1EnemyShip1TargetEntity.GetGlobalFrame().origin - _phase1EnemyShip1InitialSpawnEntity.GetGlobalFrame().origin).AsVec2.Normalized();
			_phase1EnemyShip1.SetAnchorFrame(in position, in direction);
			_phase1EnemyShip1.ShipOrder.SetOrderOarsmenLevel(0);
		}
		else
		{
			Vec2 asVec = _phase1EnemyShip1TargetEntity.GetGlobalFrame().origin.AsVec2;
			Vec2 targetDirection = (_phase1EnemyShip1TargetEntity.GetGlobalFrame().origin - _phase1EnemyShip1InitialSpawnEntity.GetGlobalFrame().origin).AsVec2.Normalized();
			_phase1EnemyShip1.ShipOrder.SetShipMovementOrder(asVec, in targetDirection);
		}
	}

	private void InitializeShipInteriorPhase()
	{
		Mission.Current.Scene.SetAtmosphereWithName("TOD_01_00_SemiCloudy");
		base.Mission.GetMissionBehavior<MissionConversationLogic>().DisableStartConversation(isDisabled: true);
		GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("phase_1_interior_player_sp");
		GameEntity gameEntity2 = Mission.Current.Scene.FindEntityWithTag("phase_1_interior_sister_sp");
		Agent.Main.TeleportToPosition(gameEntity.GlobalPosition);
		Vec3 position = gameEntity2.GlobalPosition;
		Vec2 direction = gameEntity2.GetGlobalFrame().rotation.f.AsVec2;
		Equipment equipment = StoryModeHeroes.LittleSister.CivilianEquipment.Clone();
		for (int i = 0; i < 5; i++)
		{
			equipment[i] = EquipmentElement.Invalid;
		}
		equipment[5] = EquipmentElement.Invalid;
		equipment[9] = EquipmentElement.Invalid;
		StoryModeHeroes.LittleSister.HitPoints = StoryModeHeroes.LittleSister.WoundedHealthLimit - 1;
		AgentBuildData agentBuildData = new AgentBuildData(StoryModeHeroes.LittleSister.CharacterObject).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, StoryModeHeroes.LittleSister.CharacterObject, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true)).Team(base.Mission.PlayerTeam).InitialPosition(in position)
			.InitialDirection(in direction)
			.Equipment(equipment)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false);
		SisterAgent = Mission.Current.SpawnAgent(agentBuildData);
		SisterAgent.SetMortalityState(Agent.MortalityState.Immortal);
		_mainAgentEquipmentCopyForInteriorMission = Agent.Main.SpawnEquipment.Clone();
		Equipment equipment2 = Agent.Main.SpawnEquipment.Clone();
		for (int j = 0; j < 12; j++)
		{
			if (j == 0 || j == 1 || j == 2 || j == 3 || j == 4)
			{
				equipment2[j] = EquipmentElement.Invalid;
			}
		}
		Agent.Main.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
		Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(equipment2);
		_instructionState = Quest5InstructionState.TalkSister;
		Mission.Current.SetMissionMode(MissionMode.StartUp, atStart: false);
		State = Quest5SetPieceBattleMissionState.Phase1GoToShipInteriorFadeIn;
	}

	private void InitializeGoBackToShip()
	{
		if (_stealthAgents.IsEmpty())
		{
			_gunnarAgent.TeleportToPosition(GunnarShipUsePosition.origin);
			_gunnarMovementState = GunnarMovementState.WaitForReachingToTheEscapeShip;
		}
		Mission.Current.Scene.SetAtmosphereWithName("TOD_naval_03_00_sunset");
		base.Mission.GetMissionBehavior<MissionConversationLogic>().DisableStartConversation(isDisabled: true);
		SisterAgent.SetMortalityState(Agent.MortalityState.Mortal);
		SisterAgent.FadeOut(hideInstantly: true, hideMount: false);
		Agent.Main.TeleportToPosition(_phase1EnemyShipToInteriorShipDoorEntity.GlobalPosition);
		Mission.Current.Scene.FindEntityWithTag("phase_2_barricade").SetVisibilityExcludeParents(visible: true);
		base.Mission.SetMissionMode(MissionMode.Stealth, atStart: false);
		Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(_mainAgentEquipmentCopyForInteriorMission);
		State = Quest5SetPieceBattleMissionState.Phase1GoBackToShipFadeIn;
	}

	public void GetIntendedMainAgentDirectionForPhase1InteriorTeleport(out Vec3 mainAgentDirection)
	{
		mainAgentDirection = SisterAgent.Position - Agent.Main.Position;
	}

	public void GetIntendedMainAgentDirectionForPhase1EscapeShipTeleport(out Vec3 mainAgentDirection)
	{
		mainAgentDirection = Agent.Main.Position - _gunnarAgent.Position;
	}

	public void TriggerPhase1InitializeShipInteriorPhase()
	{
		State = Quest5SetPieceBattleMissionState.Phase1InitializeShipInteriorPhase;
	}

	public void CompletePhase1GoToShipInteriorTransition()
	{
		State = Quest5SetPieceBattleMissionState.Phase1ShipInteriorPhase;
	}

	public void TriggerPhase1InitializeGoBackToShipPhase()
	{
		State = Quest5SetPieceBattleMissionState.Phase1InitializeGoBackToShip;
	}

	public void CompletePhase1InitializeGoBackToShipTransition()
	{
		State = Quest5SetPieceBattleMissionState.Phase1EscapePhase;
		HandlePlayersBridgeAndControlPointUsagesForPhase1EscapePhase();
	}

	public void SetTalkedWithSister()
	{
		_talkedWithSister = true;
		DeactivateObjectiveIfItIsActive(_talkWithYourSisterObjective);
		base.Mission.GetMissionBehavior<MissionConversationLogic>().DisableStartConversation(isDisabled: true);
		Phase1InteriorCameraSisterEntity = null;
		_instructionState = Quest5InstructionState.ReturnToDeck;
	}

	private void CalculateBuySlaveConversationPoint()
	{
		float num = float.MaxValue;
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _playerShip.AttachmentPointMachines)
		{
			foreach (ShipAttachmentPointMachine attachmentPointMachine2 in _phase1EnemyShip1.AttachmentPointMachines)
			{
				float num2 = attachmentPointMachine.GameEntity.GetGlobalFrame().origin.Distance(attachmentPointMachine2.GameEntity.GetGlobalFrame().origin);
				if (num > num2)
				{
					_crusasConversationPointFrame = attachmentPointMachine.GameEntity;
					_slaveTraderConversationPointFrame = attachmentPointMachine2.GameEntity;
					num = num2;
				}
			}
		}
	}

	private void AddConversationSounds()
	{
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=kAAkgKFB}Ahoy! Who approaches?"), MBInformationManager.NotificationPriority.Medium, _slaveTraderCharacter));
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=61hcBa4X}I am Crusas Salautas. I seek Purig of Agilting."), MBInformationManager.NotificationPriority.Medium, NavalStorylineData.Prusas.CharacterObject));
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=JAtDE00L}This is his ship, but he's away. Should be back shortly, though - we signalled him. Keep your distance for now."), MBInformationManager.NotificationPriority.Medium, _slaveTraderCharacter));
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=JPVD5sfc}I am one of Purig's longtime customers, and I am in a bit of a hurry. I made arrangements weeks ago to buy his merchandise. How long is Purig going to be? Can I come aboard?"), MBInformationManager.NotificationPriority.Medium, NavalStorylineData.Prusas.CharacterObject));
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=MNnk6LAa}You'll need to be patient, friend. Purig's instructions were to let no one aboard. But he won't be long. He's just offshore, out looking for prey."), MBInformationManager.NotificationPriority.Medium, _slaveTraderCharacter));
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=wJZTakoT}How many do you have to sell?"), MBInformationManager.NotificationPriority.Medium, NavalStorylineData.Prusas.CharacterObject));
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=4Z7a0Kre}Several score, all in good health. We've been feeding them well, sparing no expense. We take pride in our work."), MBInformationManager.NotificationPriority.Medium, _slaveTraderCharacter));
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=XEbbugis}That's fine, but I was expecting more."), MBInformationManager.NotificationPriority.Medium, NavalStorylineData.Prusas.CharacterObject));
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=AXz58qHq}You're not the only buyer, my friend! Mines, buildings, repairs... Even on the mainland, mix a handful of our fellows in with some convicts or war captives, and who's to notice?"), MBInformationManager.NotificationPriority.Medium, _slaveTraderCharacter));
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=Zu3lj2s1}So... Can we talk price?"), MBInformationManager.NotificationPriority.Medium, NavalStorylineData.Prusas.CharacterObject));
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=aYmx5ODE}You'll need to wait for our master to return before you start bargaining. Don't push your friendship with Purig too much, though - he's got expensive tastes. He likes to see the envy in other men's eyes when the sun sparkles off his fine golden helm."), MBInformationManager.NotificationPriority.Medium, _slaveTraderCharacter));
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=dndcy626}I don't like just to sit here idly. Maybe I can come aboard and inspect some of the captives? I can conclude the deal more quickly when your master arrives, and let him get back to his hunting."), MBInformationManager.NotificationPriority.Medium, NavalStorylineData.Prusas.CharacterObject));
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=ediTKoqo}My instructions were clear. No one aboard the ship."), MBInformationManager.NotificationPriority.Medium, _slaveTraderCharacter));
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=csvVz5f2}The air is stifling. I hope you've been letting the captives up on deck? No signs of disease?"), MBInformationManager.NotificationPriority.Medium, NavalStorylineData.Prusas.CharacterObject));
		_conversationSounds.Enqueue(new ConversationSound(new TextObject("{=aKq3AMpG}If you think they're sick you're welcome not to buy any."), MBInformationManager.NotificationPriority.Medium, _slaveTraderCharacter));
	}

	private void CheckAndPlayCrusasAndSlaveTraderConversationSound()
	{
		if (_crusasAndSeaHoundMovedToTheConversationPoints)
		{
			if (Agent.Main.Position.Distance(_playerShip.GetCaptainSpawnGlobalFrame().origin) < 30f)
			{
				if (!_conversationSounds.IsEmpty())
				{
					ConversationSound conversationSound = _conversationSounds.Dequeue();
					MBInformationManager.DialogNotificationHandle item = CampaignInformationManager.AddDialogLine(conversationSound.Line, conversationSound.Character, null, 0, conversationSound.Priority);
					_dialogNotificationHandleCache.Add(item);
				}
				return;
			}
			foreach (MBInformationManager.DialogNotificationHandle item2 in _dialogNotificationHandleCache)
			{
				CampaignInformationManager.ClearDialogNotification(item2);
			}
			_dialogNotificationHandleCache.Clear();
		}
		else if (_crusasAgent.Position.AsVec2.NearlyEquals(_crusasConversationPointFrame.GetGlobalFrame().origin.AsVec2, 3f) && _slaveTraderAgent.Position.AsVec2.NearlyEquals(_slaveTraderConversationPointFrame.GetGlobalFrame().origin.AsVec2, 3f))
		{
			_crusasAndSeaHoundMovedToTheConversationPoints = true;
		}
		else
		{
			WorldPosition scriptedPosition = new WorldPosition(base.Mission.Scene, _crusasConversationPointFrame.GetGlobalFrame().origin);
			Vec3 vec = _crusasConversationPointFrame.GetGlobalFrame().origin - _crusasAgent.Position;
			_crusasAgent.SetScriptedPositionAndDirection(ref scriptedPosition, vec.RotationX.ToRadians(), addHumanLikeDelay: true);
			WorldPosition scriptedPosition2 = new WorldPosition(base.Mission.Scene, _slaveTraderConversationPointFrame.GetGlobalFrame().origin);
			float scriptedDirection = (_slaveTraderConversationPointFrame.GetGlobalFrame().origin - _slaveTraderAgent.Position).RotationX.ToRadians();
			_slaveTraderAgent.SetScriptedPositionAndDirection(ref scriptedPosition2, scriptedDirection, addHumanLikeDelay: true);
		}
	}

	private Equipment GetScriptedStealthEquipment()
	{
		Equipment equipment = MBObjectManager.Instance.GetObject<MBEquipmentRoster>("naval_storyline_quest5_stealth_set").DefaultEquipment.Clone();
		if (equipment == null)
		{
			equipment = Campaign.Current.DefaultStealthEquipment.Clone();
			for (int i = 0; i < 12; i++)
			{
				switch (i)
				{
				case 5:
				{
					ItemObject itemObject4 = MBObjectManager.Instance.GetObject<ItemObject>("assassin_hood");
					if (itemObject4 != null)
					{
						equipment[i] = new EquipmentElement(itemObject4);
					}
					break;
				}
				case 9:
				{
					ItemObject itemObject2 = MBObjectManager.Instance.GetObject<ItemObject>("assassin_shoulder");
					if (itemObject2 != null)
					{
						equipment[i] = new EquipmentElement(itemObject2);
					}
					break;
				}
				case 6:
				{
					ItemObject itemObject3 = MBObjectManager.Instance.GetObject<ItemObject>("assassin_armor");
					if (itemObject3 != null)
					{
						equipment[i] = new EquipmentElement(itemObject3);
					}
					break;
				}
				case 7:
				{
					ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>("assassin_boot");
					if (itemObject != null)
					{
						equipment[i] = new EquipmentElement(itemObject);
					}
					break;
				}
				}
				if ((i == 0 || i == 1 || i == 2 || i == 3 || i == 4) && !equipment[i].IsEmpty && equipment[i].Item.WeaponComponent != null && equipment[i].Item.WeaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.Stone)
				{
					equipment[i] = EquipmentElement.Invalid;
				}
			}
		}
		return equipment;
	}

	private void HandleEscapeShipCutLoose()
	{
		if (_escapeShipCutLooseTimer == null || !_escapeShipCutLooseTimer.Check())
		{
			return;
		}
		_escapeShipCutLooseTimer = null;
		foreach (ShipAttachmentMachine attachmentMachine in _phase1EnemyShip3.AttachmentMachines)
		{
			if (attachmentMachine.IsShipAttachmentMachineBridged())
			{
				attachmentMachine.DisconnectAttachment();
			}
		}
		foreach (ShipAttachmentMachine attachmentMachine2 in _phase1EnemyShip2.AttachmentMachines)
		{
			if (attachmentMachine2.IsShipAttachmentMachineBridged() && attachmentMachine2.CurrentAttachment.AttachmentTarget.OwnerShip == _phase1EnemyShip3)
			{
				attachmentMachine2.DisconnectAttachment();
			}
		}
	}

	public bool ShouldTeleportPlayerBetweenTargetPositionAndHidingSpot()
	{
		if (Agent.Main != null && Agent.Main.IsActive() && !Agent.Main.IsInWater())
		{
			return false;
		}
		if (_allowedSwimRadiusCheckTimer == null)
		{
			_allowedSwimRadiusCheckTimer = new MissionTimer(5f);
		}
		else if (Agent.Main != null && Agent.Main.IsActive() && _allowedSwimRadiusCheckTimer.Check())
		{
			_allowedSwimRadiusCheckTimer.Reset();
			if (Agent.Main.Position.Distance(HidingSpot1Position.GlobalPosition) > 200f)
			{
				MBInformationManager.DialogNotificationHandle item = CampaignInformationManager.AddDialogLine(new TextObject("{=4O6feRM9}Hey! Over here! Let's not get separated."), NavalStorylineData.Gunnar.CharacterObject, null, 0, MBInformationManager.NotificationPriority.High);
				_dialogNotificationHandleCache.Add(item);
				return true;
			}
			if (Agent.Main.Position.Distance(_phase1EnemyShip1.GameEntity.GlobalPosition) < 25f)
			{
				MBInformationManager.DialogNotificationHandle item2 = CampaignInformationManager.AddDialogLine(new TextObject("{=y0EgxaLN}Keep away from those lookouts!"), NavalStorylineData.Gunnar.CharacterObject, null, 0, MBInformationManager.NotificationPriority.High);
				_dialogNotificationHandleCache.Add(item2);
				return true;
			}
		}
		return false;
	}

	public void TeleportPlayerBetweenTargetPositionAndHidingSpot(out Vec3 mainAgentDirection)
	{
		mainAgentDirection = Agent.Main.LookDirection;
		if (State == Quest5SetPieceBattleMissionState.Phase1GoToEnemyShip)
		{
			StandingPoint pilotStandingPoint = _playerShip.ShipControllerMachine.PilotStandingPoint;
			Agent.Main.TeleportToPosition(pilotStandingPoint.GameEntity.GlobalPosition);
			Agent.Main.HandleStartUsingAction(pilotStandingPoint, -1);
		}
		else
		{
			Vec3 vec = (_approachPointEntity.GlobalPosition + HidingSpot1Position.GlobalPosition) * 0.5f;
			mainAgentDirection = (HidingSpot1Position.GlobalPosition - vec).NormalizedCopy();
			Agent.Main.TeleportToPosition(vec);
		}
	}

	public bool ShouldTeleportPlayerShipToStartingPosition()
	{
		if (_playerShip != null)
		{
			if (_playerShip.GlobalFrame.origin.NearlyEquals(in _phase1PlayerShipSpawnPosition, 2f))
			{
				return false;
			}
			if (_lastCachedPlayerShipDistanceToTargetApproachPoint.ApproximatelyEqualsTo(0f))
			{
				_lastCachedPlayerShipDistanceToTargetApproachPoint = _playerShip.GlobalFrame.origin.Distance(_approachPointEntity.GlobalPosition);
				_playerShipsTargetApproachPointDistanceCheckTimer = new MissionTimer(6f);
			}
			else
			{
				MissionTimer playerShipsTargetApproachPointDistanceCheckTimer = _playerShipsTargetApproachPointDistanceCheckTimer;
				if (playerShipsTargetApproachPointDistanceCheckTimer != null && playerShipsTargetApproachPointDistanceCheckTimer.Check())
				{
					float num = _playerShip.GlobalFrame.origin.Distance(_approachPointEntity.GlobalPosition);
					if (num > _lastCachedPlayerShipDistanceToTargetApproachPoint)
					{
						_lastCachedPlayerShipDistanceToTargetApproachPoint = 0f;
						_playerShipsTargetApproachPointDistanceCheckTimer = null;
						return true;
					}
					_lastCachedPlayerShipDistanceToTargetApproachPoint = num;
				}
			}
		}
		return false;
	}

	public void TeleportPlayerShipToStartingPosition(out Vec3 mainAgentDirection)
	{
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("phase_1_player_ship_sp");
		_navalShipsLogic.TeleportShip(_playerShip, gameEntity.GetGlobalFrame(), checkFreeArea: true);
		mainAgentDirection = Agent.Main.LookDirection;
	}

	public Vec3 CalculateMissionStartDirection()
	{
		return (_approachPointEntity.GetGlobalFrame().origin - Agent.Main.Frame.origin).NormalizedCopy();
	}

	private void HandlePlayersBridgeAndControlPointUsagesForPhase1GoToEnemyShip()
	{
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			if (allShip != _playerShip)
			{
				foreach (ClimbingMachine climbingMachine in allShip.ClimbingMachines)
				{
					foreach (StandingPoint standingPoint in climbingMachine.StandingPoints)
					{
						standingPoint.IsDisabledForPlayers = true;
					}
				}
			}
			foreach (ShipAttachmentMachine attachmentMachine in allShip.AttachmentMachines)
			{
				foreach (StandingPoint standingPoint2 in attachmentMachine.StandingPoints)
				{
					standingPoint2.IsDisabledForPlayers = true;
				}
			}
			foreach (ShipAttachmentPointMachine attachmentPointMachine in allShip.AttachmentPointMachines)
			{
				foreach (StandingPoint standingPoint3 in attachmentPointMachine.StandingPoints)
				{
					standingPoint3.IsDisabledForPlayers = true;
				}
			}
		}
	}

	private void HandlePlayersBridgeAndControlPointUsagesForPhase1SwimmingAndStealthPhase()
	{
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			if (allShip != _phase1EnemyShip4)
			{
				foreach (ClimbingMachine climbingMachine in allShip.ClimbingMachines)
				{
					foreach (StandingPoint standingPoint in climbingMachine.StandingPoints)
					{
						standingPoint.IsDisabledForPlayers = true;
					}
				}
			}
			else
			{
				foreach (ClimbingMachine climbingMachine2 in allShip.ClimbingMachines)
				{
					foreach (StandingPoint standingPoint2 in climbingMachine2.StandingPoints)
					{
						standingPoint2.IsDisabledForPlayers = false;
					}
				}
			}
			foreach (ShipAttachmentMachine attachmentMachine in allShip.AttachmentMachines)
			{
				foreach (StandingPoint standingPoint3 in attachmentMachine.StandingPoints)
				{
					standingPoint3.IsDisabledForPlayers = true;
				}
			}
			foreach (ShipAttachmentPointMachine attachmentPointMachine in allShip.AttachmentPointMachines)
			{
				foreach (StandingPoint standingPoint4 in attachmentPointMachine.StandingPoints)
				{
					standingPoint4.IsDisabledForPlayers = true;
				}
			}
		}
	}

	private void HandlePlayersBridgeAndControlPointUsagesForPhase1EscapePhase()
	{
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			foreach (ClimbingMachine climbingMachine in allShip.ClimbingMachines)
			{
				foreach (StandingPoint standingPoint in climbingMachine.StandingPoints)
				{
					standingPoint.IsDisabledForPlayers = false;
				}
			}
			if (allShip != _phase1EnemyShip3)
			{
				foreach (ShipAttachmentMachine attachmentMachine in allShip.AttachmentMachines)
				{
					foreach (StandingPoint standingPoint2 in attachmentMachine.StandingPoints)
					{
						standingPoint2.IsDisabledForPlayers = true;
					}
				}
				foreach (ShipAttachmentPointMachine attachmentPointMachine in allShip.AttachmentPointMachines)
				{
					foreach (StandingPoint standingPoint3 in attachmentPointMachine.StandingPoints)
					{
						standingPoint3.IsDisabledForPlayers = true;
					}
				}
				continue;
			}
			foreach (ShipAttachmentMachine attachmentMachine2 in allShip.AttachmentMachines)
			{
				if (attachmentMachine2.CurrentAttachment == null)
				{
					foreach (StandingPoint standingPoint4 in attachmentMachine2.StandingPoints)
					{
						standingPoint4.IsDisabledForPlayers = true;
					}
					continue;
				}
				foreach (StandingPoint standingPoint5 in attachmentMachine2.StandingPoints)
				{
					standingPoint5.IsDisabledForPlayers = false;
				}
			}
			foreach (ShipAttachmentPointMachine attachmentPointMachine2 in allShip.AttachmentPointMachines)
			{
				if (attachmentPointMachine2.CurrentAttachment == null)
				{
					foreach (StandingPoint standingPoint6 in attachmentPointMachine2.StandingPoints)
					{
						standingPoint6.IsDisabledForPlayers = true;
					}
					continue;
				}
				foreach (StandingPoint standingPoint7 in attachmentPointMachine2.StandingPoints)
				{
					standingPoint7.IsDisabledForPlayers = false;
				}
			}
		}
	}

	private void ClearPhase1OnPhaseTransition()
	{
		_phase1EnemyShip1 = null;
		_phase1EnemyShip2 = null;
		_phase1EnemyShip4 = null;
		_dynamicPatrolAreas = null;
		_stealthAgents = null;
		_crusasConversationPointFrame = WeakGameEntity.Invalid;
		_slaveTraderConversationPointFrame = WeakGameEntity.Invalid;
		_approachPointEntity = null;
		_phase1EnemyShipToInteriorShipDoorEntity = null;
		_phase1InteriorToEnemyShip3ShipDoorEntity = null;
		_phase1EnemyShip1InitialSpawnEntity = null;
		_phase1EnemyShip1TargetEntity = null;
		_conversationSounds = null;
		_dialogNotificationHandleCache.Clear();
		_sisterWoundedAnimationActionIndexCache = ActionIndexCache.act_none;
		_slaveTraderShipOarsmanActionIndexCache = ActionIndexCache.act_none;
		Phase1InteriorCameraSisterEntity = null;
		GC.Collect();
	}

	public void TriggerInitializePhase2()
	{
		State = Quest5SetPieceBattleMissionState.InitializePhase2Part1;
		MBMusicManager.Current.StartTheme(MusicTheme.BattleNord, 0.3f);
	}

	public void CompletePhase1ToPhase2Transition()
	{
		State = Quest5SetPieceBattleMissionState.Phase2InProgress;
	}

	private void InitializePhase2Part1()
	{
		Mission.Current.Scene.SetAtmosphereWithName("TOD_naval_03_00_sunset");
		if (_gunnarAgent != null && _gunnarAgent.IsUsingGameObject)
		{
			_gunnarAgent.StopUsingGameObjectMT();
		}
		if (Agent.Main != null && Agent.Main.IsActive() && Agent.Main.IsUsingGameObject)
		{
			Agent.Main.StopUsingGameObject();
		}
		if (_slaveTraderAgent != null && _slaveTraderAgent.IsActive())
		{
			_slaveTraderAgent.FadeOut(hideInstantly: true, hideMount: false);
			for (int i = 0; i < _slaveTraderShipOarsmen.Length; i++)
			{
				_slaveTraderShipOarsmen[i]?.FadeOut(hideInstantly: true, hideMount: false);
			}
			_navalTrajectoryPlanningLogic.ForceReinitialize();
		}
		base.Mission.GetMissionBehavior<Quest5WanderingShipsMissionLogic>()?.OnPhase2Started();
		foreach (MBInformationManager.DialogNotificationHandle item in _dialogNotificationHandleCache)
		{
			CampaignInformationManager.ClearDialogNotification(item);
		}
		_dialogNotificationHandleCache.Clear();
	}

	private void MoveAgentToEscapeShip(Agent agent)
	{
		if (_navalAgentsLogic.IsAgentOnAnyShip(agent, out var onShip, EscapeShip.Team.TeamSide))
		{
			if (onShip != EscapeShip)
			{
				_navalAgentsLogic.TransferAgentToShip(agent, EscapeShip);
			}
		}
		else
		{
			_navalAgentsLogic.AddAgentToShip(agent, EscapeShip);
		}
	}

	private void InitializePhase2Part2()
	{
		_phase2AllyShip1 = CreateShip("aserai_heavy_ship", "phase_2_ally_ship_1_sp", GetAvailableAllyFormation(), spawnAnchored: false, _phase2AllyShip1UpgradePieceList);
		_phase2AllyShip2 = CreateShip("nord_medium_ship", "phase_2_ally_ship_2_sp", GetAvailableAllyFormation(), spawnAnchored: false, _phase2AllyShip2UpgradePieceList);
		_phase2AllyShip3 = CreateShip("northern_medium_ship", "phase_2_ally_ship_3_sp", GetAvailableAllyFormation(), spawnAnchored: false, _phase2AllyShip3UpgradePieceList);
		_phase2AllyShip4 = CreateShip("sturgia_heavy_ship", "phase_2_ally_ship_4_sp", GetAvailableAllyFormation(), spawnAnchored: false, _phase2AllyShip4UpgradePieceList);
		_phase2AllyShip5 = CreateShip("northern_medium_ship", "phase_2_ally_ship_5_sp", GetAvailableAllyFormation(), spawnAnchored: false, _phase2AllyShip5UpgradePieceList);
		if (_phase1EnemyShip3 == null)
		{
			_isCheckpointInitialize = true;
			TeamAINavalComponent teamAI = new TeamAINavalComponent(base.Mission, base.Mission.AttackerTeam, 5f, 1f);
			base.Mission.AttackerTeam.AddTeamAI(teamAI);
			base.Mission.AttackerTeam.AddTacticOption(new TacticNavalBalancedOffense(base.Mission.AttackerTeam));
			TeamAINavalComponent teamAI2 = new TeamAINavalComponent(base.Mission, base.Mission.DefenderTeam, 5f, 1f);
			base.Mission.DefenderTeam.AddTeamAI(teamAI2);
			base.Mission.DefenderTeam.AddTacticOption(new TacticNavalBalancedOffense(base.Mission.DefenderTeam));
			_navalAgentsLogic.SetDeploymentMode(value: true);
			_navalShipsLogic.SetDeploymentMode(value: true);
			_playerShip = CreateShip("ship_dromon_storyline", "phase_1_enemy_ship_3_sp", _playerFormation, spawnAnchored: false, _escapeShipUpgradePieceList);
			_navalAgentsLogic.SetDesiredTroopCountOfShip(_playerShip, 2);
			Hero.MainHero.Heal(Hero.MainHero.MaxHitPoints);
			_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, CharacterObject.PlayerCharacter), _playerShip);
			_navalAgentsLogic.SetDeploymentMode(value: false);
			_navalShipsLogic.SetDeploymentMode(value: false);
			_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.PlayerTeam);
			SpawnGunnarOnShip(_playerShip);
			_gunnarAgent.Controller = AgentControllerType.None;
			_gunnarAgent.SetMortalityState(Agent.MortalityState.Immortal);
			_playerShip.SetController(ShipControllerType.AI);
			_phase1EnemyShipToInteriorShipDoorEntity = Mission.Current.Scene.FindEntityWithTag("phase_1_enemy_ship_3_to_interior_door_tag");
			_phase1EnemyShipToInteriorShipDoorEntity.GetFirstScriptOfType<ShipDoorUsePoint>().SetShipDoorUsePointEnabled(isEnabled: false);
			Agent.Main.TeleportToPosition(_playerShip.GetMiddleInnerSpawnGlobalFrame().origin);
			_playerShip.SetAnchor(isAnchored: false);
			_playerShip.SetShipOrderActive(isOrderActive: true);
			_playerShip.ShipControllerMachine.PilotStandingPoint.IsDisabledForPlayers = true;
		}
		else
		{
			Formation availableAllyFormation = GetAvailableAllyFormation();
			_navalShipsLogic.TransferShipToTeam(EscapeShip, base.Mission.PlayerTeam, availableAllyFormation);
			MoveAgentToEscapeShip(_gunnarAgent);
			MoveAgentToEscapeShip(Agent.Main);
			RemoveShipInternal(_playerShip);
			AddAvailableAllyFormation(availableAllyFormation);
			_navalShipsLogic.TransferShipToFormation(EscapeShip, _playerFormation);
			_playerShip = EscapeShip;
			_navalAgentsLogic.AssignCaptainToShip(_gunnarAgent, EscapeShip);
			EscapeShip.ShipOrder.ManageShipDetachments();
			_gunnarAgent.TeleportToPosition(GunnarShipUsePosition.origin);
			EscapeShip.ShipControllerMachine.PilotStandingPoint.IsDisabledForPlayers = true;
			_navalAgentsLogic.SetDeploymentMode(value: true);
			_navalShipsLogic.SetDeploymentMode(value: true);
			_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(EscapeShip);
			_navalAgentsLogic.SetDeploymentMode(value: false);
			_navalShipsLogic.SetDeploymentMode(value: false);
			Vec3 position = Agent.Main.Position;
			Agent.Main.TeleportToPosition(position);
			AddAvailableEnemyFormation(_phase1EnemyShip1.Formation);
			RemoveShipInternal(_phase1EnemyShip1);
			AddAvailableEnemyFormation(_phase1EnemyShip4.Formation);
			RemoveShipInternal(_phase1EnemyShip4);
			_navalTrajectoryPlanningLogic.ForceReinitialize();
		}
		_phase2EnemyShip1 = CreateShip("ship_meditlight_storyline_q5", "phase_2_enemy_ship_1_sp", GetAvailableEnemyFormation());
		_phase2EnemyShip2 = CreateShip("ship_meditlight_storyline_q5", "phase_2_enemy_ship_2_sp", GetAvailableEnemyFormation());
		_phase2EnemyShip3 = CreateShip("ship_meditlight_storyline_q5", "phase_2_enemy_ship_3_sp", GetAvailableEnemyFormation());
		_phase2EnemyShip4 = CreateShip("ship_meditlight_storyline_q5", "phase_2_enemy_ship_4_sp", GetAvailableEnemyFormation());
		_phase2EnemyShip5 = CreateShip("ship_meditlight_storyline_q5", "phase_2_enemy_ship_5_sp", GetAvailableEnemyFormation());
		_phase2EnemyShip1.SetCanBeTakenOver(value: false);
		_phase2EnemyShip2.SetCanBeTakenOver(value: false);
		_phase2EnemyShip3.SetCanBeTakenOver(value: false);
		_phase2EnemyShip4.SetCanBeTakenOver(value: false);
		_phase2EnemyShip5.SetCanBeTakenOver(value: false);
		_phase2EnemyShipStationary1 = CreateShip("western_medium_ship", "phase_2_enemy_ship_stationary_1", GetAvailableEnemyFormation());
		_phase2EnemyShipStationary1.SetCanBeTakenOver(value: false);
		AddTriggerPointForPirateShip(_phase2EnemyShip1, base.Mission.Scene.FindEntityWithTag("phase_2_enemy_ship_1_target"));
		AddTriggerPointForPirateShip(_phase2EnemyShip2, base.Mission.Scene.FindEntityWithTag("phase_2_enemy_ship_2_target"));
		AddTriggerPointForPirateShip(_phase2EnemyShip3, base.Mission.Scene.FindEntityWithTag("phase_2_enemy_ship_3_target"));
		AddTriggerPointForPirateShip(_phase2EnemyShip4, base.Mission.Scene.FindEntityWithTag("phase_2_enemy_ship_4_target"));
		AddTriggerPointForPirateShip(_phase2EnemyShip5, base.Mission.Scene.FindEntityWithTag("phase_2_enemy_ship_5_target"));
		_phase2EnemyShip1.SetFoldSailsOnBridgeConnection(value: false);
		_phase2EnemyShip2.SetFoldSailsOnBridgeConnection(value: false);
		_phase2EnemyShip3.SetFoldSailsOnBridgeConnection(value: false);
		_phase2EnemyShip4.SetFoldSailsOnBridgeConnection(value: false);
		_phase2EnemyShip5.SetFoldSailsOnBridgeConnection(value: false);
		_autoCutLooseTimersForPirateShips.Add(_phase2EnemyShip1, null);
		_autoCutLooseTimersForPirateShips.Add(_phase2EnemyShip2, null);
		_autoCutLooseTimersForPirateShips.Add(_phase2EnemyShip3, null);
		_autoCutLooseTimersForPirateShips.Add(_phase2EnemyShip4, null);
		_autoCutLooseTimersForPirateShips.Add(_phase2EnemyShip5, null);
		_autoEstablishConnectionsForPirateShips.Add(_phase2EnemyShip1, null);
		_autoEstablishConnectionsForPirateShips.Add(_phase2EnemyShip2, null);
		_autoEstablishConnectionsForPirateShips.Add(_phase2EnemyShip3, null);
		_autoEstablishConnectionsForPirateShips.Add(_phase2EnemyShip4, null);
		_autoEstablishConnectionsForPirateShips.Add(_phase2EnemyShip5, null);
		EscapeShip.SetFoldSailsOnBridgeConnection(value: false);
		foreach (ShipAttachmentMachine attachmentMachine in EscapeShip.AttachmentMachines)
		{
			if (attachmentMachine.IsDisabled)
			{
				attachmentMachine.SetEnabledAndMakeVisible();
			}
		}
		SetShipAttachmentJointPhysicsEnabledForShip(_phase2EnemyShip1, enabled: false);
		SetShipAttachmentJointPhysicsEnabledForShip(_phase2EnemyShip2, enabled: false);
		SetShipAttachmentJointPhysicsEnabledForShip(_phase2EnemyShip3, enabled: false);
		SetShipAttachmentJointPhysicsEnabledForShip(_phase2EnemyShip4, enabled: false);
		SetShipAttachmentJointPhysicsEnabledForShip(_phase2EnemyShip5, enabled: false);
		EscapeShip.SetController(ShipControllerType.AI);
		base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
		_escapeShipTargetSpeed = 0f;
		_escapeShipSpeed = 0f;
		_escapeShipTargetDirection = EscapeShip.GameEntity.GetBodyWorldTransform().rotation.f.AsVec2.Normalized();
		_escapeShipDirection = EscapeShip.GameEntity.GetBodyWorldTransform().rotation.f.AsVec2.Normalized();
	}

	private void InitializePhase2Part3()
	{
		SetDisableShipAttachmentMachinesForPlayer(EscapeShip, isDisabled: true);
		SpawnPhase2AllyTroops();
		SpawnPhase2EnemyTroops();
		if (_isCheckpointInitialize)
		{
			base.Mission.OnInitialSpawnCompleted();
		}
		else
		{
			_phase2EnemyShip1.FinalizeDeployment(initializeMachines: true);
			_phase2EnemyShip2.FinalizeDeployment(initializeMachines: true);
			_phase2EnemyShip3.FinalizeDeployment(initializeMachines: true);
			_phase2EnemyShip4.FinalizeDeployment(initializeMachines: true);
			_phase2EnemyShip5.FinalizeDeployment(initializeMachines: true);
			_phase2EnemyShipStationary1.FinalizeDeployment(initializeMachines: true);
			_phase2AllyShip1.FinalizeDeployment(initializeMachines: true);
			_phase2AllyShip2.FinalizeDeployment(initializeMachines: true);
			_phase2AllyShip3.FinalizeDeployment(initializeMachines: true);
			_phase2AllyShip4.FinalizeDeployment(initializeMachines: true);
			_phase2AllyShip5.FinalizeDeployment(initializeMachines: true);
			_navalTrajectoryPlanningLogic.ForceReinitialize();
		}
		_lightScriptedFiresMissionController.TriggerFiring();
		_gunnarAgent.Controller = AgentControllerType.None;
		HandlePlayersBridgeAndControlPointUsagesForPhase2InProgress();
		RemoveShipControlPointDescriptionOfAllEnemyShips();
		_isMissionShipBoardedToTheEscapeShip.Add(_phase2EnemyShip1, value: false);
		_isMissionShipBoardedToTheEscapeShip.Add(_phase2EnemyShip2, value: false);
		_isMissionShipBoardedToTheEscapeShip.Add(_phase2EnemyShip3, value: false);
		_isMissionShipBoardedToTheEscapeShip.Add(_phase2EnemyShip4, value: false);
		_isMissionShipBoardedToTheEscapeShip.Add(_phase2EnemyShip5, value: false);
		_phase2EscapeShipPirateTargetFrame1 = Mission.Current.Scene.FindEntityWithTag("phase_2_anchor_1");
		_phase2EscapeShipPirateTargetFrame2 = Mission.Current.Scene.FindEntityWithTag("phase_2_anchor_2");
		_phase2EscapeShipPirateTargetFrame3 = Mission.Current.Scene.FindEntityWithTag("phase_2_anchor_3");
		_phase2EscapeShipPirateTargetFrame4 = Mission.Current.Scene.FindEntityWithTag("phase_2_anchor_4");
		_phase2EscapeShipPirateTargetFrame5 = Mission.Current.Scene.FindEntityWithTag("phase_2_anchor_5");
		EscapeShip.SetCustomSailSetting(enableCustomSailSetting: true, SailInput.Full);
		if (!_isCheckpointInitialize)
		{
			ClearPhase1OnPhaseTransition();
		}
	}

	private void InitializePhase2Part4()
	{
		if (_isCheckpointInitialize)
		{
			_gunnarAgent.Controller = AgentControllerType.AI;
			_navalAgentsLogic.AssignCaptainToShip(_gunnarAgent, EscapeShip);
			MoveAgentToEscapeShip(Agent.Main);
			EscapeShip.ShipOrder.ManageShipDetachments();
			_navalAgentsLogic.SetDeploymentMode(value: true);
			_navalShipsLogic.SetDeploymentMode(value: true);
			_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(EscapeShip);
			_navalAgentsLogic.SetDeploymentMode(value: false);
			_navalShipsLogic.SetDeploymentMode(value: false);
			_gunnarAgent.Controller = AgentControllerType.None;
			_gunnarMovementState = GunnarMovementState.UseTheEscapeShip;
			_playerShip.GetNextCrewSpawnGlobalFrame(out var crewSpawnGlobalFrame);
			Agent.Main.TeleportToPosition(crewSpawnGlobalFrame.origin);
			Agent.Main.SetClothingColor1(4279111698u);
			Agent.Main.SetClothingColor2(4279111698u);
			Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(GetScriptedStealthEquipment());
			_gunnarAgent.UpdateSpawnEquipmentAndRefreshVisuals(GetScriptedStealthEquipment());
			_instructionState = Quest5InstructionState.GunnarUsesShip;
			State = Quest5SetPieceBattleMissionState.Phase2InProgress;
		}
		else
		{
			State = Quest5SetPieceBattleMissionState.Phase1ToPhase2FadeIn;
		}
		RemoveGunnarsHelmet();
		ModifyMainAgentEquipmentForPhase2();
	}

	private void AddTriggerPointForPirateShip(MissionShip ship, GameEntity triggerPoint)
	{
		_pirateShipTriggerPoints[ship] = triggerPoint;
		_isPirateShipTriggered[ship] = false;
		_isPirateShipMovementDisabled[ship] = false;
		_pirateShipEnabledAttachmentMachine[ship] = null;
		_isPirateShipMovingToTheEscapeShip[ship] = false;
		_isPirateShipLostItsCrew[ship] = false;
		_limitPirateShipChasingSpeed[ship] = false;
	}

	private void SpawnPhase2AllyTroops()
	{
		_navalAgentsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic.SetDeploymentMode(value: true);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2AllyShip1, Phase2AllyShip1TroopCount);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2AllyShip2, Phase2AllyShip2TroopCount);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2AllyShip3, Phase2AllyShip3TroopCount);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2AllyShip4, Phase2AllyShip4TroopCount);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2AllyShip5, Phase2AllyShip5TroopCount);
		AddMissionShipTroops(_phase2AllyShip1Troops, _phase2AllyShip1, PartyBase.MainParty);
		AddMissionShipTroops(_phase2AllyShip2Troops, _phase2AllyShip2, PartyBase.MainParty);
		AddMissionShipTroops(_phase2AllyShip3Troops, _phase2AllyShip3, PartyBase.MainParty);
		AddMissionShipTroops(_phase2AllyShip4Troops, _phase2AllyShip4, PartyBase.MainParty);
		AddMissionShipTroops(_phase2AllyShip5Troops, _phase2AllyShip5, PartyBase.MainParty);
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.PlayerTeam);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2AllyShip1);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2AllyShip2);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2AllyShip3);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2AllyShip4);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2AllyShip5);
		_navalAgentsLogic.SetDeploymentMode(value: false);
		_navalShipsLogic.SetDeploymentMode(value: false);
	}

	private void SpawnPhase2EnemyTroops()
	{
		_navalAgentsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic.SetDeploymentMode(value: true);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2EnemyShip1, Phase2EnemyShip1TroopCount);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2EnemyShip2, Phase2EnemyShip2TroopCount);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2EnemyShip3, Phase2EnemyShip3TroopCount);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2EnemyShip4, Phase2EnemyShip4TroopCount);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2EnemyShip5, Phase2EnemyShip5TroopCount);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2EnemyShipStationary1, Phase2EnemyShipStationary1TroopCount);
		AddMissionShipTroops(_phase2EnemyShip1Troops, _phase2EnemyShip1);
		AddMissionShipTroops(_phase2EnemyShip2Troops, _phase2EnemyShip2);
		AddMissionShipTroops(_phase2EnemyShip3Troops, _phase2EnemyShip3);
		AddMissionShipTroops(_phase2EnemyShip4Troops, _phase2EnemyShip4);
		AddMissionShipTroops(_phase2EnemyShip5Troops, _phase2EnemyShip5);
		AddMissionShipTroops(_phase2EnemyShipStationary1Troops, _phase2EnemyShipStationary1);
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.EnemyTeam);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2EnemyShip1);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2EnemyShip2);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2EnemyShip3);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2EnemyShip4);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2EnemyShip5);
		_navalAgentsLogic.SetDeploymentMode(value: false);
		_navalShipsLogic.SetDeploymentMode(value: false);
	}

	private void HandleEscapeShipMovement()
	{
		if (!EscapeShip.IsAIControlled)
		{
			EscapeShip.SetController(ShipControllerType.AI);
		}
		if (_currentPhase2EscapeShipTargetPoint == null)
		{
			if (!_phase2EscapeShipTargetPoints.IsEmpty())
			{
				_currentPhase2EscapeShipTargetPoint = _phase2EscapeShipTargetPoints.Dequeue();
			}
			else
			{
				_currentPhase2EscapeShipTargetPoint = base.Mission.Scene.FindEntityWithTag("phase_3_enemy_ship_2_sp");
			}
			if (!_isPirateShipMovementDisabled[_phase2EnemyShip5])
			{
				_escapeShipTargetDirection = (_currentPhase2EscapeShipTargetPoint.GetGlobalFrame().origin - EscapeShip.GameEntity.GetBodyWorldTransform().origin).AsVec2.Normalized();
			}
			else
			{
				_escapeShipTargetDirection = EscapeShip.GameEntity.GetBodyWorldTransform().rotation.f.AsVec2.Normalized();
			}
			EscapeShip.ShipOrder.SetShipMovementOrder(_currentPhase2EscapeShipTargetPoint.GlobalPosition.AsVec2);
			EscapeShip.ShipOrder.SetOrderOarsmenLevel(2);
		}
		else
		{
			Vec3 globalPosition = _currentPhase2EscapeShipTargetPoint.GlobalPosition;
			MatrixFrame bodyWorldTransform = EscapeShip.GameEntity.GetBodyWorldTransform();
			if (globalPosition.NearlyEquals(in bodyWorldTransform.origin, 35f))
			{
				_currentPhase2EscapeShipTargetPoint = null;
			}
		}
		if (_currentPhase2EscapeShipTargetPoint != null)
		{
			EscapeShip.ShipOrder.SetOrderOarsmenLevel(2);
		}
	}

	private void HandleEscapeShipSpeed()
	{
		if (State == Quest5SetPieceBattleMissionState.Phase2InProgress)
		{
			AdjustWindDirectionAccordingToTargetFrame(EscapeShip.GlobalFrame, 1f);
			_escapeShipTargetSpeed = (GetIsThereActiveBridgeToBetweenEscapeShipAndAnyPirateShips() ? 2.7f : 5f);
		}
	}

	private void HandlePirateShipGettingCloseToEscapeShip(MissionShip pirateShip, GameEntity finalTargetFrameEntity, float gettingCloseSpeed, float fixedDt)
	{
		if (_navalAgentsLogic.GetActiveAgentCountOfShip(pirateShip) > 0 && _isPirateShipMovingToTheEscapeShip[pirateShip])
		{
			MatrixFrame globalFrameImpreciseForFixedTick = finalTargetFrameEntity.GetGlobalFrameImpreciseForFixedTick();
			MatrixFrame bodyWorldTransform = pirateShip.GameEntity.GetBodyWorldTransform();
			Vec2 asVec = bodyWorldTransform.origin.AsVec2;
			Vec2 vec = globalFrameImpreciseForFixedTick.origin.AsVec2 - asVec;
			float length = vec.Length;
			Vec2 asVec2 = EscapeShip.Physics.LinearVelocity.AsVec2;
			float num = ((length > 1E-06f) ? TaleWorlds.Library.MathF.Min(gettingCloseSpeed, length / fixedDt) : 0f);
			Vec2 vec2 = ((length > 1E-06f) ? (vec / length) : new Vec2(1f, 0f));
			Vec2 vec3 = asVec2 + vec2 * num;
			if (_limitPirateShipChasingSpeed[pirateShip])
			{
				vec3.ClampMagnitude(0f, vec3.Length * 0.5f);
			}
			Vec2 targetPosition = asVec + vec3 * fixedDt;
			Vec2 v = ((vec3.Length > 1E-06f) ? vec3.Normalized() : bodyWorldTransform.rotation.f.AsVec2.Normalized());
			float alpha = 1f - TaleWorlds.Library.MathF.Min(length, 200f) / 200f;
			Vec2 zero = Vec2.Zero;
			pirateShip.MoveShipToTheTargetWithDirection(targetDirection: (!(length <= 4f)) ? Vec2.Lerp(v, globalFrameImpreciseForFixedTick.rotation.f.AsVec2.Normalized(), alpha) : globalFrameImpreciseForFixedTick.rotation.f.AsVec2.Normalized(), currentFrame: bodyWorldTransform, targetPosition: targetPosition, maxAcceleration: 5f, maxAngularAcceleration: 2.5f, fixedDt: fixedDt);
		}
	}

	private void HandlePirateShipMovement(MissionShip pirateShip, GameEntity finalTargetFrameEntity)
	{
		if (_navalAgentsLogic.GetActiveAgentCountOfShip(pirateShip) <= 0)
		{
			return;
		}
		pirateShip.ShipOrder.SetCutLoose(enable: false);
		if (_isPirateShipMovingToTheEscapeShip[pirateShip])
		{
			if (pirateShip.GlobalFrame.origin.Distance(finalTargetFrameEntity.GetGlobalFrame().origin) <= 60f)
			{
				pirateShip.Formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
				pirateShip.Formation.SetTargetFormation(Agent.Main.Formation);
				pirateShip.ShipOrder.SetShipEngageOrder(EscapeShip);
				pirateShip.ShipOrder.SetBoardingTargetShip(EscapeShip);
			}
			if (pirateShip.GameEntity.GetBodyWorldTransform().origin.AsVec2.DistanceSquared(finalTargetFrameEntity.GetGlobalFrame().origin.AsVec2) <= 2f)
			{
				if (_pirateShipEnabledAttachmentMachine[pirateShip] == null)
				{
					ShipAttachmentMachine shipAttachmentMachine = null;
					float num = -1f;
					foreach (ShipAttachmentMachine attachmentMachine in pirateShip.AttachmentMachines)
					{
						if (!(Vec3.DotProduct(attachmentMachine.GameEntity.GetGlobalFrame().rotation.f, EscapeShip.GameEntity.GetBodyWorldTransform().origin - attachmentMachine.GameEntity.GetGlobalFrame().origin) > 0f))
						{
							continue;
						}
						foreach (ShipAttachmentPointMachine attachmentPointMachine in EscapeShip.AttachmentPointMachines)
						{
							float num2 = ShipAttachmentMachine.ComputePotentialAttachmentValue(attachmentMachine, attachmentPointMachine, checkInteractionDistance: true, checkConnectionBlock: true, allowWiderAngleBetweenConnections: false);
							if (num2 > num)
							{
								num = num2;
								shipAttachmentMachine = attachmentMachine;
							}
						}
					}
					if (shipAttachmentMachine != null)
					{
						_pirateShipEnabledAttachmentMachine[pirateShip] = shipAttachmentMachine;
					}
				}
				else
				{
					_pirateShipEnabledAttachmentMachine[pirateShip].SetEnabled(isParentObject: true);
					_pirateShipEnabledAttachmentMachine[pirateShip].SetIsDisabledForAI(isDisabledForAI: false);
				}
				return;
			}
			pirateShip.Formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
			pirateShip.Formation.SetTargetFormation(Agent.Main.Formation);
			pirateShip.ShipOrder.SetShipEngageOrder(EscapeShip);
			{
				foreach (ShipAttachmentMachine attachmentMachine2 in pirateShip.AttachmentMachines)
				{
					if (attachmentMachine2.CurrentAttachment == null)
					{
						if (attachmentMachine2.PilotAgent != null)
						{
							attachmentMachine2.PilotAgent.StopUsingGameObject();
						}
						attachmentMachine2.SetDisabled(isParentObject: true);
					}
					else if (attachmentMachine2.CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.RopesPulling || attachmentMachine2.CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.RopeThrown)
					{
						attachmentMachine2.CurrentAttachment.SetAttachmentState(ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval);
						if (attachmentMachine2.PilotAgent != null)
						{
							attachmentMachine2.PilotAgent.StopUsingGameObject();
						}
						attachmentMachine2.SetDisabled(isParentObject: true);
					}
				}
				return;
			}
		}
		if (_isPirateShipTriggered[pirateShip])
		{
			float num3 = pirateShip.GameEntity.GetBodyWorldTransform().origin.Distance(_pirateShipTriggerPoints[pirateShip].GlobalPosition);
			float num4 = pirateShip.GlobalFrame.origin.Distance(EscapeShip.GlobalFrame.origin);
			if (num3 <= 40f || num4 < 40f)
			{
				pirateShip.ShipOrder.SetShipEngageOrder(EscapeShip);
				pirateShip.Formation.SetTargetFormation(EscapeShip.Formation);
				foreach (ShipAttachmentMachine attachmentMachine3 in pirateShip.AttachmentMachines)
				{
					if (attachmentMachine3.PilotAgent != null)
					{
						attachmentMachine3.PilotAgent.StopUsingGameObject();
					}
					attachmentMachine3.SetDisabled(isParentObject: true);
				}
				foreach (ShipAttachmentPointMachine attachmentPointMachine2 in pirateShip.AttachmentPointMachines)
				{
					if (attachmentPointMachine2.PilotAgent != null)
					{
						attachmentPointMachine2.PilotAgent.StopUsingGameObject();
					}
					attachmentPointMachine2.SetDisabled();
					foreach (StandingPoint standingPoint in attachmentPointMachine2.StandingPoints)
					{
						standingPoint.SetDisabled();
					}
				}
				_isPirateShipMovingToTheEscapeShip[pirateShip] = true;
			}
			else
			{
				pirateShip.SetShipOrderActive(isOrderActive: true);
				pirateShip.ShipOrder.SetShipMovementOrder(_pirateShipTriggerPoints[pirateShip].GlobalPosition.AsVec2);
			}
			return;
		}
		if (_isPirateShipLostItsCrew[pirateShip])
		{
			_isPirateShipMovementDisabled[pirateShip] = true;
			_isPirateShipTriggered[pirateShip] = false;
			_isPirateShipMovingToTheEscapeShip[pirateShip] = false;
			pirateShip.SetAnchor(isAnchored: true);
			TargetMainAgentFormation(pirateShip);
			foreach (ShipAttachmentMachine attachmentMachine4 in pirateShip.AttachmentMachines)
			{
				attachmentMachine4.SetDisabled(isParentObject: true);
			}
			{
				foreach (ShipAttachmentPointMachine attachmentPointMachine3 in pirateShip.AttachmentPointMachines)
				{
					attachmentPointMachine3.SetDisabled();
					foreach (StandingPoint standingPoint2 in attachmentPointMachine3.StandingPoints)
					{
						standingPoint2.SetDisabled();
					}
				}
				return;
			}
		}
		if (_isPirateShipMovementDisabled[pirateShip])
		{
			pirateShip.SetAnchor(isAnchored: true);
			pirateShip.ShipOrder.SetShipStopOrder();
			pirateShip.Formation.SetMovementOrder(MovementOrder.MovementOrderStop);
			foreach (ShipAttachmentMachine attachmentMachine5 in pirateShip.AttachmentMachines)
			{
				attachmentMachine5.SetDisabled(isParentObject: true);
			}
			{
				foreach (ShipAttachmentPointMachine attachmentPointMachine4 in pirateShip.AttachmentPointMachines)
				{
					attachmentPointMachine4.SetDisabled();
					foreach (StandingPoint standingPoint3 in attachmentPointMachine4.StandingPoints)
					{
						standingPoint3.SetDisabled();
					}
				}
				return;
			}
		}
		if (_pirateShipTriggerPoints[pirateShip].GlobalPosition.Distance(EscapeShip.GlobalFrame.origin) < 170f)
		{
			_isPirateShipTriggered[pirateShip] = true;
			pirateShip.SetController(ShipControllerType.None);
			pirateShip.SetAnchor(isAnchored: false);
			pirateShip.SetShipOrderActive(isOrderActive: true);
			pirateShip.ShipOrder.SetShipMovementOrder(_pirateShipTriggerPoints[pirateShip].GlobalPosition.AsVec2);
			if (_instructionState == Quest5InstructionState.WaitForEscapeQuietly)
			{
				_instructionState = Quest5InstructionState.EscapeQuietly;
			}
		}
		else
		{
			pirateShip.SetAnchor(isAnchored: true);
			pirateShip.ShipOrder.SetShipStopOrder();
			pirateShip.SetShipOrderActive(isOrderActive: false);
		}
	}

	private void TargetMainAgentFormation(MissionShip ship)
	{
		ship.Formation.SetTargetFormation(Agent.Main.Formation);
		ship.Formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
	}

	private void HandlePirateShipSailModeAccordingToTheGlobalWindVelocity(MissionShip ship)
	{
		if (_navalAgentsLogic.GetActiveAgentCountOfShip(ship) <= 0)
		{
			ship.SetAnchor(isAnchored: true);
			return;
		}
		if (EscapeShip.GlobalFrame.origin.Distance(ship.GlobalFrame.origin) < 40f)
		{
			ship.SetCustomSailSetting(enableCustomSailSetting: true, SailInput.Raised);
			return;
		}
		Vec2 va = ship.GlobalFrame.rotation.f.AsVec2.Normalized();
		Vec2 vb = base.Mission.Scene.GetGlobalWindVelocity().Normalized();
		float num = TaleWorlds.Library.MathF.Abs(Vec2.DotProduct(va, vb));
		ship.SetCustomSailSetting(enableCustomSailSetting: true, (num > 0.75f) ? SailInput.Full : SailInput.Raised);
	}

	private bool GetIsThereActiveBridgeToBetweenEscapeShipAndAnyPirateShips()
	{
		if (!EscapeShip.GetIsThereActiveBridgeTo(_phase2EnemyShip1) && !EscapeShip.GetIsThereActiveBridgeTo(_phase2EnemyShip2) && !EscapeShip.GetIsThereActiveBridgeTo(_phase2EnemyShip3) && !EscapeShip.GetIsThereActiveBridgeTo(_phase2EnemyShip4))
		{
			return EscapeShip.GetIsThereActiveBridgeTo(_phase2EnemyShip5);
		}
		return true;
	}

	private void HandleStationaryShipMovement(MissionShip stationaryShip)
	{
		stationaryShip.SetAnchor(isAnchored: true);
		stationaryShip.ShipOrder.SetShipStopOrder();
		stationaryShip.SetShipOrderActive(isOrderActive: false);
		stationaryShip.SetCustomSailSetting(enableCustomSailSetting: true, SailInput.Raised);
		foreach (Agent item in _navalAgentsLogic.GetActiveAgentsOfShip(stationaryShip))
		{
			if (item.IsUsingGameObject)
			{
				item.StopUsingGameObject();
			}
		}
		stationaryShip.Formation.SetTargetFormation(_playerFormation);
	}

	private void AutoEstablishConnectionsForPirateShips(MissionShip ship, GameEntity finalTargetFrameEntity)
	{
		if (_isPirateShipMovementDisabled[ship] || !_isPirateShipMovingToTheEscapeShip[ship])
		{
			return;
		}
		if (_autoEstablishConnectionsForPirateShips[ship] == null)
		{
			if (!EscapeShip.GetIsThereActiveBridgeTo(ship) && ship.GameEntity.GetBodyWorldTransform().origin.AsVec2.DistanceSquared(finalTargetFrameEntity.GetGlobalFrame().origin.AsVec2) <= 2f)
			{
				_autoEstablishConnectionsForPirateShips[ship] = new MissionTimer(7f);
			}
		}
		else if (_autoEstablishConnectionsForPirateShips[ship].Check() && !EscapeShip.GetIsThereActiveBridgeTo(ship))
		{
			EscapeShip.TryToConnectionToAttachmentMachine(_pirateShipEnabledAttachmentMachine[ship]);
			_autoEstablishConnectionsForPirateShips[ship] = null;
		}
	}

	private void AutoCutLooseEmptyPirateShipIfPlayerDoesNotForALongTime(MissionShip ship)
	{
		if (_autoCutLooseTimersForPirateShips[ship] == null)
		{
			if (EscapeShip.GetIsThereActiveBridgeTo(ship))
			{
				_autoCutLooseTimersForPirateShips[ship] = new MissionTimer(25f);
			}
		}
		else
		{
			if (!_autoCutLooseTimersForPirateShips[ship].Check())
			{
				return;
			}
			_isPirateShipLostItsCrew[ship] = true;
			_isPirateShipMovingToTheEscapeShip[ship] = false;
			_isPirateShipTriggered[ship] = false;
			_isPirateShipMovementDisabled[ship] = true;
			TargetMainAgentFormation(ship);
			foreach (ShipAttachmentMachine attachmentMachine in ship.AttachmentMachines)
			{
				if (attachmentMachine.CurrentAttachment != null)
				{
					attachmentMachine.CurrentAttachment.SetAttachmentState(ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval);
				}
			}
			foreach (ShipAttachmentPointMachine attachmentPointMachine in ship.AttachmentPointMachines)
			{
				if (attachmentPointMachine.CurrentAttachment != null)
				{
					attachmentPointMachine.CurrentAttachment.SetAttachmentState(ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval);
				}
			}
			_autoCutLooseTimersForPirateShips[ship] = null;
		}
	}

	private void SetShipAttachmentJointPhysicsEnabledForShip(MissionShip ship, bool enabled)
	{
		foreach (ShipAttachmentMachine attachmentMachine in ship.AttachmentMachines)
		{
			attachmentMachine.SetShipAttachmentJointPhysicsEnabled(enabled);
		}
	}

	private void SetDisableShipAttachmentMachinesForPlayer(MissionShip ship, bool isDisabled)
	{
		foreach (ShipAttachmentMachine attachmentMachine in ship.AttachmentMachines)
		{
			if (isDisabled)
			{
				attachmentMachine.SetDisabled();
			}
			else
			{
				attachmentMachine.SetEnabled();
			}
		}
	}

	private void OnAttachmentBroken(ShipAttachmentMachine attachmentMachine, ShipAttachmentPointMachine attachmentPointMachine)
	{
		MissionShip ownerShip = attachmentMachine.OwnerShip;
		if (ownerShip == EscapeShip || attachmentPointMachine == null || attachmentPointMachine.PilotAgent == null || attachmentPointMachine.PilotAgent != Agent.Main || !_isPirateShipMovingToTheEscapeShip.TryGetValue(ownerShip, out var _))
		{
			return;
		}
		_isPirateShipMovingToTheEscapeShip[ownerShip] = false;
		_isPirateShipLostItsCrew[ownerShip] = true;
		TargetMainAgentFormation(ownerShip);
		foreach (ShipAttachmentMachine attachmentMachine2 in ownerShip.AttachmentMachines)
		{
			_ = attachmentMachine2;
			if (attachmentMachine.CurrentAttachment != null)
			{
				attachmentMachine.CurrentAttachment.SetAttachmentState(ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval);
			}
			attachmentMachine.SetDisabled(isParentObject: true);
		}
		ownerShip.ShipControllerMachine.SetDisabled(isParentObject: true);
		foreach (ShipOarMachine leftSideShipOarMachine in ownerShip.LeftSideShipOarMachines)
		{
			leftSideShipOarMachine.SetDisabled(isParentObject: true);
		}
		foreach (ShipOarMachine rightSideShipOarMachine in ownerShip.RightSideShipOarMachines)
		{
			rightSideShipOarMachine.SetDisabled(isParentObject: true);
		}
	}

	private void HandleAllyShipMovementDuringPhase2(MissionShip ship)
	{
		ship.SetAnchor(isAnchored: true);
		ship.ShipOrder.SetShipStopOrder();
		ship.SetController(ShipControllerType.None);
	}

	private void HandlePirateShipBridgeConnectionCount(MissionShip pirateShip)
	{
		if (EscapeShip.GetIsThereActiveBridgeTo(pirateShip))
		{
			if (!_isMissionShipBoardedToTheEscapeShip[pirateShip])
			{
				_isMissionShipBoardedToTheEscapeShip[pirateShip] = true;
				MBInformationManager.DialogNotificationHandle item = CampaignInformationManager.AddDialogLine(new TextObject("{=s3PsXlsG}They've grappled us!"), NavalStorylineData.Gunnar.CharacterObject, null, 0, MBInformationManager.NotificationPriority.High);
				_dialogNotificationHandleCache.Add(item);
			}
			bool flag = true;
			pirateShip.Formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
			foreach (Agent item3 in _navalAgentsLogic.GetActiveAgentsOfShip(pirateShip))
			{
				item3.SetAutomaticTargetSelection(enable: false);
				item3.SetTargetAgent(Agent.Main);
				flag = flag && EscapeShip.GetIsAgentOnShip(item3);
			}
			if (!flag || !Agent.Main.IsActive() || !EscapeShip.GetIsAgentOnShip(Agent.Main))
			{
				return;
			}
			MBInformationManager.DialogNotificationHandle item2 = CampaignInformationManager.AddDialogLine(new TextObject("{=RUavLWSF}They're on deck!"), NavalStorylineData.Gunnar.CharacterObject, null, 0, MBInformationManager.NotificationPriority.High);
			_dialogNotificationHandleCache.Add(item2);
			_isPirateShipMovingToTheEscapeShip[pirateShip] = false;
			_isPirateShipLostItsCrew[pirateShip] = true;
			_isPirateShipTriggered[pirateShip] = false;
			pirateShip.SetAnchor(isAnchored: true);
			TargetMainAgentFormation(pirateShip);
			foreach (ShipAttachmentMachine attachmentMachine in pirateShip.AttachmentMachines)
			{
				if (attachmentMachine.CurrentAttachment != null)
				{
					attachmentMachine.CurrentAttachment.SetAttachmentState(ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval);
				}
				attachmentMachine.SetDisabled(isParentObject: true);
			}
			pirateShip.ShipControllerMachine.SetDisabled(isParentObject: true);
			foreach (ShipOarMachine leftSideShipOarMachine in pirateShip.LeftSideShipOarMachines)
			{
				leftSideShipOarMachine.SetDisabled(isParentObject: true);
			}
			{
				foreach (ShipOarMachine rightSideShipOarMachine in pirateShip.RightSideShipOarMachines)
				{
					rightSideShipOarMachine.SetDisabled(isParentObject: true);
				}
				return;
			}
		}
		if (_isMissionShipBoardedToTheEscapeShip[pirateShip])
		{
			_isMissionShipBoardedToTheEscapeShip[pirateShip] = false;
		}
	}

	private bool AreAllPhase2PirateShipsEliminated()
	{
		if (_phase2EnemyShip1.Formation.CountOfUnits <= 0 && _phase2EnemyShip2.Formation.CountOfUnits <= 0 && _phase2EnemyShip3.Formation.CountOfUnits <= 0 && _phase2EnemyShip4.Formation.CountOfUnits <= 0)
		{
			return _phase2EnemyShip5.Formation.CountOfUnits <= 0;
		}
		return false;
	}

	private void HandlePlayersBridgeAndControlPointUsagesForPhase2InProgress()
	{
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			foreach (ShipAttachmentMachine attachmentMachine in allShip.AttachmentMachines)
			{
				foreach (StandingPoint standingPoint in attachmentMachine.StandingPoints)
				{
					standingPoint.IsDisabledForPlayers = false;
				}
			}
			foreach (ShipAttachmentPointMachine attachmentPointMachine in allShip.AttachmentPointMachines)
			{
				foreach (StandingPoint standingPoint2 in attachmentPointMachine.StandingPoints)
				{
					standingPoint2.IsDisabledForPlayers = false;
				}
			}
		}
	}

	private void CheckForEscapeShipStuck()
	{
		if (!CheckIfThereIsAnActiveAgentOfShip(_phase2EnemyShip1) || !CheckIfThereIsAnActiveAgentOfShip(_phase2EnemyShip2) || !CheckIfThereIsAnActiveAgentOfShip(_phase2EnemyShip3) || !CheckIfThereIsAnActiveAgentOfShip(_phase2EnemyShip4) || !CheckIfThereIsAnActiveAgentOfShip(_phase2EnemyShip5))
		{
			return;
		}
		if (_phase2EscapeShipStuckTimer == null)
		{
			_phase2EscapeShipStuckTimer = new MissionTimer(10f);
			_phase2EscapeShipStuckCheckPosition = EscapeShip.GlobalFrame.origin;
		}
		else if (_phase2EscapeShipStuckTimer.Check())
		{
			if (EscapeShip.GlobalFrame.origin.NearlyEquals(in _phase2EscapeShipStuckCheckPosition, 3f))
			{
				IsEscapeShipStuck = true;
				return;
			}
			_phase2EscapeShipStuckTimer = null;
			_phase2EscapeShipStuckCheckPosition = Vec3.Invalid;
		}
	}

	private bool CheckIfThereIsAnActiveAgentOfShip(MissionShip ship)
	{
		if (ship != null && _isPirateShipTriggered.ContainsKey(ship) && _isPirateShipTriggered[ship] && _navalAgentsLogic.GetActiveAgentCountOfShip(ship) > 0)
		{
			return false;
		}
		return true;
	}

	public void HandleEscapeShipStuck()
	{
		IsEscapeShipStuck = false;
		_phase2EscapeShipStuckTimer = null;
		_phase2EscapeShipStuckCheckPosition = Vec3.Invalid;
		_navalShipsLogic.TeleportShip(EscapeShip, _currentPhase2EscapeShipTargetPoint.GetGlobalFrame(), checkFreeArea: true);
	}

	private void MoveEscapeShipAlongTheTrack(float fixedDt)
	{
		if (_escapeShipSpeed != 0f)
		{
			Vec2 vec = _escapeShipDirection * _escapeShipSpeed * fixedDt;
			MatrixFrame bodyWorldTransform = EscapeShip.GameEntity.GetBodyWorldTransform();
			Vec2 targetPosition = bodyWorldTransform.origin.AsVec2 + vec;
			EscapeShip.MoveShipToTheTargetWithDirection(bodyWorldTransform, targetPosition, _escapeShipDirection, 100f, 2.5f, fixedDt);
		}
	}

	private void UpdatePhase2MovingShipParameters(float dt)
	{
		_escapeShipSpeed = TaleWorlds.Library.MathF.Lerp(_escapeShipSpeed, _escapeShipTargetSpeed, dt * 0.25f);
		_escapeShipDirection = Vec2.Slerp(_escapeShipDirection, _escapeShipTargetDirection, dt * 0.15f);
	}

	private void ModifyMainAgentEquipmentForPhase2()
	{
		ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>("nord_shield_tier_2_d");
		Equipment equipment = Agent.Main.SpawnEquipment.Clone();
		for (int i = 0; i < 12; i++)
		{
			ItemObject item2 = equipment[i].Item;
			if (item2 != null && item2.StringId.Equals("Broad_Skaen"))
			{
				equipment[i] = new EquipmentElement(item);
				break;
			}
		}
		Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(equipment);
	}

	public void TriggerInitializePhase3()
	{
		State = Quest5SetPieceBattleMissionState.InitializePhase3Part1;
	}

	public void CompletePhase2ToPhase3Transition()
	{
		State = Quest5SetPieceBattleMissionState.Phase3InProgress;
	}

	private void InitializePhase3Part1()
	{
		_gunnarMovementState = GunnarMovementState.End;
		if (_phase2EnemyShip1 != null)
		{
			_phase2EnemyShip1.BreakAllExistingConnections();
			AddAvailableEnemyFormation(_phase2EnemyShip1.Formation);
			RemoveShipInternal(_phase2EnemyShip1);
			_phase2EnemyShip1 = null;
		}
		else
		{
			_isCheckpointInitialize = true;
		}
		if (_isCheckpointInitialize)
		{
			TeamAINavalComponent teamAI = new TeamAINavalComponent(base.Mission, base.Mission.AttackerTeam, 5f, 1f);
			base.Mission.AttackerTeam.AddTeamAI(teamAI);
			base.Mission.AttackerTeam.AddTacticOption(new TacticNavalBalancedOffense(base.Mission.AttackerTeam));
			TeamAINavalComponent teamAI2 = new TeamAINavalComponent(base.Mission, base.Mission.DefenderTeam, 5f, 1f);
			base.Mission.DefenderTeam.AddTeamAI(teamAI2);
			base.Mission.DefenderTeam.AddTacticOption(new TacticNavalBalancedOffense(base.Mission.DefenderTeam));
		}
		if (_phase2EnemyShip2 != null)
		{
			_phase2EnemyShip2.BreakAllExistingConnections();
			AddAvailableEnemyFormation(_phase2EnemyShip2.Formation);
			RemoveShipInternal(_phase2EnemyShip2);
			_phase2EnemyShip2 = null;
		}
		else
		{
			_isCheckpointInitialize = true;
		}
		if (_phase2EnemyShip3 != null)
		{
			_phase2EnemyShip3.BreakAllExistingConnections();
			AddAvailableEnemyFormation(_phase2EnemyShip3.Formation);
			RemoveShipInternal(_phase2EnemyShip3);
			_phase2EnemyShip3 = null;
		}
		else
		{
			_isCheckpointInitialize = true;
		}
		if (_phase2EnemyShip4 != null)
		{
			_phase2EnemyShip4.BreakAllExistingConnections();
			AddAvailableEnemyFormation(_phase2EnemyShip4.Formation);
			RemoveShipInternal(_phase2EnemyShip4);
			_phase2EnemyShip4 = null;
		}
		else
		{
			_isCheckpointInitialize = true;
		}
		if (_phase2EnemyShip5 != null)
		{
			_phase2EnemyShip5.BreakAllExistingConnections();
			AddAvailableEnemyFormation(_phase2EnemyShip5.Formation);
			RemoveShipInternal(_phase2EnemyShip5);
			_phase2EnemyShip5 = null;
		}
		else
		{
			_isCheckpointInitialize = true;
		}
		if (_phase1EnemyShip2 != null)
		{
			_phase1EnemyShip2.BreakAllExistingConnections();
			AddAvailableEnemyFormation(_phase1EnemyShip2.Formation);
			RemoveShipInternal(_phase1EnemyShip2);
		}
		if (_phase1EnemyShip4 != null)
		{
			_phase1EnemyShip4.BreakAllExistingConnections();
			AddAvailableEnemyFormation(_phase1EnemyShip4.Formation);
			RemoveShipInternal(_phase1EnemyShip4);
		}
		if (_phase2EnemyShipStationary1 != null)
		{
			_phase2EnemyShipStationary1.BreakAllExistingConnections();
			AddAvailableEnemyFormation(_phase2EnemyShipStationary1.Formation);
			RemoveShipInternal(_phase2EnemyShipStationary1);
		}
		_phase3EnemyShip1 = CreateShip("eastern_heavy_ship", "phase_3_enemy_ship_1_sp", GetAvailableEnemyFormation(), spawnAnchored: false, _phase3EnemyShip1UpgradePieceList);
		_phase3EnemyShip2 = CreateShip("aserai_heavy_ship", "phase_3_enemy_ship_2_sp", GetAvailableEnemyFormation(), spawnAnchored: false, _phase3EnemyShip2UpgradePieceList);
		_phase3EnemyShip3 = CreateShip("nord_medium_ship", "phase_3_enemy_ship_3_sp", GetAvailableEnemyFormation(), spawnAnchored: false, _phase3EnemyShip3UpgradePieceList);
		_phase3EnemyShip4 = CreateShip("nord_medium_ship", "phase_3_enemy_ship_4_sp", GetAvailableEnemyFormation(), spawnAnchored: false, _phase3EnemyShip4UpgradePieceList);
		_phase3EnemyShip5 = CreateShip("khuzait_heavy_ship", "phase_3_enemy_ship_5_sp", GetAvailableEnemyFormation(), spawnAnchored: false, _phase3EnemyShip5UpgradePieceList);
		_phase3EnemyShip1.SetCanBeTakenOver(value: false);
		_phase3EnemyShip2.SetCanBeTakenOver(value: false);
		_phase3EnemyShip3.SetCanBeTakenOver(value: false);
		_phase3EnemyShip4.SetCanBeTakenOver(value: false);
		_phase3EnemyShip5.SetCanBeTakenOver(value: false);
		if (_phase2AllyShip1 != null)
		{
			GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("phase_3_ally_ship_1_sp");
			_navalShipsLogic.TeleportShip(_phase2AllyShip1, gameEntity.GetGlobalFrame(), checkFreeArea: true);
		}
		else
		{
			_phase2AllyShip1 = CreateShip("aserai_heavy_ship", "phase_3_ally_ship_1_sp", GetAvailableAllyFormation(), spawnAnchored: false, _phase2AllyShip1UpgradePieceList);
		}
		if (_phase2AllyShip2 != null)
		{
			GameEntity gameEntity2 = Mission.Current.Scene.FindEntityWithTag("phase_3_ally_ship_2_sp");
			_navalShipsLogic.TeleportShip(_phase2AllyShip2, gameEntity2.GetGlobalFrame(), checkFreeArea: true);
		}
		else
		{
			_phase2AllyShip2 = CreateShip("nord_medium_ship", "phase_3_ally_ship_2_sp", GetAvailableAllyFormation(), spawnAnchored: false, _phase2AllyShip2UpgradePieceList);
		}
		if (_phase2AllyShip3 != null)
		{
			GameEntity gameEntity3 = Mission.Current.Scene.FindEntityWithTag("phase_3_ally_ship_3_sp");
			_navalShipsLogic.TeleportShip(_phase2AllyShip3, gameEntity3.GetGlobalFrame(), checkFreeArea: true);
		}
		else
		{
			_phase2AllyShip3 = CreateShip("northern_medium_ship", "phase_3_ally_ship_3_sp", GetAvailableAllyFormation(), spawnAnchored: false, _phase2AllyShip3UpgradePieceList);
		}
		if (_phase2AllyShip4 != null)
		{
			GameEntity gameEntity4 = Mission.Current.Scene.FindEntityWithTag("phase_3_ally_ship_4_sp");
			_navalShipsLogic.TeleportShip(_phase2AllyShip4, gameEntity4.GetGlobalFrame(), checkFreeArea: true);
		}
		else
		{
			_phase2AllyShip4 = CreateShip("sturgia_heavy_ship", "phase_3_ally_ship_4_sp", GetAvailableAllyFormation(), spawnAnchored: false, _phase2AllyShip4UpgradePieceList);
		}
		if (_phase2AllyShip5 != null)
		{
			GameEntity gameEntity5 = Mission.Current.Scene.FindEntityWithTag("phase_3_ally_ship_5_sp");
			_navalShipsLogic.TeleportShip(_phase2AllyShip5, gameEntity5.GetGlobalFrame(), checkFreeArea: true);
		}
		else
		{
			_phase2AllyShip5 = CreateShip("northern_medium_ship", "phase_3_ally_ship_5_sp", GetAvailableAllyFormation(), spawnAnchored: false, _phase2AllyShip5UpgradePieceList);
		}
		_navalTrajectoryPlanningLogic.ForceReinitialize();
		foreach (MBInformationManager.DialogNotificationHandle item in _dialogNotificationHandleCache)
		{
			CampaignInformationManager.ClearDialogNotification(item);
		}
		_dialogNotificationHandleCache.Clear();
		if (!_isCheckpointInitialize)
		{
			_lightScriptedFiresMissionController.PutOutFires();
		}
	}

	private void InitializePhase3Part2()
	{
		Mission.Current.Scene.SetAtmosphereWithName("TOD_naval_05_30_sunset");
		if (_playerShip != null)
		{
			foreach (ShipAttachmentMachine attachmentMachine in _playerShip.AttachmentMachines)
			{
				if (attachmentMachine.CurrentAttachment != null)
				{
					attachmentMachine.CurrentAttachment.SetAttachmentState(ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval);
				}
			}
			if (Agent.Main.IsUsingGameObject)
			{
				Agent.Main.StopUsingGameObject();
			}
			_navalAgentsLogic.TransferAgentToShip(Agent.Main, _phase2AllyShip1);
			Agent gunnarAgent = _gunnarAgent;
			if (gunnarAgent != null && gunnarAgent.IsActive())
			{
				_gunnarAgent.Controller = AgentControllerType.AI;
				_navalAgentsLogic.TransferAgentToShip(_gunnarAgent, _phase2AllyShip1);
			}
			if (_playerShip != null)
			{
				if (_gunnarAgent.IsUsingGameObject)
				{
					_gunnarAgent.Controller = AgentControllerType.AI;
					_gunnarAgent.StopUsingGameObject();
				}
				RemoveShipInternal(_playerShip);
			}
			if (_phase1EnemyShip3 != null && _phase1EnemyShip3.Team != null)
			{
				Agent gunnarAgent2 = _gunnarAgent;
				if (gunnarAgent2 != null && gunnarAgent2.IsUsingGameObject)
				{
					_gunnarAgent.StopUsingGameObjectMT();
				}
				_navalAgentsLogic.UnassignCaptainOfShip(_phase1EnemyShip3);
				RemoveShipInternal(_phase1EnemyShip3);
			}
			_navalShipsLogic.SetDeploymentMode(value: true);
			_navalAgentsLogic.SetDeploymentMode(value: true);
			_playerShip = CreateShip("empire_heavy_ship", "phase_3_player_ship_sp", _playerFormation, spawnAnchored: false, _escapeShipUpgradePieceList, EscapeShipFigurehead);
			_navalShipsLogic.SetDeploymentMode(value: false);
			_navalAgentsLogic.SetDeploymentMode(value: false);
			_navalAgentsLogic.TransferAgentToShip(Agent.Main, _playerShip);
			Agent gunnarAgent3 = _gunnarAgent;
			if (gunnarAgent3 != null && gunnarAgent3.IsActive())
			{
				_navalAgentsLogic.TransferAgentToShip(_gunnarAgent, _playerShip);
			}
			_playerShip.ShipOrder.SetShipStopOrder();
			_playerShip.Formation.PlayerOwner = Agent.Main;
		}
		else
		{
			_playerShip = CreateShip("empire_heavy_ship", "phase_3_player_ship_sp", _playerFormation, spawnAnchored: false, _escapeShipUpgradePieceList, EscapeShipFigurehead);
			_navalAgentsLogic.SetDeploymentMode(value: true);
			_navalShipsLogic.SetDeploymentMode(value: true);
			_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, CharacterObject.PlayerCharacter), _playerShip);
			_navalAgentsLogic.SetDeploymentMode(value: false);
			_navalShipsLogic.SetDeploymentMode(value: false);
			_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.PlayerTeam);
			_playerShip.Formation.PlayerOwner = Agent.Main;
		}
		_navalTrajectoryPlanningLogic.ForceReinitialize();
		Agent.Main.TeleportToPosition(_playerShip.GetCaptainSpawnGlobalFrame().origin);
	}

	private void ClearPhase2OnPhaseTransition()
	{
		_phase2EnemyShip1 = null;
		_phase2EnemyShip2 = null;
		_phase2EnemyShip3 = null;
		_phase2EnemyShip4 = null;
		_phase2EnemyShip5 = null;
		_phase2EnemyShipStationary1 = null;
		_phase2EscapeShipPirateTargetFrame1 = null;
		_phase2EscapeShipPirateTargetFrame2 = null;
		_phase2EscapeShipPirateTargetFrame3 = null;
		_phase2EscapeShipPirateTargetFrame4 = null;
		_phase2EscapeShipPirateTargetFrame5 = null;
		_currentPhase2EscapeShipTargetPoint = null;
		_pirateShipTriggerPoints.Clear();
		_isPirateShipTriggered.Clear();
		_isPirateShipMovingToTheEscapeShip.Clear();
		_isPirateShipLostItsCrew.Clear();
		_limitPirateShipChasingSpeed.Clear();
		_autoCutLooseTimersForPirateShips.Clear();
		_isMissionShipBoardedToTheEscapeShip.Clear();
		_phase2EscapeShipTargetPointEntities.Clear();
		_phase2EscapeShipTargetPoints.Clear();
		_playerLeftTheEscapeShipTimer = null;
		_phase2EscapeShipStuckTimer = null;
		GC.Collect();
	}

	private void InitializePhase3Part3()
	{
		_navalAgentsLogic.AssignCaptainToShip(Agent.Main, _playerShip);
		_playerShip.Formation.PlayerOwner = Agent.Main;
		SpawnPhase3EnemyTroops();
		SpawnPhase3AllyTroops();
		Agent.Main.SetClothingColor1(4279111698u);
		Agent.Main.SetClothingColor2(4279111698u);
		Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(Hero.MainHero.BattleEquipment);
		_gunnarAgent.UpdateSpawnEquipmentAndRefreshVisuals(NavalStorylineData.Gunnar.CharacterObject.Equipment);
		_gunnarAgent.TeleportToPosition(_playerShip.GetCaptainSpawnGlobalFrame().origin);
		if (_isCheckpointInitialize)
		{
			base.Mission.OnInitialSpawnCompleted();
		}
		else
		{
			_phase3EnemyShip1.FinalizeDeployment(initializeMachines: true);
			_phase3EnemyShip2.FinalizeDeployment(initializeMachines: true);
			_phase3EnemyShip3.FinalizeDeployment(initializeMachines: true);
			_phase3EnemyShip4.FinalizeDeployment(initializeMachines: true);
			_phase3EnemyShip5.FinalizeDeployment(initializeMachines: true);
			_playerShip.FinalizeDeployment(initializeMachines: true);
			_navalTrajectoryPlanningLogic.ForceReinitialize();
		}
		TriggerShip(_phase3EnemyShip1);
		TriggerShip(_phase3EnemyShip2);
		TriggerShip(_phase3EnemyShip3);
		TriggerShip(_phase3EnemyShip4);
		TriggerShip(_phase3EnemyShip5);
		TriggerShip(_phase2AllyShip1);
		TriggerShip(_phase2AllyShip2);
		TriggerShip(_phase2AllyShip3);
		TriggerShip(_phase2AllyShip4);
		TriggerShip(_phase2AllyShip5);
		_gunnarAgent.Controller = AgentControllerType.AI;
		_instructionState = Quest5InstructionState.DefeatEnemies;
		State = (_isCheckpointInitialize ? Quest5SetPieceBattleMissionState.Phase3InProgress : Quest5SetPieceBattleMissionState.Phase2ToPhase3FadeIn);
		_playerShip.SetController(ShipControllerType.Player);
		HandlePlayersBridgeAndControlPointUsagesForPhase3InProgress();
		AdjustWindDirectionAccordingToTargetFrame(_playerShip.GlobalFrame, 3f);
		ShowStartNotifications();
		RemoveShipControlPointDescriptionOfAllEnemyShips();
		_phase3TotalEnemyCount = Phase3EnemyShip1TroopCount + Phase3EnemyShip2TroopCount + Phase3EnemyShip3TroopCount + Phase3EnemyShip4TroopCount + Phase3EnemyShip5TroopCount;
		foreach (Formation item in base.Mission.PlayerTeam.FormationsIncludingEmpty)
		{
			item.PlayerOwner = Agent.Main;
		}
		if (!_gunnarAgent.IsAlarmed())
		{
			_gunnarAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
		}
		if (!_isCheckpointInitialize)
		{
			ClearPhase2OnPhaseTransition();
		}
	}

	private void SpawnPhase3EnemyTroops()
	{
		_navalAgentsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic.SetDeploymentMode(value: true);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase3EnemyShip1, Phase3EnemyShip1TroopCount);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase3EnemyShip2, Phase3EnemyShip2TroopCount);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase3EnemyShip3, Phase3EnemyShip3TroopCount);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase3EnemyShip4, Phase3EnemyShip4TroopCount);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase3EnemyShip5, Phase3EnemyShip5TroopCount);
		AddMissionShipTroops(_phase3EnemyShip1Troops, _phase3EnemyShip1);
		AddMissionShipTroops(_phase3EnemyShip2Troops, _phase3EnemyShip2);
		AddMissionShipTroops(_phase3EnemyShip3Troops, _phase3EnemyShip3);
		AddMissionShipTroops(_phase3EnemyShip4Troops, _phase3EnemyShip4);
		AddMissionShipTroops(_phase3EnemyShip5Troops, _phase3EnemyShip5);
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.EnemyTeam);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase3EnemyShip1);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase3EnemyShip2);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase3EnemyShip3);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase3EnemyShip4);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase3EnemyShip5);
		_navalAgentsLogic.SetDeploymentMode(value: false);
		_navalShipsLogic.SetDeploymentMode(value: false);
	}

	private void SpawnPhase3AllyTroops()
	{
		_navalAgentsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic.SetDeploymentMode(value: true);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_playerShip, Phase3PlayerShipTroopCount + 2);
		AddMissionShipTroops(_phase3PlayerShipTroops, _playerShip, PartyBase.MainParty);
		if (_isCheckpointInitialize)
		{
			_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2AllyShip1, Phase2AllyShip1TroopCount + 2);
			_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2AllyShip2, Phase2AllyShip2TroopCount + 2);
			_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2AllyShip3, Phase2AllyShip3TroopCount + 2);
			_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2AllyShip4, Phase2AllyShip4TroopCount + 2);
			_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase2AllyShip5, Phase2AllyShip5TroopCount + 2);
			AddMissionShipTroops(_phase2AllyShip1Troops, _phase2AllyShip1, PartyBase.MainParty);
			AddMissionShipTroops(_phase2AllyShip2Troops, _phase2AllyShip2, PartyBase.MainParty);
			AddMissionShipTroops(_phase2AllyShip3Troops, _phase2AllyShip3, PartyBase.MainParty);
			AddMissionShipTroops(_phase2AllyShip4Troops, _phase2AllyShip4, PartyBase.MainParty);
			AddMissionShipTroops(_phase2AllyShip5Troops, _phase2AllyShip5, PartyBase.MainParty);
		}
		SpawnBjolgurOnShip(_phase2AllyShip2);
		SpawnLaharOnShip(_phase2AllyShip3);
		if (_gunnarAgent == null || !_gunnarAgent.IsActive())
		{
			SpawnGunnarOnShip(_playerShip);
		}
		_gunnarAgent.SetMortalityState(Agent.MortalityState.Immortal);
		_bjolgurAgent.SetMortalityState(Agent.MortalityState.Immortal);
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.PlayerTeam);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_playerShip);
		if (_isCheckpointInitialize)
		{
			_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2AllyShip1);
			_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2AllyShip2);
			_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2AllyShip3);
			_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2AllyShip4);
			_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase2AllyShip5);
		}
		_navalAgentsLogic.SetDeploymentMode(value: false);
		_navalShipsLogic.SetDeploymentMode(value: false);
	}

	private void CallReinforcement()
	{
		_isReinforcementCalled = true;
		_phase3EnemyReinforcementShip1 = CreateShip("empire_medium_ship", "phase_3_enemy_reinforcement_1_sp", GetAvailableEnemyFormation());
		_phase3EnemyReinforcementShip2 = CreateShip("nord_medium_ship", "phase_3_enemy_reinforcement_2_sp", GetAvailableEnemyFormation());
		_phase3EnemyReinforcementShip1.SetCanBeTakenOver(value: false);
		_phase3EnemyReinforcementShip2.SetCanBeTakenOver(value: false);
	}

	private void InitializeReinforcement()
	{
		_isReinforcementInitialized = true;
		_navalAgentsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic.SetDeploymentMode(value: true);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase3EnemyReinforcementShip1, Phase3EnemyReinforcementShip1TroopCount);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_phase3EnemyReinforcementShip2, Phase3EnemyReinforcementShip2TroopCount);
		_phase3TotalEnemyCount += Phase3EnemyReinforcementShip1TroopCount + Phase3EnemyReinforcementShip2TroopCount;
		AddMissionShipTroops(_phase3EnemyReinforcementShip1Troops, _phase3EnemyReinforcementShip1);
		AddMissionShipTroops(_phase3EnemyReinforcementShip2Troops, _phase3EnemyReinforcementShip2);
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.EnemyTeam);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase3EnemyReinforcementShip1);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_phase3EnemyReinforcementShip2);
		_navalAgentsLogic.SetDeploymentMode(value: false);
		_navalShipsLogic.SetDeploymentMode(value: false);
		_phase3EnemyReinforcementShip1.FinalizeDeployment(initializeMachines: true);
		_phase3EnemyReinforcementShip2.FinalizeDeployment(initializeMachines: true);
		_navalTrajectoryPlanningLogic.ForceReinitialize();
		base.Mission.PlayerEnemyTeam.MasterOrderController.SelectAllFormations();
		base.Mission.PlayerEnemyTeam.MasterOrderController.SetOrder(OrderType.Charge);
		MBInformationManager.DialogNotificationHandle item = CampaignInformationManager.AddDialogLine(new TextObject("{=jxQc5JVQ}Ah, gods - I see more of them coming up... No rest for my sword-arm today!"), NavalStorylineData.Gunnar.CharacterObject, null, 0, MBInformationManager.NotificationPriority.High);
		_dialogNotificationHandleCache.Add(item);
		_phase3EnemyReinforcementShip1.ShipOrder.SetShipEngageOrder();
		_phase3EnemyReinforcementShip2.ShipOrder.SetShipEngageOrder();
	}

	private bool CanProceedToPhase4()
	{
		MBReadOnlyList<Agent> activeAgents = base.Mission.PlayerEnemyTeam.ActiveAgents;
		bool flag = activeAgents.Count <= 0;
		if (!flag)
		{
			bool flag2 = true;
			foreach (Agent item in activeAgents)
			{
				if (item.Formation != null)
				{
					flag2 = false;
					break;
				}
			}
			flag = flag2;
		}
		return flag;
	}

	public void TriggerInitializePhase4()
	{
		if (Agent.Main.IsUsingGameObject)
		{
			Agent.Main.StopUsingGameObject();
		}
		State = Quest5SetPieceBattleMissionState.InitializePhase4Part1;
	}

	public void CompletePhase3ToPhase4Transition()
	{
		State = Quest5SetPieceBattleMissionState.Phase4InProgress;
	}

	private void ShowStartNotifications()
	{
		MBInformationManager.DialogNotificationHandle item = CampaignInformationManager.AddDialogLine(new TextObject("{=a1IqRXcx}Ahoy to you, Gunnar! An exemplary escape! Is the captive safe?"), NavalStorylineData.Lahar.CharacterObject, null, 0, MBInformationManager.NotificationPriority.High);
		_dialogNotificationHandleCache.Add(item);
		MBInformationManager.DialogNotificationHandle item2 = CampaignInformationManager.AddDialogLine(new TextObject("{=EdYmUbcM}You two snatched their ship right out from under their noses! A fine story to tell my brothers, if we survive this."), NavalStorylineData.Bjolgur.CharacterObject, null, 0, MBInformationManager.NotificationPriority.High);
		_dialogNotificationHandleCache.Add(item2);
		MBInformationManager.DialogNotificationHandle item3 = CampaignInformationManager.AddDialogLine(new TextObject("{=HgdLgYtA}Ahoy to you, Bjolgur! And ahoy to you, Lahar! She is indeed safe, with us. But now it looks like the whole pack of Hounds are coming baying out to meet us. You two brave fellows get on our flanks, and we'll meet them prow to prow"), NavalStorylineData.Gunnar.CharacterObject, null, 0, MBInformationManager.NotificationPriority.High);
		_dialogNotificationHandleCache.Add(item3);
	}

	private void ClearPhase4OnPhaseTransition()
	{
		if (_phase2AllyShip1 != null)
		{
			((Ship)_phase2AllyShip1.ShipOrigin).Owner = null;
		}
		if (_phase2AllyShip2 != null)
		{
			((Ship)_phase2AllyShip2.ShipOrigin).Owner = null;
		}
		if (_phase2AllyShip3 != null)
		{
			((Ship)_phase2AllyShip3.ShipOrigin).Owner = null;
		}
		if (_phase2AllyShip4 != null)
		{
			((Ship)_phase2AllyShip4.ShipOrigin).Owner = null;
		}
		if (_phase2AllyShip5 != null)
		{
			((Ship)_phase2AllyShip5.ShipOrigin).Owner = null;
		}
		_phase2AllyShip1 = null;
		_phase2AllyShip2 = null;
		_phase2AllyShip3 = null;
		_phase2AllyShip4 = null;
		_phase2AllyShip5 = null;
		_phase3EnemyShip1 = null;
		_phase3EnemyShip2 = null;
		_phase3EnemyShip3 = null;
		_phase3EnemyShip4 = null;
		_phase3EnemyShip5 = null;
		_phase3EnemyReinforcementShip1 = null;
		_phase3EnemyReinforcementShip2 = null;
		_phase3TriggerVolumeBox = null;
		_allyShipTargetKeysBuffer.Clear();
		_assignedEnemyShips.Clear();
		GC.Collect();
	}

	public void TriggerInitializeBossFight()
	{
		State = Quest5SetPieceBattleMissionState.InitializeBossFightPart1;
	}

	public void CompletePhase4ToBossFightTransition()
	{
		State = Quest5SetPieceBattleMissionState.StartBossFightConversation;
	}

	private void HandlePlayersBridgeAndControlPointUsagesForPhase3InProgress()
	{
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			foreach (ShipAttachmentMachine attachmentMachine in allShip.AttachmentMachines)
			{
				foreach (StandingPoint standingPoint in attachmentMachine.StandingPoints)
				{
					standingPoint.IsDisabledForPlayers = false;
				}
			}
			foreach (ShipAttachmentPointMachine attachmentPointMachine in allShip.AttachmentPointMachines)
			{
				foreach (StandingPoint standingPoint2 in attachmentPointMachine.StandingPoints)
				{
					standingPoint2.IsDisabledForPlayers = false;
				}
			}
		}
	}

	public void OnPurigCutsceneStarted()
	{
		_isPurigCutsceneStarted = true;
		_playerShip.ShipOrder.SetShipStopOrder();
		_playerShip.SetAnchor(isAnchored: true);
	}

	public void OnPurigShipCutsceneEnded()
	{
		_playerShip.SetAnchor(isAnchored: false);
		if (_isPlayerUsingShipAtTheStartOfThePurigCutscene)
		{
			Agent.Main.HandleStartUsingAction(_playerStandingPointAtTheStartOfThePurigCutscene, -1);
			_isPlayerUsingShipAtTheStartOfThePurigCutscene = false;
			_playerStandingPointAtTheStartOfThePurigCutscene = null;
		}
		_playerShip.ShipOrder.SetShipEngageOrder(Phase4PurigShip);
		Phase4PurigShip.ShipOrder.SetShipEngageOrder(_playerShip);
		_instructionState = Quest5InstructionState.DefeatPurigsShip;
	}

	private void CheckIfEnemyAgentFallIntoTheWater()
	{
		MBReadOnlyList<Agent> activeAgents = base.Mission.PlayerEnemyTeam.ActiveAgents;
		if (activeAgents.Count >= 10)
		{
			return;
		}
		for (int num = activeAgents.Count - 1; num >= 0; num--)
		{
			Agent agent = activeAgents[num];
			if (agent.IsInWater())
			{
				agent.FadeOut(hideInstantly: true, hideMount: false);
			}
		}
	}

	public void GetIntendedMainAgentDirectionForBossFight(out Vec3 direction)
	{
		GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("naval_boss_fight_player_sp");
		GameEntity gameEntity2 = Mission.Current.Scene.FindEntityWithTag("naval_boss_fight_enemy_boss_sp");
		direction = (gameEntity2.GlobalPosition - gameEntity.GlobalPosition).NormalizedCopy();
	}

	private void CollectPurigCutsceneNotifications()
	{
		StringHelpers.SetCharacterProperties("QUEST_5_COMPANION", NavalStorylineData.Gunnar.CharacterObject);
		_purigNotifications.Enqueue(new ConversationSound(new TextObject("{=jm8pWVv6}Who dares provoke the Hounds in their lair? Is that you, {QUEST_5_COMPANION.NAME}? You and your companion? I will fall upon you like an eagle and tear out your livers, I will shatter your ships to splinters!"), MBInformationManager.NotificationPriority.Medium, NavalStorylineData.Purig.CharacterObject));
		_purigNotifications.Enqueue(new ConversationSound(new TextObject("{=qPaqVlQX}I will spill your blood upon the waters, I will send your corpses to the slimy depths!"), MBInformationManager.NotificationPriority.Medium, NavalStorylineData.Purig.CharacterObject));
		_purigNotifications.Enqueue(new ConversationSound(new TextObject("{=SdqOuRuL}Your skull will be a home for scuttling things and Ran shall make a toothpick of your shin-bone! Do you hear me!"), MBInformationManager.NotificationPriority.Medium, NavalStorylineData.Purig.CharacterObject));
	}

	private void CheckAndPlayPurigCutsceneNotifications()
	{
		if (_isPurigCutsceneStarted && !_purigNotifications.IsEmpty())
		{
			ConversationSound conversationSound = _purigNotifications.Dequeue();
			MBInformationManager.DialogNotificationHandle item = CampaignInformationManager.AddDialogLine(conversationSound.Line, conversationSound.Character, null, 0, conversationSound.Priority);
			_dialogNotificationHandleCache.Add(item);
		}
	}

	private void InitializePhase4Part1()
	{
		_playerShip.ShipOrder.SetShipStopOrder();
		Phase4PurigShip = CreateShip("purigs_roundship_storyline", "phase_4_purig_ship_sp", GetAvailableEnemyFormation(), spawnAnchored: false, _phase4PurigsShipUpgradePieceList);
		Phase4PurigShip.SetCanBeTakenOver(value: false);
		if (_playerShip == null)
		{
			_isCheckpointInitialize = true;
			_playerShip = CreateShip("ship_dromon_storyline", "phase_3_player_ship_sp", _playerFormation, spawnAnchored: false, _escapeShipUpgradePieceList);
		}
		CollectPurigCutsceneNotifications();
		State = Quest5SetPieceBattleMissionState.InitializePhase4Part2;
	}

	private void InitializePhase4Part2()
	{
		Phase4PurigShip.SetController(ShipControllerType.AI);
		Phase4PurigShip.ShipOrder.SetShipMovementOrder(base.Mission.Scene.FindEntityWithTag("phase_3_enemy_ship_5_sp").GlobalPosition.AsVec2);
		SpawnPhase4EnemyTroops();
		Phase4PurigShip.FinalizeDeployment(initializeMachines: true);
		_navalTrajectoryPlanningLogic.ForceReinitialize();
		if (_isCheckpointInitialize)
		{
			_navalAgentsLogic.SetDeploymentMode(value: true);
			_navalShipsLogic.SetDeploymentMode(value: true);
			_playerShip.Formation.PlayerOwner = Agent.Main;
			_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, CharacterObject.PlayerCharacter), _playerShip);
			SpawnGunnarOnShip(_playerShip);
			_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.PlayerTeam);
			_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_playerShip);
			_navalAgentsLogic.SetDeploymentMode(value: false);
			_navalShipsLogic.SetDeploymentMode(value: false);
			State = Quest5SetPieceBattleMissionState.Phase4InProgress;
		}
		else
		{
			State = Quest5SetPieceBattleMissionState.Phase3ToPhase4FadeIn;
		}
		RemoveShipControlPointDescriptionOfAllEnemyShips();
		_purigShipAgents = new List<Agent>(_navalAgentsLogic.GetActiveAgentsOfShip(Phase4PurigShip));
	}

	private void SpawnPhase4EnemyTroops()
	{
		_navalAgentsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic.SetDeploymentMode(value: true);
		CharacterObject troop = MBObjectManager.Instance.GetObject<CharacterObject>("sea_hounds");
		_navalAgentsLogic.SetDesiredTroopCountOfShip(Phase4PurigShip, 40);
		for (int i = 0; i < 40; i++)
		{
			_navalAgentsLogic.AddReservedTroopToShip(new SimpleAgentOrigin(troop), Phase4PurigShip);
		}
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.EnemyTeam);
		SpawnImmortalAgents();
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(Phase4PurigShip);
		_navalAgentsLogic.SetDeploymentMode(value: false);
		_navalShipsLogic.SetDeploymentMode(value: false);
	}

	private void SpawnImmortalAgents()
	{
		GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("sp_immortal_purig");
		AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Purig.CharacterObject).TroopOrigin(new PartyAgentOrigin(_enemyParty.Party, NavalStorylineData.Purig.CharacterObject)).Team(base.Mission.PlayerEnemyTeam).InitialPosition(gameEntity.GlobalPosition)
			.InitialDirection(gameEntity.GetGlobalFrame().rotation.f.AsVec2)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false);
		_purigAgent = Mission.Current.SpawnAgent(agentBuildData);
		_purigAgent.SetTeam(Team.Invalid, sync: true);
		_purigAgent.SetAlarmState(Agent.AIStateFlag.None);
		_purigAgent.SetIsAIPaused(isPaused: true);
		_purigAgent.SetMortalityState(Agent.MortalityState.Immortal);
		foreach (Agent agent in base.Mission.Agents)
		{
			if (agent.Team == base.Mission.PlayerEnemyTeam && Phase4PurigShip.GetIsAgentOnShip(agent))
			{
				_purigShipAgents.Add(agent);
			}
		}
		CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>("sea_hounds");
		GameEntity gameEntity2 = Mission.Current.Scene.FindEntityWithTag("sp_immortal_bodyguard_1");
		AgentBuildData agentBuildData2 = new AgentBuildData(characterObject).TroopOrigin(new SimpleAgentOrigin(characterObject)).Team(base.Mission.PlayerEnemyTeam).InitialPosition(gameEntity2.GlobalPosition)
			.InitialDirection(gameEntity2.GetGlobalFrame().rotation.f.AsVec2)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false);
		_purigBodyguard1 = Mission.Current.SpawnAgent(agentBuildData2);
		_purigBodyguard1.SetTeam(Team.Invalid, sync: true);
		_purigBodyguard1.SetAlarmState(Agent.AIStateFlag.None);
		_purigBodyguard1.SetIsAIPaused(isPaused: true);
		_purigBodyguard1.SetMortalityState(Agent.MortalityState.Immortal);
		GameEntity gameEntity3 = Mission.Current.Scene.FindEntityWithTag("sp_immortal_bodyguard_2");
		AgentBuildData agentBuildData3 = new AgentBuildData(characterObject).TroopOrigin(new SimpleAgentOrigin(characterObject)).Team(base.Mission.PlayerEnemyTeam).InitialPosition(gameEntity3.GlobalPosition)
			.InitialDirection(gameEntity3.GetGlobalFrame().rotation.f.AsVec2)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false);
		_purigBodyguard2 = Mission.Current.SpawnAgent(agentBuildData3);
		_purigBodyguard2.SetTeam(Team.Invalid, sync: true);
		_purigBodyguard2.SetAlarmState(Agent.AIStateFlag.None);
		_purigBodyguard2.SetIsAIPaused(isPaused: true);
		_purigBodyguard2.SetMortalityState(Agent.MortalityState.Immortal);
	}

	private void InitializeNavalBossFightPart1()
	{
		_navalAgentsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic.SetDeploymentMode(value: true);
		Phase4PurigShip.ShipOrder.SetShipStopOrder();
		Phase4PurigShip.SetShipOrderActive(isOrderActive: false);
		Phase4PurigShip.SetAnchor(isAnchored: true);
		BossFightConversationCameraGameEntity = Mission.Current.Scene.FindEntityWithTag("sp_boss_fight_camera");
		MBObjectManager.Instance.GetObject<CharacterObject>("gangradirs_kin_melee");
		MBObjectManager.Instance.GetObject<CharacterObject>("sea_hounds");
		_duelPhaseAllyAgents = new List<Agent>();
		_duelPhaseEnemyAgents = new List<Agent>();
		_playerSpawnPointEntity = Mission.Current.Scene.FindEntityWithTag("naval_boss_fight_player_sp");
		if (Agent.Main.IsUsingGameObject)
		{
			Agent.Main.StopUsingGameObject();
		}
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			allShip.SetController(ShipControllerType.None, autoUpdateController: false);
		}
		Agent.Main.TeleportToPosition(_playerSpawnPointEntity.GlobalPosition);
		List<GameEntity> allyFrames = new List<GameEntity>();
		GetAllyFrames(out allyFrames);
		if (_gunnarAgent != null && _gunnarAgent.IsActive())
		{
			_gunnarAgent.ClearTargetFrame();
			GameEntity gameEntity = allyFrames.First();
			_gunnarAgent.TeleportToPosition(gameEntity.GlobalPosition);
			allyFrames.Remove(gameEntity);
			_duelPhaseAllyAgents.Add(_gunnarAgent);
		}
		if (_bjolgurAgent != null && _bjolgurAgent.IsActive())
		{
			_bjolgurAgent.ClearTargetFrame();
			GameEntity gameEntity2 = allyFrames.First();
			_bjolgurAgent.TeleportToPosition(gameEntity2.GlobalPosition);
			allyFrames.Remove(gameEntity2);
			_duelPhaseAllyAgents.Add(_bjolgurAgent);
		}
		_enemyBossSpawnPointEntity = Mission.Current.Scene.FindEntityWithTag("naval_boss_fight_enemy_boss_sp");
		_purigAgent.SetTeam(base.Mission.PlayerEnemyTeam, sync: true);
		_purigAgent.TeleportToPosition(_enemyBossSpawnPointEntity.GlobalPosition);
		_purigAgent.SetIsAIPaused(isPaused: false);
		_purigAgent.SetMortalityState(Agent.MortalityState.Mortal);
		_duelPhaseEnemyAgents.Add(_purigAgent);
		_navalAgentsLogic.AddAgentToShip(_purigAgent, Phase4PurigShip);
		List<GameEntity> enemyFrames = new List<GameEntity>();
		GetEnemyFrames(out enemyFrames);
		_purigBodyguard1.SetTeam(base.Mission.PlayerEnemyTeam, sync: true);
		_purigBodyguard1.TeleportToPosition(enemyFrames[0].GlobalPosition);
		_purigBodyguard1.SetIsAIPaused(isPaused: false);
		_purigBodyguard1.SetMortalityState(Agent.MortalityState.Mortal);
		_duelPhaseEnemyAgents.Add(_purigBodyguard1);
		_navalAgentsLogic.AddAgentToShip(_purigBodyguard1, Phase4PurigShip);
		_purigBodyguard2.SetTeam(base.Mission.PlayerEnemyTeam, sync: true);
		_purigBodyguard2.TeleportToPosition(enemyFrames[1].GlobalPosition);
		_purigBodyguard2.SetIsAIPaused(isPaused: false);
		_purigBodyguard2.SetMortalityState(Agent.MortalityState.Mortal);
		_duelPhaseEnemyAgents.Add(_purigBodyguard2);
		_navalAgentsLogic.AddAgentToShip(_purigBodyguard2, Phase4PurigShip);
		RemoveAllAgentsExcept(new List<Agent>
		{
			Agent.Main,
			_gunnarAgent,
			_bjolgurAgent,
			_purigAgent,
			_purigBodyguard1,
			_purigBodyguard2
		});
		foreach (ShipAttachmentMachine attachmentMachine in Phase4PurigShip.AttachmentMachines)
		{
			if (attachmentMachine.IsShipAttachmentMachineBridged())
			{
				attachmentMachine.DisconnectAttachment();
			}
			foreach (StandingPoint standingPoint in attachmentMachine.StandingPoints)
			{
				standingPoint.IsDisabledForPlayers = true;
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in Phase4PurigShip.AttachmentPointMachines)
		{
			if (attachmentPointMachine.CurrentAttachment != null)
			{
				attachmentPointMachine.CurrentAttachment.SetAttachmentState(ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval);
			}
			foreach (StandingPoint standingPoint2 in attachmentPointMachine.StandingPoints)
			{
				standingPoint2.IsDisabledForPlayers = true;
			}
		}
		Phase4PurigShip.SetCustomSailSetting(enableCustomSailSetting: true, SailInput.Raised);
		Phase4PurigShip.ShipOrder.SetShipStopOrder();
		Phase4PurigShip.SetAnchor(isAnchored: true);
		_playerShip.ShipOrder.SetShipStopOrder();
		_navalAgentsLogic.SetDeploymentMode(value: false);
		_navalShipsLogic.SetDeploymentMode(value: false);
		ClearPhase4OnPhaseTransition();
		_navalTrajectoryPlanningLogic.ForceReinitialize();
	}

	private void InitializeNavalBossFightPart2()
	{
		foreach (Agent item in _duelPhaseAllyAgents.Concat(_duelPhaseEnemyAgents))
		{
			if (item != Agent.Main)
			{
				ResetAgentForBossFight(item);
			}
		}
		if (_gunnarAgent != null && _gunnarAgent.IsActive())
		{
			_gunnarAgent.SetTargetPosition(_gunnarAgent.Position.AsVec2);
			_gunnarAgent.SetAlarmState(Agent.AIStateFlag.None);
		}
		if (_bjolgurAgent != null && _bjolgurAgent.IsActive())
		{
			_bjolgurAgent.SetTargetPosition(_bjolgurAgent.Position.AsVec2);
			_bjolgurAgent.SetAlarmState(Agent.AIStateFlag.None);
		}
		Agent.Main.SetLookAgent(_purigAgent);
		_purigAgent.SetLookAgent(Agent.Main);
		foreach (Formation item2 in base.Mission.Teams.Attacker.FormationsIncludingEmpty)
		{
			if (item2.CountOfUnits > 0)
			{
				item2.SetMovementOrder(MovementOrder.MovementOrderStop);
			}
		}
		foreach (Formation item3 in base.Mission.Teams.Defender.FormationsIncludingEmpty)
		{
			if (item3.CountOfUnits > 0)
			{
				item3.SetMovementOrder(MovementOrder.MovementOrderStop);
			}
		}
	}

	private void RemoveAllAgentsExcept(List<Agent> exceptionAgents)
	{
		for (int num = base.Mission.Agents.Count - 1; num >= 0; num--)
		{
			Agent agent = base.Mission.Agents[num];
			if (agent.IsActive() && !exceptionAgents.Contains(agent))
			{
				agent.FadeOut(hideInstantly: true, hideMount: false);
			}
		}
	}

	public void StartBossFight(bool isDuel)
	{
		_instructionState = Quest5InstructionState.DefeatPurig;
		BossFightConversationCameraGameEntity = null;
		if (isDuel)
		{
			BossFightState = BossFightStateEnum.Duel;
			StartBossFightDuelModeInternal();
		}
		else
		{
			BossFightState = BossFightStateEnum.All;
			BossFightOutCome = BossFightOutComeEnum.PlayerRefusedTheDuel;
			StartBossFightBattleModeInternal();
		}
	}

	private void StartBossFightDuelModeInternal()
	{
		ResetAgentForBossFight(_purigAgent);
		base.Mission.GetMissionBehavior<MissionConversationLogic>().DisableStartConversation(isDisabled: true);
		foreach (Agent duelPhaseAllyAgent in _duelPhaseAllyAgents)
		{
			if (!duelPhaseAllyAgent.IsMainAgent)
			{
				duelPhaseAllyAgent.SetTeam(Team.Invalid, sync: true);
				WorldPosition position = duelPhaseAllyAgent.GetWorldPosition();
				duelPhaseAllyAgent.SetScriptedPosition(ref position, addHumanLikeDelay: false);
				duelPhaseAllyAgent.SetLookAgent(Agent.Main);
			}
		}
		foreach (Agent duelPhaseEnemyAgent in _duelPhaseEnemyAgents)
		{
			if (duelPhaseEnemyAgent != _purigAgent)
			{
				duelPhaseEnemyAgent.SetTeam(Team.Invalid, sync: true);
				WorldPosition position2 = duelPhaseEnemyAgent.GetWorldPosition();
				duelPhaseEnemyAgent.SetScriptedPosition(ref position2, addHumanLikeDelay: false);
				duelPhaseEnemyAgent.SetLookAgent(_purigAgent);
				duelPhaseEnemyAgent.SetTargetPosition(duelPhaseEnemyAgent.Position.AsVec2);
			}
		}
		_purigAgent.SetTargetAgent(Agent.Main);
		_purigAgent.Formation.AI.ResetBehaviorWeights();
		_purigAgent.HumanAIComponent.RefreshBehaviorValues(MovementOrder.MovementOrderEnum.Charge, ArrangementOrder.ArrangementOrderEnum.Line);
		_purigAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
		State = Quest5SetPieceBattleMissionState.BossFightInProgressAsDuel;
	}

	private void StartBossFightBattleModeInternal()
	{
		foreach (Agent item in _duelPhaseAllyAgents.Concat(_duelPhaseEnemyAgents))
		{
			if (item != Agent.Main)
			{
				ResetAgentForBossFight(item);
			}
		}
		base.Mission.GetMissionBehavior<MissionConversationLogic>().DisableStartConversation(isDisabled: true);
		_purigAgent.Formation.AI.ResetBehaviorWeights();
		_purigAgent.HumanAIComponent.RefreshBehaviorValues(MovementOrder.MovementOrderEnum.Charge, ArrangementOrder.ArrangementOrderEnum.Line);
		_purigAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
		base.Mission.PlayerTeam.SetIsEnemyOf(base.Mission.PlayerEnemyTeam, isEnemyOf: true);
		State = Quest5SetPieceBattleMissionState.BossFightInProgressAsAll;
		foreach (Agent duelPhaseEnemyAgent in _duelPhaseEnemyAgents)
		{
			duelPhaseEnemyAgent.Formation.AI.ResetBehaviorWeights();
			duelPhaseEnemyAgent.HumanAIComponent.RefreshBehaviorValues(MovementOrder.MovementOrderEnum.Charge, ArrangementOrder.ArrangementOrderEnum.Line);
			duelPhaseEnemyAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
		}
		foreach (Agent duelPhaseAllyAgent in _duelPhaseAllyAgents)
		{
			if (!duelPhaseAllyAgent.IsMainAgent)
			{
				duelPhaseAllyAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
			}
		}
		base.Mission.PlayerTeam.PlayerOrderController.SelectAllFormations();
		base.Mission.PlayerTeam.PlayerOrderController.SetOrder(OrderType.Charge);
		base.Mission.PlayerEnemyTeam.MasterOrderController.SelectAllFormations();
		base.Mission.PlayerEnemyTeam.MasterOrderController.SetOrder(OrderType.Charge);
	}

	private void ResetAgentForBossFight(Agent agent)
	{
		if (agent.IsUsingGameObject)
		{
			agent.StopUsingGameObject();
		}
		agent.ClearTargetFrame();
		ActionIndexCache actionIndexCache = ActionIndexCache.act_none;
		float blendInPeriod = -0.2f;
		agent.SetActionChannel(1, in actionIndexCache, ignorePriority: false, (AnimFlags)72uL, 0f, 1f, blendInPeriod);
		agent.SetActionChannel(0, in actionIndexCache, ignorePriority: false, (AnimFlags)72uL, 0f, 1f, blendInPeriod);
	}

	private void StartBossFightConversation()
	{
		_gunnarAgent.SetMortalityState(Agent.MortalityState.Mortal);
		_bjolgurAgent.SetMortalityState(Agent.MortalityState.Mortal);
		MissionConversationLogic missionBehavior = base.Mission.GetMissionBehavior<MissionConversationLogic>();
		missionBehavior.DisableStartConversation(isDisabled: false);
		missionBehavior.StartConversation(_purigAgent, setActionsInstantly: false);
	}

	private void GetAllyFrames(out List<GameEntity> allyFrames)
	{
		allyFrames = new List<GameEntity>();
		for (int i = 0; i < 2; i++)
		{
			GameEntity item = Mission.Current.Scene.FindEntityWithTag("naval_boss_fight_player_ally_sp_" + (i + 1));
			allyFrames.Add(item);
		}
	}

	private void GetEnemyFrames(out List<GameEntity> enemyFrames)
	{
		enemyFrames = new List<GameEntity>();
		for (int i = 0; i < 2; i++)
		{
			GameEntity item = Mission.Current.Scene.FindEntityWithTag("naval_boss_fight_player_enemy_sp_" + (i + 1));
			enemyFrames.Add(item);
		}
	}

	private void OnDuelOver(BattleSideEnum winnerSide)
	{
		AgentVictoryLogic missionBehavior = base.Mission.GetMissionBehavior<AgentVictoryLogic>();
		missionBehavior?.SetCheerActionGroup(AgentVictoryLogic.CheerActionGroupEnum.HighCheerActions);
		missionBehavior?.SetCheerReactionTimerSettings(0.25f, 3f);
		_winnerSide = winnerSide;
		if (winnerSide == base.Mission.PlayerTeam.Side)
		{
			if (BossFightState == BossFightStateEnum.Duel)
			{
				BossFightOutCome = BossFightOutComeEnum.PlayerAcceptedAndWonTheDuel;
			}
			MapEvent.PlayerMapEvent.SetOverrideWinner(base.Mission.PlayerTeam.Side);
		}
		else
		{
			BossFightOutCome = BossFightOutComeEnum.PlayerDefeatedWaitingForConversation;
			MapEvent.PlayerMapEvent.SetOverrideWinner(base.Mission.PlayerEnemyTeam.Side);
		}
		LastHitCheckpoint = Quest5SetPieceBattleMissionState.End;
		State = Quest5SetPieceBattleMissionState.End;
	}

	private MissionShip CreateShip(string shipHullId, string spawnPointId, Formation formation, bool spawnAnchored = false, List<KeyValuePair<string, string>> additionalUpgradePieces = null, Figurehead figurehead = null, bool checkForFreeArea = true)
	{
		GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag(spawnPointId);
		MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
		float waterLevelAtPosition = Mission.Current.Scene.GetWaterLevelAtPosition(gameEntity.GlobalPosition.AsVec2, useWaterRenderer: false, checkWaterBodyEntities: false);
		globalFrame.origin = new Vec3(gameEntity.GlobalPosition.x, gameEntity.GlobalPosition.y, waterLevelAtPosition);
		Ship ship = new Ship(Campaign.Current.ObjectManager.GetObject<ShipHull>(shipHullId));
		if (formation.Team == base.Mission.PlayerEnemyTeam)
		{
			ship.Owner = _enemyParty.Party;
		}
		else if (formation.Team == base.Mission.PlayerTeam)
		{
			ship.Owner = PartyBase.MainParty;
			ship.CanHaveUnlockedPieces = true;
		}
		if (additionalUpgradePieces != null)
		{
			foreach (KeyValuePair<string, string> additionalUpgradePiece in additionalUpgradePieces)
			{
				if (!string.IsNullOrEmpty(additionalUpgradePiece.Value))
				{
					ShipUpgradePiece newUpgradePiece = MBObjectManager.Instance.GetObject<ShipUpgradePiece>(additionalUpgradePiece.Value);
					ship.EquipUpgradePiece(additionalUpgradePiece.Key, newUpgradePiece);
				}
			}
		}
		if (figurehead != null)
		{
			ship.ChangeFigurehead(figurehead);
		}
		MatrixFrame shipFrame = MatrixFrame.Identity;
		Vec3 globalPosition = gameEntity.GlobalPosition;
		globalPosition.z = base.Mission.Scene.GetWaterLevelAtPosition(gameEntity.GlobalPosition.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
		shipFrame.origin = globalPosition;
		shipFrame.rotation.f = globalFrame.rotation.f.AsVec2.Normalized().ToVec3();
		shipFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		MissionShip missionShip = _navalShipsLogic.SpawnShip(ship, in shipFrame, formation.Team, formation, spawnAnchored, FormationClass.NumberOfRegularFormations, checkForFreeArea);
		missionShip.ShipOrder.FormationJoinShip(formation);
		return missionShip;
	}

	private Formation GetAvailableAllyFormation()
	{
		Formation formation = _availableAllyFormations.FirstOrDefault();
		if (formation != null)
		{
			_availableAllyFormations.Remove(formation);
		}
		else
		{
			MBReadOnlyList<MissionShip> allShips = _navalShipsLogic.AllShips;
			for (int num = allShips.Count - 1; num >= 0; num--)
			{
				MissionShip missionShip = allShips[num];
				if (missionShip.Formation.Team == base.Mission.PlayerTeam)
				{
					MBReadOnlyList<Agent> activeAgentsOfShip = _navalAgentsLogic.GetActiveAgentsOfShip(missionShip);
					if (activeAgentsOfShip == null || activeAgentsOfShip.IsEmpty())
					{
						formation = missionShip.Formation;
						RemoveShipInternal(missionShip);
						_navalTrajectoryPlanningLogic.ForceReinitialize();
						break;
					}
				}
			}
		}
		return formation;
	}

	private void SpawnGunnarOnShip(MissionShip ship)
	{
		WeakGameEntity gameEntity = ship.LeftSideShipOarMachines.GetRandomElement().GameEntity;
		AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Gunnar.CharacterObject).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, NavalStorylineData.Gunnar.CharacterObject, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true)).Team(base.Mission.PlayerTeam).InitialPosition(gameEntity.GlobalPosition)
			.InitialDirection(gameEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized())
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false)
			.Banner(base.Mission.PlayerTeam.Banner);
		_gunnarAgent = Mission.Current.SpawnAgent(agentBuildData);
		_navalAgentsLogic.SetIgnoreTroopCapacities(value: true);
		_navalAgentsLogic.AddAgentToShip(_gunnarAgent, ship);
		_gunnarAgentNavalComponent = _gunnarAgent.GetComponent<AgentNavalComponent>();
	}

	private void TriggerShip(MissionShip ship)
	{
		ship.SetAnchor(isAnchored: false);
		ship.Formation.SetControlledByAI(isControlledByAI: true);
		ship.SetShipOrderActive(isOrderActive: true);
		ship.ShipOrder.SetShipEngageOrder();
	}

	private void SpawnCrusasOnShip(MissionShip ship)
	{
		WeakGameEntity gameEntity = ship.LeftSideShipOarMachines.GetRandomElement().GameEntity;
		AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Prusas.CharacterObject).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, NavalStorylineData.Prusas.CharacterObject, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true)).Team(base.Mission.PlayerTeam).InitialPosition(gameEntity.GlobalPosition)
			.InitialDirection(gameEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized())
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false);
		_crusasAgent = Mission.Current.SpawnAgent(agentBuildData);
		_navalAgentsLogic.AddAgentToShip(_crusasAgent, ship);
	}

	private void SpawnLaharOnShip(MissionShip ship)
	{
		WeakGameEntity gameEntity = ship.LeftSideShipOarMachines.GetRandomElement().GameEntity;
		AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Lahar.CharacterObject).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, NavalStorylineData.Lahar.CharacterObject, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true)).Team(base.Mission.PlayerTeam).InitialPosition(gameEntity.GlobalPosition)
			.InitialDirection(gameEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized())
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false);
		_laharAgent = Mission.Current.SpawnAgent(agentBuildData);
		_navalAgentsLogic.AddAgentToShip(_laharAgent, ship);
	}

	private void SpawnBjolgurOnShip(MissionShip ship)
	{
		WeakGameEntity gameEntity = ship.LeftSideShipOarMachines.GetRandomElement().GameEntity;
		AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Bjolgur.CharacterObject).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, NavalStorylineData.Bjolgur.CharacterObject, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true)).Team(base.Mission.PlayerTeam).InitialPosition(gameEntity.GlobalPosition)
			.InitialDirection(gameEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized())
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false);
		_bjolgurAgent = Mission.Current.SpawnAgent(agentBuildData);
		_navalAgentsLogic.AddAgentToShip(_bjolgurAgent, ship);
	}

	private void AddAvailableAllyFormation(Formation formation)
	{
		if (!_availableAllyFormations.Contains(formation))
		{
			_availableAllyFormations.Add(formation);
		}
		else
		{
			Debug.FailedAssert("Formation has been already added.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\MissionControllers\\Quest5SetPieceBattleMissionController.cs", "AddAvailableAllyFormation", 6284);
		}
	}

	private Formation GetAvailableEnemyFormation()
	{
		Formation formation = _availableEnemyFormations.FirstOrDefault();
		if (formation != null)
		{
			_availableEnemyFormations.Remove(formation);
		}
		else
		{
			foreach (Formation item in base.Mission.PlayerEnemyTeam.FormationsIncludingEmpty)
			{
				if (!_navalShipsLogic.IsAShipAssignedToFormation(item))
				{
					formation = item;
					break;
				}
			}
			if (formation == null)
			{
				MissionShip missionShip = null;
				int num = 0;
				MBReadOnlyList<MissionShip> allShips = _navalShipsLogic.AllShips;
				for (int num2 = allShips.Count - 1; num2 >= 0; num2--)
				{
					MissionShip missionShip2 = allShips[num2];
					if (missionShip2.Formation.Team == base.Mission.PlayerEnemyTeam)
					{
						MBReadOnlyList<Agent> activeAgentsOfShip = _navalAgentsLogic.GetActiveAgentsOfShip(missionShip2);
						if (missionShip2 != _phase3EnemyReinforcementShip1 && missionShip2 != _phase3EnemyReinforcementShip2)
						{
							if (activeAgentsOfShip == null || activeAgentsOfShip.IsEmpty())
							{
								formation = missionShip2.Formation;
								RemoveShipInternal(missionShip2);
								_navalTrajectoryPlanningLogic.ForceReinitialize();
								break;
							}
							if (missionShip == null || activeAgentsOfShip.Count < num)
							{
								missionShip = missionShip2;
								num = activeAgentsOfShip.Count;
							}
						}
					}
				}
				if (formation == null && missionShip != null)
				{
					formation = missionShip.Formation;
					RemoveShipInternal(missionShip);
					_navalTrajectoryPlanningLogic.ForceReinitialize();
				}
			}
		}
		return formation;
	}

	private void AddAvailableEnemyFormation(Formation formation)
	{
		if (!_availableEnemyFormations.Contains(formation))
		{
			_availableEnemyFormations.Add(formation);
		}
		else
		{
			Debug.FailedAssert("Formation has been already added.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\MissionControllers\\Quest5SetPieceBattleMissionController.cs", "AddAvailableEnemyFormation", 6370);
		}
	}

	private void AdjustWindDirectionAccordingToTargetFrame(MatrixFrame frame, float windPowerMultiplier, bool addRandomRotation = false)
	{
		Vec2 vec = frame.rotation.f.AsVec2.Normalized();
		Mission.Current.Scene.SetGlobalWindVelocity(vec * windPowerMultiplier);
		Mission.Current.Scene.SetGlobalWindStrengthVector(vec * windPowerMultiplier);
	}

	private void TriggerMissionFailPopup()
	{
		_isMissionFailPopUpTriggered = true;
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=wQbfWNZO}Mission Failed!").ToString(), new TextObject("{=xOhvBfoE}You have been caught.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), string.Empty, EndMissionWithAutoContinueFromCheckpoint, null), pauseGameActiveState: true);
	}

	private void CheckIfMainAgentLeftTheEscapeShip()
	{
		if (Agent.Main.IsActive())
		{
			if (EscapeShip.GetIsAgentOnShip(Agent.Main))
			{
				_playerLeftTheEscapeShipTimer = null;
			}
			else if (_playerLeftTheEscapeShipTimer == null)
			{
				MBInformationManager.DialogNotificationHandle item = CampaignInformationManager.AddDialogLine(new TextObject("{=n17xuLkd*}Get back on our ship! Don't risk getting left behind!"), NavalStorylineData.Gunnar.CharacterObject, null, 0, MBInformationManager.NotificationPriority.High);
				_dialogNotificationHandleCache.Add(item);
				_playerLeftTheEscapeShipTimer = new MissionTimer(10f);
			}
			else if (!_isMissionFailPopUpTriggered && _playerLeftTheEscapeShipTimer.Check())
			{
				TriggerMissionFailPopup();
				_playerLeftTheEscapeShipTimer = null;
			}
		}
	}

	private void EndMissionWithAutoContinueFromCheckpoint()
	{
		ShouldMissionContinueFromCheckpoint = true;
		MakeGunnarStopUsingGameObjectBeforeMissionEnd();
		foreach (MBInformationManager.DialogNotificationHandle item in _dialogNotificationHandleCache)
		{
			CampaignInformationManager.ClearDialogNotification(item);
		}
		_dialogNotificationHandleCache.Clear();
		State = Quest5SetPieceBattleMissionState.End;
	}

	private void RemoveGunnarsHelmet()
	{
		if (_gunnarAgent == null || !_gunnarAgent.IsActive())
		{
			return;
		}
		Equipment equipment = GetScriptedStealthEquipment().Clone();
		for (int i = 0; i < 12; i++)
		{
			if (i == 5)
			{
				equipment[i] = EquipmentElement.Invalid;
				break;
			}
		}
		_gunnarAgent.UpdateSpawnEquipmentAndRefreshVisuals(equipment);
	}

	private void AddMissionShipTroops(List<KeyValuePair<string, int>> troops, MissionShip ship, PartyBase party = null)
	{
		_navalAgentsLogic.SetIgnoreTroopCapacities(ship, value: true);
		foreach (KeyValuePair<string, int> troop in troops)
		{
			CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>(troop.Key);
			int value = troop.Value;
			for (int i = 0; i < value; i++)
			{
				if (party != null)
				{
					_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(party, characterObject, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true), ship);
				}
				else
				{
					_navalAgentsLogic.AddReservedTroopToShip(new SimpleAgentOrigin(characterObject), ship);
				}
			}
		}
	}

	private void HealMainHero()
	{
		Hero.MainHero.Heal(Hero.MainHero.MaxHitPoints);
		if (Agent.Main != null && Agent.Main.IsActive())
		{
			Agent.Main.Health = Agent.Main.HealthLimit;
		}
	}

	private void RemoveShipInternal(MissionShip ship)
	{
		ship.BreakAllExistingConnections();
		Formation formation = ship.Formation;
		_navalShipsLogic.RemoveShip(ship.Formation);
		formation.AI.ResetBehaviorWeights();
	}

	private void CutLooseAllBridgesOfTheShip(MissionShip ship)
	{
		foreach (ShipAttachmentMachine attachmentMachine in ship.AttachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment != null)
			{
				attachmentMachine.CurrentAttachment.SetAttachmentState(ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval);
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in ship.AttachmentPointMachines)
		{
			if (attachmentPointMachine.CurrentAttachment != null)
			{
				attachmentPointMachine.CurrentAttachment.SetAttachmentState(ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval);
			}
		}
	}

	private void MakeGunnarStopUsingGameObjectBeforeMissionEnd()
	{
		if (_gunnarAgent == null || !_gunnarAgent.IsActive())
		{
			return;
		}
		_gunnarAgent.Controller = AgentControllerType.AI;
		if (_gunnarAgent.IsUsingGameObject)
		{
			_gunnarAgent.StopUsingGameObjectMT();
			return;
		}
		_gunnarAgent.DisableScriptedMovement();
		if (_gunnarAgent.IsAIControlled && _gunnarAgent.AIMoveToGameObjectIsEnabled())
		{
			_gunnarAgent.AIMoveToGameObjectDisable();
			_gunnarAgent.Formation?.Team.DetachmentManager.RemoveScoresOfAgentFromDetachments(_gunnarAgent);
		}
	}

	private void SetLastCheckpoint(Quest5SetPieceBattleMissionState state)
	{
		if (state == Quest5SetPieceBattleMissionState.InitializeStealthPhasePart1 || state == Quest5SetPieceBattleMissionState.InitializePhase2Part1 || state == Quest5SetPieceBattleMissionState.InitializePhase3Part1)
		{
			LastHitCheckpoint = state;
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=BWSp3Uyj}Checkpoint reached.").ToString(), new Color(0f, 1f, 0f)));
		}
		else
		{
			Debug.FailedAssert("Unexpected checkpoint set!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\MissionControllers\\Quest5SetPieceBattleMissionController.cs", "SetLastCheckpoint", 6558);
		}
	}

	private void TriggerPurigsDeadPopUp()
	{
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=dS3R9lW7}Success").ToString(), new TextObject("{=suHWcRSn}As you cut Purig down, there is a moment of silence. Then a great cheer wells up from your men. Gunnar closes his eyes and offers a muttered prayer to his gods. Meanwhile, with your sister foremost in your mind, you hurry back to the roundship.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), string.Empty, delegate
		{
			LastHitCheckpoint = Quest5SetPieceBattleMissionState.End;
			MapEvent.PlayerMapEvent.SetOverrideWinner(base.Mission.PlayerTeam.Side);
			foreach (MBInformationManager.DialogNotificationHandle item in _dialogNotificationHandleCache)
			{
				CampaignInformationManager.ClearDialogNotification(item);
			}
			_dialogNotificationHandleCache.Clear();
			base.Mission.EndMission();
		}, null), pauseGameActiveState: true);
	}

	private void MakeShipOarsInvisible(MissionShip ship)
	{
		foreach (WeakGameEntity child in ship.GameEntity.GetChildren())
		{
			if (child.Name.Equals("oars_holder"))
			{
				child.SetVisibilityExcludeParents(visible: false);
				break;
			}
		}
	}

	private void DisableAllShipOrderControllers()
	{
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			if (allShip != _playerShip)
			{
				DisableShipOrderController(allShip);
			}
		}
	}

	private void DisableShipOrderController(MissionShip ship)
	{
		ship.ShipOrder.SetShipStopOrder();
		ship.SetController(ShipControllerType.None);
		ship.SetShipOrderActive(isOrderActive: false);
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			if (allShip != ship && allShip.ShipOrder.TargetShip == ship)
			{
				allShip.ShipOrder.SetShipStopOrder();
				allShip.ShipOrder.SetShipEngageOrder();
			}
		}
	}

	private void RemoveShipControlPointDescriptionOfAllEnemyShips()
	{
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			if (allShip.Team == base.Mission.PlayerEnemyTeam)
			{
				RemoveShipControlPointDescriptionOfShip(allShip);
			}
		}
	}

	private void RemoveShipControlPointDescriptionOfShip(MissionShip ship)
	{
		ship.ShipControllerMachine.SetOverridenDescriptionForActiveEnemyShipControllerMachine(TextObject.GetEmpty());
	}

	private bool IsThereAnyShipBoardedToThePlayerShip()
	{
		bool flag = false;
		foreach (ShipAttachmentMachine attachmentMachine in _playerShip.AttachmentMachines)
		{
			if (attachmentMachine.IsShipAttachmentMachineBridged())
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			foreach (ShipAttachmentPointMachine attachmentPointMachine in _playerShip.AttachmentPointMachines)
			{
				if (attachmentPointMachine.CurrentAttachment != null)
				{
					flag = true;
					break;
				}
			}
		}
		return flag;
	}

	private bool IsThereAnyEnemyShipsWithinRange(MissionShip missionShip, float range)
	{
		bool result = false;
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			if (allShip.Team != missionShip.Team && allShip.Team != Team.Invalid && _navalAgentsLogic.GetActiveAgentCountOfShip(allShip) > 0 && allShip.GameEntity.GlobalPosition.Distance(missionShip.GameEntity.GlobalPosition) <= range)
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public void StartSpawner(BattleSideEnum side)
	{
	}

	public void StopSpawner(BattleSideEnum side)
	{
	}

	public bool IsSideSpawnEnabled(BattleSideEnum side)
	{
		return true;
	}

	public float GetReinforcementInterval(BattleSideEnum side = BattleSideEnum.None)
	{
		return 0f;
	}

	public bool IsSideDepleted(BattleSideEnum side)
	{
		return false;
	}

	public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
	{
		return new List<IAgentOriginBase>();
	}

	public bool GetSpawnHorses(BattleSideEnum side)
	{
		return true;
	}

	public int GetNumberOfPlayerControllableTroops()
	{
		return base.Mission.PlayerTeam.ActiveAgents.Count - 1;
	}
}
