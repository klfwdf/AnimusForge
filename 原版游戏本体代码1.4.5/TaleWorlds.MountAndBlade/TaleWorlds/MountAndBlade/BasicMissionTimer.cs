namespace TaleWorlds.MountAndBlade;

public class BasicMissionTimer
{
	private float _startTime;

	public float ElapsedTime => MBCommon.GetTotalMissionTime() - _startTime;

	public BasicMissionTimer()
	{
		_startTime = MBCommon.GetTotalMissionTime();
	}

	public void Reset()
	{
		_startTime = MBCommon.GetTotalMissionTime();
	}

	public void Set(float newElapsedTime)
	{
		_startTime = MBCommon.GetTotalMissionTime() - newElapsedTime;
	}
}
