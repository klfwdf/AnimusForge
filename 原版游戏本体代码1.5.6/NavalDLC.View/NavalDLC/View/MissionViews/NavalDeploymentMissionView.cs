using NavalDLC.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

namespace NavalDLC.View.MissionViews;

public class NavalDeploymentMissionView : DeploymentMissionView
{
	public override void AfterStart()
	{
		_orderTroopPlacer = base.Mission.GetMissionBehavior<NavalOrderTroopPlacer>();
		_deploymentBoundaryMarkerHandler = base.Mission.GetMissionBehavior<NavalMissionDeploymentBoundaryMarker>();
	}
}
