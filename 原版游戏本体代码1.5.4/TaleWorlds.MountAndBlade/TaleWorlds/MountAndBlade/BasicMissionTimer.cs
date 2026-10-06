namespace TaleWorlds.MountAndBlade;

public class BasicMissionTimer
{
	private float _startTime;

	public float ElapsedTime => Mission.Current.CurrentTime - _startTime;

	public BasicMissionTimer()
	{
		_startTime = Mission.Current.CurrentTime;
	}

	public void Reset()
	{
		_startTime = Mission.Current.CurrentTime;
	}

	public void Set(float newElapsedTime)
	{
		_startTime = Mission.Current.CurrentTime - newElapsedTime;
	}
}
