using System;
using System.Collections.Generic;
using NavalDLC.Missions.BattleScore;
using NavalDLC.View.MissionViews;
using NavalDLC.View.MissionViews.Order;
using NavalDLC.ViewModelCollection;
using SandBox.View;
using SandBox.View.Missions;
using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews.Sound;

namespace NavalDLC.View;

[ViewCreatorModule]
public class NavalViews
{
	[ViewMethod("NavalBattle")]
	public static MissionView[] OpenNavalBattleMission(Mission mission)
	{
		List<MissionView> obj = new List<MissionView>
		{
			ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, NavalScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission)
		};
		MissionView item = NavalViewCreator.CreateNavalOrderUIHandler(mission);
		obj.Add(item);
		obj.Add(new MissionFormationTargetSelectionHandler());
		obj.Add(new NavalOrderTroopPlacer(null));
		obj.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		obj.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		obj.Add(new MusicNavalBattleMissionView());
		obj.Add(new NavalAmbientShoutsView());
		obj.Add(new NavalDeploymentMissionView());
		obj.Add(ViewCreator.CreateMissionBoundaryCrossingView());
		obj.Add(new MissionBoundaryWallView());
		obj.Add(new NavalMissionDeploymentBoundaryMarker("buoy_small_a", "buoy_big_a"));
		obj.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
		obj.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		obj.Add(ViewCreator.CreatePhotoModeView());
		obj.Add(new MissionItemContourControllerView());
		obj.Add(new MissionAgentContourControllerView());
		obj.Add(NavalViewCreator.CreateMissionShipControlView(mission));
		obj.Add(new NavalMissionPrepareView());
		obj.Add(SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission));
		obj.Add(NavalViewCreator.CreateNavalShipMarkerUIHandler(mission));
		obj.Add(NavalViewCreator.CreateNavalOrderOfBattleView(mission));
		obj.Add(NavalViewCreator.CreateNavalShipTargetSelectionHandler(mission));
		obj.Add(NavalViewCreator.CreateNavalMissionCaptureShipView(mission));
		obj.Add(new NavalMissionShipHighlightView());
		obj.Add(new MissionCampaignView());
		obj.Add(new MissionPreloadView());
		obj.Add(new NavalShipsPreloadView());
		return obj.ToArray();
	}

	[ViewMethod("NavalRaid")]
	public static MissionView[] OpenNavalRaidMission(Mission mission)
	{
		List<MissionView> obj = new List<MissionView>
		{
			ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, NavalScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission)
		};
		MissionView item = ViewCreator.CreateMissionOrderUIHandler(mission);
		obj.Add(item);
		obj.Add(new MissionFormationTargetSelectionHandler());
		obj.Add(new NavalOrderTroopPlacer(null));
		obj.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		obj.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		obj.Add(new MusicNavalBattleMissionView());
		obj.Add(new NavalAmbientShoutsView());
		obj.Add(new NavalDeploymentMissionView());
		obj.Add(ViewCreator.CreateMissionBoundaryCrossingView());
		obj.Add(new MissionBoundaryWallView());
		obj.Add(new NavalMissionDeploymentBoundaryMarker("buoy_small_a", "buoy_big_a"));
		obj.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
		obj.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		obj.Add(ViewCreator.CreatePhotoModeView());
		obj.Add(new MissionItemContourControllerView());
		obj.Add(new MissionAgentContourControllerView());
		obj.Add(new NavalMissionPrepareView());
		obj.Add(SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionFormationMarkerUIHandler(mission));
		obj.Add(new MissionCampaignView());
		obj.Add(new MissionPreloadView());
		obj.Add(new NavalShipsPreloadView());
		return obj.ToArray();
	}

	[ViewMethod("NavalCustomBattle")]
	public static MissionView[] OpenNavalBattleForCustomMission(Mission mission)
	{
		List<MissionView> obj = new List<MissionView>
		{
			ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, NavalCustomBattleScoreboardVM.Create(mission)),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission)
		};
		MissionView item = NavalViewCreator.CreateNavalOrderUIHandler(mission);
		obj.Add(item);
		obj.Add(new MissionFormationTargetSelectionHandler());
		obj.Add(new NavalOrderTroopPlacer(null));
		obj.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		obj.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		obj.Add(new MusicNavalBattleMissionView());
		obj.Add(new NavalAmbientShoutsView());
		obj.Add(new NavalDeploymentMissionView());
		obj.Add(ViewCreator.CreateMissionBoundaryCrossingView());
		obj.Add(new MissionBoundaryWallView());
		obj.Add(new NavalMissionDeploymentBoundaryMarker("buoy_small_a", "buoy_big_a"));
		obj.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
		obj.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		obj.Add(ViewCreator.CreatePhotoModeView());
		obj.Add(new MissionItemContourControllerView());
		obj.Add(new MissionAgentContourControllerView());
		obj.Add(NavalViewCreator.CreateMissionShipControlView(mission));
		obj.Add(new NavalMissionPrepareView());
		obj.Add(SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission));
		obj.Add(NavalViewCreator.CreateNavalShipMarkerUIHandler(mission));
		obj.Add(NavalViewCreator.CreateNavalOrderOfBattleView(mission));
		obj.Add(NavalViewCreator.CreateNavalShipTargetSelectionHandler(mission));
		obj.Add(NavalViewCreator.CreateNavalMissionCaptureShipView(mission));
		obj.Add(new NavalMissionShipHighlightView());
		obj.Add(new MissionCampaignView());
		obj.Add(new MissionCustomBattlePreloadView());
		obj.Add(new NavalShipsPreloadView());
		return obj.ToArray();
	}

	[ViewMethod("NavalRaidCustomBattle")]
	public static MissionView[] OpenNavalRaidBattleForCustomMission(Mission mission)
	{
		List<MissionView> obj = new List<MissionView>
		{
			ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, NavalCustomBattleScoreboardVM.Create(mission)),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission)
		};
		MissionView item = ViewCreator.CreateMissionOrderUIHandler(mission);
		obj.Add(item);
		obj.Add(new MissionFormationTargetSelectionHandler());
		obj.Add(new NavalOrderTroopPlacer(null));
		obj.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		obj.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		obj.Add(new MusicNavalBattleMissionView());
		obj.Add(new NavalAmbientShoutsView());
		obj.Add(new NavalDeploymentMissionView());
		obj.Add(ViewCreator.CreateMissionBoundaryCrossingView());
		obj.Add(new MissionBoundaryWallView());
		obj.Add(new NavalMissionDeploymentBoundaryMarker("buoy_small_a", "buoy_big_a"));
		obj.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
		obj.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		obj.Add(ViewCreator.CreatePhotoModeView());
		obj.Add(new MissionItemContourControllerView());
		obj.Add(new MissionAgentContourControllerView());
		obj.Add(new NavalMissionPrepareView());
		obj.Add(SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionFormationMarkerUIHandler(mission));
		obj.Add(new MissionCampaignView());
		obj.Add(new MissionCustomBattlePreloadView());
		obj.Add(new NavalShipsPreloadView());
		return obj.ToArray();
	}

	[ViewMethod("NavalCaptivityBattle")]
	public static MissionView[] OpenNavalCaptivityBattleMission(Mission mission)
	{
		return new List<MissionView>
		{
			ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			new MusicSilencedMissionView(),
			new MissionBoundaryWallView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			ViewCreator.CreateMissionSpectatorControlView(mission),
			ViewCreator.CreatePhotoModeView(),
			NavalViewCreator.CreateMissionShipControlView(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionConversationCameraView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionLeaveView(),
			new NavalMissionPrepareView(),
			new MissionCampaignView(),
			ViewCreator.CreateMissionHintView(mission),
			ViewCreator.CreateMissionObjectiveView(mission),
			SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
			NavalViewCreator.CreateCaptivityMissionView(mission),
			NavalViewCreator.CreateNavalMissionCaptureShipView(mission)
		}.ToArray();
	}

	[ViewMethod("BlockedEstuary")]
	public static MissionView[] OpenNavalSetPieceBattleMission(Mission mission)
	{
		return new List<MissionView>
		{
			ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, NavalScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			ViewCreator.CreateMissionObjectiveView(mission),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			new MusicSilencedMissionView(),
			NavalViewCreator.CreateNavalShipMarkerUIHandler(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			ViewCreator.CreateMissionSpectatorControlView(mission),
			ViewCreator.CreatePhotoModeView(),
			NavalViewCreator.CreateMissionShipControlView(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionConversationCameraView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionLeaveView(),
			SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
			new NavalMissionPrepareView(),
			new BlockedEstuaryView(),
			NavalViewCreator.CreateNavalMissionCaptureShipView(mission),
			new MissionCampaignView()
		}.ToArray();
	}

	[ViewMethod("NavalStorylinePirateBattle")]
	public static MissionView[] OpenNavalStorylinePirateBattleMission(Mission mission)
	{
		List<MissionView> obj = new List<MissionView>
		{
			ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, NavalScoreboardVM.CreateCustom(new NavalStorylinePirateBattleScoreContext(mission))),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission)
		};
		MissionView item = NavalViewCreator.CreateNavalOrderUIHandler(mission);
		obj.Add(item);
		obj.Add(new MissionFormationTargetSelectionHandler());
		obj.Add(new NavalOrderTroopPlacer(null));
		obj.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		obj.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		obj.Add(new MusicSilencedMissionView());
		obj.Add(ViewCreator.CreateMissionBoundaryCrossingView());
		obj.Add(new MissionBoundaryWallView());
		obj.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
		obj.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		obj.Add(ViewCreator.CreatePhotoModeView());
		obj.Add(new MissionItemContourControllerView());
		obj.Add(new MissionAgentContourControllerView());
		obj.Add(NavalViewCreator.CreateMissionShipControlView(mission));
		obj.Add(NavalViewCreator.CreateNavalShipMarkerUIHandler(mission));
		obj.Add(NavalViewCreator.CreateNavalShipTargetSelectionHandler(mission));
		obj.Add(new NavalMissionShipHighlightView());
		obj.Add(NavalViewCreator.CreatePirateBattleMissionView(mission));
		obj.Add(new MissionConversationCameraView());
		obj.Add(SandBoxViewCreator.CreateMissionConversationView(mission));
		obj.Add(ViewCreator.CreateMissionLeaveView());
		obj.Add(new MissionCampaignView());
		obj.Add(new NavalMissionPrepareView());
		obj.Add(SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionObjectiveView(mission));
		obj.Add(NavalViewCreator.CreateNavalMissionCaptureShipView(mission));
		return obj.ToArray();
	}

	[ViewMethod("HelpAnAllySetPieceBattle")]
	public static MissionView[] OpenHelpAnAllySetPieceBattle(Mission mission)
	{
		List<MissionView> obj = new List<MissionView>
		{
			NavalViewCreator.CreateHelpingAnAllyMissionView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, NavalScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission)
		};
		MissionView item = NavalViewCreator.CreateNavalOrderUIHandler(mission);
		obj.Add(item);
		obj.Add(new MissionFormationTargetSelectionHandler());
		obj.Add(new NavalOrderTroopPlacer(null));
		obj.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		obj.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		obj.Add(new MusicNavalBattleMissionView());
		obj.Add(new NavalMissionPrepareView());
		obj.Add(ViewCreator.CreateMissionBoundaryCrossingView());
		obj.Add(new MissionBoundaryWallView());
		obj.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
		obj.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		obj.Add(ViewCreator.CreatePhotoModeView());
		obj.Add(new MissionItemContourControllerView());
		obj.Add(new MissionAgentContourControllerView());
		obj.Add(NavalViewCreator.CreateMissionShipControlView(mission));
		obj.Add(NavalViewCreator.CreateNavalShipMarkerUIHandler(mission));
		obj.Add(NavalViewCreator.CreateNavalShipTargetSelectionHandler(mission));
		obj.Add(new NavalMissionShipHighlightView());
		obj.Add(ViewCreator.CreateMissionObjectiveView());
		obj.Add(SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission));
		obj.Add(new MissionConversationCameraView());
		obj.Add(SandBoxViewCreator.CreateMissionConversationView(mission));
		obj.Add(ViewCreator.CreateMissionLeaveView());
		obj.Add(NavalViewCreator.CreateNavalMissionCaptureShipView(mission));
		return obj.ToArray();
	}

	[ViewMethod("NavalStorylineQuest5SetPieceBattleMission")]
	public static MissionView[] OpenNavalStorylineQuest5SetPieceBattleMission(Mission mission)
	{
		List<MissionView> obj = new List<MissionView>
		{
			ViewCreator.CreateMissionObjectiveView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, NavalScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission)
		};
		MissionView item = NavalViewCreator.CreateNavalOrderUIHandler(mission);
		obj.Add(item);
		obj.Add(new MissionFormationTargetSelectionHandler());
		obj.Add(new NavalOrderTroopPlacer(null));
		obj.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		obj.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		obj.Add(new MusicSilencedMissionView());
		obj.Add(ViewCreator.CreateMissionBoundaryCrossingView());
		obj.Add(new MissionBoundaryWallView());
		obj.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
		obj.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		obj.Add(ViewCreator.CreatePhotoModeView());
		obj.Add(new MissionItemContourControllerView());
		obj.Add(new MissionAgentContourControllerView());
		obj.Add(NavalViewCreator.CreateMissionShipControlView(mission));
		obj.Add(new NavalMissionPrepareView());
		obj.Add(NavalViewCreator.CreateNavalShipMarkerUIHandler(mission));
		obj.Add(NavalViewCreator.CreateNavalShipTargetSelectionHandler(mission));
		obj.Add(new NavalMissionShipHighlightView());
		obj.Add(new MusicStealthMissionView());
		obj.Add(new MissionCampaignView());
		obj.Add(new MissionConversationCameraView());
		obj.Add(SandBoxViewCreator.CreateMissionConversationView(mission));
		obj.Add(ViewCreator.CreateMissionLeaveView());
		obj.Add(NavalViewCreator.CreateQuest5SetPieceBattleMissionView(mission));
		obj.Add(NavalViewCreator.CreateQuest5SetPieceBattleBossFightCameraView(mission));
		obj.Add(NavalViewCreator.CreateQuest5SetPieceBattleInteriorConversationCameraView(mission));
		obj.Add(SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission));
		obj.Add(SandBoxViewCreator.CreateMissionAgentAlarmStateView(mission));
		obj.Add(NavalViewCreator.CreateNavalMissionCaptureShipView(mission));
		return obj.ToArray();
	}

	[ViewMethod("NavalFinalConversationMission")]
	public static MissionView[] OpenNavalFinalConversationMission(Mission mission)
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
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			new NavalFinalConversationMissionView()
		}.ToArray();
	}

	[ViewMethod("NavalStorylineWoundedBeastBattle")]
	public static MissionView[] OpenNavalStorylineWoundedBeastBattleMission(Mission mission)
	{
		List<MissionView> obj = new List<MissionView>
		{
			ViewCreator.CreateMissionObjectiveView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, NavalScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission)
		};
		MissionView item = NavalViewCreator.CreateNavalOrderUIHandler(mission);
		obj.Add(item);
		obj.Add(new MissionFormationTargetSelectionHandler());
		obj.Add(new NavalOrderTroopPlacer(null));
		obj.Add(new MusicNavalBattleMissionView());
		obj.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		obj.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		obj.Add(new MusicSilencedMissionView());
		obj.Add(ViewCreator.CreateMissionBoundaryCrossingView());
		obj.Add(new MissionBoundaryWallView());
		obj.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
		obj.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		obj.Add(ViewCreator.CreatePhotoModeView());
		obj.Add(new MissionItemContourControllerView());
		obj.Add(new MissionAgentContourControllerView());
		obj.Add(new NavalMissionShipHighlightView());
		obj.Add(new NavalMissionPrepareView());
		obj.Add(SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission));
		obj.Add(NavalViewCreator.CreateMissionShipControlView(mission));
		obj.Add(NavalViewCreator.CreateNavalShipMarkerUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionLeaveView());
		obj.Add(new WoundedBeastView());
		obj.Add(NavalViewCreator.CreateNavalMissionCaptureShipView(mission));
		return obj.ToArray();
	}

	[ViewMethod("FloatingFortressSetPieceBattleMission")]
	public static MissionView[] OpenFloatingFortressSetPieceBattleMission(Mission mission)
	{
		List<MissionView> obj = new List<MissionView>
		{
			ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, NavalScoreboardVM.CreateMission(mission)),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission)
		};
		MissionView item = NavalViewCreator.CreateNavalOrderUIHandler(mission);
		obj.Add(item);
		obj.Add(new MissionFormationTargetSelectionHandler());
		obj.Add(new NavalOrderTroopPlacer(null));
		obj.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		obj.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		obj.Add(new MusicSilencedMissionView());
		obj.Add(ViewCreator.CreateMissionBoundaryCrossingView());
		obj.Add(new MissionBoundaryWallView());
		obj.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
		obj.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		obj.Add(ViewCreator.CreatePhotoModeView());
		obj.Add(new MissionItemContourControllerView());
		obj.Add(new MissionAgentContourControllerView());
		obj.Add(NavalViewCreator.CreateMissionShipControlView(mission));
		obj.Add(NavalViewCreator.CreateNavalShipMarkerUIHandler(mission));
		obj.Add(NavalViewCreator.CreateNavalShipTargetSelectionHandler(mission));
		obj.Add(new NavalMissionShipHighlightView());
		obj.Add(new MissionConversationCameraView());
		obj.Add(SandBoxViewCreator.CreateMissionConversationView(mission));
		obj.Add(ViewCreator.CreateMissionLeaveView());
		obj.Add(NavalViewCreator.CreateFloatingFortressView());
		obj.Add(ViewCreator.CreateMissionObjectiveView());
		obj.Add(SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission));
		obj.Add(NavalViewCreator.CreateNavalMissionCaptureShipView(mission));
		obj.Add(new NavalMissionPrepareView());
		obj.Add(new MissionCampaignView());
		return obj.ToArray();
	}

	[ViewMethod("NavalStorylineAlleyFight")]
	public static MissionView[] OpenNavalStorylineAlleyFight(Mission mission)
	{
		return new List<MissionView>
		{
			ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, SPScoreboardVM.CreateCustom(new NavalAlleyFightBattleScoreContext(mission))),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			new MissionFormationTargetSelectionHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionAgentLockVisualizerView(mission),
			new MusicSilencedMissionView(),
			new NavalStorylineAlleyFightCinematicView(),
			ViewCreatorManager.CreateMissionView<MissionHintView>(isNetwork: false, mission, Array.Empty<object>()),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			ViewCreator.CreatePhotoModeView(),
			new MissionBoundaryWallView(),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			ViewCreator.CreateMissionSpectatorControlView(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new MissionConversationCameraView(),
			SandBoxViewCreator.CreateMissionConversationView(mission),
			ViewCreator.CreateMissionLeaveView()
		}.ToArray();
	}
}
