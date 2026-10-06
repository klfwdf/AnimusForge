using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.Source.Missions.Handlers;

public class MissionFacialAnimationHandler : MissionLogic
{
	private Timer _animRefreshTimer;

	public override void EarlyStart()
	{
		_animRefreshTimer = new Timer(base.Mission.CurrentTime, 5f);
	}

	public override void AfterStart()
	{
	}

	public override void OnMissionTick(float dt)
	{
	}

	private void SetDefaultFacialAnimationsForAllAgents()
	{
		foreach (Agent agent in base.Mission.Agents)
		{
			if (agent.IsActive() && agent.IsHuman)
			{
				agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.Low, "idle_tired", loop: true);
			}
		}
	}
}
