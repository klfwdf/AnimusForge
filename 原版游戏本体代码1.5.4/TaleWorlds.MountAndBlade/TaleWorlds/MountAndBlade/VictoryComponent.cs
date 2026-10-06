namespace TaleWorlds.MountAndBlade;

public class VictoryComponent : AgentComponent
{
	private readonly RandomTimer _timer;

	public VictoryComponent(Agent agent, RandomTimer timer)
		: base(agent)
	{
		_timer = timer;
	}

	public bool CheckTimer()
	{
		return _timer.Check(Mission.Current.CurrentTime);
	}

	public void ChangeTimerDuration(float min, float max)
	{
		_timer.ChangeDuration(min, max);
	}
}
