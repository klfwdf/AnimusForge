namespace TaleWorlds.MountAndBlade;

public class MissionTimeTracker
{
	private long _lastSyncDifference;

	public long NumberOfTicks { get; private set; }

	public long DeltaTimeInTicks { get; private set; }

	public MissionTimeTracker(MissionTime initialMapTime)
	{
		NumberOfTicks = initialMapTime.NumberOfTicks;
	}

	public MissionTimeTracker()
	{
		NumberOfTicks = 0L;
	}

	public void Tick(float seconds)
	{
		DeltaTimeInTicks = (long)(seconds * 10000000f);
		NumberOfTicks += DeltaTimeInTicks;
	}

	public void UpdateSync(float newValue)
	{
		long num = (long)(newValue * 10000000f);
		_lastSyncDifference = num - NumberOfTicks;
	}

	public float GetLastSyncDifference()
	{
		return (float)_lastSyncDifference / 10000000f;
	}
}
