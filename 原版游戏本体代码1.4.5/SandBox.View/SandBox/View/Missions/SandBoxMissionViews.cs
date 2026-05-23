using System;
using System.Collections.Generic;
using SandBox.View.Missions.Sound.Components;
using SandBox.View.Missions.Tournaments;
using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews.Sound;

namespace SandBox.View.Missions;

[ViewCreatorModule]
public class SandBoxMissionViews
{
	[ViewMethod("TownCenter")]
	public static MissionView[] OpenTownCenterMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			ViewCreator.CreateMissionLeaveView(),
			ViewCreator.CreatePhotoModeView(),
			SandBoxViewCreator.CreateMissionBarterView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			new MissionBoundaryWallView(),
			new MissionCampaignBattleSpectatorView()
		}.ToArray();
	}

	[ViewMethod("FacialAnimationTest")]
	public static MissionView[] OpenFacialAnimationTest(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			SandBoxViewCreator.CreateMissionBarterView(),
			new MissionBoundaryWallView(),
			ViewCreator.CreatePhotoModeView()
		}.ToArray();
	}

	[ViewMethod("Indoor")]
	public static MissionView[] OpenTavernMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			new MusicSilencedMissionView(),
			SandBoxViewCreator.CreateMissionBarterView(),
			ViewCreator.CreateMissionLeaveView(),
			SandBoxViewCreator.CreateBoardGameView(),
			SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
		}.ToArray();
	}

	[ViewMethod("PrisonBreak")]
	public static MissionView[] OpenPrisonBreakMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			new MusicSilencedMissionView(),
			ViewCreator.CreateMissionLeaveView(),
			SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
			SandBoxViewCreator.CreateMissionStealthFailCounter(),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			SandBoxViewCreator.CreateMissionAgentAlarmStateView(mission),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
		}.ToArray();
	}

	[ViewMethod("Village")]
	public static MissionView[] OpenVillageMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			SandBoxViewCreator.CreateMissionBarterView(),
			ViewCreator.CreateMissionLeaveView(),
			new MissionBoundaryWallView(),
			SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
		}.ToArray();
	}

	[ViewMethod("Retirement")]
	public static MissionView[] OpenRetirementMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			SandBoxViewCreator.CreateMissionBarterView(),
			ViewCreator.CreateMissionLeaveView(),
			new MissionBoundaryWallView(),
			SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			ViewCreator.CreatePhotoModeView()
		}.ToArray();
	}

	[ViewMethod("ArenaPracticeFight")]
	public static MissionView[] OpenArenaStartMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			new MissionAudienceHandler(0.4f + MBRandom.RandomFloat * 0.3f),
			SandBoxViewCreator.CreateMissionArenaPracticeFightView(),
			ViewCreator.CreateMissionLeaveView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			new MusicArenaPracticeMissionView(),
			SandBoxViewCreator.CreateMissionBarterView(),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView(),
			new ArenaPreloadView()
		}.ToArray();
	}

	[ViewMethod("ArenaDuelMission")]
	public static MissionView[] OpenArenaDuelMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			ViewCreator.CreateMissionLeaveView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			new MissionSingleplayerViewHandler(),
			new MusicSilencedMissionView(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			new MissionAudienceHandler(0.4f + MBRandom.RandomFloat * 0.3f),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView()
		}.ToArray();
	}

	[ViewMethod("TownMerchant")]
	public static MissionView[] OpenTownMerchantMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			ViewCreator.CreateMissionLeaveView(),
			SandBoxViewCreator.CreateMissionBarterView(),
			SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
		}.ToArray();
	}

	[ViewMethod("Alley")]
	public static MissionView[] OpenAlleyMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateMissionLeaveView(),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			SandBoxViewCreator.CreateMissionBarterView(),
			new MissionBoundaryWallView(),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
		}.ToArray();
	}

	[ViewMethod("SneakTeam3")]
	public static MissionView[] OpenSneakTeam3Mission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionConversationCameraView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
		}.ToArray();
	}

	[ViewMethod("SimpleMountedPlayer")]
	public static MissionView[] OpenSimpleMountedPlayerMission(Mission mission)
	{
		return new List<MissionView>().ToArray();
	}

	[ViewMethod("Battle")]
	public static MissionView[] OpenBattleMission(Mission mission)
	{
		List<MissionView> obj = new List<MissionView>
		{
			new MissionCampaignView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, SPScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission)
		};
		MissionView missionView = ViewCreator.CreateMissionOrderUIHandler();
		obj.Add(missionView);
		obj.Add(new OrderTroopPlacer(null));
		obj.Add(new MissionSingleplayerViewHandler());
		obj.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		obj.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		obj.Add(new MusicBattleMissionView(isSiegeBattle: false));
		obj.Add(new DeploymentMissionView());
		obj.Add(new MissionDeploymentBoundaryMarker("swallowtail_banner"));
		obj.Add(ViewCreator.CreateMissionBoundaryCrossingView());
		obj.Add(new MissionBoundaryWallView());
		obj.Add(ViewCreator.CreateMissionFormationMarkerUIHandler(mission));
		obj.Add(new MissionFormationTargetSelectionHandler());
		obj.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
		obj.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		obj.Add(new MissionItemContourControllerView());
		obj.Add(new MissionAgentContourControllerView());
		obj.Add(new MissionPreloadView());
		obj.Add(new MissionCampaignBattleSpectatorView());
		obj.Add(ViewCreator.CreatePhotoModeView());
		obj.Add(new MissionFaceCacheView());
		ISiegeDeploymentView siegeDeploymentView = missionView as ISiegeDeploymentView;
		obj.Add(new MissionEntitySelectionUIHandler(siegeDeploymentView.OnEntitySelection, siegeDeploymentView.OnEntityHover));
		obj.Add(ViewCreator.CreateMissionOrderOfBattleUIHandler(mission, new SPOrderOfBattleVM()));
		return obj.ToArray();
	}

	[ViewMethod("AlleyFight")]
	public static MissionView[] OpenAlleyFightMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, SPScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionOrderUIHandler(),
			new OrderTroopPlacer(null),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
			new MissionFormationTargetSelectionHandler(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			ViewCreator.CreateMissionSpectatorControlView(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView()
		}.ToArray();
	}

	[ViewMethod("HideoutBattle")]
	public static MissionView[] OpenHideoutBattleMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			new MissionHideoutCinematicView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, SPScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionOrderUIHandler(),
			new OrderTroopPlacer(null),
			new MissionSingleplayerViewHandler(),
			new MusicSilencedMissionView(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
			new MissionFormationTargetSelectionHandler(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			ViewCreator.CreateMissionSpectatorControlView(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionCampaignBattleSpectatorView(),
			new MissionPreloadView(),
			ViewCreator.CreatePhotoModeView(),
			ViewCreator.CreateMissionObjectiveView()
		}.ToArray();
	}

	[ViewMethod("HideoutAmbushMission")]
	public static MissionView[] OpenHideoutAmbushMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			new MissionSingleplayerViewHandler(),
			new MusicStealthMissionView(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
			SandBoxViewCreator.CreateMissionAgentAlarmStateView(mission),
			ViewCreator.CreateMissionSpectatorControlView(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView(),
			ViewCreator.CreateMissionLeaveView(),
			new MissionConversationCameraView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, SPScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateMissionOrderUIHandler(),
			SandBoxViewCreator.CreateMissionStealthFailCounter(),
			new OrderTroopPlacer(null),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
			new MissionPreloadView(),
			ViewCreatorManager.CreateMissionView<MissionHideoutAmbushCinematicView>(mission != null, mission, Array.Empty<object>()),
			ViewCreatorManager.CreateMissionView<MissionHideoutAmbushBossFightCinematicView>(mission != null, mission, Array.Empty<object>()),
			ViewCreator.CreateMissionObjectiveView()
		}.ToArray();
	}

	[ViewMethod("EnteringSettlementBattle")]
	public static MissionView[] OpenBattleMissionWhileEnteringSettlement(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, SPScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionOrderUIHandler(),
			new OrderTroopPlacer(null),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
			new MissionFormationTargetSelectionHandler(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			ViewCreator.CreateMissionSpectatorControlView(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView()
		}.ToArray();
	}

	[ViewMethod("CombatWithDialogue")]
	public static MissionView[] OpenCombatMissionWithDialogue(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, SPScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionOrderUIHandler(),
			new OrderTroopPlacer(null),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
			new MissionFormationTargetSelectionHandler(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			ViewCreator.CreateMissionSpectatorControlView(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView()
		}.ToArray();
	}

	[ViewMethod("SiegeEngine")]
	public static MissionView[] OpenTestSiegeEngineMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionConversationCameraView(),
			ViewCreator.CreateMissionOrderUIHandler(),
			new OrderTroopPlacer(null)
		}.ToArray();
	}

	[ViewMethod("CustomCameraMission")]
	public static MissionView[] OpenCustomCameraMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionConversationCameraView(),
			new MissionCustomCameraView()
		}.ToArray();
	}

	[ViewMethod("AmbushBattle")]
	public static MissionView[] OpenAmbushBattleMission(Mission mission)
	{
		throw new NotImplementedException("Ambush battle is not implemented.");
	}

	[ViewMethod("Camp")]
	public static MissionView[] OpenCampMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
		}.ToArray();
	}

	[ViewMethod("SiegeMissionWithDeployment")]
	public static MissionView[] OpenSiegeMissionWithDeployment(Mission mission)
	{
		List<MissionView> list = new List<MissionView>();
		mission.GetMissionBehavior<SiegeDeploymentHandler>();
		list.Add(new MissionCampaignView());
		list.Add(new MissionConversationCameraView());
		list.Add(ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode));
		list.Add(ViewCreator.CreateOptionsUIHandler());
		list.Add(ViewCreator.CreateMissionMainAgentEquipDropView(mission));
		list.Add(ViewCreator.CreateMissionAgentLabelUIHandler(mission));
		list.Add(ViewCreator.CreateMissionBattleScoreUIHandler(mission, SPScoreboardVM.CreateMission(mission)));
		MissionView missionView = ViewCreator.CreateMissionOrderUIHandler();
		list.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		list.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		list.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		list.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		list.Add(missionView);
		list.Add(new OrderTroopPlacer(null));
		list.Add(new MissionSingleplayerViewHandler());
		list.Add(new MusicBattleMissionView(isSiegeBattle: true));
		list.Add(new DeploymentMissionView());
		list.Add(new MissionDeploymentBoundaryMarker("swallowtail_banner"));
		list.Add(ViewCreator.CreateMissionBoundaryCrossingView());
		list.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
		list.Add(ViewCreator.CreatePhotoModeView());
		list.Add(ViewCreator.CreateMissionFormationMarkerUIHandler(mission));
		list.Add(new MissionFormationTargetSelectionHandler());
		ISiegeDeploymentView siegeDeploymentView = missionView as ISiegeDeploymentView;
		list.Add(new MissionEntitySelectionUIHandler(siegeDeploymentView.OnEntitySelection, siegeDeploymentView.OnEntityHover));
		list.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		list.Add(new MissionItemContourControllerView());
		list.Add(new MissionAgentContourControllerView());
		list.Add(new MissionPreloadView());
		list.Add(new MissionCampaignBattleSpectatorView());
		list.Add(ViewCreator.CreateMissionOrderOfBattleUIHandler(mission, new SPOrderOfBattleVM()));
		list.Add(ViewCreator.CreateMissionSiegeEngineMarkerView(mission));
		list.Add(new MissionFaceCacheView());
		return list.ToArray();
	}

	[ViewMethod("SiegeMissionNoDeployment")]
	public static MissionView[] OpenSiegeMissionNoDeployment(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, SPScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateMissionOrderUIHandler(),
			new OrderTroopPlacer(null),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			ViewCreator.CreatePhotoModeView(),
			ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
			new MissionFormationTargetSelectionHandler(),
			new MusicBattleMissionView(isSiegeBattle: true),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			ViewCreator.CreateMissionSpectatorControlView(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionPreloadView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreateMissionSiegeEngineMarkerView(mission)
		}.ToArray();
	}

	[ViewMethod("SiegeLordsHallFightMission")]
	public static MissionView[] OpenSiegeLordsHallFightMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, SPScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionOrderUIHandler(),
			new OrderTroopPlacer(null),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
			new MissionFormationTargetSelectionHandler(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			ViewCreator.CreateMissionSpectatorControlView(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionPreloadView(),
			ViewCreator.CreatePhotoModeView()
		}.ToArray();
	}

	[ViewMethod("Siege")]
	public static MissionView[] OpenSiegeMission(Mission mission)
	{
		List<MissionView> list = new List<MissionView>();
		mission.GetMissionBehavior<SiegeDeploymentHandler>();
		list.Add(new MissionCampaignView());
		list.Add(new MissionConversationCameraView());
		list.Add(ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode));
		list.Add(ViewCreator.CreateOptionsUIHandler());
		list.Add(ViewCreator.CreateMissionMainAgentEquipDropView(mission));
		MissionView missionView = ViewCreator.CreateMissionOrderUIHandler();
		list.Add(missionView);
		list.Add(new OrderTroopPlacer(null));
		list.Add(new MissionSingleplayerViewHandler());
		list.Add(new DeploymentMissionView());
		list.Add(new MissionDeploymentBoundaryMarker("swallowtail_banner"));
		list.Add(ViewCreator.CreateMissionBoundaryCrossingView());
		list.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
		list.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		list.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		list.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		list.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		list.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		list.Add(ViewCreator.CreatePhotoModeView());
		ISiegeDeploymentView siegeDeploymentView = missionView as ISiegeDeploymentView;
		list.Add(new MissionEntitySelectionUIHandler(siegeDeploymentView.OnEntitySelection, siegeDeploymentView.OnEntityHover));
		list.Add(ViewCreator.CreateMissionFormationMarkerUIHandler(mission));
		list.Add(new MissionFormationTargetSelectionHandler());
		list.Add(new MissionItemContourControllerView());
		list.Add(new MissionAgentContourControllerView());
		list.Add(new MissionCampaignBattleSpectatorView());
		list.Add(ViewCreator.CreateMissionSiegeEngineMarkerView(mission));
		list.Add(new MissionFaceCacheView());
		return list.ToArray();
	}

	[ViewMethod("SiegeMissionForTutorial")]
	public static MissionView[] OpenSiegeMissionForTutorial(Mission mission)
	{
		Debug.FailedAssert("Do not use SiegeForTutorial! Use campaign!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Missions\\SandBoxMissionViews.cs", "OpenSiegeMissionForTutorial", 883);
		List<MissionView> obj = new List<MissionView>
		{
			new MissionConversationCameraView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission)
		};
		MissionView missionView = ViewCreator.CreateMissionOrderUIHandler();
		obj.Add(missionView);
		obj.Add(new OrderTroopPlacer(null));
		obj.Add(new MissionSingleplayerViewHandler());
		obj.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		obj.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		obj.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		obj.Add(ViewCreator.CreatePhotoModeView());
		obj.Add(ViewCreator.CreateMissionSiegeEngineMarkerView(mission));
		ISiegeDeploymentView siegeDeploymentView = missionView as ISiegeDeploymentView;
		obj.Add(new MissionEntitySelectionUIHandler(siegeDeploymentView.OnEntitySelection, siegeDeploymentView.OnEntityHover));
		obj.Add(new MissionDeploymentBoundaryMarker("swallowtail_banner"));
		obj.Add(new MissionCampaignBattleSpectatorView());
		return obj.ToArray();
	}

	[ViewMethod("FormationTest")]
	public static MissionView[] OpenFormationTestMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionConversationCameraView(),
			ViewCreator.CreateMissionOrderUIHandler(),
			new OrderTroopPlacer(null)
		}.ToArray();
	}

	[ViewMethod("SettlementTest")]
	public static MissionView[] OpenSettlementTestMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionConversationCameraView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			ViewCreator.CreateMissionSpectatorControlView(mission),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView()
		}.ToArray();
	}

	[ViewMethod("EquipmentTest")]
	public static MissionView[] OpenEquipmentTestMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionConversationCameraView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView()
		}.ToArray();
	}

	[ViewMethod("FacialAnimTest")]
	public static MissionView[] OpenFacialAnimTestMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionConversationCameraView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			SandBoxViewCreator.CreateMissionBarterView(),
			new MissionBoundaryWallView(),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView()
		}.ToArray();
	}

	[ViewMethod("EquipItemTool")]
	public static MissionView[] OpenEquipItemToolMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionConversationCameraView(),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionEquipItemToolView(),
			ViewCreator.CreateMissionLeaveView()
		}.ToArray();
	}

	[ViewMethod("Conversation")]
	public static MissionView[] OpenConversationMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			new MissionSingleplayerViewHandler(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			SandBoxViewCreator.CreateMissionBarterView(),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView(),
			new MissionConversationPrepareView()
		}.ToArray();
	}

	[ViewMethod("ShadowingATargetMission")]
	public static MissionView[] OpenShadowingATargetMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			new MusicStealthMissionView(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			ViewCreator.CreateMissionLeaveView(),
			ViewCreator.CreatePhotoModeView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			new MissionBoundaryWallView(),
			new MissionCampaignBattleSpectatorView(),
			SandBoxViewCreator.CreateMissionAgentAlarmStateView(mission),
			SandBoxViewCreator.CreateMissionMainAgentDetectionView(),
			ViewCreatorManager.CreateMissionView<EavesdroppingMissionCameraView>(mission != null, mission, Array.Empty<object>())
		}.ToArray();
	}

	[ViewMethod("DisguiseMission")]
	public static MissionView[] OpenDisguiseMission(Mission mission)
	{
		return new List<MissionView>
		{
			new MissionCampaignView(),
			new MissionConversationCameraView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionSingleplayerViewHandler(),
			new MusicStealthMissionView(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			ViewCreator.CreateMissionLeaveView(),
			ViewCreator.CreatePhotoModeView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			new MissionBoundaryWallView(),
			new MissionCampaignBattleSpectatorView(),
			SandBoxViewCreator.CreateMissionAgentAlarmStateView(mission),
			SandBoxViewCreator.CreateMissionMainAgentDetectionView(),
			new StealthMissionUIHandler()
		}.ToArray();
	}
}
