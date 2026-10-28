using SandBox;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions;
using SandBox.Missions.MissionEvents;
using SandBox.Missions.MissionLogics;
using Storymode.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;

namespace StoryMode.Missions;

[MissionManager]
public static class StoryModeMissions
{
	[MissionMethod]
	public static Mission OpenTrainingFieldMission(string scene, Location location, CharacterObject talkToChar = null, string sceneLevels = null)
	{
		return MissionState.OpenNew("TrainingField", SandBoxMissions.CreateSandBoxTrainingMissionInitializerRecord(scene, sceneLevels), (Mission mission) => new MissionBehavior[23]
		{
			new MissionOptionsComponent(),
			new CampaignMissionComponent(),
			new MissionBasicTeamLogic(),
			new TrainingFieldMissionController(),
			new BasicLeaveMissionLogic(),
			new LeaveMissionLogic(),
			new MissionAgentLookHandler(),
			new SandBoxMissionHandler(),
			new MissionConversationLogic(talkToChar),
			new MissionFightHandler(),
			new MissionAgentHandler(),
			new MissionAlleyHandler(),
			new HeroSkillHandler(),
			new MissionFacialAnimationHandler(),
			new MissionAgentPanicHandler(),
			new BattleAgentLogic(),
			new AgentHumanAILogic(),
			new MissionCrimeHandler(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new VisualTrackerMissionBehavior(),
			new EquipmentControllerLeaveLogic()
		});
	}

	[MissionMethod]
	public static Mission OpenSneakIntoTheVillaMission(string scene, CampaignTime overridenCt, string sceneLevels = null)
	{
		MissionInitializerRecord missionInitializerRecord = new MissionInitializerRecord(scene);
		missionInitializerRecord.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		missionInitializerRecord.DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		missionInitializerRecord.PlayingInCampaignMode = Campaign.Current.GameMode == CampaignGameMode.Campaign;
		missionInitializerRecord.AtmosphereOnCampaign = ((Campaign.Current.GameMode == CampaignGameMode.Campaign) ? Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position) : AtmosphereInfo.GetInvalidAtmosphereInfo());
		missionInitializerRecord.TerrainType = (int)((Campaign.Current.MapSceneWrapper != null) ? Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace) : ((TerrainType)0));
		missionInitializerRecord.SceneLevels = sceneLevels;
		missionInitializerRecord.DoNotUseLoadingScreen = false;
		missionInitializerRecord.DisableCorpseFadeOut = true;
		missionInitializerRecord.DecalAtlasGroup = 3;
		MissionInitializerRecord rec = missionInitializerRecord;
		return MissionState.OpenNew("SneakIntoTheVillaMission", rec, (Mission mission) => new MissionBehavior[23]
		{
			new MissionOptionsComponent(),
			new CampaignMissionComponent(),
			new AgentHumanAILogic(),
			new MissionBasicTeamLogic(),
			new StealthPatrolPointMissionLogic(),
			new MissionAgentHandler(),
			new SneakIntoTheVillaMissionController(),
			new MissionConversationLogic(),
			new BattleAgentLogic(),
			new MountAgentLogic(),
			new AgentVictoryLogic(),
			new MissionAgentPanicHandler(),
			new MissionHardBorderPlacer(),
			new MissionBoundaryPlacer(),
			new MissionBoundaryCrossingHandler(),
			new HighlightsController(),
			new BattleHighlightsController(),
			new EquipmentControllerLeaveLogic(),
			new BattleSurgeonLogic(),
			new StealthFailCounterMissionLogic(),
			new MissionAIActivationDeactivationEventListenerLogic(),
			new CorpseDraggingMissionLogic(),
			new ShowQuickInformationEventListenerLogic()
		});
	}
}
