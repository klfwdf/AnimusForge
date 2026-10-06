namespace TaleWorlds.MountAndBlade;

public class BasicTimer
{
	private float _startTime;

	public float ElapsedTime => MBCommon.GetApplicationTime() - _startTime;

	public BasicTimer()
	{
		_startTime = MBCommon.GetApplicationTime();
	}

	public void Reset()
	{
		_startTime = MBCommon.GetApplicationTime();
	}

	public void Set(float newElapsedTime)
	{
		_startTime = MBCommon.GetApplicationTime() - newElapsedTime;
	}
}
