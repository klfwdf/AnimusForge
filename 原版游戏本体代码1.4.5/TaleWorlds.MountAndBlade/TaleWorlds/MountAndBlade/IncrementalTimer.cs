using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class IncrementalTimer
{
	private readonly float _totalDuration;

	private readonly float _tickInterval;

	private readonly Timer _timer;

	public float TimerCounter { get; private set; }

	public IncrementalTimer(float totalDuration, float tickInterval)
	{
		_tickInterval = MathF.Max(tickInterval, 0.01f);
		_totalDuration = MathF.Max(totalDuration, 0.01f);
		TimerCounter = 0f;
		_timer = new Timer(MBCommon.GetTotalMissionTime(), _tickInterval);
	}

	public bool Check()
	{
		if (_timer.Check(MBCommon.GetTotalMissionTime()))
		{
			TimerCounter += _tickInterval / _totalDuration;
			return true;
		}
		return false;
	}

	public bool HasEnded()
	{
		return TimerCounter >= 1f;
	}
}
