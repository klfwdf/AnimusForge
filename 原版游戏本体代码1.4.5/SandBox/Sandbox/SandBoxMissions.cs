using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionEvents;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.Missions.MissionLogics.Hideout;
using SandBox.Missions.MissionLogics.Towns;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
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
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;
using TaleWorlds.ObjectSystem;

namespace SandBox;

[MissionManager]
public static class SandBoxMissions
{
	public static MissionInitializerRecord CreateSandBoxMissionInitializerRecord(string sceneName, string sceneLevels, bool doNotUseLoadingScreen, DecalAtlasGroup decalAtlasGroup)
	{
		CampaignVec2 position = MobileParty.MainParty.Position;
		if (MobileParty.MainParty.MapEvent != null)
		{
			position = MobileParty.MainParty.MapEvent.Position;
		}
		MissionInitializerRecord result = new MissionInitializerRecord(sceneName);
		result.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		result.DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		result.PlayingInCampaignMode = Campaign.Current.GameMode == CampaignGameMode.Campaign;
		result.AtmosphereOnCampaign = ((Campaign.Current.GameMode == CampaignGameMode.Campaign) ? Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(position) : AtmosphereInfo.GetInvalidAtmosphereInfo());
		result.TerrainType = (int)((Campaign.Current.MapSceneWrapper != null) ? Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace) : ((TerrainType)0));
		result.SceneLevels = sceneLevels;
		result.DoNotUseLoadingScreen = doNotUseLoadingScreen;
		result.DecalAtlasGroup = (int)decalAtlasGroup;
		return result;
	}

	public static MissionInitializerRecord CreateSandBoxTrainingMissionInitializerRecord(string sceneName, string sceneLevels = "", bool doNotUseLoadingScreen = false)
	{
		MissionInitializerRecord result = new MissionInitializerRecord(sceneName);
		result.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		result.DamageFromPlayerToFriendsMultiplier = 1f;
		result.PlayingInCampaignMode = Campaign.Current.GameMode == CampaignGameMode.Campaign;
		result.AtmosphereOnCampaign = ((Campaign.Current.GameMode == CampaignGameMode.Campaign) ? Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position) : AtmosphereInfo.GetInvalidAtmosphereInfo());
		result.TerrainType = (int)Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace);
		result.SceneLevels = sceneLevels;
		result.DoNotUseLoadingScreen = doNotUseLoadingScreen;
		result.DecalAtlasGroup = 3;
		return result;
	}

	[MissionMethod]
	public static Mission OpenTownCenterMission(string scene, int townUpgradeLevel, Location location, CharacterObject talkToChar, string playerSpawnTag)
	{
		string civilianUpgradeLevelTag = Campaign.Current.Models.LocationModel.GetCivilianUpgradeLevelTag(townUpgradeLevel);
		return OpenTownCenterMission(scene, civilianUpgradeLevelTag, location, talkToChar, playerSpawnTag);
	}

	[MissionMethod]
	public static Mission OpenTownCenterMission(string scene, string sceneLevels, Location location, CharacterObject talkToChar, string playerSpawnTag)
	{
		return MissionState.OpenNew("TownCenter", CreateSandBoxMissionInitializerRecord(scene, sceneLevels, doNotUseLoadingScreen: false, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[28]
		{
			new MissionOptionsComponent(),
			new CampaignMissionComponent(),
			new MissionBasicTeamLogic(),
			new MissionSettlementPrepareLogic(),
			new TownCenterMissionController(),
			new MissionAgentLookHandler(),
			new SandBoxMissionHandler(),
			new WorkshopMissionHandler(GetCurrentTown()),
			new BasicLeaveMissionLogic(),
			new LeaveMissionLogic(),
			new BattleAgentLogic(),
			new MountAgentLogic(),
			new NotableSpawnPointHandler(),
			new MissionAgentPanicHandler(),
			new AgentHumanAILogic(),
			new MissionAlleyHandler(),
			new MissionCrimeHandler(),
			new MissionConversationLogic(talkToChar),
			new MissionAgentHandler(),
			new MissionLocationLogic(location, playerSpawnTag),
			new HeroSkillHandler(),
			new MissionFightHandler(),
			new MissionFacialAnimationHandler(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new VisualTrackerMissionBehavior(),
			new EquipmentControllerLeaveLogic()
		});
	}

	[MissionMethod]
	public static Mission OpenTownCenterShadowATargetMission(string scene, string sceneLevels, Location location, CharacterObject talkToChar, string playerSpawnTag)
	{
		return MissionState.OpenNew("TownCenter", CreateSandBoxMissionInitializerRecord(scene, sceneLevels, doNotUseLoadingScreen: false, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[27]
		{
			new MissionOptionsComponent(),
			new CampaignMissionComponent(),
			new MissionBasicTeamLogic(),
			new MissionSettlementPrepareLogic(),
			new TownCenterMissionController(),
			new MissionAgentLookHandler(),
			new SandBoxMissionHandler(),
			new WorkshopMissionHandler(GetCurrentTown()),
			new BasicLeaveMissionLogic(),
			new LeaveMissionLogic(),
			new BattleAgentLogic(),
			new MountAgentLogic(),
			new NotableSpawnPointHandler(),
			new MissionAgentPanicHandler(),
			new AgentHumanAILogic(),
			new MissionAlleyHandler(),
			new MissionCrimeHandler(),
			new MissionConversationLogic(talkToChar),
			new MissionAgentHandler(),
			new HeroSkillHandler(),
			new MissionFightHandler(),
			new MissionFacialAnimationHandler(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new VisualTrackerMissionBehavior(),
			new EquipmentControllerLeaveLogic()
		});
	}

	[MissionMethod]
	public static Mission OpenCastleCourtyardMission(string scene, int castleUpgradeLevel, Location location, CharacterObject talkToChar)
	{
		string civilianUpgradeLevelTag = Campaign.Current.Models.LocationModel.GetCivilianUpgradeLevelTag(castleUpgradeLevel);
		return OpenCastleCourtyardMission(scene, civilianUpgradeLevelTag, location, talkToChar);
	}

	[MissionMethod]
	public static Mission OpenCastleCourtyardMission(string scene, string sceneLevels, Location location, CharacterObject talkToChar)
	{
		return MissionState.OpenNew("TownCenter", CreateSandBoxMissionInitializerRecord(scene, sceneLevels, doNotUseLoadingScreen: false, DecalAtlasGroup.Town), delegate
		{
			List<MissionBehavior> list = new List<MissionBehavior>
			{
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new MissionBasicTeamLogic(),
				new MissionSettlementPrepareLogic(),
				new TownCenterMissionController(),
				new MissionAgentLookHandler(),
				new SandBoxMissionHandler(),
				new BasicLeaveMissionLogic(),
				new LeaveMissionLogic(),
				new BattleAgentLogic(),
				new MountAgentLogic()
			};
			Settlement currentTown = GetCurrentTown();
			if (currentTown != null)
			{
				list.Add(new WorkshopMissionHandler(currentTown));
			}
			list.Add(new MissionAgentPanicHandler());
			list.Add(new AgentHumanAILogic());
			list.Add(new MissionConversationLogic(talkToChar));
			list.Add(new MissionAgentHandler());
			list.Add(new MissionLocationLogic(location));
			list.Add(new HeroSkillHandler());
			list.Add(new MissionFightHandler());
			list.Add(new MissionFacialAnimationHandler());
			list.Add(new MissionHardBorderPlacer());
			list.Add(new MissionBoundaryPlacer());
			list.Add(new EquipmentControllerLeaveLogic());
			list.Add(new MissionBoundaryCrossingHandler());
			list.Add(new VisualTrackerMissionBehavior());
			return list.ToArray();
		});
	}

	[MissionMethod]
	public static Mission OpenIndoorMission(string scene, int townUpgradeLevel, Location location, CharacterObject talkToChar)
	{
		string civilianUpgradeLevelTag = Campaign.Current.Models.LocationModel.GetCivilianUpgradeLevelTag(townUpgradeLevel);
		return OpenIndoorMission(scene, location, talkToChar, civilianUpgradeLevelTag);
	}

	[MissionMethod]
	public static Mission OpenIndoorMission(string scene, Location location, CharacterObject talkToChar = null, string sceneLevels = "")
	{
		List<Ship> mainPartyShips = new List<Ship>();
		List<Ship> townLordShips = new List<Ship>();
		if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown && location.StringId == "port")
		{
			mainPartyShips = MobileParty.MainParty.Ships.ToList();
			foreach (MobileParty party in Settlement.CurrentSettlement.Parties)
			{
				townLordShips.AddRange(party.Ships);
			}
		}
		return MissionState.OpenNew("Indoor", CreateSandBoxMissionInitializerRecord(scene, sceneLevels, doNotUseLoadingScreen: true, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[23]
		{
			new MissionOptionsComponent(),
			new CampaignMissionComponent(),
			new MissionBasicTeamLogic(),
			new BasicLeaveMissionLogic(),
			new LeaveMissionLogic(),
			new SandBoxMissionHandler(),
			new MissionAgentLookHandler(),
			new MissionConversationLogic(talkToChar),
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
	public static Mission OpenPrisonBreakMission(string scene, Location location, CharacterObject prisonerCharacter)
	{
		MissionInitializerRecord rec = CreateSandBoxMissionInitializerRecord(scene, "prison_break", doNotUseLoadingScreen: true, DecalAtlasGroup.Town);
		rec.DisableCorpseFadeOut = true;
		Mission mission = MissionState.OpenNew("PrisonBreak", rec, delegate
		{
			List<MissionBehavior> obj = new List<MissionBehavior>
			{
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new MissionBasicTeamLogic(),
				new BasicLeaveMissionLogic(),
				new LeaveMissionLogic(),
				new SandBoxMissionHandler(),
				new BattleAgentLogic(),
				new MissionAgentLookHandler(),
				new MissionAgentHandler(),
				new StealthAreaMissionLogic(),
				new MissionLocationLogic(location, "sp_prison_break"),
				new HeroSkillHandler(),
				new AgentHumanAILogic(),
				new MissionCrimeHandler(),
				new MissionFacialAnimationHandler(),
				new LocationItemSpawnHandler(),
				new PrisonBreakMissionController(prisonerCharacter),
				new CorpseDraggingMissionLogic(),
				new StealthFailCounterMissionLogic(),
				new VisualTrackerMissionBehavior(),
				new EquipmentControllerLeaveLogic(),
				new BattleSurgeonLogic(),
				new StealthPatrolPointMissionLogic()
			};
			_ = Game.Current.IsDevelopmentMode;
			return obj.ToArray();
		});
		mission.ForceNoFriendlyFire = true;
		return mission;
	}

	[MissionMethod]
	public static Mission OpenVillageMission(string scene, Location location, CharacterObject talkToChar = null, string sceneLevels = null)
	{
		string sceneLevels2 = (string.IsNullOrEmpty(sceneLevels) ? "land_raid" : sceneLevels);
		return MissionState.OpenNew("Village", CreateSandBoxMissionInitializerRecord(scene, sceneLevels2, doNotUseLoadingScreen: false, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[27]
		{
			new MissionOptionsComponent(),
			new CampaignMissionComponent(),
			new MissionBasicTeamLogic(),
			new VillageMissionController(),
			new NotableSpawnPointHandler(),
			new BasicLeaveMissionLogic(),
			new LeaveMissionLogic(),
			new MissionAgentLookHandler(),
			new SandBoxMissionHandler(),
			new MissionConversationLogic(talkToChar),
			new MissionFightHandler(),
			new MissionAgentHandler(),
			new MissionLocationLogic(location),
			new MissionAlleyHandler(),
			new HeroSkillHandler(),
			new MissionFacialAnimationHandler(),
			new MissionAgentPanicHandler(),
			new BattleAgentLogic(),
			new MountAgentLogic(),
			new AgentHumanAILogic(),
			new MissionCrimeHandler(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new EquipmentControllerLeaveLogic(),
			new MissionBoundaryCrossingHandler(),
			new VisualTrackerMissionBehavior(),
			new BattleSurgeonLogic()
		});
	}

	[MissionMethod]
	public static Mission OpenArenaStartMission(string scene, Location location, CharacterObject talkToChar = null, string sceneLevels = "")
	{
		return MissionState.OpenNew("ArenaPracticeFight", CreateSandBoxMissionInitializerRecord(scene, sceneLevels, doNotUseLoadingScreen: false, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[14]
		{
			new MissionOptionsComponent(),
			new EquipmentControllerLeaveLogic(),
			new ArenaPracticeFightMissionController(),
			new BasicLeaveMissionLogic(),
			new MissionConversationLogic(talkToChar),
			new HeroSkillHandler(),
			new MissionFacialAnimationHandler(),
			new MissionAgentPanicHandler(),
			new AgentHumanAILogic(),
			new ArenaAgentStateDeciderLogic(),
			new VisualTrackerMissionBehavior(),
			new CampaignMissionComponent(),
			new MissionAgentHandler(),
			new MissionLocationLogic(location)
		});
	}

	[MissionMethod]
	public static Mission OpenRetirementMission(string scene, Location location, CharacterObject talkToChar = null, string sceneLevels = null, string unconsciousMenuId = "")
	{
		return MissionState.OpenNew("Retirement", CreateSandBoxMissionInitializerRecord(scene, sceneLevels, doNotUseLoadingScreen: false, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[26]
		{
			new MissionOptionsComponent(),
			new CampaignMissionComponent(),
			new MissionBasicTeamLogic(),
			new VillageMissionController(),
			new NotableSpawnPointHandler(),
			new BasicLeaveMissionLogic(),
			new MissionAgentLookHandler(),
			new MissionConversationLogic(talkToChar),
			new MissionFightHandler(),
			new MissionAgentHandler(),
			new MissionLocationLogic(location),
			new MissionAlleyHandler(),
			new HeroSkillHandler(),
			new MissionFacialAnimationHandler(),
			new MissionAgentPanicHandler(),
			new MountAgentLogic(),
			new AgentHumanAILogic(),
			new MissionCrimeHandler(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new EquipmentControllerLeaveLogic(),
			new MissionBoundaryCrossingHandler(),
			new VisualTrackerMissionBehavior(),
			new BattleSurgeonLogic(),
			new RetirementMissionLogic(),
			new LeaveMissionLogic(unconsciousMenuId)
		});
	}

	[MissionMethod]
	public static Mission OpenArenaDuelMission(string scene, Location location, CharacterObject duelCharacter, bool requireCivilianEquipment, bool spawnBOthSidesWithHorse, Action<CharacterObject> onDuelEnd, float customAgentHealth, string sceneLevels = "")
	{
		return MissionState.OpenNew("ArenaDuelMission", CreateSandBoxMissionInitializerRecord(scene, sceneLevels, doNotUseLoadingScreen: false, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[11]
		{
			new MissionOptionsComponent(),
			new ArenaDuelMissionController(duelCharacter, requireCivilianEquipment, spawnBOthSidesWithHorse, onDuelEnd, customAgentHealth),
			new MissionFacialAnimationHandler(),
			new MissionAgentPanicHandler(),
			new AgentHumanAILogic(),
			new ArenaAgentStateDeciderLogic(),
			new VisualTrackerMissionBehavior(),
			new CampaignMissionComponent(),
			new EquipmentControllerLeaveLogic(),
			new MissionAgentHandler(),
			new MissionLocationLogic(location)
		});
	}

	[MissionMethod]
	public static Mission OpenArenaDuelMission(string scene, Location location)
	{
		return MissionState.OpenNew("ArenaDuel", CreateSandBoxMissionInitializerRecord(scene, "", doNotUseLoadingScreen: false, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[12]
		{
			new MissionOptionsComponent(),
			new CampaignMissionComponent(),
			new ArenaDuelMissionBehavior(),
			new BasicLeaveMissionLogic(),
			new MissionAgentHandler(),
			new MissionLocationLogic(location),
			new HeroSkillHandler(),
			new MissionFacialAnimationHandler(),
			new MissionAgentPanicHandler(),
			new AgentHumanAILogic(),
			new EquipmentControllerLeaveLogic(),
			new ArenaAgentStateDeciderLogic()
		});
	}

	[MissionMethod]
	public static Mission OpenBattleMission(MissionInitializerRecord rec)
	{
		bool isPlayerSergeant = MobileParty.MainParty.MapEvent.IsPlayerSergeant();
		bool isPlayerInArmy = MobileParty.MainParty.Army != null;
		List<string> heroesOnPlayerSideByPriority = HeroHelper.OrderHeroesOnPlayerSideByPriority();
		bool isPlayerAttacker = !MobileParty.MainParty.MapEvent.AttackerSide.Parties.Where((MapEventParty p) => p.Party == MobileParty.MainParty.Party).IsEmpty();
		Mission mission = MissionState.OpenNew("Battle", rec, (Mission mission2) => new MissionBehavior[29]
		{
			CreateCampaignMissionAgentSpawnLogic(Mission.BattleSizeType.Battle),
			new BattlePowerCalculationLogic(),
			new BattleSpawnLogic("battle_set"),
			new SandBoxBattleMissionSpawnHandler(),
			new CampaignMissionComponent(),
			new BattleAgentLogic(),
			new MountAgentLogic(),
			new BannerBearerLogic(),
			new MissionOptionsComponent(),
			new BattleEndLogic(),
			new BattleReinforcementsSpawnController(),
			new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.FieldBattle, isPlayerSergeant),
			new BattleObserverMissionLogic(),
			new AgentHumanAILogic(),
			new AgentVictoryLogic(),
			new BattleSurgeonLogic(),
			new MissionAgentPanicHandler(),
			new BattleMissionAgentInteractionLogic(),
			new AgentMoraleInteractionLogic(),
			new AssignPlayerRoleInTeamMissionController(!isPlayerSergeant, isPlayerSergeant, isPlayerInArmy, heroesOnPlayerSideByPriority),
			new SandboxGeneralsAndCaptainsAssignmentLogic(MapEvent.PlayerMapEvent.AttackerSide.LeaderParty.LeaderHero?.Name, MapEvent.PlayerMapEvent.DefenderSide.LeaderParty.LeaderHero?.Name),
			new EquipmentControllerLeaveLogic(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new HighlightsController(),
			new BattleHighlightsController(),
			new BattleDeploymentMissionController(isPlayerAttacker),
			new BattleDeploymentHandler(isPlayerAttacker)
		});
		mission.SetPlayerCanTakeControlOfAnotherAgentWhenDead();
		return mission;
	}

	[MissionMethod]
	public static Mission OpenCaravanBattleMission(MissionInitializerRecord rec, bool isCaravan)
	{
		bool isPlayerAttacker = !MobileParty.MainParty.MapEvent.AttackerSide.Parties.Where((MapEventParty p) => p.Party == MobileParty.MainParty.Party).IsEmpty();
		bool isPlayerSergeant = MobileParty.MainParty.MapEvent.IsPlayerSergeant();
		bool isPlayerInArmy = MobileParty.MainParty.Army != null;
		return MissionState.OpenNew("Battle", rec, (Mission mission) => new MissionBehavior[31]
		{
			new MissionOptionsComponent(),
			new CampaignMissionComponent(),
			new BattleEndLogic(),
			new BattleReinforcementsSpawnController(),
			new BannerBearerLogic(),
			new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.FieldBattle, isPlayerSergeant),
			new BattleSpawnLogic("battle_set"),
			new AgentHumanAILogic(),
			CreateCampaignMissionAgentSpawnLogic(Mission.BattleSizeType.Battle),
			new BattlePowerCalculationLogic(),
			new SandBoxBattleMissionSpawnHandler(),
			new BattleObserverMissionLogic(),
			new BattleAgentLogic(),
			new MountAgentLogic(),
			new AgentVictoryLogic(),
			new MissionAgentPanicHandler(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new BattleMissionAgentInteractionLogic(),
			new AgentMoraleInteractionLogic(),
			new HighlightsController(),
			new BattleHighlightsController(),
			new AssignPlayerRoleInTeamMissionController(!isPlayerSergeant, isPlayerSergeant, isPlayerInArmy),
			new SandboxGeneralsAndCaptainsAssignmentLogic(MapEvent.PlayerMapEvent.AttackerSide.LeaderParty.LeaderHero?.Name, MapEvent.PlayerMapEvent.DefenderSide.LeaderParty.LeaderHero?.Name),
			new EquipmentControllerLeaveLogic(),
			new MissionCaravanOrVillagerTacticsHandler(),
			new CaravanBattleMissionHandler(TaleWorlds.Library.MathF.Min(MapEvent.PlayerMapEvent.InvolvedParties.Where((PartyBase ip) => ip.Side == BattleSideEnum.Attacker).Sum((PartyBase ip) => ip.MobileParty.Party.MemberRoster.TotalManCount - ip.MobileParty.Party.MemberRoster.TotalWounded), MapEvent.PlayerMapEvent.InvolvedParties.Where((PartyBase ip) => ip.Side == BattleSideEnum.Defender).Sum((PartyBase ip) => ip.MobileParty.Party.MemberRoster.TotalManCount - ip.MobileParty.Party.MemberRoster.TotalWounded)), MapEvent.PlayerMapEvent.InvolvedParties.Any((PartyBase ip) => (ip.MobileParty.IsCaravan || ip.MobileParty.IsVillager) && (ip.Culture.StringId == "aserai" || ip.Culture.StringId == "khuzait")), isCaravan),
			new BattleDeploymentHandler(isPlayerAttacker),
			new BattleDeploymentMissionController(isPlayerAttacker),
			new BattleSurgeonLogic()
		});
	}

	[MissionMethod]
	public static Mission OpenAlleyFightMission(MissionInitializerRecord rec, Location location, TroopRoster playerSideTroops, TroopRoster rivalSideTroops)
	{
		return MissionState.OpenNew("AlleyFight", rec, delegate
		{
			List<MissionBehavior> list = new List<MissionBehavior>
			{
				new MissionOptionsComponent(),
				new BattleEndLogic(),
				new AgentHumanAILogic(),
				new BattlePowerCalculationLogic(),
				new CampaignMissionComponent(),
				new AlleyFightMissionHandler(playerSideTroops, rivalSideTroops),
				new BattleObserverMissionLogic(),
				new AgentVictoryLogic(),
				new MissionHardBorderPlacer(),
				new MissionAgentHandler(),
				new MissionLocationLogic(location),
				new MissionFightHandler(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(),
				new BattleMissionAgentInteractionLogic(),
				new HighlightsController(),
				new BattleHighlightsController(),
				new EquipmentControllerLeaveLogic()
			};
			Settlement currentTown = GetCurrentTown();
			if (currentTown != null)
			{
				list.Add(new WorkshopMissionHandler(currentTown));
			}
			return list.ToArray();
		});
	}

	[MissionMethod]
	public static Mission OpenCombatMissionWithDialogue(MissionInitializerRecord rec, CharacterObject characterToTalkTo)
	{
		return MissionState.OpenNew("CombatWithDialogue", rec, delegate
		{
			IMissionTroopSupplier[] suppliers = new IMissionTroopSupplier[2]
			{
				new PartyGroupTroopSupplier(PlayerEncounter.Battle, BattleSideEnum.Defender),
				new PartyGroupTroopSupplier(PlayerEncounter.Battle, BattleSideEnum.Attacker)
			};
			List<MissionBehavior> list = new List<MissionBehavior>
			{
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new BattleEndLogic(),
				new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NoTeamAI, isPlayerSergeant: false),
				new BattleSpawnLogic("battle_set"),
				new MissionAgentPanicHandler(),
				new AgentHumanAILogic(),
				new CombatMissionWithDialogueController(suppliers, characterToTalkTo),
				new MissionConversationLogic(null),
				new BattleObserverMissionLogic(),
				new BattleAgentLogic(),
				new AgentVictoryLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(),
				new BattleMissionAgentInteractionLogic(),
				new HighlightsController(),
				new BattleHighlightsController(),
				new EquipmentControllerLeaveLogic(),
				new BattleSurgeonLogic()
			};
			Settlement currentTown = GetCurrentTown();
			if (currentTown != null)
			{
				list.Add(new WorkshopMissionHandler(currentTown));
			}
			return list.ToArray();
		});
	}

	[MissionMethod]
	public static Mission OpenBattleMissionWhileEnteringSettlement(string scene, int upgradeLevel, int numberOfMaxTroopToBeSpawnedForPlayer, int numberOfMaxTroopToBeSpawnedForOpponent)
	{
		return MissionState.OpenNew("EnteringSettlementBattle", new MissionInitializerRecord(scene)
		{
			PlayingInCampaignMode = (Campaign.Current.GameMode == CampaignGameMode.Campaign),
			AtmosphereOnCampaign = ((Campaign.Current.GameMode == CampaignGameMode.Campaign) ? Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position) : AtmosphereInfo.GetInvalidAtmosphereInfo()),
			DecalAtlasGroup = 3,
			SceneLevels = Campaign.Current.Models.LocationModel.GetCivilianUpgradeLevelTag(upgradeLevel)
		}, delegate
		{
			IMissionTroopSupplier[] suppliers = new IMissionTroopSupplier[2]
			{
				new PartyGroupTroopSupplier(PlayerEncounter.Battle, BattleSideEnum.Defender),
				new PartyGroupTroopSupplier(PlayerEncounter.Battle, BattleSideEnum.Attacker)
			};
			List<MissionBehavior> list = new List<MissionBehavior>
			{
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new BattleEndLogic(),
				new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NoTeamAI, isPlayerSergeant: false),
				new BattleSpawnLogic("battle_set"),
				new MissionAgentPanicHandler(),
				new AgentHumanAILogic(),
				new BattleObserverMissionLogic(),
				new WhileEnteringSettlementBattleMissionController(suppliers, numberOfMaxTroopToBeSpawnedForPlayer, numberOfMaxTroopToBeSpawnedForOpponent),
				new MissionFightHandler(),
				new BattleAgentLogic(),
				new MountAgentLogic(),
				new AgentVictoryLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(),
				new BattleMissionAgentInteractionLogic(),
				new HighlightsController(),
				new BattleHighlightsController(),
				new EquipmentControllerLeaveLogic(),
				new BattleSurgeonLogic()
			};
			Settlement currentTown = GetCurrentTown();
			if (currentTown != null)
			{
				list.Add(new WorkshopMissionHandler(currentTown));
			}
			return list.ToArray();
		});
	}

	[MissionMethod]
	public static Mission OpenBattleMission(string scene, bool usesTownDecalAtlas, string sceneLevels)
	{
		return OpenBattleMission(CreateSandBoxMissionInitializerRecord(scene, sceneLevels, doNotUseLoadingScreen: false, usesTownDecalAtlas ? DecalAtlasGroup.Town : DecalAtlasGroup.Battle));
	}

	[MissionMethod]
	public static Mission OpenAlleyFightMission(string scene, int upgradeLevel, Location location, TroopRoster playerSideTroops, TroopRoster rivalSideTroops)
	{
		return OpenAlleyFightMission(CreateSandBoxMissionInitializerRecord(scene, Campaign.Current.Models.LocationModel.GetCivilianUpgradeLevelTag(upgradeLevel), doNotUseLoadingScreen: false, DecalAtlasGroup.Town), location, playerSideTroops, rivalSideTroops);
	}

	[MissionMethod]
	public static Mission OpenCombatMissionWithDialogue(string scene, CharacterObject characterToTalkTo, int upgradeLevel)
	{
		return OpenCombatMissionWithDialogue(CreateSandBoxMissionInitializerRecord(scene, Campaign.Current.Models.LocationModel.GetCivilianUpgradeLevelTag(upgradeLevel), doNotUseLoadingScreen: false, DecalAtlasGroup.Town), characterToTalkTo);
	}

	[MissionMethod]
	public static Mission OpenHideoutBattleMission(string scene, FlattenedTroopRoster playerTroops, bool isTutorial)
	{
		List<MobileParty> list = new List<MobileParty>();
		foreach (MapEventParty item in MapEvent.PlayerMapEvent.PartiesOnSide(BattleSideEnum.Defender))
		{
			if (item.Party.IsMobile)
			{
				list.Add(item.Party.MobileParty);
			}
		}
		string sceneLevels = (isTutorial ? "level_1" : "level_2");
		int firstPhaseEnemySideTroopCount;
		FlattenedTroopRoster banditPriorityList = MapEventHelper.GetPriorityListForHideoutMission(list, out firstPhaseEnemySideTroopCount);
		FlattenedTroopRoster playerPriorityList = playerTroops ?? MobilePartyHelper.GetStrongestAndPriorTroops(MobileParty.MainParty, Campaign.Current.Models.BanditDensityModel.GetMaximumTroopCountForHideoutMission(MobileParty.MainParty, isAssault: true), includePlayer: true).ToFlattenedRoster();
		int firstPhasePlayerSideTroopCount = playerPriorityList.Count();
		MissionInitializerRecord rec = CreateSandBoxMissionInitializerRecord(scene, sceneLevels, doNotUseLoadingScreen: false, DecalAtlasGroup.Town);
		return MissionState.OpenNew("HideoutBattle", rec, delegate
		{
			IMissionTroopSupplier[] suppliers = new IMissionTroopSupplier[2]
			{
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender, banditPriorityList),
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker, playerPriorityList)
			};
			return new MissionBehavior[22]
			{
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new BattleEndLogic(),
				new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NoTeamAI, isPlayerSergeant: false),
				new AgentHumanAILogic(),
				new HideoutCinematicController(),
				new MissionConversationLogic(),
				new HideoutMissionController(suppliers, PartyBase.MainParty.Side, firstPhaseEnemySideTroopCount, firstPhasePlayerSideTroopCount),
				new BattleObserverMissionLogic(),
				new BattleAgentLogic(),
				new MountAgentLogic(),
				new AgentVictoryLogic(),
				new MissionAgentPanicHandler(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(),
				new AgentMoraleInteractionLogic(),
				new HighlightsController(),
				new BattleHighlightsController(),
				new EquipmentControllerLeaveLogic(),
				new BattleSurgeonLogic(),
				new MissionObjectiveLogic()
			};
		});
	}

	[MissionMethod]
	public static Mission OpenHideoutAmbushMission(string sceneName, FlattenedTroopRoster playerTroops, Location location)
	{
		FlattenedTroopRoster priorAllyTroops = playerTroops ?? MobilePartyHelper.GetStrongestAndPriorTroops(MobileParty.MainParty, Campaign.Current.Models.BanditDensityModel.GetMaximumTroopCountForHideoutMission(MobileParty.MainParty, isAssault: false), includePlayer: false).ToFlattenedRoster();
		priorAllyTroops.RemoveIf((FlattenedTroopRosterElement x) => x.Troop.IsPlayerCharacter);
		MissionInitializerRecord missionInitializerRecord = new MissionInitializerRecord(sceneName);
		missionInitializerRecord.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		missionInitializerRecord.DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		missionInitializerRecord.PlayingInCampaignMode = Campaign.Current.GameMode == CampaignGameMode.Campaign;
		missionInitializerRecord.AtmosphereOnCampaign = ((Campaign.Current.GameMode == CampaignGameMode.Campaign) ? Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position) : AtmosphereInfo.GetInvalidAtmosphereInfo());
		missionInitializerRecord.TerrainType = (int)((Campaign.Current.MapSceneWrapper != null) ? Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace) : ((TerrainType)0));
		missionInitializerRecord.SceneLevels = "level_1";
		missionInitializerRecord.DoNotUseLoadingScreen = false;
		missionInitializerRecord.DisableCorpseFadeOut = true;
		missionInitializerRecord.DecalAtlasGroup = 3;
		MissionInitializerRecord rec = missionInitializerRecord;
		FlattenedTroopRoster banditPriorityList = new FlattenedTroopRoster();
		foreach (MapEventParty item in MapEvent.PlayerMapEvent.PartiesOnSide(BattleSideEnum.Defender))
		{
			if (item.Party.IsMobile)
			{
				banditPriorityList.Add(item.Party.MemberRoster.GetTroopRoster());
			}
		}
		int playerTroopCount = priorAllyTroops.Count();
		return MissionState.OpenNew("HideoutAmbushMission", rec, delegate
		{
			IMissionTroopSupplier[] suppliers = new IMissionTroopSupplier[2]
			{
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender, banditPriorityList),
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker, priorAllyTroops)
			};
			return new MissionBehavior[31]
			{
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NoTeamAI, isPlayerSergeant: false),
				new AgentHumanAILogic(),
				new StealthPatrolPointMissionLogic(),
				new HideoutAmbushMissionController(suppliers, BattleSideEnum.Attacker, playerTroopCount),
				new BattleEndLogic(),
				new MountAgentLogic(),
				new HideoutAmbushBossFightCinematicController(),
				new MissionConversationLogic(),
				new BattleObserverMissionLogic(),
				new MissionAgentHandler(),
				new BattleAgentLogic(),
				new MissionLocationLogic(location),
				new AgentVictoryLogic(),
				new MissionAgentPanicHandler(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(),
				new StealthFailCounterMissionLogic(),
				new HighlightsController(),
				new BattleHighlightsController(),
				new AgentMoraleInteractionLogic(),
				new EquipmentControllerLeaveLogic(),
				new BattleSurgeonLogic(),
				new MissionAIActivationDeactivationEventListenerLogic(),
				new CorpseDraggingMissionLogic(),
				new ShowQuickInformationEventListenerLogic(),
				new VisualTrackerMissionBehavior(),
				new StealthAreaMissionLogic(),
				new MissionObjectiveLogic()
			};
		});
	}

	[MissionMethod]
	public static Mission OpenCampMission(string scene)
	{
		return MissionState.OpenNew("Camp", CreateSandBoxMissionInitializerRecord(scene, "", doNotUseLoadingScreen: false, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[9]
		{
			new MissionOptionsComponent(),
			new CampaignMissionComponent(),
			new BattleEndLogic(),
			new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NoTeamAI, isPlayerSergeant: false),
			new BasicLeaveMissionLogic(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new EquipmentControllerLeaveLogic()
		});
	}

	[MissionMethod]
	public static Mission OpenSiegeMissionWithDeployment(string scene, float[] wallHitPointPercentages, bool hasAnySiegeTower, List<MissionSiegeWeapon> siegeWeaponsOfAttackers, List<MissionSiegeWeapon> siegeWeaponsOfDefenders, bool isPlayerAttacker, int sceneUpgradeLevel = 0, bool isSallyOut = false, bool isReliefForceAttack = false)
	{
		string upgradeLevelTag = Campaign.Current.Models.LocationModel.GetUpgradeLevelTag(sceneUpgradeLevel);
		upgradeLevelTag += " siege";
		bool isPlayerSergeant = MobileParty.MainParty.MapEvent.IsPlayerSergeant();
		bool isPlayerInArmy = MobileParty.MainParty.Army != null;
		List<string> heroesOnPlayerSideByPriority = HeroHelper.OrderHeroesOnPlayerSideByPriority();
		Mission mission = MissionState.OpenNew("SiegeMissionWithDeployment", CreateSandBoxMissionInitializerRecord(scene, upgradeLevelTag, doNotUseLoadingScreen: false, DecalAtlasGroup.Town), delegate
		{
			List<MissionBehavior> list = new List<MissionBehavior>
			{
				new BattleSpawnLogic(isSallyOut ? "sally_out_set" : (isReliefForceAttack ? "relief_force_attack_set" : "battle_set")),
				new MissionOptionsComponent(),
				new CampaignMissionComponent()
			};
			BattleEndLogic battleEndLogic = new BattleEndLogic();
			if (MobileParty.MainParty.MapEvent.PlayerSide == BattleSideEnum.Attacker)
			{
				battleEndLogic.EnableEnemyDefenderPullBack(Campaign.Current.Models.SiegeLordsHallFightModel.DefenderTroopNumberForSuccessfulPullBack);
			}
			list.Add(battleEndLogic);
			list.Add(new BattleReinforcementsSpawnController());
			list.Add(new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), isSallyOut ? Mission.MissionTeamAITypeEnum.SallyOut : Mission.MissionTeamAITypeEnum.Siege, isPlayerSergeant));
			list.Add(new SiegeMissionPreparationHandler(isSallyOut, isReliefForceAttack, wallHitPointPercentages, hasAnySiegeTower));
			list.Add(new CampaignSiegeStateHandler());
			Settlement currentTown = GetCurrentTown();
			if (currentTown != null)
			{
				list.Add(new WorkshopMissionHandler(currentTown));
			}
			Mission.BattleSizeType battleSizeType = Mission.BattleSizeType.Siege;
			if (isSallyOut)
			{
				battleSizeType = Mission.BattleSizeType.SallyOut;
				FlattenedTroopRoster priorityTroopsForSallyOutAmbush = Campaign.Current.Models.SiegeEventModel.GetPriorityTroopsForSallyOutAmbush();
				list.Add(new SandBoxSallyOutMissionController(isSallyOutAmbush: true));
				list.Add(CreateCampaignMissionAgentSpawnLogic(battleSizeType, priorityTroopsForSallyOutAmbush));
			}
			else
			{
				if (isReliefForceAttack)
				{
					list.Add(new SandBoxSallyOutMissionController(isSallyOutAmbush: false));
				}
				else
				{
					list.Add(new SandBoxSiegeMissionSpawnHandler());
				}
				list.Add(CreateCampaignMissionAgentSpawnLogic(battleSizeType));
			}
			list.Add(new BattlePowerCalculationLogic());
			list.Add(new BattleObserverMissionLogic());
			list.Add(new BattleAgentLogic());
			list.Add(new BattleSurgeonLogic());
			list.Add(new MountAgentLogic());
			list.Add(new BannerBearerLogic());
			list.Add(new AgentHumanAILogic());
			if (!isSallyOut)
			{
				list.Add(new AmmoSupplyLogic(new List<BattleSideEnum> { BattleSideEnum.Defender }));
			}
			list.Add(new AgentVictoryLogic());
			list.Add(new AssignPlayerRoleInTeamMissionController(!isPlayerSergeant, isPlayerSergeant, isPlayerInArmy, heroesOnPlayerSideByPriority));
			list.Add(new SandboxGeneralsAndCaptainsAssignmentLogic(MapEvent.PlayerMapEvent.AttackerSide.LeaderParty.LeaderHero?.Name, MapEvent.PlayerMapEvent.DefenderSide.LeaderParty.LeaderHero?.Name, null, null, createBodyguard: false));
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
	public static Mission OpenSiegeMissionNoDeployment(string scene, bool isSallyOut = false, bool isReliefForceAttack = false)
	{
		string upgradeLevelTag = Campaign.Current.Models.LocationModel.GetUpgradeLevelTag(3);
		upgradeLevelTag += " siege";
		bool isPlayerSergeant = MobileParty.MainParty.MapEvent.IsPlayerSergeant();
		bool isPlayerInArmy = MobileParty.MainParty.Army != null;
		List<string> heroesOnPlayerSideByPriority = HeroHelper.OrderHeroesOnPlayerSideByPriority();
		return MissionState.OpenNew("SiegeMissionNoDeployment", CreateSandBoxMissionInitializerRecord(scene, upgradeLevelTag, doNotUseLoadingScreen: false, DecalAtlasGroup.Town), delegate
		{
			List<MissionBehavior> list = new List<MissionBehavior>
			{
				new MissionOptionsComponent(),
				new BattleSpawnLogic(isSallyOut ? "sally_out_set" : (isReliefForceAttack ? "relief_force_attack_set" : "battle_set")),
				new CampaignMissionComponent()
			};
			BattleEndLogic battleEndLogic = new BattleEndLogic();
			if (!isSallyOut && !isReliefForceAttack && MobileParty.MainParty.MapEvent.PlayerSide == BattleSideEnum.Attacker)
			{
				battleEndLogic.EnableEnemyDefenderPullBack(Campaign.Current.Models.SiegeLordsHallFightModel.DefenderTroopNumberForSuccessfulPullBack);
			}
			list.Add(battleEndLogic);
			list.Add(new BattleReinforcementsSpawnController());
			list.Add(new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.FieldBattle, isPlayerSergeant));
			list.Add(new CampaignSiegeStateHandler());
			Mission.BattleSizeType battleSizeType = ((!isSallyOut) ? Mission.BattleSizeType.Siege : Mission.BattleSizeType.SallyOut);
			list.Add(CreateCampaignMissionAgentSpawnLogic(battleSizeType));
			list.Add(new BattlePowerCalculationLogic());
			list.Add(new SandBoxBattleMissionSpawnHandler());
			Settlement currentTown = GetCurrentTown();
			if (currentTown != null)
			{
				list.Add(new WorkshopMissionHandler(currentTown));
			}
			list.Add(new BattleObserverMissionLogic());
			list.Add(new BattleAgentLogic());
			list.Add(new BattleSurgeonLogic());
			list.Add(new MountAgentLogic());
			list.Add(new AgentVictoryLogic());
			list.Add(new AmmoSupplyLogic(new List<BattleSideEnum> { BattleSideEnum.Defender }));
			list.Add(new MissionAgentPanicHandler());
			list.Add(new MissionHardBorderPlacer());
			list.Add(new MissionBoundaryPlacer());
			list.Add(new EquipmentControllerLeaveLogic());
			list.Add(new MissionBoundaryCrossingHandler());
			list.Add(new AgentHumanAILogic());
			list.Add(new AgentMoraleInteractionLogic());
			list.Add(new HighlightsController());
			list.Add(new BattleHighlightsController());
			list.Add(new AssignPlayerRoleInTeamMissionController(!isPlayerSergeant, isPlayerSergeant, isPlayerInArmy, heroesOnPlayerSideByPriority));
			list.Add(new SandboxGeneralsAndCaptainsAssignmentLogic(MapEvent.PlayerMapEvent.AttackerSide.LeaderParty.LeaderHero?.Name, MapEvent.PlayerMapEvent.DefenderSide.LeaderParty.LeaderHero?.Name, null, null, createBodyguard: false));
			return list.ToArray();
		});
	}

	[MissionMethod]
	public static Mission OpenSiegeLordsHallFightMission(string scene, FlattenedTroopRoster attackerPriorityList)
	{
		int remainingDefenderArcherCount = Campaign.Current.Models.SiegeLordsHallFightModel.MaxDefenderArcherCount;
		FlattenedTroopRoster defenderPriorityList = Campaign.Current.Models.SiegeLordsHallFightModel.GetPriorityListForLordsHallFightMission(MapEvent.PlayerMapEvent, BattleSideEnum.Defender, Campaign.Current.Models.SiegeLordsHallFightModel.MaxDefenderSideTroopCount);
		int attackerSideTroopCountMax = TaleWorlds.Library.MathF.Min(Campaign.Current.Models.SiegeLordsHallFightModel.MaxAttackerSideTroopCount, attackerPriorityList.Troops.Count());
		int defenderSideTroopCountMax = TaleWorlds.Library.MathF.Min(Campaign.Current.Models.SiegeLordsHallFightModel.MaxDefenderSideTroopCount, defenderPriorityList.Troops.Count());
		MissionInitializerRecord rec = CreateSandBoxMissionInitializerRecord(scene, "siege", doNotUseLoadingScreen: false, DecalAtlasGroup.Town);
		return MissionState.OpenNew("SiegeLordsHallFightMission", rec, delegate
		{
			IMissionTroopSupplier[] suppliers = new IMissionTroopSupplier[2]
			{
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender, defenderPriorityList, delegate(UniqueTroopDescriptor uniqueTroopDescriptor, MapEventParty mapEventParty)
				{
					bool result = true;
					if (mapEventParty.GetTroop(uniqueTroopDescriptor).IsRanged)
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
				}),
				new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker, attackerPriorityList)
			};
			return new MissionBehavior[19]
			{
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new BattleEndLogic(),
				new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.NoTeamAI, isPlayerSergeant: false),
				new CampaignSiegeStateHandler(),
				new AgentHumanAILogic(),
				new LordsHallFightMissionController(suppliers, Campaign.Current.Models.SiegeLordsHallFightModel.AreaLostRatio, Campaign.Current.Models.SiegeLordsHallFightModel.AttackerDefenderTroopCountRatio, attackerSideTroopCountMax, defenderSideTroopCountMax, PartyBase.MainParty.Side),
				new BattleObserverMissionLogic(),
				new BattleAgentLogic(),
				new AgentVictoryLogic(),
				new AmmoSupplyLogic(new List<BattleSideEnum> { BattleSideEnum.Defender }),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(),
				new EquipmentControllerLeaveLogic(),
				new BattleMissionAgentInteractionLogic(),
				new HighlightsController(),
				new BattleHighlightsController(),
				new BattleSurgeonLogic()
			};
		});
	}

	[MissionMethod]
	public static Mission OpenConversationMission(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData, string specialScene = "", string sceneLevels = "", bool isMultiAgentConversation = false)
	{
		CampaignVec2 campaignPosition = conversationPartnerData.Party?.Position ?? PartyBase.MainParty.Position;
		string sceneName = (specialScene.IsEmpty() ? Campaign.Current.Models.SceneModel.GetConversationSceneForMapPosition(campaignPosition) : specialScene);
		return MissionState.OpenNew("Conversation", CreateSandBoxMissionInitializerRecord(sceneName, sceneLevels, doNotUseLoadingScreen: true, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[5]
		{
			new CampaignMissionComponent(),
			new MissionConversationLogic(),
			new MissionOptionsComponent(),
			new ConversationMissionLogic(playerCharacterData, conversationPartnerData, isMultiAgentConversation),
			new EquipmentControllerLeaveLogic()
		}, addDefaultMissionBehaviors: true, needsMemoryCleanup: false);
	}

	[MissionMethod]
	public static Mission OpenMeetingMission(string scene, CharacterObject character)
	{
		Debug.FailedAssert("This mission was broken", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\SandBoxMissions.cs", "OpenMeetingMission", 1335);
		return MissionState.OpenNew("Conversation", CreateSandBoxMissionInitializerRecord(scene, "", doNotUseLoadingScreen: false, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[5]
		{
			new CampaignMissionComponent(),
			new MissionSettlementPrepareLogic(),
			new MissionOptionsComponent(),
			new MissionConversationLogic(),
			new EquipmentControllerLeaveLogic()
		}, addDefaultMissionBehaviors: true, needsMemoryCleanup: false);
	}

	private static Settlement GetCurrentTown()
	{
		if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown)
		{
			return Settlement.CurrentSettlement;
		}
		if (MapEvent.PlayerMapEvent != null && MapEvent.PlayerMapEvent.MapEventSettlement != null && MapEvent.PlayerMapEvent.MapEventSettlement.IsTown)
		{
			return MapEvent.PlayerMapEvent.MapEventSettlement;
		}
		return null;
	}

	private static DefaultBattleMissionAgentSpawnLogic CreateCampaignMissionAgentSpawnLogic(Mission.BattleSizeType battleSizeType, FlattenedTroopRoster priorTroopsForDefenders = null, FlattenedTroopRoster priorTroopsForAttackers = null)
	{
		return new DefaultBattleMissionAgentSpawnLogic(new IMissionTroopSupplier[2]
		{
			new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender, priorTroopsForDefenders),
			new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker, priorTroopsForAttackers)
		}, PartyBase.MainParty.Side, battleSizeType);
	}

	[MissionMethod]
	public static Mission OpenDisguiseMission(string scene, bool willSetUpContact, Location fromLocation, string sceneLevels = null)
	{
		CharacterObject defaultContractorCharacter = MBObjectManager.Instance.GetObject<CharacterObject>("disguise_contractor_character");
		MissionInitializerRecord rec = CreateSandBoxMissionInitializerRecord(scene, sceneLevels, doNotUseLoadingScreen: false, DecalAtlasGroup.Town);
		return MissionState.OpenNew("DisguiseMission", rec, (Mission mission) => new MissionBehavior[27]
		{
			new MissionOptionsComponent(),
			new CampaignMissionComponent(),
			new MissionBasicTeamLogic(),
			new MissionSettlementPrepareLogic(),
			new BasicLeaveMissionLogic(),
			new BattleAgentLogic(),
			new MountAgentLogic(),
			new MissionAgentPanicHandler(),
			new AgentHumanAILogic(),
			new MissionCrimeHandler(),
			new MissionConversationLogic(),
			new HeroSkillHandler(),
			new MissionFightHandler(),
			new MissionFacialAnimationHandler(),
			new MissionHardBorderPlacer(),
			new CheckpointMissionLogic(),
			new NotableSpawnPointHandler(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new VisualTrackerMissionBehavior(),
			new EquipmentControllerLeaveLogic(),
			new MissionAIActivationDeactivationEventListenerLogic(),
			new StealthPatrolPointMissionLogic(),
			new MissionAlleyHandler(),
			new MissionAgentHandler(),
			new DisguiseMissionLogic(defaultContractorCharacter, fromLocation, willSetUpContact),
			new ShowQuickInformationEventListenerLogic()
		});
	}

	[MissionMethod(UsableByEditor = true)]
	public static Mission OpenSimpleMountedPlayerMission(string scene, string sceneLevels)
	{
		return MissionState.OpenNew("SimpleMountedPlayer", new MissionInitializerRecord(scene)
		{
			PlayingInCampaignMode = (Campaign.Current.GameMode == CampaignGameMode.Campaign),
			AtmosphereOnCampaign = ((Campaign.Current.GameMode == CampaignGameMode.Campaign) ? Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position) : AtmosphereInfo.GetInvalidAtmosphereInfo()),
			SceneLevels = sceneLevels
		}, (Mission missionController) => new MissionBehavior[3]
		{
			new MissionOptionsComponent(),
			new SimpleMountedPlayerMissionController(),
			new EquipmentControllerLeaveLogic()
		});
	}
}
