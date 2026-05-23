using TaleWorlds.MountAndBlade.View.MissionViews.Order;

namespace TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

public class DeploymentMissionView : MissionView
{
	protected OrderTroopPlacer _orderTroopPlacer;

	protected MissionDeploymentBoundaryMarker _deploymentBoundaryMarkerHandler;

	protected MissionEntitySelectionUIHandler _entitySelectionHandler;

	public override void AfterStart()
	{
		_orderTroopPlacer = base.Mission.GetMissionBehavior<OrderTroopPlacer>();
		_entitySelectionHandler = base.Mission.GetMissionBehavior<MissionEntitySelectionUIHandler>();
		_deploymentBoundaryMarkerHandler = base.Mission.GetMissionBehavior<MissionDeploymentBoundaryMarker>();
	}

	public override void OnDeploymentPlanMade(Team team, bool isFirstPlan)
	{
		if (team == base.Mission.PlayerTeam && base.Mission.DeploymentPlan.HasDeploymentBoundaries(base.Mission.PlayerTeam))
		{
			_orderTroopPlacer?.RestrictOrdersToDeploymentBoundaries(enabled: true);
		}
	}

	public override void OnDeploymentFinished()
	{
		if (_entitySelectionHandler != null)
		{
			base.Mission.RemoveMissionBehavior(_entitySelectionHandler);
		}
		if (_deploymentBoundaryMarkerHandler != null)
		{
			if (base.Mission.DeploymentPlan.HasDeploymentBoundaries(base.Mission.PlayerTeam))
			{
				_orderTroopPlacer?.RestrictOrdersToDeploymentBoundaries(enabled: false);
			}
			base.Mission.RemoveMissionBehavior(_deploymentBoundaryMarkerHandler);
		}
		if (!base.Mission.HasMissionBehavior<MissionBoundaryWallView>())
		{
			MissionBoundaryWallView missionView = new MissionBoundaryWallView();
			base.MissionScreen.AddMissionView(missionView);
		}
	}
}
