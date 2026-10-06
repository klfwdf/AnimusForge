namespace SandBox.Missions.AgentBehaviors;

public class IdleAgentBehavior : AgentBehavior
{
	public IdleAgentBehavior(AgentBehaviorGroup behaviorGroup)
		: base(behaviorGroup)
	{
	}

	public override float GetAvailability(bool isSimulation)
	{
		return 1f;
	}

	protected override void OnActivate()
	{
		base.OwnerAgent.SetIsAIPaused(isPaused: true);
		base.OwnerAgent.SetTargetPosition(base.OwnerAgent.GetWorldPosition().AsVec2);
	}

	protected override void OnDeactivate()
	{
		base.OwnerAgent.SetIsAIPaused(isPaused: false);
		base.OwnerAgent.ClearTargetFrame();
	}

	public override string GetDebugInfo()
	{
		return "Idle Behavior";
	}
}
