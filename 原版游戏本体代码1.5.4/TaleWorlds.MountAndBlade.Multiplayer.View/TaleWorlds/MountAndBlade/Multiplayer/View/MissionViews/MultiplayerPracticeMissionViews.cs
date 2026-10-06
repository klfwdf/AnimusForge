using System.Collections.Generic;
using TaleWorlds.MountAndBlade.Missions.BattleScore;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;
using TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

namespace TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;

[ViewCreatorModule]
public static class MultiplayerPracticeMissionViews
{
	[ViewMethod("MultiplayerPractice")]
	public static MissionView[] OpenMultiplayerPracticeMission(Mission mission)
	{
		List<MissionView> obj = new List<MissionView>
		{
			MultiplayerViewCreator.CreateMissionMultiplayerPracticeEscapeMenu(),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, new CustomBattleScoreboardVM(new CustomBattleScoreContext(mission))),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission)
		};
		MissionView item = ViewCreator.CreateMissionOrderUIHandler();
		obj.Add(item);
		obj.Add(new OrderTroopPlacer(null));
		obj.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
		obj.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
		obj.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
		obj.Add(new DeploymentMissionView());
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
		return obj.ToArray();
	}
}
