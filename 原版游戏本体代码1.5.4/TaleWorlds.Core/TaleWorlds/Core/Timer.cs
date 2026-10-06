namespace TaleWorlds.Core;

public class Timer
{
	private float _latestGameTime;

	private bool _autoReset;

	public float StartTime { get; protected set; }

	public float Duration { get; protected set; }

	public float PreviousDeltaTime { get; private set; }

	public Timer(float gameTime, float duration, bool autoReset = true)
	{
		StartTime = gameTime;
		_latestGameTime = gameTime;
		_autoReset = autoReset;
		Duration = duration;
	}

	public virtual bool Check(float gameTime)
	{
		_latestGameTime = gameTime;
		if (Duration <= 0f)
		{
			PreviousDeltaTime = ElapsedTime();
			StartTime = gameTime;
			return true;
		}
		bool result = false;
		if (ElapsedTime() >= Duration)
		{
			PreviousDeltaTime = ElapsedTime();
			if (_autoReset)
			{
				while (ElapsedTime() >= Duration)
				{
					StartTime += Duration;
				}
			}
			result = true;
		}
		return result;
	}

	public float ElapsedTime()
	{
		return _latestGameTime - StartTime;
	}

	public void Reset(float gameTime)
	{
		Reset(gameTime, Duration);
	}

	public void Reset(float gameTime, float newDuration)
	{
		StartTime = gameTime;
		_latestGameTime = gameTime;
		Duration = newDuration;
	}

	public void AdjustStartTime(float deltaTime)
	{
		StartTime += deltaTime;
	}
}
