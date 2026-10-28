namespace TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

public class DeploymentView : MissionView
{
	private DeploymentHandler _deploymentHandler;

	public override void AfterStart()
	{
		base.AfterStart();
		_deploymentHandler = base.Mission.GetMissionBehavior<DeploymentHandler>();
		CreateWidgets();
	}

	public override void OnRemoveBehavior()
	{
		RemoveWidgets();
		base.OnRemoveBehavior();
	}

	protected virtual void CreateWidgets()
	{
	}

	protected virtual void RemoveWidgets()
	{
	}
}
