using System.Collections.Generic;
using TaleWorlds.MountAndBlade.Missions.BattleScore;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews.Sound;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;
using TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

namespace TaleWorlds.MountAndBlade.CustomBattle;

[ViewCreatorModule]
public class CustomBattleViews
{
	[ViewMethod("CustomBattle")]
	public static MissionView[] OpenCustomBattleMission(Mission mission)
	{
		List<MissionView> obj = new List<MissionView>
		{
			ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, new CustomBattleScoreboardVM(new CustomBattleScoreContext(mission))),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission)
		};
		MissionView missionView = ViewCreator.CreateMissionOrderUIHandler();
		obj.Add(missionView);
		obj.Add(new OrderTroopPlacer(null));
		obj.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		obj.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		obj.Add(new MusicBattleMissionView(isSiegeBattle: false));
		obj.Add(new DeploymentMissionView());
		ISiegeDeploymentView siegeDeploymentView = missionView as ISiegeDeploymentView;
		obj.Add(new MissionEntitySelectionUIHandler(siegeDeploymentView.OnEntitySelection, siegeDeploymentView.OnEntityHover));
		obj.Add(new MissionFormationTargetSelectionHandler());
		obj.Add(ViewCreator.CreateMissionBoundaryCrossingView());
		obj.Add(new MissionBoundaryWallView());
		obj.Add(new MissionDeploymentBoundaryMarker("swallowtail_banner"));
		obj.Add(ViewCreator.CreateMissionFormationMarkerUIHandler(mission));
		obj.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
		obj.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		obj.Add(ViewCreator.CreatePhotoModeView());
		obj.Add(new MissionItemContourControllerView());
		obj.Add(new MissionAgentContourControllerView());
		obj.Add(new MissionCustomBattlePreloadView());
		obj.Add(ViewCreator.CreateMissionOrderOfBattleUIHandler(mission, new OrderOfBattleVM()));
		obj.Add(ViewCreator.CreateMissionObjectiveView(mission));
		obj.Add(new MissionFaceCacheView());
		return obj.ToArray();
	}

	[ViewMethod("CustomSiegeBattle")]
	public static MissionView[] OpenCustomSiegeBattleMission(Mission mission)
	{
		List<MissionView> list = new List<MissionView>();
		mission.GetMissionBehavior<SiegeDeploymentHandler>();
		list.Add(ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false));
		list.Add(ViewCreator.CreateMissionAgentLabelUIHandler(mission));
		list.Add(ViewCreator.CreateMissionBattleScoreUIHandler(mission, new CustomBattleScoreboardVM(new CustomBattleScoreContext(mission))));
		list.Add(ViewCreator.CreateOptionsUIHandler());
		list.Add(ViewCreator.CreateMissionMainAgentEquipDropView(mission));
		MissionView missionView = ViewCreator.CreateMissionOrderUIHandler();
		list.Add(missionView);
		list.Add(new OrderTroopPlacer(null));
		list.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		list.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		list.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		list.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		list.Add(new MusicBattleMissionView(isSiegeBattle: true));
		list.Add(new DeploymentMissionView());
		ISiegeDeploymentView siegeDeploymentView = missionView as ISiegeDeploymentView;
		list.Add(new MissionEntitySelectionUIHandler(siegeDeploymentView.OnEntitySelection, siegeDeploymentView.OnEntityHover));
		list.Add(new MissionFormationTargetSelectionHandler());
		list.Add(ViewCreator.CreateMissionBoundaryCrossingView());
		list.Add(new MissionDeploymentBoundaryMarker("swallowtail_banner"));
		list.Add(ViewCreator.CreateMissionFormationMarkerUIHandler(mission));
		list.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
		list.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
		list.Add(ViewCreator.CreatePhotoModeView());
		list.Add(new SiegeDeploymentVisualizationMissionView());
		list.Add(new MissionItemContourControllerView());
		list.Add(new MissionAgentContourControllerView());
		list.Add(new MissionCustomBattlePreloadView());
		list.Add(ViewCreator.CreateMissionSiegeEngineMarkerView(mission));
		list.Add(ViewCreator.CreateMissionOrderOfBattleUIHandler(mission, new OrderOfBattleVM()));
		return list.ToArray();
	}

	[ViewMethod("CustomBattleLordsHall")]
	public static MissionView[] OpenCustomBattleLordsHallMission(Mission mission)
	{
		return new List<MissionView>
		{
			ViewCreator.CreateMissionSingleplayerEscapeMenu(isIronmanMode: false),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, new CustomBattleScoreboardVM(new CustomBattleScoreContext(mission))),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionOrderUIHandler(),
			new OrderTroopPlacer(null),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
			ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
			ViewCreator.CreateMissionSpectatorControlView(mission),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			ViewCreator.CreatePhotoModeView(),
			new MissionCustomBattlePreloadView()
		}.ToArray();
	}
}
