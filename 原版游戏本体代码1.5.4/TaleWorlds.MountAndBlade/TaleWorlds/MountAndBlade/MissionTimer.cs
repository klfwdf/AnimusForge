namespace TaleWorlds.MountAndBlade;

public class MissionTimer
{
	private MissionTime _startTime;

	private float _duration;

	private MissionTimer()
	{
	}

	public MissionTimer(float duration)
	{
		_startTime = MissionTime.Now;
		_duration = duration;
	}

	public MissionTime GetStartTime()
	{
		return _startTime;
	}

	public float GetTimerDuration()
	{
		return _duration;
	}

	public float GetRemainingTimeInSeconds(bool synched = false)
	{
		if (_duration < 0f)
		{
			return 0f;
		}
		float num = _duration - _startTime.ElapsedSeconds;
		if (synched && GameNetwork.IsClientOrReplay)
		{
			num -= Mission.Current.MissionTimeTracker.GetLastSyncDifference();
		}
		if (!(num > 0f))
		{
			return 0f;
		}
		return num;
	}

	public bool Check(bool reset = false)
	{
		bool num = GetRemainingTimeInSeconds() <= 0f;
		if (num && reset)
		{
			_startTime = MissionTime.Now;
		}
		return num;
	}

	public void Reset()
	{
		_startTime = MissionTime.Now;
	}

	public void Set(float timeInSeconds)
	{
		_startTime = new MissionTime(Mission.Current.MissionTimeTracker.NumberOfTicks + (long)(timeInSeconds * 10000000f));
	}

	public void SetDuration(float duration)
	{
		_duration = duration;
	}

	public static MissionTimer CreateSynchedTimerClient(float startTimeInSeconds, float duration)
	{
		return new MissionTimer
		{
			_startTime = new MissionTime((long)(startTimeInSeconds * 10000000f)),
			_duration = duration
		};
	}
}
