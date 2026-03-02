using System.Collections.Generic;
using SandBox.View.Missions;
using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;
using TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

namespace Voxforge;

[ViewCreatorModule]
public class VoxforgeMeetingMissionViews
{
	[ViewMethod("VoxforgeBattleMeeting")]
	public static MissionView[] OpenVoxforgeBattleMeetingMission(Mission mission)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Expected O, but got Unknown
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Expected O, but got Unknown
		List<MissionView> list = new List<MissionView>
		{
			(MissionView)new MissionCampaignView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateMissionBattleScoreUIHandler(mission, (ScoreboardBaseVM)new SPScoreboardVM((BattleSimulation)null)),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			ViewCreator.CreateMissionOrderUIHandler(),
			new OrderTroopPlacer(null),
			(MissionView)new MissionSingleplayerViewHandler(),
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
			(MissionView)new MissionCampaignBattleSpectatorView(),
			ViewCreator.CreatePhotoModeView(),
			new MissionFaceCacheView(),
			ViewCreator.CreateMissionOrderOfBattleUIHandler(mission, (OrderOfBattleVM)new SPOrderOfBattleVM())
		};
		return list.ToArray();
	}
}
