using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerTimerComponent : MissionNetwork
{
	private MissionTimer _missionTimer;

	public bool IsTimerRunning { get; private set; }

	public void StartTimerAsServer(float duration)
	{
		_missionTimer = new MissionTimer(duration);
		IsTimerRunning = true;
	}

	public void StartTimerAsClient(float startTime, float duration)
	{
		_missionTimer = MissionTimer.CreateSynchedTimerClient(startTime, duration);
		IsTimerRunning = true;
	}

	public float GetRemainingTime(bool isSynched)
	{
		if (!IsTimerRunning)
		{
			return 0f;
		}
		float remainingTimeInSeconds = _missionTimer.GetRemainingTimeInSeconds(isSynched);
		if (isSynched)
		{
			return MathF.Min(remainingTimeInSeconds, _missionTimer.GetTimerDuration());
		}
		return remainingTimeInSeconds;
	}

	public bool CheckIfTimerPassed()
	{
		if (IsTimerRunning)
		{
			return _missionTimer.Check();
		}
		return false;
	}

	public MissionTime GetCurrentTimerStartTime()
	{
		return _missionTimer.GetStartTime();
	}
}
